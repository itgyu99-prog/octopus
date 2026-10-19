# 속성·요소

기본 속성, 합성 요소, 특수능력 및 효과

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- `Source/문어_유한맵/Assets/Scripts/Elements/AbilityRunner.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/BlueFlameVisual.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/ElectricSparkBurst.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/ElementEffects.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/ElementEffectVisual.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/ElementRules.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/FireOrbAbility.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/FireOrbVisual.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/GrassThornVisual.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/HdSpellVisual.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/PlannedAbilities.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/SodaAbility.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/SodaVisual.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/SpecialAbilities.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/TreeAbility.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/TreeAbilityTuning.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/TreeRainAbility.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/TreeRainGeometry.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/TreeSkillVisual.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/UpgradeAbilities.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/WaterBalloonAbility.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/WaterBalloonVisual.cs`
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
- `Source/문어_유한맵/Assets/Resources/Vfx/FireOrbPixel.bytes`
- `Source/문어_유한맵/Assets/Resources/Vfx/FireOrbPixel.bytes.meta`
- `Source/문어_유한맵/Assets/Resources/Vfx/GrassThornPixel.bytes`
- `Source/문어_유한맵/Assets/Resources/Vfx/GrassThornPixel.bytes.meta`
- `Source/문어_유한맵/Assets/Resources/Vfx/LegacyAtlas.png`
- `Source/문어_유한맵/Assets/Resources/Vfx/LegacyAtlas.png.meta`
- `Source/문어_유한맵/Assets/Resources/Vfx/SodaFlow.shader`
- `Source/문어_유한맵/Assets/Resources/Vfx/SodaFlow.shader.meta`
- `Source/문어_유한맵/Assets/Resources/Vfx/SodaGlass.bytes`
- `Source/문어_유한맵/Assets/Resources/Vfx/SodaGlass.bytes.meta`
- `Source/문어_유한맵/Assets/Resources/Vfx/TreeVariants.png`
- `Source/문어_유한맵/Assets/Resources/Vfx/TreeVariants.png.meta`
- `Source/문어_유한맵/Assets/Resources/Vfx/WaterAtlas.png`
- `Source/문어_유한맵/Assets/Resources/Vfx/WaterAtlas.png.meta`
- `Source/문어_유한맵/Assets/Resources/Vfx/WaterBalloonBurst.bytes`
- `Source/문어_유한맵/Assets/Resources/Vfx/WaterBalloonBurst.bytes.meta`
- `Source/문어_유한맵/Assets/Resources/Vfx/WaterBalloonFlow.shader`
- `Source/문어_유한맵/Assets/Resources/Vfx/WaterBalloonFlow.shader.meta`
- `Source/문어_유한맵/Assets/Resources/Vfx/WaterBalloonPixel.bytes`
- `Source/문어_유한맵/Assets/Resources/Vfx/WaterBalloonPixel.bytes.meta`
- `Source/문어_유한맵/Assets/Scripts/Core/GameplayRules.cs`
- `Source/문어_유한맵/Assets/Scripts/Core/UpgradeTuning.cs`
- `Source/문어_유한맵/Assets/Scripts/Elements/TreeAbilityTuning.cs`
- `Source/문어_유한맵/Assets/Scripts/Player/OctopusPlayer.cs`
- `Source/문어_유한맵/Assets/Scripts/UI/CombinePanel.cs`

## `Assets/Scripts/World/GameSession.cs` 진입 함수

- `SpawnTransient` — 1073행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `SpawnAllySpirit` — 1815행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `SpawnPiercingMeteor` — 1824행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CastBlackFlame` — 1845행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `InfectFlame` — 1880행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Player/OctopusPlayer.cs` 진입 함수

- `GrantElement` — 463행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickAttributeCycle` — 349행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CanCombine` — 474행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ServerCombine` — 491행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/PlannedAbilities.cs` 진입 함수

- `CastBasicAbility` — 9행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CastPlannedCombo` — 56행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickPlannedAbility` — 136행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TrackedFireDirection` — 227행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TurnFireDirection` — 244행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CastChain` — 253행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `StrikeBlueFlameLink` — 293행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `InsideBlueFlameCorridor` — 314행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ShowBlueFlameArc` — 321행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/GrassThornVisual.cs` 진입 함수

- `Configure` — 44행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `FrameIndex` — 17행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `RenderAt` — 62행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/HdSpellVisual.cs` 진입 함수

- `Configure` — 113행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `LateUpdate` — 172행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/ElectricSparkBurst.cs` 진입 함수

- `Spawn` — 48행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `Begin` — 109행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `Draw` — 166행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `DrawBolts` — 218행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/BlueFlameVisual.cs` 진입 함수

- `Configure` — 26행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `RenderAt` — 86행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/TreeSkillVisual.cs` 진입 함수

- `Configure` — 44행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ReleasePoint` — 111행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `RenderAt` — 153행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/TreeRainGeometry.cs` 진입 함수

- `LandingPoint` — 14행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `ReleasePoint` — 22행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/TreeRainAbility.cs` 진입 함수

- `CastTreeRain` — 7행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickTreeRain` — 21행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `LandTreeRain` — 34행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TreeRainLandingPoint` — 41행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/FireOrbAbility.cs` 진입 함수

- `CastFireOrbs` — 50행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickFireOrb` — 86행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `DamageFireTrail` — 185행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `MoveFireOrbToTarget` — 127행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `FindFireLaunchTarget` — 67행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/FireOrbVisual.cs` 진입 함수

- `Configure` — 25행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `LateUpdate` — 70행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/WaterBalloonAbility.cs` 진입 함수

- `CastWaterBalloons` — 27행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `WaterBalloonTarget` — 39행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `TickWaterBalloon` — 74행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/WaterBalloonVisual.cs` 진입 함수

- `Configure` — 73행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `LateUpdate` — 90행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/SodaAbility.cs` 진입 함수

- `TickSoda` — 22행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `DamageSodaTrail` — 55행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## `Assets/Scripts/Elements/SodaVisual.cs` 진입 함수

- `Configure` — 27행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `Quad` — 98행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `AnimateBubbles` — 133행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## 범위 검색

```powershell
python tools/project_index.py --category abilities
python tools/project_index.py --category abilities --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
