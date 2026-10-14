# 속성·요소

기본 속성, 합성 요소, 특수능력 및 효과

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- `Source/문어_유한맵/Assets/Scripts/Elements/AbilityRunner.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/ElementEffects.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/ElementEffectVisual.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/ElementRules.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/HdSpellVisual.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/PlannedAbilities.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/SpecialAbilities.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/UpgradeAbilities.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/WaterVfx.cs`

## 관련 파일

- `Source/문어_유한맵/Assets/Resources/Art/Element_Electric.bytes`
- `Source/문어_유한맵/Assets/Resources/Art/Element_Fire.bytes`
- `Source/문어_유한맵/Assets/Resources/Art/Element_Grass.bytes`
- `Source/문어_유한맵/Assets/Resources/Art/Element_Stone.bytes`
- `Source/문어_유한맵/Assets/Resources/Art/Element_Water.bytes`
- `Source/문어_유한맵/Assets/Resources/Art/Special_DarkMatter.bytes`
- `Source/문어_유한맵/Assets/Resources/Art/Special_Hellfire.bytes`
- `Source/문어_유한맵/Assets/Resources/Art/Special_Meteorite.bytes`
- `Source/문어_유한맵/Assets/Resources/Art/Special_Spirit.bytes`
- `Source/문어_유한맵/Assets/Resources/GameplayRules.json`
- `Source/문어_유한맵/Assets/Resources/UpgradeTuning.asset`
- `Source/문어_유한맵/Assets/Resources/Vfx/ComboAtlas.png`
- `Source/문어_유한맵/Assets/Resources/Vfx/ComboAtlas.png.meta`
- `Source/문어_유한맵/Assets/Resources/Vfx/DetailAtlas.png`
- `Source/문어_유한맵/Assets/Resources/Vfx/DetailAtlas.png.meta`
- `Source/문어_유한맵/Assets/Resources/Vfx/ElementAtlas.png`
- `Source/문어_유한맵/Assets/Resources/Vfx/ElementAtlas.png.meta`
- `Source/문어_유한맵/Assets/Resources/Vfx/LegacyAtlas.png`
- `Source/문어_유한맵/Assets/Resources/Vfx/LegacyAtlas.png.meta`
- `Source/문어_유한맵/Assets/Resources/Vfx/WaterAtlas.png`
- `Source/문어_유한맵/Assets/Resources/Vfx/WaterAtlas.png.meta`
- `Source/문어_유한맵/Assets/Scripts/Core/GameplayRules.cs`
- `Source/문어_유한맵/Assets/Scripts/Core/UpgradeTuning.cs`
- `Source/문어_유한맵/Assets/Scripts/Player/OctopusPlayer.cs`
- `Source/문어_유한맵/Assets/Scripts/UI/CombinePanel.cs`

## `Assets/Scripts/World/GameSession.cs` 진입 함수

- `SpawnAllySpirit` — 1804행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `SpawnPiercingMeteor` — 1813행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CastBlackFlame` — 1834행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `InfectFlame` — 1869행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Player/OctopusPlayer.cs` 진입 함수

- `GrantElement` — 501행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickAttributeCycle` — 371행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CanCombine` — 512행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ServerCombine` — 529행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/PlannedAbilities.cs` 진입 함수

- `CastBasicAbility` — 9행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CastPlannedCombo` — 57행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickPlannedAbility` — 137행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## 범위 검색

```powershell
python tools/project_index.py --category abilities
python tools/project_index.py --category abilities --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
