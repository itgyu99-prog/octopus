# 성장·아이템

스테이지 진행, 능력 선택, 납품, 드랍·회복

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- `Source/문어_유한맵/Assets/Scripts/Core/Upgrades.cs`
- `Source/문어_유한맵/Assets/Scripts/Core/UpgradeTuning.cs`
- `Source/문어_유한맵/Assets/Scripts/World/PearlStackVisual.cs`

## 관련 파일

- `Source/문어_유한맵/Assets/Resources/Art/RelicPearl.bytes`
- `Source/문어_유한맵/Assets/Resources/Art/RelicSandGlyph.bytes`
- `Source/문어_유한맵/Assets/Resources/GameplayRules.json`
- `Source/문어_유한맵/Assets/Resources/UpgradeTuning.asset`
- `Source/문어_유한맵/Assets/Scripts/Core/GameplayRules.cs`
- `Source/문어_유한맵/Assets/Scripts/Player/OctopusPlayer.cs`

## `Assets/Scripts/World/GameSession.cs` 진입 함수

- `TickStageTransition` — 450행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CompleteStage` — 470행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `StartArtifactStage` — 1480행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ClearFieldForNextStage` — 1510행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickArtifacts` — 1879행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickPotions` — 1525행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CollectPickup` — 1326행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TryDropAttribute` — 1474행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `DropBossSpecial` — 1025행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `AttributeNeeded` — 1468행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Player/OctopusPlayer.cs` 진입 함수

- `GrantStageChoice` — 360행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `RollOffers` — 620행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ChooseUpgradeServerRpc` — 687행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TryPickup` — 484행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## 범위 검색

```powershell
python tools/project_index.py --category progression
python tools/project_index.py --category progression --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
