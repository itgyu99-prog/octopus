# 최신 시스템 검증 요약

2026-10-13. Unity 6000.5.7f1 / Windows. 원본 로그는 `Source/문어_유한맵/Logs`에 보관하며 여기에 전체 로그를 복제하지 않는다.

| 검증 | 결과 | 로그 |
|---|---|---|
| 개발 빌드 컴파일·데이터 검사 | 통과 | `system-validation-build.log` |
| 배경 비율·캐릭터 크기·지형·72방향 경계 검사 | 통과 | 위 로그의 `FINITE_MAP_CHECKS_PASSED` |
| 1인 시작·4속성·공격·합성·보상·보스·필드 정리 | 통과 (`SOLO TEST PASS`, 종료 0) | `system-solo.log` |
| 2인 LAN 호스트 | 통과 (`COOP HOST PASS`, 종료 0) | `system-coop-host.log` |
| 2인 LAN 참가자 | 통과 (`COOP CLIENT PASS`, 종료 0) | `system-coop-guest.log` |
| 실제 Unity Relay 호스트/참가자 | 양쪽 통과 (`COOP HOST PASS`, `COOP CLIENT PASS`, 종료 0/0) | `system-relay-host.log`, `system-relay-guest.log` |
| 일반 Windows 빌드 | 통과 (종료 0) | `system-windows-build.log` |

## 확인한 동작

- 배경 실제 이미지 비율 보존 및 2/3 축소. 카메라 8.5와 캐릭터 화면 크기 유지. 해안·바위·이동·스폰 경계.
- 물 1/2/3회 전방위 발사의 개수·분포·동일 방향·실제 피해, 전기 1/2/3회 전방위 확산 및 모든 방향 피해. 불 내부 지속 피해·외부 비피해·강화 시 범위 증가.
- 4개 속성 보유와 순차 발동, 같은 주기 중복 발동 방지, 중복 획득 강화 제거, 합성 재료 보존, 요소 최대 3개.
- 2% 드랍 설정 및 팀 전체가 보유한 속성 후보 제외.
- 보상 1/2·2/2에서 각각 3개 선택지, 즉시 적용, 중복 클릭·오래된 선택 RPC의 재적용 방지, 두 번째 선택 전 전환 금지, 전원 완료 후 3초.
- 실제 참가자 소유 RPC를 통한 합성 및 네 번째 속성 복제. 호스트·참가자 카운트다운 이동.
- 5보스만 특수요소 2개, 처치 전 지급 제한, 클리어 중 보상 획득. 10·15보스에서 특수요소 드랍 없음.
- 전환 후 이전 경험치·속성·유물·회복 아이템 제거, 미획득 특수요소 제거, 획득한 장비·강화 유지.
- `Build/Validation/Screenshots`의 화염지대·4속성 합성창·두 번째 능력 선택 이미지를 열어 확인했다. 캐릭터 주위 큰 흰 원은 보유한 안개의 효과이며 화염지대 판정과 별개다.

## 검사 중 수정과 한계

중앙 오브젝트를 제거한 공격 검사 뒤 납품 검사를 시작하던 준비 누락을 수정했다. 참가자 반복 이동이 맵 경계에 도달하는 상황은 시작 위치 초기화로 분리하고, 다음 검사로 위치가 초기화되기 전 카운트다운 중 실제 이동 거리를 측정하도록 했다. 관련 실패 로그는 `system-coop-guest.failed.log`와 기존 루트 로그에 보존한다.

Relay 자동 검증에서만 사용하는 참가 코드 파일의 읽기/쓰기 충돌은 파일을 읽을 수 있을 때까지 재시도하도록 수정했다. 실패 로그는 `system-relay-guest.failed.log`, 중단한 호스트 로그는 `system-relay-host.failed.log`로 보존했고 재실행은 양쪽 모두 통과했다.

자동 검사는 같은 PC의 게임 프로세스를 사용하며, 보스 스테이지는 개발 이동과 직접 피해로 처치 경로를 확인한다. 실제 두 PC, 정상 플레이로 15스테이지까지의 생존·밸런스, 장시간/끊김 환경은 확인하지 않았다. 화면 캡처는 렌더링 확인이며 사람의 마우스·키보드 플레이 검사를 대신하지 않는다.


## 2026-10-13 속성 개별 쿨타임·이동속도 수정 검증

