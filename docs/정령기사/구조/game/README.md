# 게임·전투

정령기사 이동, 룬 획득, 정령 공격, 적과 보스, 성장, 승패

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- `Source/정령기사/Assets/Scripts/OathGame.cs`

## 관련 파일


## `Assets/Scripts/OathGame.cs` 진입 함수

- `StartRun` — 110행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `AcquireSpirit` — 186행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CastSpirit` — 202행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickWaves` — 253행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `Bless` — 371행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `Finish` — 311행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## 범위 검색

```powershell
python tools/project_index.py --project ashen-oath --category game
python tools/project_index.py --project ashen-oath --category game --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
