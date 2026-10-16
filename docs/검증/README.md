# 최신 시스템 검증 요약

## 2026-10-15 나무 도트 시안 A/B/C 플레이 영상

- Unity 6000.5.7f1 실제 호스트 Play Mode에서 플레이어 이동·적 추적·나무 자동 발동·열매 착지 피해를 고정 30Hz로 진행했다. 같은 난수·이동·적 배치를 재생하는 비교용 전투이며 사용자 수동 조작 녹화는 아니다. 색상별 총 피해 567/567/567 일치, 렌더링 예외 없음.
- 각 영상은 H.264 MP4, 1280×720, 30fps, 180프레임/6초다. `Source/문어_유한맵/Build/Validation/TreeSkill/tree-A-gameplay.mp4`, `tree-B-gameplay.mp4`, `tree-C-gameplay.mp4`; 세 영상을 순서대로 잇는 `tree-ABC-comparison.mp4`는 18초다. 같은 폴더의 프레임 50/88 PNG로 나무·열매·캐릭터·HUD 구도를 확인했다. 영상과 중간 캡처는 소스 커밋에서 제외한다.
- 검사: 강화 0~5 × 30/60/144Hz/0.25초 지연 틱. 나무 수 1~6, 그루당 기본 70발/최종 88발, 수명 종료 뒤 추가 발사 없음, 모든 열매 1회 착지 피해, 착지 전 피해 없음, 반경 밖 적 제외, 기존 25바이트 스냅샷 유지. A/B/C 잎 흔들림과 실제 적 피해 검사 통과. 두 PC 동시 접속 검사는 이번 범위에서 실행하지 않았다.
- 재생성: Unity 메뉴 `문어/나무 A B C 플레이 영상 및 검증`, 또는 배치 실행의 `-executeMethod OctopusSurvivors.EditorTools.TreeSkillChecks.Run`(다른 에디터가 프로젝트를 점유하지 않을 때). 로그 `Source/문어_유한맵/Logs/tree-skill-checks.log`, 상태 `tree-skill-check.status=PASS`. 실패한 초기 촬영의 진단은 같은 Logs의 `tree-skill-checks.failed.log`에 보존했다.
- 아트: 내장 imagegen으로 `Assets/Resources/Vfx/TreeVariants.png` 생성. 프롬프트 핵심: "three equal-width columns, identical rounded three-lobed canopy and slender trunk, A fresh green/B warm olive/C deep green, clean full trees only, crisp chunky pixel art, transparent background, remove fruit/motion studies/glow/text". 런타임에서 열매와 잔상·잎 흔들림을 따로 표시한다. Inspector `TreeAbilityTuning.palette`로 세 색상을 선택한다. 기본 A는 선택 전 임시값이며 촬영 후 원래 설정을 복원한다.

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
- 적중점의 둥근 전기/청염 농도 [4종 확대 비교](blueflame-hit-comparison.png): A=1(현재), B=1.5, C=2, D=3. 같은 프레임·크기·카메라로 실제 Unity 렌더 후 동일 영역을 잘라 배치했다. blueFlameImpactIntensity는 촬영 후1로 복구했고 시각 배율도1 유지. Play Mode 검사는 PASS.
- 최종 선택: 연결 번개5배/B농도1.5 적용. 몸 전기 효과는 몬스터 지름의1.15배, 원형 적중 링은1.55배까지 퍼지도록 연결 배율과 분리했다. 크기0.4/1.2 적의 몸 지름 비율3배 및 실제 적중 스프라이트 bounds 일치를 Unity Play Mode에서 확인했다. 최신 GIF와 전투 스틸은 이 선택값으로 갱신했다. 앞선 3/5/10 및 A~D 이미지는 이전 디자인의 비교 기록이다.

- 2026-10-15 청염 갈래 성장: 기본3갈래, 강화1~5회에4/5/6/7/8갈래. Unity Play Mode에서 마스터리0~5 발동기 수와 시작 대상 분산12항목, 기존20연결/경로50%/몸 크기/렌더 검사를 모두 통과했다. 실제3갈래 동시 진행으로 GIF와 전투 스틸 갱신. 갈래별 판정은 독립적이므로 서로 다른 갈래가 같은 적에게 적중하면 피해가 각각 적용된다.


## 2026-10-15 도트 불덩이 왕복·회수

- 2026-10-15: 기본 불을 도트 불덩이 3발 왕복(B)·복귀 공격으로 변경. 사용자 그림 기반 4프레임 FireOrbPixel을 쓰며 선택2안의 긴 꼬리/약한 열기 표현을 추가했다. 방향은 시전 시 고정(±32도), 적 추적 없음. 4.8초 중 발사0.68초/복귀0.9초, 발사 간격0.16초, 왕복 폭1.4, 초당24 피해/발+연소4. 본체·그림 자체의 불꽃·최근0.48초 꼬리 영역 합집합으로 범위 내 모든 일반 적/보스를 지속 타격하고 복귀 중에도 판정 유지. 1/60초 이하 하위 틱으로 고속 이동을 스윕하며 같은 발의 겹친 꼬리는 피해 중복 없음. 기본 사거리6.45/최종8.7 유지. 기본 속성 쿨타임은 지속시간 뒤부터 계산해 다음 시전이 복귀를 끊지 않는다. 실제 Play Mode 30/60/144FPS와0.25초 지연 검사 및 이동한 소유자에게 복귀 검사 PASS. 네트워크25바이트 포맷 유지, 실제 두 PC 참가자 검증은 미실행.

