# 캐릭터

플레이어 이동, 입력, 카메라, 문어 애니메이션

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- `Source/문어_유한맵/Assets/Scripts/Player/OctopusPlayer.cs`
- `Source/문어_유한맵/Assets/Scripts/Player/OctopusSheet.cs`
- `Source/문어_유한맵/Assets/Scripts/Player/OctopusWalkSprites.cs`
- `Source/문어_유한맵/Assets/Scripts/Player/PlayerCameraFollow.cs`
- `Source/문어_유한맵/Assets/Scripts/Player/PlayerInputReader.cs`

## 관련 파일

- `Source/문어_유한맵/../../docs/검증/octopus-character-assets/hit-64.png`
- `Source/문어_유한맵/../../docs/검증/octopus-character-assets/idle-64.png`
- `Source/문어_유한맵/../../docs/검증/octopus-character-assets/stop-64.png`
- `Source/문어_유한맵/../../docs/검증/octopus-character-assets/victory-64.png`
- `Source/문어_유한맵/../../docs/검증/octopus-character-assets/walk-64.png`
- `Source/문어_유한맵/Assets/Resources/Art/Attack.bytes`
- `Source/문어_유한맵/Assets/Resources/Art/Basic.bytes`
- `Source/문어_유한맵/Assets/Resources/Art/Whip.bytes`
- `Source/문어_유한맵/Assets/Resources/OctopusArmStretch.shader`
- `Source/문어_유한맵/Assets/Resources/OctopusWalk.bytes`
- `Source/문어_유한맵/Assets/Scripts/Core/GameConfig.cs`
- `Source/문어_유한맵/Assets/Scripts/Core/RuntimeArt.cs`

## 범위 검색

```powershell
python tools/project_index.py --category characters
python tools/project_index.py --category characters --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
