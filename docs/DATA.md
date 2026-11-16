# 가이드 데이터

오버레이가 보여 주는 내용은 전부 `data/` 의 JSON 두 개에서 온다. 빌드하면 exe 옆 `Data\` 로 복사되므로, 배포한 뒤에도 그 폴더의 파일을 고치고 앱을 다시 시작하면 된다.

## `guide.ko.json` — 액트별 목표 (손으로 작성)

```json
{
  "acts": [
    {
      "act": 1,
      "goals": [
        {
          "id": "a1-passive-dweller",
          "kind": "passive",
          "areas": ["1_1_4_1", "1_1_4_0"],
          "text": "물에 잠긴 심연에서 심연의 주인 처치",
          "note": "물에 잠긴 길에서 진입 · 보상: 타클레이 (심연의 주인)",
          "key": false
        }
      ]
    }
  ]
}
```

| 필드 | 필수 | 설명 |
|---|---|---|
| `id` | ✓ | 전체에서 유일한 식별자. 관례: `a<액트>-<kind>-<이름>` |
| `kind` | ✓ | `main`(진행) · `trial`(전직) · `passive`(패시브 포인트) · `recipe`(조합법) · `quest`(유용한 퀘스트). 섹션도 이 순서로 나온다 |
| `areas` | ✓ | 이 목표를 하는 지역 ID 목록. 플레이어가 이 중 한 곳에 들어가면 강조된다. 진입로가 되는 지역도 넣어 두면 미리 강조된다 |
| `text` | ✓ | 한 줄 목표. "~에서 ~ 처치" 형식을 권장 |
| `note` | | 둘째 줄 (보상 NPC, 가는 길, 퀘스트 이름) |
| `key` | | `true` 면 ★ 표시, 간단히 보기에서도 남는다 |

`areas` 에 다른 액트의 지역을 넣어도 된다 (예: 2장에서 1장 베스텔에게 보고하는 '나아가는 길').

### 지역 ID 찾기

`areas.ko.json` 에서 한국어 이름으로 검색하면 된다. 실제 게임에서 확인하려면 `Client.txt` 에서 `Generating level` 을 찾으면 방금 들어간 지역의 ID가 보인다.

## `areas.ko.json` — 지역 표 (생성됨)

```json
"1_1_2": { "name": "해안 지대", "nameEn": "The Coast", "act": 1, "level": 2, "town": false }
```

현재 액트 판단(`act`)과 헤더 표시(`name`, `level`)에 쓴다. 직접 고치지 말고 생성기를 다시 돌린다.

```bash
python3 tools/build_areas.py
```

## 검증

`dotnet test` 의 `GuideDataTests` 가 실제 데이터 파일을 읽어 다음을 확인한다. 앱도 시작할 때 같은 검사를 하고 문제가 있으면 경고 창을 띄운다.

- 1~10장이 각각 정확히 하나씩 있는지
- `id` 중복, 빈 `text`/`areas`
- `areas` 의 모든 ID가 지역 표에 있는지
- 각 액트에 진행 목표와 전직/패시브 목표가 있는지, 1~3장 전직 시험 6곳이 다 있는지

## 출처

| 내용 | 출처 |
|---|---|
| 지역 ID · 액트 · 지역 레벨 · 마을 여부 · 조합법 위치 | [HeartofPhos/exile-leveling](https://github.com/HeartofPhos/exile-leveling) `common/data/json/areas.json` |
| 액트 진행 순서, 보스와 퀘스트 아이템 | 같은 저장소 `common/data/routes/act-*.txt` |
| 한국어 지역·보스·퀘스트·NPC 이름, 퀘스트 보상 | [poedb.tw/kr](https://poedb.tw/kr/) 액트·퀘스트 페이지 (2026-11 기준) |

조합법 이름(예: "소켓 색상 변경", "이동 속도 1단계")은 영문 레시피 이름을 우리말로 옮긴 것이라 게임 내 표기와 조금 다를 수 있다.