- Unity 6000.5.7f1 개발 검증 빌드: 데이터 검사 및 컴파일 통과, 종료 0. 기존 `Build/Validation`을 재사용했으며 배포 빌드/ZIP은 만들지 않았다.
- 1인 자동 검사: `SOLO TEST PASS`, 종료 0. 속성 4개 동시 준비 시 전체 발동, 기본 3초 대기, 물만 먼저 준비된 경우 독립 발동을 확인했다.
- 불: 이동 후에도 설치 위치 고정, 내부 지속 피해/외부 비피해 및 강화 범위 증가 통과.
- 물: 단계별 8방향 × 반복 횟수, 전방위 분포, 반복 및 다음 발동의 12도 회전, 실제 투사체 피해 통과. 전기의 기존 확산/반복 검사도 통과.
- 풀: 첫 펄스에서 실제 3개 적 피해 및 가시 표시 3개 생성 통과.
- 탄산수: 5개 액터 생성, 서로 다른 무작위 출발 위치, 3.5초 이동 중 맵 내부 유지 및 방향 변화 통과. `Build/Validation/Screenshots/host-soda-bottles.png`를 열어 PET병 실루엣·뚜껑·라벨 표시를 확인했다.
- 이동속도: 기본 5.875(4.7의 1.25배), 과거 이동속도 투자 값의 영향 없음, 반복 선택지 생성에서 이동속도 제외 확인.
- 2인 LAN: `COOP HOST PASS` / `COOP CLIENT PASS`, 종료 0/0. 속성·합성·보상 RPC·이동·스테이지 전환 회귀 검사 통과. 새 효과도 기존 서버 액터 스냅샷 경로를 사용하며 네트워크 레코드 형식은 유지했다.
- 로그: 프로젝트 `Logs/system-validation-build.log`, `system-solo.log`, `system-coop-host.log`, `system-coop-guest.log` 갱신. 이번 변경에서는 Relay 및 실제 두 PC 검사를 재실행하지 않았다. 정상 생존 플레이의 난이도/장기 밸런스는 자동 검사 범위 밖이다.


## 2026-10-13 넓은 시야·화염 방사·충전 대시·속성 몬스터

- Unity 6000.5.7f1 검증 빌드 및 시스템 데이터/지형 경계 검사 통과. `Build/Validation` 재사용, 배포 ZIP 없음.
- 카메라 11, 캐릭터 화면 높이 4%(1080p 기준 43.2px). 기존 해변 그림 비율과 2×2 화면 크기의 맵 유지.
- 1인: `SOLO TEST PASS`. 화염이 적이 더 많은 전방 부채꼴을 선택하고 뒤쪽 적은 맞히지 않는지, 이동 중 시전자 추적/강화 범위를 확인.
- 대시: 1회 소모/빈 게이지 재사용 거부/3초 충전/강화 중복 적용 방지/획득 후 선택지 제외/강화 후 2회 연속 사용 통과.
- 불·물·풀·전기 적 처치 경로마다 고정 시드 400회 표본 검사: 드랍 발생과 같은 속성만 드랍됨을 확인. 기존 2% 확률 및 팀 중복 제외 유지.
- 2인 LAN 회귀: `COOP HOST PASS`, `COOP CLIENT PASS`. 합성·보상·이동·스테이지 전환과 새 공격/드랍/대시의 호스트 판정 검사 통과.
- `Build/Validation/Screenshots/host-fire-field.png`를 열어 캐릭터 비율, 4종 몬스터 실루엣, 화염 방사, 대시 횟수/게이지를 확인.
- 최신 로그: `Logs/system-validation-build.log`, `system-solo.log`, `system-coop-host.log`, `system-coop-guest.log`. 구 카메라 8.5 기대값 때문에 중단된 첫 검사는 `system-solo.failed.log`에 보존하고 새 기준으로 수정 후 통과.
- 실제 두 PC 및 Relay 재검사, 참가자 키보드 대시의 지연 체감, 장시간 생존 밸런스는 이번 자동 검사 범위 밖이다.


## 2026-10-14 거대 몬스터·고해상도 전투 VFX

### 구현 파일 및 설정

