# 몬스터

일반 적 생성·이동, 보스 패턴·처치

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- 현재 독립 파일 없음. 아래 공통 파일의 진입 함수를 사용한다.

## 관련 파일

- `Source/문어_유한맵/Assets/Scripts/Core/GameConfig.cs`
- `Source/문어_유한맵/Assets/Scripts/Core/GameTypes.cs`
- `Source/문어_유한맵/Assets/Scripts/Core/RuntimeArt.cs`
- `Source/문어_유한맵/Assets/Scripts/World/WorldView.cs`

## `Assets/Scripts/World/GameSession.cs` 진입 함수

- `TickEnemy` — 815행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `EnemyContact` — 890행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `SpawnWave` — 969행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `SpawnBoss` — 985행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ClearLiveBosses` — 369행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `OnBossDefeated` — 995행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `KillEnemy` — 1426행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickBossPattern` — 1523행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ChaseBoss` — 1539행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickChargeBoss` — 1549행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickBulletBoss` — 1613행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickAreaBoss` — 1634행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickBossBullet` — 1701행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## 범위 검색

```powershell
python tools/project_index.py --category monsters
python tools/project_index.py --category monsters --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
