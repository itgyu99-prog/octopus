# 검증·빌드

Windows 빌드, 실제 플레이 흐름 검증과 화면 캡처

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- `Source/정령기사/Assets/Editor/OathBuild.cs`
- `Source/정령기사/Assets/Scripts/OathChecks.cs`

## 관련 파일

- `Source/정령기사/Packages/manifest.json`
- `Source/정령기사/ProjectSettings/ProjectVersion.txt`

## 범위 검색

```powershell
python tools/project_index.py --project ashen-oath --category checks
python tools/project_index.py --project ashen-oath --category checks --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
