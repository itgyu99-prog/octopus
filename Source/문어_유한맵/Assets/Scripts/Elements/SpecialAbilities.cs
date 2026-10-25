using UnityEngine;

namespace OctopusSurvivors
{
    public static class SpecialAbilities
    {
        public static bool IsSupported(SpecialId id) => id == SpecialId.Cosmos || id == SpecialId.YinYang;

        public static string Description(SpecialId id)
        {
            switch (id)
            {
                case SpecialId.Cosmos: return "별을 소환해 적에게 연속으로 낙하시킨다";
                case SpecialId.YinYang: return "두 곡옥이 공전하고 맞물린 뒤 흑백 폭발을 일으킨다";
                default: return string.Empty;
            }
        }

        public static void Cast(OctopusPlayer player, GameSession session, SpecialId id, int level)
        {
            if (player == null || session == null || !IsSupported(id)) return;
            session.CastCelestial(player.Pos, player, id, level);
        }

        public static void CastPair(GameSession session, Vector2 center, SpecialId a, SpecialId b, int level)
        {
            if (session == null || !IsSupported(a) || !IsSupported(b)) return;
            session.CastCelestial(center, null, a, level);
            session.CastCelestial(center, null, b, level);
        }
    }
}
