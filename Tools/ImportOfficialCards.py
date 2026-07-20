#!/usr/bin/env python3
"""Download the official Love Live card list and images into Unity Assets.

The crawler is resumable: existing non-empty images are not downloaded again.
Run from the project root, then run OfficialCardPrefabImporter in Unity.
"""

from concurrent.futures import ThreadPoolExecutor, as_completed
from html import unescape
from pathlib import Path
import json
import re
import sys
import time
import urllib.parse
import urllib.request
import urllib.error

BASE = "https://llofficial-cardgame.com"
ROOT = Path(__file__).resolve().parents[1]
CARDS_ROOT = ROOT / "Assets" / "Resources" / "Cards"
MANIFEST = CARDS_ROOT / "official_cards.json"
HEADERS = {"User-Agent": "loveca-unity-card-importer/1.0 (+local game asset import)"}

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")


def fetch(url, attempts=4):
    for attempt in range(attempts):
        try:
            request = urllib.request.Request(url, headers=HEADERS)
            with urllib.request.urlopen(request, timeout=45) as response:
                return response.read()
        except Exception:
            if attempt == attempts - 1:
                raise
            time.sleep(1.5 * (attempt + 1))


def text(url):
    return fetch(url).decode("utf-8", errors="replace")


def clean_html(value):
    return unescape(re.sub(r"<[^>]+>", "", value)).strip()


def folder_name(title, code):
    title = clean_html(title)
    title = title.replace("ブースターパック vol.1", "ブースターパックvol1")
    title = re.sub(r"[\\/:*?\"<>|]", "", title)
    title = re.sub(r"[\s.]+", "", title)
    return title or code


def products():
    page = text(BASE + "/cardlist/?change_ex")
    pattern = re.compile(
        r'<a[^>]+href="/cardlist/searchresults/\?expansion=([^"&]+)"[^>]*>(.*?)</a>',
        re.S,
    )
    found = []
    seen = set()
    for code, block in pattern.findall(page):
        if code in seen:
            continue
        match = re.search(r'<p class="item-Title">(.*?)</p>', block, re.S)
        title = clean_html(match.group(1)) if match else code
        found.append((code, title, folder_name(title, code)))
        seen.add(code)
    if not found:
        raise RuntimeError("No products found; the official product page structure may have changed.")
    return found


def parse_cards(page, code, product_title, folder):
    starts = list(re.finditer(r'<div class="[^"]*cardlist-Result_Item[^"]*"[^>]+card="([^"]+)"', page))
    cards = []
    for index, match in enumerate(starts):
        end = starts[index + 1].start() if index + 1 < len(starts) else len(page)
        block = page[match.start():end]
        image = re.search(r'<div class="image-Inner"><img src="([^"]+)" alt="([^"]*)"', block)
        if not image:
            continue
        values = [clean_html(value) for value in re.findall(r'<dd>(.*?)</dd>', block, re.S)]
        card_type = values[1] if len(values) > 1 else ""
        number = clean_html(match.group(1)).replace("＋", "+")
        image_url = urllib.parse.urljoin(BASE, unescape(image.group(1)))
        image_file = Path(urllib.parse.unquote(urllib.parse.urlparse(image_url).path)).name
        cards.append({
            "productCode": code,
            "product": product_title,
            "folder": folder,
            "cardNumber": number,
            "name": clean_html(image.group(2)),
            "type": card_type,
            "imageUrl": image_url,
            "imageFile": image_file,
        })
    return cards


def crawl_product(code, title, folder):
    url = BASE + f"/cardlist/searchresults/?expansion={urllib.parse.quote(code)}&sort=new&view=text"
    cards = parse_cards(text(url), code, title, folder)
    page_number = 2
    while True:
        fragment_url = BASE + f"/cardlist/cardsearch_ex?expansion={urllib.parse.quote(code)}&view=text&page={page_number}"
        try:
            batch = parse_cards(text(fragment_url), code, title, folder)
        except urllib.error.HTTPError as error:
            if error.code == 404:
                break
            raise
        if not batch:
            break
        cards.extend(batch)
        page_number += 1
    unique = {}
    for card in cards:
        unique[(card["cardNumber"], card["imageUrl"])] = card
    print(f"{code}: {len(unique)} cards in {folder}", flush=True)
    return list(unique.values())


def download_image(card):
    folder = CARDS_ROOT / card["folder"] / "Images"
    folder.mkdir(parents=True, exist_ok=True)
    destination = folder / card["imageFile"]
    if not destination.exists() or destination.stat().st_size == 0:
        data = fetch(card["imageUrl"])
        destination.write_bytes(data)
    card["imageAssetPath"] = destination.relative_to(ROOT).as_posix()
    return card


def main():
    CARDS_ROOT.mkdir(parents=True, exist_ok=True)
    all_cards = []
    product_list = products()
    print(f"Found {len(product_list)} products", flush=True)
    for product in product_list:
        all_cards.extend(crawl_product(*product))

    all_cards.sort(key=lambda c: (c["productCode"], c["cardNumber"], c["imageUrl"]))
    with ThreadPoolExecutor(max_workers=8) as pool:
        futures = [pool.submit(download_image, card) for card in all_cards]
        completed = 0
        for future in as_completed(futures):
            future.result()
            completed += 1
            if completed % 100 == 0 or completed == len(futures):
                print(f"Downloaded/verified {completed}/{len(futures)} images", flush=True)

    manifest_cards = []
    for index, card in enumerate(all_cards):
        card.pop("imageUrl", None)
        card.pop("imageFile", None)
        card["id"] = 10000 + index
        manifest_cards.append(card)
    MANIFEST.write_text(json.dumps({"cards": manifest_cards}, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote {MANIFEST} with {len(manifest_cards)} cards", flush=True)


if __name__ == "__main__":
    main()
