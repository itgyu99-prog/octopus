# 정령기사 작업 범위

- 사용자 요청에 따라 `Source/정령기사`는 기존 `Source/문어_유한맵`과 별개의 게임이다. 이 폴더에서 요청된 정령기사 작업은 여기를 기준으로 수정한다.
- 먼저 `README.md` 및 필요한 `docs/정령기사/구조/<범주>/README.md`를 읽는다. 작업 폴더 루트 기준 조회: `python tools/project_index.py --project ashen-oath --category game`.
- 기사·정령·룬·적·성소는 `OathArt.cs`의 독자적 절차적 아트다. 문어 프로젝트의 캐시·설정·씬을 가져오지 않는다.
- 기존 `Build/Windows`, `Build/Validation`, `Logs` 경로를 재사용한다. 배포 파일과 캐시는 소스 Git 커밋에서 제외한다.
- 소스 추가/변경 시 루트 `tools/project_categories.json`의 `additional_projects.ashen-oath`와 색인을 갱신한다.
- 일반 Windows 빌드에는 자동 검증 코드를 포함하지 않는다. 검증 실행으로 사용자 최고 기록을 변경하지 않는다.
