# 가이드 데이터

오버레이가 보여 주는 내용은 전부 `data/` 의 JSON 두 개에서 온다. 빌드하면 exe 옆 `Data\` 로 복사되므로, 배포한 뒤에도 그 폴더의 파일을 고치고 앱을 다시 시작하면 된다.

## `guide.ko.json` — 장별 루트 단계 (손으로 작성)

```json
{
  "schemaVersion": 2,
  "acts": [
    {
      "act": 1,
      "steps": [
        { "kind": "main", "areas": ["1_1_1"], "text": "황혼의 해안: 힐록 처치", "note": "타클레이에게 보상 (눈 앞의 적)" },
        { "kind": "passive", "areas": ["1_1_4_0"], "text": "물에 잠긴 심연: 심연의 주인 처치", "note": "..." }
      ]
    }
  ]
}
```

`steps` 는 **진행 순서대로** 적는다. 순서가 곧 루트다.

| 필드 | 필수 | 설명 |
|---|---|---|
| `kind` | ✓ | `main`(진행) · `trial`(전직) · `passive`(패시브) · `recipe`(작업대) · `quest`(퀘스트) |
| `areas` | ✓ | 이 단계를 하는 지역 ID 목록. 플레이어가 이 중 한 곳에 들어가면 루트가 이 단계로 넘어온다 |
| `text` | ✓ | 한 줄 설명. "지역: 할 일" 형식 |
| `note` | | 둘째 줄 (방향, 권장 레벨, 보상 NPC, 퀘스트 이름) |
| `key` | | `true` 면 ★ 표시 |

### `areas` 를 고를 때

진행 추적은 `areas` 만 보고 움직이므로, 여기에 무엇을 넣느냐가 자동 진행의 정확도를 정한다.

- **그 단계를 하러 처음 들어가는 지역**을 넣는다. 지나가기만 하는 통로를 넣으면, 그 통로가 다음 단계로 가는 길이기도 할 때 루트가 미리 넘어가 버린다 (4단계 앞까지 내다보기 때문).
- **마을**은 바로 다음 단계일 때만 진행되므로, 마을에서 하는 보고 단계에는 마을을 넣어도 안전하다.
- 다른 장의 지역도 넣을 수 있다 (2장의 1장 라이온아이 초소, 4·7·10장의 `Labyrinth_Airlock`). 루트가 가리키는 동안에는 장이 바뀌지 않는다.
- 같은 지역에서 연달아 하는 단계는 같은 지역 ID를 반복해서 쓰면 된다. 그 지역을 떠나면 함께 완료 처리된다.

### 용어

한국어 클라이언트 표기만 쓴다. 영어 알파벳이 들어가면 테스트(`Step_texts_use_korean_terms_only`)가 실패한다 (예외: 채팅 명령어 `/passives`).

| 영문 | 한국어 클라이언트 |
|---|---|
| Waypoint | 거점 |
| Portal | 포탈 |
| Socket / Linked sockets | 홈 / 연결된 홈 |
| Crafting bench | 작업대 |
| Trial of Ascendancy (지역) | ~의 미궁 도전 |
| Labyrinth: Normal / Cruel / Merciless | 지망자의 시험 (군주의 미궁) / 잔혹한 미궁 / 무자비한 미궁 |
| Aspirants' Plaza | 지망자의 광장 |
| Pantheon major / minor god | 판테온 주신 / 약신 |
| Book of Skill | 기술의 서 |
| Quicksilver Flask / Orb of Augmentation | 수은 플라스크 / 확장의 오브 |

## `areas.ko.json` — 지역 표 (생성됨)

```json
"1_1_2": { "name": "해안 지대", "nameEn": "The Coast", "act": 1, "level": 2, "town": false }
```

현재 장 판단(`act`), 마을 여부(`town`), 헤더 표시(`name`, `level`)에 쓴다. 직접 고치지 말고 생성기를 다시 돌린다.

```bash
python3 tools/build_areas.py
```

## 검증

`dotnet test` 의 `GuideDataTests` 가 실제 데이터 파일을 읽어 다음을 확인한다. 앱도 시작할 때 같은 검사를 하고 문제가 있으면 경고 창을 띄운다.

- 1~10장이 각각 정확히 하나씩 있는지, 빈 `text`/`areas`, 지역 표에 없는 지역 ID
- 각 장의 첫 단계가 그 장의 지역에서 시작하는지
- 미궁 도전 12곳이 모두 루트에 있는지
- 패시브 단계가 23개인지 (퀘스트 패시브 24포인트 = 1포인트 퀘스트 21개 + 허기의 끝 2포인트 + 도적 모두 처치 1포인트)
- 단계 문구에 영어 알파벳이 없는지

`ProgressTrackerTests` 에는 실제 루트 데이터로 1장 초반, 2장 도적·나아가는 길, 8장 피의 수로 우회를 따라가는 테스트가 있다.

## 출처

| 내용 | 출처 |
|---|---|
| 루트 순서, 단계별 방향·권장 레벨 힌트 | [HeartofPhos/exile-leveling](https://github.com/HeartofPhos/exile-leveling) (MIT) `common/data/routes/act-*.txt` — 2026-07-24 갱신분 |
| 지역 ID · 장 · 지역 레벨 · 마을 여부 · 작업대 옵션 지역(미궁 도전 구분) | 같은 저장소 `common/data/json/areas.json` |
| 한국어 지역·보스·퀘스트·NPC·아이템 이름, 퀘스트 목표와 보상 | [poedb.tw/kr](https://poedb.tw/kr/) 장(Act_1~10)·퀘스트 페이지 (2026-11 기준) |
| 작업대 옵션 문구와 잠금 해제 지역 | poedb.tw/kr `Crafting_Bench` (작업대) |
| 3.30 캠페인 변경 사항 | 3.30.0 코븐스 오브 움브라 패치 노트 |