- 실제 게임 촬영: [불 3발 왕복 및 복귀 GIF](fire-orb-gameplay.gif). `문어 → 불 왕복 검증`으로 재검사하며 상세 로그는 `Source/문어_유한맵/Logs/fire-orb-checks.log`, 결과 상태는 `fire-orb-check.status`. 본체 밖의 꼬리 단독 적중/일반 적 복수·보스/구간 중복 피해 방지/발사 전 무피해/복귀 피해/프레임별 안정성을 확인했다. Windows 빌드·배포는 만들지 않았다.


### 불 자동 조준·대상 교대 갱신

- 2026-10-15: 불 자동 조준·대상 교대 적용. 첫3발은 기존 ±32도/0.16초 간격/투척0.68초를 유지하고, 각 발이 투척을 마치면 소유자 사거리×1.5 이내의 일반 적/보스를 초당14 속도로 추적한다. 같은 시전의 다른 불이 잡은 적·이미 방문한 적·직전 대상을 구분해 다른 적을 우선하며, 닿은 뒤0.18초 또는 이동시간을 고려한0.65초 이상 기한에 대상을 교대한다. 사망/범위 이탈은 재탐색, 빈 필드는 왕복 대기, 한 마리만 있으면 집중 공격 허용. 3.9초에 조준을 끝내고 실제 현재 위치에서 소유자에게0.9초 복귀한다. 본체/꼬리/복귀 피해와 시전 간격은 유지. Unity에서 분산 조준, 이동 적 추적, 각 발의 대상 교대, 2/3마리 교대, 사망 대상 교체, 빈 필드/1마리 예외와 기존 피해·프레임 검사를 모두 통과했다.

- 최신 실제 GIF는 위 `fire-orb-gameplay.gif`에 덮어썼다. 상세 검사 45개 로그 항목과 PASS를 `Logs/fire-orb-checks.log`에서 확인했다. 기존 25바이트 스냅샷 유지, 두 PC 실제 접속 검사는 이번 작업에서 수행하지 않았다.


### 불 최초 발사 선조준 갱신

- 2026-10-15: 불 최초 발사를 시선 기준 부채꼴에서 적 선조준으로 변경했다. 시전 시 가까운 적을 세 발에 분산 예약하고 각 발은 실제 출발 시 적의 현재 방향·거리로 조준한다. 기존0.16초 간격은 유지하며 가까운 적은 최대 투척 거리까지 지나치지 않는다. 대기 중 대상이 죽거나 사거리 밖으로 나가면 출발 시 재선택하고, 주변 적이 없을 때만 기존 방향을 사용한다. 이후 자동 추적·교대와 본체/꼬리/복귀 피해는 유지. 반대 시선을 향한 발사, 세 대상 분산, 근거리 과투척 방지, 대기 중 사망 재조준과 기존 자동 조준/피해 검사를 Unity Play Mode에서 통과했다.

- 최신 실제 GIF와 `Logs/fire-orb-checks.log`를 갱신했다. 54개 PASS 로그 항목. Windows 빌드·배포는 이번에 만들지 않았다.

## 2026-10-15 탄산 유리병 · 잔류 탄산 거품 5안

- `문어/탄산 거품 실제 플레이 5안 및 검증`을 실제 Unity 6000.5.7f1 Play Mode의 NGO 로컬 호스트에서 실행했다. 컴파일 및 30/60/144FPS·0.25초 지연 틱의 지속 피해/범위 밖 무피해/속도 제한/만료 틱 검사를 통과했다. 기본5병의 무작위 맵 생성, 1000 이동 틱에서 속도 증폭 방지, 초당270도 연속 회전, 25바이트 스냅샷 유지도 확인했다.
- 물 리본의 UV 늘어짐·접힌 삼각형을 개별 원형 거품 패치로 교체했다. 병의 입구 변환을 분사 원점으로 사용하고 곡선 연결부를 거쳐 팽창하는 거품길에 합류한다. 촬영 중 입구/분사 원점 오차0.015 이하, 누적 궤적 샘플 수가 만료 전 감소하지 않음, 종료 후 병과 거품 메시가 모두 제거됨을 검사했다. 반투명 기포는 궤적 전체에서 계속 흐르고 성장·파열한다.
- 5개 GIF는 실제 게임의 배경·문어·몬스터·체력바·UI를 Unity 카메라로 촬영했다. 각 영상은 입구 근접 화면에서 시작해 전투 시야로 넓어지며4초 공격과 종료 후 소멸을 담는다. 비교를 위해 적 위치를 고정하고 기본5병 중1병만 촬영했으며 시뮬레이션을1/30초씩 직접 진행한 통제 검증이다. 일반 플레이는 기존5병을 유지한다. 두 PC/Relay·중도 참가·장기 밸런스는 이번 검사에 포함하지 않았다.
- [1안 미세 탄산](soda-foam-1-gameplay.gif), [2안 큰 기포](soda-foam-2-gameplay.gif), [3안 풍성한 흰 거품](soda-foam-3-gameplay.gif), [4안 두 갈래 거품](soda-foam-4-gameplay.gif), [5안 가벼운 발포](soda-foam-5-gameplay.gif). 960×540, 78프레임, 5.2초 반복. 동일한 시드와 피해 판정을 사용하고 표시만 바꾼 비교이며 게임 기본 표시는1안이다.
- 최신 상태 `Source/문어_유한맵/Logs/soda-check.status`=PASS. 발췌 로그 `Logs/soda-checks.log`, 원본 Unity 로그는 로컬 에디터의 `Editor.log`. 캡처 프레임은 `Build/Validation/SodaFrames/foam-1`~`foam-5` 고정 경로를 덮어쓴다. `Logs/soda-check.request` 파일로 에디터의 최신 소스 가져오기·컴파일 후 동일 검사를 요청할 수 있다. Windows 배포/ZIP은 생성하지 않았다.

