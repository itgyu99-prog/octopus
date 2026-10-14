# UI

로비, 대기실, HUD, 합성창, 개발 메뉴

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- `Source/문어_유한맵/Assets/Scripts/UI/CombinePanel.cs`
- `Source/문어_유한맵/Assets/Scripts/UI/DevMenu.cs`
- `Source/문어_유한맵/Assets/Scripts/UI/GameHud.cs`
- `Source/문어_유한맵/Assets/Scripts/UI/LanLobby.cs`
- `Source/문어_유한맵/Assets/Scripts/UI/UiUtil.cs`
- `Source/문어_유한맵/Assets/Scripts/UI/WaitingRoom.cs`

## 관련 파일

- `Source/문어_유한맵/Assets/Scripts/Core/GameTypes.cs`

## `Assets/Scripts/World/GameSession.cs` 진입 함수

- `ApplyPhaseUi` — 181행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## 범위 검색

```powershell
python tools/project_index.py --category ui
python tools/project_index.py --category ui --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
