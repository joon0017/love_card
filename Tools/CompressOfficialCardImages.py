#!/usr/bin/env python3
"""Convert downloaded card PNGs to smaller on-demand StreamingAssets JPEGs."""

from pathlib import Path
import json
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "Assets/Resources/Cards/official_cards.json"
STREAMING = ROOT / "Assets/StreamingAssets/CardImages"
MAX_WIDTH = 512
JPEG_QUALITY = 72


def main():
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    cards = manifest["cards"]
    converted = 0
    original_bytes = 0
    compressed_bytes = 0

    for card in cards:
        source = ROOT / card["imageAssetPath"]
        relative = Path(card["folder"]) / (source.stem + ".jpg")
        destination = STREAMING / relative
        destination.parent.mkdir(parents=True, exist_ok=True)

        if source.exists():
            original_bytes += source.stat().st_size
            if not destination.exists() or destination.stat().st_size == 0:
                with Image.open(source) as image:
                    image = image.convert("RGB")
                    if image.width > MAX_WIDTH:
                        height = round(image.height * MAX_WIDTH / image.width)
                        image = image.resize((MAX_WIDTH, height), Image.Resampling.LANCZOS)
                    image.save(destination, "JPEG", quality=JPEG_QUALITY, optimize=True, progressive=True)
                converted += 1
        if not destination.exists():
            raise FileNotFoundError(f"Neither source nor compressed image exists for {card['cardNumber']}")

        compressed_bytes += destination.stat().st_size
        card["streamingImagePath"] = (Path("CardImages") / relative).as_posix()
        card.pop("imageAssetPath", None)

    # Remove only files enumerated by the official manifest and their Unity metadata.
    for card in cards:
        # Old paths were removed above, so reconstruct from product image directories by stem.
        folder = ROOT / "Assets/Resources/Cards" / card["folder"] / "Images"
        for candidate in folder.glob(Path(card["streamingImagePath"]).stem + ".*"):
            if candidate.suffix.lower() in {".png", ".jpg", ".webp", ".meta"}:
                candidate.unlink(missing_ok=True)

    # Remove generated categorized prefabs; Unity will recreate lightweight versions.
    for folder in {card["folder"] for card in cards}:
        product = ROOT / "Assets/Resources/Cards" / folder
        for prefab in product.glob("*.prefab"):
            prefab.unlink(missing_ok=True)
            Path(str(prefab) + ".meta").unlink(missing_ok=True)

    MANIFEST.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Converted {converted} images; {original_bytes / 1048576:.1f} MB -> {compressed_bytes / 1048576:.1f} MB")


if __name__ == "__main__":
    main()
