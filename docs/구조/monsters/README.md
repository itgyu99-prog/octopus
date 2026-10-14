# 몬스터

일반 적 생성·이동, 보스 패턴·처치

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- `Source/문어_유한맵/Assets/Scripts/World/GiantMonsterDefinition.cs`
- `Source/문어_유한맵/Assets/Scripts/World/GiantMonsters.cs`
- `Source/문어_유한맵/Assets/Scripts/World/GiantMonsterVisual.cs`

## 관련 파일

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

- `TickEnemy` — 825행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `EnemyContact` — 902행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `SpawnWave` — 982행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `SpawnBoss` — 999행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ClearLiveBosses` — 373행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `OnBossDefeated` — 1009행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `KillEnemy` — 1448행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickBossPattern` — 1549행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ChaseBoss` — 1565행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickChargeBoss` — 1575행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickBulletBoss` — 1639행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickAreaBoss` — 1660행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickBossBullet` — 1727행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## 범위 검색

```powershell
python tools/project_index.py --category monsters
python tools/project_index.py --category monsters --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