## 2026-10-16 풀 가시 픽셀 공격 적용

- 사용자 최종 승인 8프레임을 게임의 기본 풀 공격(Vine)에 연결했다. 무광 초록색과 초록 가시, 중앙을 향한 뾰족한 끝, 밝은 모래색 흙을 유지한다. 큰 발광/기존 고해상도 덩굴/잔상은 기본 풀 공격에서 제거했다. 빠른 표시로 풀 공격을 구별하기 어렵다는 피드백을 반영해 표시 수명만 0.18→0.72초로 늘렸다. 전조0.16초·성장0.10초·완전히 솟은 모습0.32초·회수0.14초로, 식물 형태를 충분히 보여준 뒤 사라진다. 공격 발동 주기와 피해 발생 시점은 기존대로다.
- Unity 6000.5.7f1 배치 컴파일 및 실제 NGO 로컬 호스트 Play Mode 검사에서 `GRASS THORN CHECKS PASS` 확인. 8프레임 로딩/Point 필터/고정 지면 피벗, 전조·찌르기·정지·회수, 기본 최근접3명/타격당19피해/4회 타격, 4번째 대상·사거리 밖 무피해, 기존12개 이펙트/0.64초 공격 지속시간 유지, 0.72초 시각 수명, 0.18초 이후에도 표시 유지, 서버 표시 경과시간 동기화, 표시 연장의 추가 피해 없음, 실제 WorldView 연결, 잔상 부재와 만료 후 렌더러 제거를 확인했다.
- [실제 게임 적용 화면](grass-thorn-gameplay.png). 캡처 원본은 `Source/문어_유한맵/Build/Validation/GrassThornFrames/frame-00.png`~`frame-07.png` 고정 경로. 검사 상태 `Logs/grass-thorn-check.status`=PASS, 상세 `Logs/grass-thorn-checks.log`, Unity 로그 `Logs/grass-thorn-unity.log`.
- 재실행: `문어/풀 가시 픽셀 검증` 또는 열린 편집 모드 에디터에 `Logs/grass-thorn-check.request` 파일로 요청. 프로젝트가 닫혀 있을 때는 Unity에 `-batchmode -acceptSoftwareTermsForThisRunOnly -projectPath <프로젝트> -executeMethod OctopusSurvivors.EditorTools.GrassThornChecks.Run -logFile <프로젝트>/Logs/grass-thorn-unity.log` 전달. 통제된 1회 공격 표시·피해 검사이며 두 PC/Relay와 장기 밸런스는 미검증. 기존25바이트 스냅샷 유지. Windows 빌드/ZIP은 생성하지 않았다.

### 물풍선 기본 공격 적용 (2026-10-16)

- Unity WaterBalloonChecks PASS: 5개 준비, 0.22초 순차 투척, 밀집 적 조준, 반경1.9 단발 AOE 및 감속, 프레임률별 동일 피해, 다운/무타겟/만료, 기존 스냅샷을 통한 표시 전환.
- 실제 게임 촬영: [물풍선 기본 공격 GIF](water-balloon-gameplay.gif), [4배 확대·0.67배속 GIF](water-balloon-surface-detail.gif). 기본 카메라 크기11에서 캡처. 둥근 물덩이의 두께를 유지한 곡선 발사체와 가늘게 휘는 후면, 물 표면의 이동 파동/내부 흐름/분리 액적, 얇은 파열막으로 재작업 후 WaterBalloonChecks 재검사 PASS. 이전 water-balloon-assets-preview.gif는 게임 외 연출 시안이다.
- 기본 5개, 물 연속 강화는 +1개. 물 분열/히든 풍선 유지. 별도 Windows 배포 빌드/ZIP 생성 없음.
