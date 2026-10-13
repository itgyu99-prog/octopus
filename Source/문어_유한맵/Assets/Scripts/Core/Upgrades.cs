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
            UpgradeId.ElementCooldown
        };

        public static string Name(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.ExtraDash: return "대시 1회 추가";
                case UpgradeId.Attack: return "평타 공격력";
                case UpgradeId.Range: return "평타 사거리";
                case UpgradeId.MoveSpeed: return "이동속도";
                case UpgradeId.MaxHp: return "최대 체력";
                case UpgradeId.AttributeRank: return "속성 공격 강화";
                case UpgradeId.ElementRank: return "요소/특수요소 데미지";
                case UpgradeId.AttributeCooldown: return "속성 쿨타임 감소";
                case UpgradeId.ElementCooldown: return "요소/특수 쿨타임 감소";
                default: return "-";
            }
        }

        public static Color Tint(UpgradeId id)
        {
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
            switch (id)
            {
                case UpgradeId.ExtraDash: return 1;
                case UpgradeId.AttributeRank:
                case UpgradeId.ElementRank:
                    return GameConfig.MaxRankPicks;
                case UpgradeId.AttributeCooldown:
                case UpgradeId.ElementCooldown:
                    return GameConfig.MaxCooldownPicks;
                default:
                    return byte.MaxValue;
            }
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
                    return $"피해 +22% · 물/전기 발사 +1회\n불 범위 +0.3";
                case UpgradeId.ElementRank:
                    return $"요소/특수요소 피해 +{picks * 30}% → +{(picks + 1) * 30}%";
                case UpgradeId.AttributeCooldown:
                    return $"{GameConfig.AttributeCycle * GameConfig.AttributeCdMul(picks):0.0}초 → {GameConfig.AttributeCycle * GameConfig.AttributeCdMul(picks + 1):0.0}초 · 각 속성";
                case UpgradeId.ElementCooldown:
                    return $"{(1f - GameConfig.ElementCdMul(picks)) * 100f:0}% → {(1f - GameConfig.ElementCdMul(picks + 1)) * 100f:0}% 감소";
                default:
                    return string.Empty;
            }
        }
    }
}