- `Assets/Scripts/World/GiantMonsterDefinition.cs`: 5종 데이터. `Resources/Monsters/Giant1~5.asset`의 Inspector에서 ID/이름/패턴/해금/가중치/체력·공격·속도·크기 배율/공격 범위·쿨타임/준비·회복/돌진/투사체/방어·반격을 수정한다. 기존 일반 적의 시간 성장 공식을 그대로 곱한다.
- `GiantMonsters.cs`: 일반 적 확률 대체, 생존 상한, 양 플레이어 시야 밖 후보 검사, 서버 상태 기계·공격·투사체. `GameSession.cs`: 기존 속성 피해·상태이상·드랍과 통합. 보스 스테이지 전환은 살아 있는 거대 적을 보존한다.
- `GiantMonsterVisual.cs`와 5개 Prefab: 독립 도트 외형 4자세, HP 바, 반투명 예고. 각 데이터의 `prefab`, `idleFrames`, `attackFrames`로 교체 가능. Animator를 연결하면 Idle/Chase/PrepareAttack/Attack/Recovery/Stunned/Dead 이름의 상태를 선택하며 기본 스프라이트 교체는 중지한다.
- `WaterVfx.cs`: 4 비행 프레임·8 물보라 프레임, 잔상·파편 최대 192개 로컬 풀. `HdSpellVisual.cs`: 기본 불·전기·풀, 현재 합성 6종 및 기존 호출 가능 합성 7종. 과일·물고기·씨앗·용암탄도 별도 이미지 사용. 물 적중의 액터 종류만 WaterImpact로 구분하고 피해·범위·수명은 기존 그대로 유지한다.
- `WorldView.cs`: 로컬 표시 연결. `PlayerCameraFollow.cs`: 강한 합성 폭발에만 .045 월드 단위 이하/.1초 흔들림. 캐릭터·배경·카메라 크기11·화면높이4%·대시·기존 불 화염 방사·드랍 규칙 유지.
- 신규 PNG는 프로젝트 Resources/Vfx 및 Resources/Monsters에 포함. 외부 생성 이미지 경로는 실행에 필요하지 않다. 기본 Built-in 렌더러의 SpriteRenderer를 사용하며 파이프라인/Bloom 설정을 바꾸지 않았다.

### 검증

- Unity 6000.5.7f1에서 컴파일/시스템 데이터·맵·조합 검사 및 개발용 Windows 빌드 통과. `Build/Validation` 재사용. 로그 `Logs/combat-validation-build.log`.
- 1인 회귀 검사: 기존 물 반복 발사·전기·화염 방사·대시·드랍·보상 흐름과 거대 5종 준비/공격/기절/넉백/방어/반격/중복 사망 검사.
- 최종 2인 LAN: COOP HOST PASS / COOP CLIENT PASS / GIANT CLIENT SNAPSHOT PASS. 호스트/참가자 검사, 참가자의 5종 스냅샷 표시, 해금/확률 대체의 실제 생존 상한, 보스 등장 중 생성 중지, 보스 전환 시 거대 적 보존, 불가사리 저체력 추가 발사·투사체 거리 만료 검사.
- 고정 로그: `Logs/combat-solo.log`, `Logs/combat-lan-host.log`, `Logs/combat-lan-client.log`. 최종 통과 여부는 각 파일의 SOLO TEST PASS / COOP HOST PASS / COOP CLIENT PASS / GIANT CLIENT SNAPSHOT PASS로 확인한다.
- Unity 렌더 캡처: [물 프레임](water-vfx-preview.png), [거대 5종 자세](giant-monsters-preview.png), [기본 속성](ElementAtlas-preview.png), [현재 합성](ComboAtlas-preview.png), [기존 합성](LegacyAtlas-preview.png), [보조 투사체](DetailAtlas-preview.png). 처음 발견한 행/열 침범은 이미지 여백 재제작과 실제 행·열 경계 지정으로 수정했다.
- 실제 게임 캡처는 `Build/Validation/Screenshots/host-giant-monsters.png`, `host-water-hd.png`, `guest-giant-monsters-client.png`. [게임 내 거대 몬스터](giant-combat-screen.png), [게임 내 물 발사](water-combat-screen.png)도 저장했다.
- 렌더 제출 시간 샘플은 프레임 시트 16~28개에 대한 에디터 CPU 측정이며 전체 게임 FPS나 저사양 보장을 뜻하지 않는다. 반복 물 발사 + 거대 5종의 2초 실제 실행 샘플은 `VFX LIVE SAMPLE`로 기록한다. 최종 LAN 호스트 샘플은 2.00초/875프레임이며 짧은 동일 PC 개발 빌드 샘플로만 해석한다. 장시간 플레이, 실제 두 PC, 이번 변경 후 실제 Relay 접속은 별도 미검증이다.
- 현재 게임에 처치 경험치 지급/획득 로직이 없어 경험치 보상 필드는 연결 준비용이다. 기존 유물 조각/포션 경로는 유지했다. 새로운 경험치 성장 규칙을 임의로 추가하지 않았다.

