# 프로젝트 작업 색인

수정 기준: `Source/정령기사`. 요청에 해당하는 범주 하나부터 읽는다.

| 범주 | 작업 내용 | 검색 키 |
|---|---|---|
| [게임·전투](game/README.md) | 정령기사 이동, 룬 획득, 정령 공격, 적과 보스, 성장, 승패 | `game` |
| [아트·성소](art/README.md) | 기사·정령·적 스프라이트, 룬, 성소 바닥과 장식 | `art` |
| [UI](ui/README.md) | 시작, 안내, HUD, 축복 선택, 일시정지, 결과 화면 | `ui` |
| [검증·빌드](checks/README.md) | Windows 빌드, 실제 플레이 흐름 검증과 화면 캡처 | `checks` |

## 사용법

`python tools/project_index.py --project ashen-oath --category game`

`python tools/project_index.py --project ashen-oath --refresh`

별도 게임 프로젝트다. 기존 문어 게임 파일을 변경하지 않는다. 실행·조작·검증 안내는 프로젝트 README.md를 참조한다.
