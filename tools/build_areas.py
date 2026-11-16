#!/usr/bin/env python3
"""data/areas.ko.json 생성기.

지역 ID · 액트 · 지역 레벨 · 마을 여부는 exile-leveling(HeartofPhos/exile-leveling)의
areas.json에서, 한국어 지역명은 poedb.tw/kr 의 액트 페이지(Act_1 ~ Act_10)에서 가져온다.

Client.txt 의 `Generating level N area "<ID>"` 줄은 클라이언트 언어와 무관하게 ID를 찍으므로,
앱은 ID로 현재 액트를 판단하고 이 파일의 한국어 이름은 화면 표시에만 쓴다.

사용법:  python3 tools/build_areas.py            (네트워크에서 받아 data/areas.ko.json 덮어쓰기)
"""
import html
import json
import pathlib
import re
import sys
import time
import urllib.request

AREAS_URL = "https://raw.githubusercontent.com/HeartofPhos/exile-leveling/main/common/data/json/areas.json"
POEDB_ACT_URL = "https://poedb.tw/kr/Act_{n}"
OUT = pathlib.Path(__file__).resolve().parent.parent / "data" / "areas.ko.json"

# poedb 액트 페이지 표에 없는 지역 (미궁 입구)
EXTRA_KO = {"Labyrinth_Airlock": "지망자의 광장"}


def fetch(url: str) -> str:
    req = urllib.request.Request(url, headers={"User-Agent": "poe-act-goal-guide-data-builder"})
    with urllib.request.urlopen(req, timeout=30) as r:
        return r.read().decode("utf-8")


def main() -> int:
    areas = json.loads(fetch(AREAS_URL))

    ko: dict[str, str] = dict(EXTRA_KO)
    pattern = re.compile(r'data-hover="\?s=Data%5CWorldAreas%2F([^"]+)" href="[^"]+">([^<]+)</a>')
    for n in range(1, 11):
        page = fetch(POEDB_ACT_URL.format(n=n))
        for area_id, name in pattern.findall(page):
            ko[area_id] = html.unescape(name).strip()
        time.sleep(1)

    out = {}
    missing = []
    for area_id, a in sorted(areas.items(), key=lambda kv: (kv[1]["act"], kv[1]["level"], kv[0])):
        if not 1 <= a["act"] <= 10:
            continue
        if area_id not in ko:
            missing.append(f'{area_id} ({a["name"]})')
            continue
        out[area_id] = {
            "name": ko[area_id],
            "nameEn": a["name"],
            "act": a["act"],
            "level": a["level"],
            "town": bool(a["is_town_area"]),
        }

    OUT.write_text(json.dumps(out, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {len(out)} areas -> {OUT}")
    if missing:
        print("한국어 이름이 없어 제외한 지역 (미궁 보스방·아레나 등):", ", ".join(missing))
    return 0


if __name__ == "__main__":
    sys.exit(main())