### 이미지 제작 기록

내장 imagegen으로 생성·편집했다. 원본 에셋은 `Source/문어_유한맵/Assets/Resources` 아래에 저장했다. 최종 제작 지시는 다음과 같다(편집 입력은 같은 주제의 최초 생성 시트).

<details><summary>최종 프롬프트 세트</summary>

- WaterAtlas: “Production Unity 2D water magic VFX sprite atlas. Transparent PNG, exact 4 by 4 equal cell grid, no text, no dividers. Row 1 four looping frames of a luminous translucent aqua water projectile pointing RIGHT, refractive white highlights, flowing trailing liquid ribbons and spray. Rows 2 and 3 eight sequential water splash impact frames: compact droplet, expanding asymmetric crown, breaking splash, outward droplets, fragmentation, fading mist, dispersed droplets, fine spray. Row 4 refractive water droplet, curved liquid wisp, irregular spray cluster, blue-white impact glint. Organic irregular silhouettes, liquid refraction, translucent alpha.”
- ElementAtlas 최종 편집: “Edit target: supplied VFX atlas. Remove ALL opaque colored background; genuine transparent alpha everywhere outside the flames, lightning, vines and particles. Remove the metal nozzles from four fire effects. Keep exact 4x4 grid, all artwork positions, colors, effect contents and frame order. Black, brown, purple and green cloudy background must become empty transparency. Keep thin bright energy filaments and natural antialiased translucent edges. No backdrop, no background glow rectangles. Game texture cutout.”
- ComboAtlas 최종 편집: “Fix this game sprite atlas layout. EXACT 4 columns and 6 equal rows, all 24 cells identical. Preserve all 24 effect images and their order. Shrink EVERY individual image to fit the central 50 percent of its cell, centered, with fully empty TRANSPARENT padding of 25 percent on all four sides inside EACH cell. Each effect MUST be completely isolated, no image touches cell boundaries or another effect. Use a portrait 2:3 canvas so all 24 cells are SQUARE. Row1 steam, row2 chili, row3 tree, row4 soda, row5 lemon, row6 blue flame. Four animation frames each. No grid lines, no text. IMPORTANT: small isolated sprites with large transparent gutters. Keep full objects including roots and liquid splashes; don't crop anything. Truly transparent background.”
- LegacyAtlas 최종 편집: “Fix technical sprite atlas spacing. EXACT 4 columns and 7 equal rows. 28 identical SQUARE cells on a portrait canvas width:height 4:7. Preserve the 28 effect subjects and order. Make every individual effect SMALL, fitting only the central 50 percent of the cell width and height. Fully transparent gutters of at least 25 percent on all four sides of EVERY cell. No image extends into any other cell. Recenter each. Row1 volcano; row2 seaweed; row3 moss stone; row4 hot electric burst; row5 leaf water blade; row6 electric jellyfish; row7 paired magnets. Four animation frames each. Keep all flames, rocks, leaves and tentacles within each sprite's small central area. Genuine transparent alpha background, no grid lines, no text. Preserve high definition textures and organic details.”
- DetailAtlas: “Production Unity game sprite atlas, genuine transparent alpha background. Square canvas with EXACT 4 columns and 4 rows of equal square cells. Sixteen isolated images. Keep every sprite SMALL within middle 55 percent of its cell, with at least 20 percent EMPTY TRANSPARENT margins on all sides. Row1 four animation frames of one glowing orange-red magical fruit with green leaf and refractive droplets, falling and sparkling, ONLY fruit not tree. Row2 four swimming frames of one luminous turquoise magical fish facing RIGHT with watery fins and a liquid tail wake, ONLY fish not kelp. Row3 four frames of one glowing brown seed cracking and sprouting small green roots and leaves. Row4 four rotating flight frames of a single jagged lava rock, dark crust, molten orange cracks, fiery embers. High resolution painterly textured spell assets, organic curves and intricate highlights. No background, no text, no grid lines. Strictly no overlap between cells. All rows same cell height, each sprite centered and entirely contained inside its cell. Do not enlarge sprites to fill space.”
- GiantAtlas 최종 편집: “Edit target: giant monster sprite sheet. Crucial technical correction: exactly 4 equal columns and 5 equal rows. Shrink EVERY individual sprite to only 55% of the width and height of its own cell and CENTER it, with at least 22% totally TRANSPARENT padding on ALL FOUR sides inside EACH cell. No part of any sprite, spike, tentacle, claw, star, or effect may enter another cell. Keep same five characters, same four poses each, same pixel style and colors. Row1 crab; row2 hermit; row3 jellyfish; row4 starfish; row5 urchin. Each row has exactly idle, preparing, attacking, recovering. Particularly shrink jellyfish attacking tentacles and urchin spikes and remove launched projectile stars from the starfish attacking pose (only starfish body remains). Transparent background, no grid drawn, no words. Uniform cell grid is mandatory; characters should look SMALL with large empty spacing.”

