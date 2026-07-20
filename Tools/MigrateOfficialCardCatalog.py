#!/usr/bin/env python3
"""Enrich official_cards.json and replace bundled images with official URLs."""
from pathlib import Path
import json, re, shutil, urllib.parse

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "Assets/Resources/Cards/official_cards.json"
STREAMING_IMAGES = ROOT / "Assets/StreamingAssets/CardImages"
IMAGE_BASE = "https://llofficial-cardgame.com/wordpress/wp-content/images/cardlist"

NAME_KR = {
"高坂穂乃果":"코사카 호노카","絢瀬絵里":"아야세 에리","南ことり":"미나미 코토리","園田海未":"소노다 우미","星空凛":"호시조라 린","西木野真姫":"니시키노 마키","東條希":"토죠 노조미","小泉花陽":"코이즈미 하나요","矢澤にこ":"야자와 니코",
"高海千歌":"타카미 치카","桜内梨子":"사쿠라우치 리코","松浦果南":"마츠우라 카난","黒澤ダイヤ":"쿠로사와 다이아","渡辺曜":"와타나베 요우","津島善子":"츠시마 요시코","国木田花丸":"쿠니키다 하나마루","小原鞠莉":"오하라 마리","黒澤ルビィ":"쿠로사와 루비",
"上原歩夢":"우에하라 아유무","中須かすみ":"나카스 카스미","桜坂しずく":"오사카 시즈쿠","朝香果林":"아사카 카린","宮下愛":"미야시타 아이","近江彼方":"코노에 카나타","優木せつ菜":"유키 세츠나","エマヴェルデ":"엠마 베르데","天王寺璃奈":"텐노지 리나","三船栞子":"미후네 시오리코","ミアテイラー":"미아 테일러","鐘嵐珠":"쇼우 란쥬","高咲侑":"타카사키 유우",
"澁谷かのん":"시부야 카논","唐可可":"탕 쿠쿠","嵐千砂都":"아라시 치사토","平安名すみれ":"헤안나 스미레","葉月恋":"하즈키 렌","桜小路きな子":"사쿠라코지 키나코","米女メイ":"요네메 메이","若菜四季":"와카나 시키","鬼塚夏美":"오니츠카 나츠미","ウィーンマルガレーテ":"빈 마르가레테","マルガレーテ":"빈 마르가레테","鬼塚冬毬":"오니츠카 토마리",
"乙宗梢":"오토무네 코즈에","夕霧綴理":"유기리 츠즈리","藤島慈":"후지시마 메구미","日野下花帆":"히노시타 카호","村野さやか":"무라노 사야카","大沢瑠璃乃":"오사와 루리노","百生吟子":"모모세 긴코","徒町小鈴":"카치마치 코스즈","安養寺姫芽":"안요지 히메","桂城泉":"카츠라기 이즈미","セラス柳田リリエンフェルト":"세라스 야나기다 릴리엔펠트","金城マイカ":"킨죠 마이카","令沢葵":"레이자와 아오이","紫藤美音":"시린 미온",
"高橋ポルカ":"타카하시 폴카","麻布麻衣":"아자부 마이","五桐玲":"고토 아키라","駒形花火":"코마가타 하나비","金澤奇跡":"카나자와 미라클","調布のりこ":"쵸후 노리코","春宮ゆくり":"하루미야 유쿠리","此花輝夜":"코노하나 오로라","山田真緑":"야마다 미도리","佐々木翔音":"사사키 시온",
"綺羅ツバサ":"키라 츠바사","優木あんじゅ":"유우키 안쥬","統堂英玲奈":"토도 에레나","鹿角聖良":"카즈노 세이라","鹿角理亞":"카즈노 리아","柊摩央":"히이라기 마오","聖澤悠奈":"히지리사와 유우나",
}

