# 공통

공용 타입, 설정 데이터, 아트 로딩 및 세션 중심부

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- `Source/문어_유한맵/Assets/Scripts/Core/GameConfig.cs`
- `Source/문어_유한맵/Assets/Scripts/Core/GameplayRules.cs`
- `Source/문어_유한맵/Assets/Scripts/Core/GameTypes.cs`
- `Source/문어_유한맵/Assets/Scripts/Core/RuntimeArt.cs`
- `Source/문어_유한맵/Assets/Scripts/Core/SpriteFactory.cs`
- `Source/문어_유한맵/Assets/Scripts/World/GameSession.cs`

## 관련 파일

- `Source/문어_유한맵/Assets/Resources/GameplayRules.json`

## `Assets/Scripts/World/GameSession.cs` 진입 함수

- `ServerTick` — 407행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `Update` — 382행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `BeginGame` — 228행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CheckGameOver` — 2029행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## 범위 검색

```powershell
python tools/project_index.py --category shared
python tools/project_index.py --category shared --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
