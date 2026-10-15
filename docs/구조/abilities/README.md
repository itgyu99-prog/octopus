# 속성·요소

기본 속성, 합성 요소, 특수능력 및 효과

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- `Source/문어_유한맵/Assets/Scripts/Elements/AbilityRunner.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/BlueFlameVisual.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/ElementEffects.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/ElementEffectVisual.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/ElementRules.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/HdSpellVisual.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/PlannedAbilities.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/SpecialAbilities.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/TreeAbility.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/TreeAbilityTuning.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/UpgradeAbilities.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/WaterVfx.cs`

## 관련 파일

- `Source/문어_유한맵/../../tools/balance_simulator.html`
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
- `Source/문어_유한맵/Assets/Resources/TreeAbilityTuning.asset`
- `Source/문어_유한맵/Assets/Resources/UpgradeTuning.asset`
- `Source/문어_유한맵/Assets/Resources/Vfx/BlueFlameGlow.shader`
- `Source/문어_유한맵/Assets/Resources/Vfx/BlueFlameGlow.shader.meta`
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
- `Source/문어_유한맵/Assets/Scripts/Elements/TreeAbilityTuning.cs`
- `Source/문어_유한맵/Assets/Scripts/Player/OctopusPlayer.cs`
- `Source/문어_유한맵/Assets/Scripts/UI/CombinePanel.cs`

## `Assets/Scripts/World/GameSession.cs` 진입 함수

- `SpawnAllySpirit` — 1807행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `SpawnPiercingMeteor` — 1816행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CastBlackFlame` — 1837행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `InfectFlame` — 1872행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Player/OctopusPlayer.cs` 진입 함수

- `GrantElement` — 510행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickAttributeCycle` — 380행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CanCombine` — 521행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ServerCombine` — 538행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/PlannedAbilities.cs` 진입 함수

- `CastBasicAbility` — 9행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CastPlannedCombo` — 60행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickPlannedAbility` — 140행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TrackedFireDirection` — 248행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TurnFireDirection` — 265행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CastChain` — 274행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `StrikeBlueFlameLink` — 314행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `InsideBlueFlameCorridor` — 335행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ShowBlueFlameArc` — 342행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/HdSpellVisual.cs` 진입 함수

- `Configure` — 83행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `LateUpdate` — 130행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/BlueFlameVisual.cs` 진입 함수

- `Configure` — 24행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `RenderAt` — 79행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/TreeAbility.cs` 진입 함수

- `CastTreeAbility` — 7행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickTreeAbility` — 26행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `LandTreeFruit` — 46행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TreeLandingPoint` — 55행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## 범위 검색

```powershell
python tools/project_index.py --category abilities
python tools/project_index.py --category abilities --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