</details>


## 2026-10-14 능력치 시스템 Windows 패키지

- 능력치·히든·합성 재료 소모를 반영한 Windows 패키지를 `문어지지마_2안_v0.1.0_Windows.zip`으로 만들었다. 크기 58,542,305바이트, 214개 파일. ZIP CRC 및 실행 필수 파일을 확인했다. Unity 6000.5.7f1 Windows 빌드와 기존 시스템 데이터 사전 검사가 통과했다.
- SHA-256: `4768f1001789e71524754dfa8022844d7e0342f4b699f5cac7b8115fb99872c5`. 게임을 실제 실행하는 검사는 자동 승인 검토에서 거절되어 수행하지 않았다.
- 히든 20%는 슬롯별 설정값이며 결과는 난수에 따른다. 정상 플레이 장기 밸런스, 네트워크 참가자 화면 및 에셋 실제 배치는 실행 테스트 전까지 미확인이다.

## 2026-10-14 A~D 밸런스·Esc 검증

- 사전 검증 기록 커밋: `bb6c008`. 승인 후 Esc 확인창, 나무 연사/조준/확대, 기본 불 확대/추적을 적용했다. Windows 빌드·ZIP은 생성하지 않았다.
- Unity6000.5.7f1 배치 컴파일과 SystemDataChecks 통과(`Logs/balance-checks.log`). 배포 빌드 없이 에디터 Play Mode 로컬 NGO 호스트로 `BALANCE A-D CHECKS PASS` 확인(`Logs/balance-playmode.log`, 그래픽 재검사 `Logs/balance-visual-recheck.log`).
- 재실행: Unity에 `-batchmode -projectPath <프로젝트절대경로> -executeMethod OctopusSurvivors.EditorTools.SystemDataChecks.RunBalancePlayMode -octopus-test-solo -octopus-balance-check -logFile <프로젝트>/Logs/balance-playmode.log`를 전달한다. 프로젝트가 열린 다른 Unity 인스턴스가 없어야 한다. Windows 실행 파일을 빌드하지 않는다.
- 나무 마스터리0~5, 30/60/144FPS 및0.25초 지연 틱에서 나무 수·발사 수·착지 피해·적 위치 조준·나무 자체 무피해·빈 범위 낙하 검증. 기본315/시전, 만렙6336/시전(요소 피해 강화 제외), 기본 재시전13.5초 기준23.33/469.33 DPS. 기존 이상적 만렙6480보다2.22% 감소하나 조준 개선으로 실전 적중률은 증가할 수 있다.
- 불6.45 경계 안/밖 피해, 강화5회 반경8.7, 180도/초 제한, 적 없는 경우 방향 유지, 새 군집 우선 및 단일 대상 유지, 기존25바이트 방향 인코딩 검증. 기본70DPS/1.25초 유지. 사거리1.5배로 면적2.25배이며 실제 피해2.25배를 보장하지 않는다.
- Esc 개발 메뉴/합성창 순서, 같은 프레임 중복 소비, 능력선택·카운트다운 무시, 확인창 표시/취소·세션 초기화, 명시적 호스트 종료 검증. 실제 키보드 수동 조작과 두 PC 접속/참가자 이탈/LAN·Relay는 미검증이다.
- 첫 그래픽 캡처의 준비 중 셰이더 표시는 동기 컴파일·렌더 준비 대기로 해결했다. 최종 화면에서 나무/열매/화염과 판정 경계 및 확인창을 확인했다. [나무·불](host-balance-tree-fire.png), [Esc 확인창](host-balance-exit-confirmation.png).
- 그래픽 에디터 실행에는 UnityEditor.Search.SearchDatabase 시작 색인의 ArgumentOutOfRangeException이 별도로 기록되었다. 게임 검사 실패나 게임 스크립트 예외는 없고 A~D 검사는 완료됐지만, 에디터 전체 로그가 무오류라는 뜻은 아니다. 기존 캐시를 삭제하지 않았다.
- HTML 비교 모델은 `tools/balance_simulator.html`. Node 실행으로 기본/만렙/쿨다운 피해 예산 및 표 출력의 NaN/Infinity 부재 확인. 적중률·대상 수는 명시적인 가정값이며 전체 전투 시간을 예측하지 않는다. 원본 Downloads 파일은 보존했다.
- 수동 확인: F1+F 열린 상태에서 Esc 두 번으로 각 창만 닫기, 다음 Esc 확인창에서 취소/나가기 확인. 나무 각 강화 단계의 연속 낙하와 반경 표시, 이동하는 적 군집을 따라 회전하는 불 확인. 서로 같은 프로토콜4 빌드로 참가자 이탈 시 방장 유지, 방장 이탈 시 참가자 연결 종료를 확인할 것.

