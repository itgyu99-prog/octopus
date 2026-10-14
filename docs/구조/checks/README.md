# 검증·빌드

Unity 에디터 검사, 협동 검사, 빌드·배포 설정

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- `Source/문어_유한맵/Assets/Editor/BuildTool.cs`
- `Source/문어_유한맵/Assets/Editor/CharacterVisualChecks.cs`
- `Source/문어_유한맵/Assets/Editor/CombatVisualChecks.cs`
- `Source/문어_유한맵/Assets/Editor/ElementEffectChecks.cs`
- `Source/문어_유한맵/Assets/Editor/FiniteMapChecks.cs`
- `Source/문어_유한맵/Assets/Editor/GiantMonsterSetup.cs`
- `Source/문어_유한맵/Assets/Editor/SystemDataChecks.cs`
- `Source/문어_유한맵/Assets/Scripts/Net/CoopSystemChecks.cs`
- `Source/문어_유한맵/Assets/Scripts/Net/GiantCombatChecks.cs`

## 관련 파일

- `Source/문어_유한맵/../../.gitignore`
- `Source/문어_유한맵/Packages/manifest.json`
- `Source/문어_유한맵/ProjectSettings/EditorBuildSettings.asset`
- `Source/문어_유한맵/ProjectSettings/ProjectVersion.txt`

## 범위 검색

```powershell
python tools/project_index.py --category checks
python tools/project_index.py --category checks --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
