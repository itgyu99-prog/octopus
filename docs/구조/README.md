# 프로젝트 작업 색인

수정 기준: `Source/문어_유한맵`. 요청에 해당하는 범주 하나부터 읽는다.

| 범주 | 작업 내용 | 검색 키 |
|---|---|---|
| [맵](maps/README.md) | 해변 배경, 맵 경계, 월드 표시 | `maps` |
| [캐릭터](characters/README.md) | 플레이어 이동, 입력, 카메라, 문어 애니메이션 | `characters` |
| [몬스터](monsters/README.md) | 일반 적 생성·이동, 보스 패턴·처치 | `monsters` |
| [전투](combat/README.md) | 피해 판정, 상태이상, 근접·투사체 공격 | `combat` |
| [속성·요소](abilities/README.md) | 기본 속성, 합성 요소, 특수능력 및 효과 | `abilities` |
| [성장·아이템](progression/README.md) | 스테이지 진행, 능력 선택, 납품, 드랍·회복 | `progression` |
| [UI](ui/README.md) | 로비, 대기실, HUD, 합성창, 개발 메뉴 | `ui` |
| [네트워크](network/README.md) | LAN·Relay, 접속·준비, 서버 권한·상태 동기화 | `network` |
| [공통](shared/README.md) | 공용 타입, 설정 데이터, 아트 로딩 및 세션 중심부 | `shared` |
| [검증·빌드](checks/README.md) | Unity 에디터 검사, 협동 검사, 빌드·배포 설정 | `checks` |

## 읽는 순서

1. 위 표에서 범주를 선택하고 해당 README만 읽는다.
2. 함수명으로 위치를 검색하고 필요한 부분만 읽는다.
3. 연관 동작을 바꿀 때만 관련 범주로 확장한다.

전체 탐색 대신 `python tools/project_index.py --locate "파일명"`을 사용한다.
파일 추가·이름 변경 후 `python tools/project_index.py --refresh`로 색인을 갱신한다.

## 보관·도구 위치

- `docs/검증`: 결과 로그 요약과 확인 이미지. 검증 요청이 있을 때만 읽는다.
- `개발기록.md`, `작업결과.md`: 과거 개발 기록. 현재 동작·수치는 코드와 JSON으로 확인한다.
- `실행안내.md`: 게임 실행 및 접속 안내.
- `tools/package_release.py`: 배포 패키징. 나머지 기존 구현/수정 도구는 과거 코드 재작성용이므로 일반 작업에서 실행하지 않는다.
- `My project`, Windows 폴더, ZIP: 별도 프로젝트·배포 사본. 기본 검색에서 제외.
- `Library`, `Temp`, `Build`, `Logs`: Unity 생성물. 기본 검색에서 제외.

현재 몬스터와 여러 시스템은 GameSession.cs를 공유한다. 이 색인은 실제 위치를 안내하며 코드·Resources 경로·Unity GUID는 유지한다.
