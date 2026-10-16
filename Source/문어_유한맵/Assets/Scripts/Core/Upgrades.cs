using UnityEngine;

namespace OctopusSurvivors
{
    public static class Upgrades
    {
        public static readonly UpgradeId[] All =
        {
            UpgradeId.MaxHp,
            UpgradeId.AttributeRank,
            UpgradeId.ElementRank,
            UpgradeId.AttributeCooldown,
            UpgradeId.ElementCooldown,
            UpgradeId.GrassTargets, UpgradeId.WaterRepeats, UpgradeId.ElectricRepeats, UpgradeId.FireRange,
            UpgradeId.LingeringThorns, UpgradeId.ThornBurst, UpgradeId.WaterSplit, UpgradeId.WaterBalloon,
            UpgradeId.FireTornado, UpgradeId.Fireworks, UpgradeId.SkyLightning, UpgradeId.Paralysis,
            UpgradeId.SodaMastery, UpgradeId.ChiliMastery, UpgradeId.TreeMastery,
            UpgradeId.LemonMastery, UpgradeId.FogMastery, UpgradeId.BlueFlameMastery
        };

        public static bool IsHidden(UpgradeId id) => (int)id >= 14 && (int)id <= 21;
        public static string Grade(UpgradeId id) => IsHidden(id) ? "히든" : RequiredElement(id) != ElementId.None ? "레어" : RequiredCombo(id) != ComboId.None ? "요소" : "일반";
        public static UpgradeId ForElement(ElementId id) => id == ElementId.Grass ? UpgradeId.GrassTargets :
            id == ElementId.Water ? UpgradeId.WaterRepeats : id == ElementId.Electric ? UpgradeId.ElectricRepeats :
            id == ElementId.Fire ? UpgradeId.FireRange : UpgradeId.None;
        public static UpgradeId ForCombo(ComboId id) => id == ComboId.Soda ? UpgradeId.SodaMastery :
            id == ComboId.Chili ? UpgradeId.ChiliMastery : id == ComboId.Tree ? UpgradeId.TreeMastery :
            id == ComboId.Lemon ? UpgradeId.LemonMastery : id == ComboId.Fog ? UpgradeId.FogMastery :
            id == ComboId.BlueFlame ? UpgradeId.BlueFlameMastery : UpgradeId.None;
        public static ElementId RequiredElement(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.GrassTargets: case UpgradeId.LingeringThorns: case UpgradeId.ThornBurst: return ElementId.Grass;
                case UpgradeId.WaterRepeats: case UpgradeId.WaterSplit: case UpgradeId.WaterBalloon: return ElementId.Water;
                case UpgradeId.FireRange: case UpgradeId.FireTornado: case UpgradeId.Fireworks: return ElementId.Fire;
                case UpgradeId.ElectricRepeats: case UpgradeId.SkyLightning: case UpgradeId.Paralysis: return ElementId.Electric;
                default: return ElementId.None;
            }
        }
        public static ComboId RequiredCombo(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.SodaMastery: return ComboId.Soda;
                case UpgradeId.ChiliMastery: return ComboId.Chili;
                case UpgradeId.TreeMastery: return ComboId.Tree;
                case UpgradeId.LemonMastery: return ComboId.Lemon;
                case UpgradeId.FogMastery: return ComboId.Fog;
                case UpgradeId.BlueFlameMastery: return ComboId.BlueFlame;
                default: return ComboId.None;
            }
        }

        public static string Name(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.ExtraDash: return "대시 1회 추가";
                case UpgradeId.Attack: return "평타 공격력";
                case UpgradeId.Range: return "평타 사거리";
                case UpgradeId.MoveSpeed: return "이동속도";
                case UpgradeId.MaxHp: return "최대 체력";
                case UpgradeId.AttributeRank: return "속성 데미지 증가";
                case UpgradeId.ElementRank: return "요소 데미지 증가";
                case UpgradeId.AttributeCooldown: return "속성 쿨타임 감소";
                case UpgradeId.ElementCooldown: return "요소/특수 쿨타임 감소";
                case UpgradeId.GrassTargets: return "풀 · 타겟 수 증가";
                case UpgradeId.WaterRepeats: return "물 · 물풍선 추가";
                case UpgradeId.ElectricRepeats: return "전기 · 방출 횟수 증가";
                case UpgradeId.FireRange: return "불 · 공격 범위 증가";
                case UpgradeId.LingeringThorns: return "가시덩쿨";
                case UpgradeId.ThornBurst: return "가시 발사";
                case UpgradeId.WaterSplit: return "물방울 분열";
                case UpgradeId.WaterBalloon: return "물풍선";
                case UpgradeId.FireTornado: return "토네이도";
                case UpgradeId.Fireworks: return "불꽃놀이";
                case UpgradeId.SkyLightning: return "벼락";
                case UpgradeId.Paralysis: return "마비";
                case UpgradeId.SodaMastery: return "탄산수 강화";
                case UpgradeId.ChiliMastery: return "고추 강화";
                case UpgradeId.TreeMastery: return "나무 강화";
                case UpgradeId.LemonMastery: return "레몬 강화";
                case UpgradeId.FogMastery: return "안개 강화";
                case UpgradeId.BlueFlameMastery: return "청염 강화";
                default: return "-";
            }
        }

        public static Color Tint(UpgradeId id)
        {
            if (IsHidden(id)) return new Color(1f, .72f, .2f);
            if (RequiredElement(id) != ElementId.None) return new Color(.4f, .8f, 1f);
            if (RequiredCombo(id) != ComboId.None) return new Color(.8f, .6f, 1f);
            switch (id)
            {
                case UpgradeId.Attack: return new Color(0.95f, 0.35f, 0.3f);
                case UpgradeId.Range: return new Color(0.98f, 0.6f, 0.3f);
                case UpgradeId.MoveSpeed: return new Color(0.4f, 0.85f, 0.5f);
                case UpgradeId.MaxHp: return new Color(0.95f, 0.45f, 0.7f);
                case UpgradeId.AttributeRank: return new Color(0.6f, 0.85f, 1f);
                case UpgradeId.ElementRank: return new Color(0.8f, 0.6f, 1f);
                case UpgradeId.AttributeCooldown: return new Color(0.45f, 0.7f, 1f);
                case UpgradeId.ElementCooldown: return new Color(0.7f, 0.55f, 1f);
                default: return Color.white;
            }
        }

        public static int MaxPicks(UpgradeId id)
        {
            return IsHidden(id) || id == UpgradeId.ExtraDash ? 1 : 5;
        }

        public static string Delta(UpgradeId id, int picks)
        {
            switch (id)
            {
                case UpgradeId.ExtraDash: return "최대 충전 1회 → 2회\n희귀 · 한 번만 획득";
                case UpgradeId.Attack:
                    return $"{GameConfig.MeleeDamage(picks):0} → {GameConfig.MeleeDamage(picks + 1):0}";
                case UpgradeId.Range:
                    return $"{GameConfig.MeleeRange(picks):0.0} → {GameConfig.MeleeRange(picks + 1):0.0}";
                case UpgradeId.MoveSpeed:
                    return $"{GameConfig.MoveSpeed(picks):0.0} → {GameConfig.MoveSpeed(picks + 1):0.0}";
                case UpgradeId.MaxHp:
                    return $"{GameConfig.MaxHp(picks):0} → {GameConfig.MaxHp(picks + 1):0}";
                case UpgradeId.AttributeRank:
                    return $"속성·히든 피해 +{UpgradeTuning.Data.attributeDamagePerLevel * 100:0}% · 요소 제외";
                case UpgradeId.ElementRank:
                    return $"요소·마스터 피해 +{UpgradeTuning.Data.elementDamagePerLevel * 100:0}% · 속성 제외";
                case UpgradeId.AttributeCooldown:
                    return $"{GameConfig.AttributeCycle * GameConfig.AttributeCdMul(picks):0.0}초 → {GameConfig.AttributeCycle * GameConfig.AttributeCdMul(picks + 1):0.0}초 · 각 속성";
                case UpgradeId.ElementCooldown:
                    return $"{(1f - GameConfig.ElementCdMul(picks)) * 100f:0}% → {(1f - GameConfig.ElementCdMul(picks + 1)) * 100f:0}% 감소";
                case UpgradeId.GrassTargets: return "타겟 +1 · 같은 공격에 동일 대상 중복 없음";
                case UpgradeId.WaterRepeats: return "물풍선 +1개 · 0.22초 순차 투척 · 기본 주기 유지";
                case UpgradeId.ElectricRepeats: return "전기 방출 +1회 · 기본 주기 유지";
                case UpgradeId.FireRange: return "불덩이 3발 사거리 증가 · 자동 조준/대상 교대 · 꼬리와 복귀 중에도 공격";
                case UpgradeId.LingeringThorns: return "가시가 필드에 남아 주기적으로 피해";
                case UpgradeId.ThornBurst: return "풀 공격 처치 위치에서 가시 발사 · 재연쇄 없음";
                case UpgradeId.WaterSplit: return "물방울이 터질 때 작은 물방울 발사 · 재분열 없음";
                case UpgradeId.WaterBalloon: return "기본 공격과 함께 대형 물풍선 1개 추가";
                case UpgradeId.FireTornado: return "회전 화염 지속 공격 후 전방으로 발사 · 발사 후 3초";
                case UpgradeId.Fireworks: return "몬스터를 향해 날아가 범위 폭발";
                case UpgradeId.SkyLightning: return "몬스터 위치에 예고 후 범위 낙뢰";
                case UpgradeId.Paralysis: return "전기 적중 시 기절 · 보스 면역";
                case UpgradeId.SodaMastery: return "병 +1 · 지속시간 증가\n5레벨: 거대 페트병 1개 추가";
                case UpgradeId.ChiliMastery: return "고추 공격 +1회\n5레벨: 미니 고추를 생성하는 고추밭";
                case UpgradeId.TreeMastery: return "나무 +1\n5레벨: 열매 생성 간격 대폭 감소";
                case UpgradeId.LemonMastery: return "레몬 공격 +1회\n5레벨: 거대 레몬 바퀴 2개 추가";
                case UpgradeId.FogMastery: return "확산 범위·피해 지속시간 증가\n5레벨: 모든 안개 피해 증가";
                case UpgradeId.BlueFlameMastery: return $"기본 {UpgradeTuning.Data.blueFlameBaseBranches}갈래 · 강화마다 +{UpgradeTuning.Data.blueFlameBranchesPerLevel}갈래\n갈래마다 최대 {UpgradeTuning.Data.blueFlameChainTargets}마리 연결 · 경로 주변 {UpgradeTuning.Data.blueFlameSplashDamageMultiplier * 100:0}% 피해 · 폭 +{UpgradeTuning.Data.blueFlameSplashRangePerLevel:0.##}";
                default:
                    return string.Empty;
            }
        }
    }
}
