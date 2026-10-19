# 몬스터

일반 적 생성·이동, 보스 패턴·처치

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- `Source/문어_유한맵/Assets/Scripts/World/GiantMonsterDefinition.cs`
- `Source/문어_유한맵/Assets/Scripts/World/GiantMonsters.cs`
- `Source/문어_유한맵/Assets/Scripts/World/GiantMonsterVisual.cs`

## 관련 파일

- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/01-pattern.gif`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/02-pattern.gif`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/03-pattern.gif`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/04-pattern.gif`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/05-detail.gif`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/body.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/grip-throw-first.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/grip-throw-sheet.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/ground-back.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/ground-first.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/ground-front.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/ground-sheet.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/manifest.json`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/rock-held.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/rock.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/slam-contact.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/slam-first.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/slam-sheet.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/tentacle-burrow.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/tentacle-design-reference.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/tentacle-front.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/tentacle-poses-prompts.json`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/tentacle-rock-lift.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/tentacle-slam.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/assets/tentacle-throw.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/boss-field.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/layout-reference.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/player-reference.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/PROMPT.txt`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/qa-1-consecutive.jpg`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/qa-2-consecutive.jpg`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/qa-3-consecutive.jpg`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/qa-4-consecutive.jpg`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/qa-barrage-directions.jpg`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/qa-detail.png`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/qa-ground-detail.jpg`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/qa-report.json`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/README.md`
- `Source/문어_유한맵/../../docs/검증/giant-octopus-boss/video-reference.jpg`
- `Source/문어_유한맵/../../tools/render_octopus_boss.py`
- `Source/문어_유한맵/Assets/Editor/GiantMonsterSetup.cs`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant1.asset`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant1.asset.meta`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant1.prefab`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant1.prefab.meta`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant2.asset`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant2.asset.meta`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant2.prefab`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant2.prefab.meta`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant3.asset`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant3.asset.meta`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant3.prefab`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant3.prefab.meta`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant4.asset`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant4.asset.meta`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant4.prefab`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant4.prefab.meta`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant5.asset`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant5.asset.meta`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant5.prefab`
- `Source/문어_유한맵/Assets/Resources/Monsters/Giant5.prefab.meta`
- `Source/문어_유한맵/Assets/Resources/Monsters/GiantAtlas.png`
- `Source/문어_유한맵/Assets/Resources/Monsters/GiantAtlas.png.meta`
- `Source/문어_유한맵/Assets/Scripts/Core/GameConfig.cs`
- `Source/문어_유한맵/Assets/Scripts/Core/GameTypes.cs`
- `Source/문어_유한맵/Assets/Scripts/Core/RuntimeArt.cs`
- `Source/문어_유한맵/Assets/Scripts/World/WorldView.cs`

## `Assets/Scripts/World/GameSession.cs` 진입 함수

- `TickEnemy` — 827행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `EnemyContact` — 904행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `SpawnWave` — 984행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `SpawnBoss` — 1001행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ClearLiveBosses` — 374행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `OnBossDefeated` — 1011행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `KillEnemy` — 1453행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickBossPattern` — 1554행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ChaseBoss` — 1570행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickChargeBoss` — 1580행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickBulletBoss` — 1644행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickAreaBoss` — 1665행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickBossBullet` — 1732행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## 범위 검색

```powershell
python tools/project_index.py --category monsters
python tools/project_index.py --category monsters --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