ROMAN_KR = {
"HINOSHITA KAHO":"히노시타 카호","OTOMUNE KOZUE":"오토무네 코즈에","YUGIRI TSUZURI":"유기리 츠즈리","FUJISHIMA MEGUMI":"후지시마 메구미","ANYOJI HIME":"안요지 히메","MURANO SAYAKA":"무라노 사야카","OSAWA RURINO":"오사와 루리노","MOMOSE GINKO":"모모세 긴코","KACHIMACHI KOSUZU":"카치마치 코스즈","UEHARA AYUMU":"우에하라 아유무","NAKASU KASUMI":"나카스 카스미","KONOE KANATA":"코노에 카나타","TENNOJI RINA":"텐노지 리나","MIA TAYLOR":"미아 테일러","SHIBUYA KANON":"시부야 카논","ARASHI CHISATO":"아라시 치사토","HAZUKI REN":"하즈키 렌","TANG KEKE":"탕 쿠쿠","YONEME MEI":"요네메 메이","WAKANA SHIKI":"와카나 시키","WIEN MARUGARETE":"빈 마르가레테","ONITSUKA NATSUMI":"오니츠카 나츠미","ONITUKA TOMARI":"오니츠카 토마리","SAKURAKOJI KINAKO":"사쿠라코지 키나코","SAKURAKOJIKINAKO":"사쿠라코지 키나코","HEANNA SUMIRE":"헤안나 스미레","SAKURAUCHI RIKO":"사쿠라우치 리코","MATSUURA KANAN":"마츠우라 카난","KUROSAWA DIA":"쿠로사와 다이아","TSUSHIMA YOSHIKO":"츠시마 요시코","HOSHIZORA RIN":"호시조라 린","NISHIKINO MAKI":"니시키노 마키","YAZAWA NICO":"야자와 니코","ASAKA KARIN":"아사카 카린","MIYASHITA AI":"미야시타 아이","YUKI SETSUNA":"유키 세츠나","OSAKA SHIZUIKU":"오사카 시즈쿠",
}

def normalize(value): return re.sub(r"[\s・]", "", value or "")

def korean_name(name):
    if normalize(name) in NAME_KR: return NAME_KR[normalize(name)]
    roman = re.sub(r"\s+", " ", (name or "").strip()).upper()
    if roman in ROMAN_KR: return ROMAN_KR[roman]
    parts = re.split(r"([&＆])", name or "")
    if len(parts) > 1:
        result = []
        for part in parts:
            if part in {"&", "＆"}: result.append(part); continue
            translated = NAME_KR.get(normalize(part))
            if not translated: return ""
            result.append(translated)
        return "".join(result)
    return ""

def enrich_card(card):
    parts = card["cardNumber"].replace("＋", "+").split("-")
    old_path = card.get("streamingImagePath", "")
    stem = Path(old_path).stem or card["cardNumber"].replace("+", "2")
    remote_path = urllib.parse.quote(f"{card['productCode']}/{stem}.png", safe="/!$&'()*+,-._~")
    image_url = card.get("imageUrl") or f"{IMAGE_BASE}/{remote_path}"
    return {
        "productCode":card["productCode"], "numberCode":parts[-2] if len(parts)>1 else "", "rarityCode":parts[-1],
        "product":card["product"], "folder":card["folder"], "cardNumber":card["cardNumber"],
        "name":card.get("name",""), "name_kr":korean_name(card.get("name","")),
        "type":card.get("type",""), "type_kr":{"メンバー":"멤버","ライブ":"라이브","エネルギー":"에너지"}.get(card.get("type",""),""),
        "id":card["id"], "imageUrl":image_url
    }

def main():
    data=json.loads(MANIFEST.read_text(encoding="utf-8"))
    data["cards"]=[enrich_card(card) for card in data["cards"]]
    MANIFEST.write_text(json.dumps(data,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    if STREAMING_IMAGES.exists(): shutil.rmtree(STREAMING_IMAGES)
    Path(str(STREAMING_IMAGES)+".meta").unlink(missing_ok=True)
    print(f"Migrated {len(data['cards'])} cards and removed bundled images")

if __name__=="__main__": main()
