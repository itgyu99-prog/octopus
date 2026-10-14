# 전투

피해 판정, 상태이상, 근접·투사체 공격

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- `Source/문어_유한맵/Assets/Scripts/Combat/StatusEffects.cs`

## 관련 파일

- `Source/문어_유한맵/Assets/Scripts/Core/GameTypes.cs`
- `Source/문어_유한맵/Assets/Scripts/Player/OctopusPlayer.cs`

## `Assets/Scripts/World/GameSession.cs` 진입 함수

- `ApplyDamage` — 1391행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `DamageCircle` — 1121행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `DamageCone` — 1147행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `DamageLine` — 1169행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `HitEnemy` — 1197행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TryMeleeArc` — 1214행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `StrikeWhip` — 1244행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `NearestEnemy` — 1282행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## 범위 검색

```powershell
python tools/project_index.py --category combat
python tools/project_index.py --category combat --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