## 2026-10-15 플레이 종료 후 빌드 검사 수정

- Unity 에디터에서 Play Mode를 종료한 뒤 Windows 빌드 사전 검사 중 캐시된 배경 `Texture2D`가 이미 해제되어 `MissingReferenceException`이 발생했다. `RuntimeArt`가 해제된 텍스처와 그 스프라이트를 감지해 다시 만들도록 수정했다.
- 수정 후 실제 Unity 빌드는 사용자가 에디터에서 재시도해야 하므로 이 항목에서는 통과를 주장하지 않는다.

## 2026-10-15 청염 전용 VFX와 피해 검증

- Unity 6000.5.7f1의 열린 에디터에서 `문어/청염 개발 검증`을 실행했다. 에디터 컴파일 및 시스템 데이터 검사, Play Mode 실제 NGO 로컬 호스트 검사를 통과하고 편집 모드로 복귀했다. 별도 Windows 빌드/ZIP 없음.
- 30/60/144FPS와 0.25초 지연 틱에서 첫 적 포함 정확히20명 직접 공격, 최초 사거리 밖 전이, 21번째 이후 비피격, 직접 피해100%, 선분 주변50%, 범위 밖 무피해, 교차 경로의 주변 피해 중복 방지, 이후 직접 피격 시 차액만 추가, 대상 부족 시 종료를 확인했다.
- `BlueFlameVisual` 리본 메시/전용 알파·가산 셰이더의 실제 게임 표시를 확인했다. 밝은 모래 배경의 푸른 외곽 대비를 보정한 뒤 재실행에서도 `BLUE FLAME CHECKS PASS`를 확인했다. 네트워크 검사 포트는 기존 플레이와 충돌하지 않는 임시 루프백 포트를 사용한다.
- 결과 발췌: `Source/문어_유한맵/Logs/blueflame-checks.log`. 원본은 같은 프로젝트 `Logs/Editor.log`, 최신 상태는 `Logs/blueflame-check.status`. 캡처 원본 프레임은 `Build/Validation/BlueFlameFrames`의 고정 경로를 재사용한다.
- [실제 Unity 전투 GIF](blueflame-preview.gif), [전이 중](host-balance-blueflame-44.png), [연결 경로](host-balance-blueflame-84.png). 정지된 적과 수동 시뮬레이션을 쓰는 통제 검사이며, 두 PC/Relay 및 정상 장기 플레이 밸런스는 이번에 검증하지 않았다.
- 같은 전투 프레임의 시각 배율 비교: [3배](host-balance-blueflame-size-3.png), [5배](host-balance-blueflame-size-5.png), [10배](host-balance-blueflame-size-10.png). 번개 폭·섬광·불씨 배율만 변경하고 캡처 직후 기본1로 복구했다. 피해 범위는 변경하지 않았다.
