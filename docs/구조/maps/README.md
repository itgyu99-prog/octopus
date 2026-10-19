# 맵

해변 배경, 맵 경계, 월드 표시

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- `Source/문어_유한맵/Assets/Scripts/World/ArenaBuilder.cs`
- `Source/문어_유한맵/Assets/Scripts/World/WorldView.cs`

## 관련 파일

- `Source/문어_유한맵/Assets/Resources/Art/Beach.bytes`
- `Source/문어_유한맵/Assets/Scenes/SampleScene.unity`
- `Source/문어_유한맵/Assets/Scripts/Core/GameConfig.cs`
- `Source/문어_유한맵/Assets/Scripts/Core/RuntimeArt.cs`

## `Assets/Scripts/World/WorldView.cs` 진입 함수

- `TryEnemyBodyDiameter` — 48행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/World/GameSession.cs` 진입 함수

- `RandomInterior` — 1979행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `RandomEdge` — 2053행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ActiveCenter` — 2004행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/World/ArenaBuilder.cs` 진입 함수

- `BuildBackdrop` — 42행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## 범위 검색

```powershell
python tools/project_index.py --category maps
python tools/project_index.py --category maps --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
