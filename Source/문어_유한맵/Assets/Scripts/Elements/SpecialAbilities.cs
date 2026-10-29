using UnityEngine;

namespace OctopusSurvivors
{
    public static class SpecialAbilities
    {
        public static bool IsSupported(SpecialId id) => id == SpecialId.Cosmos || id == SpecialId.YinYang || id == SpecialId.DimensionRift;

        public static string Description(SpecialId id)
        {
            switch (id)
            {
                case SpecialId.Cosmos: return "어두워진 필드에 별이 차오르고, 모두 모이면 위성처럼 적을 추적하는 별똥별이 된다";
                case SpecialId.YinYang: return "안개 속 곡옥이 접촉 피해를 주고, 넓은 음양 폭발로 10회 연타한다";
                case SpecialId.DimensionRift: return "공중 균열에서 전 필드 파동과 흡입 처형을 일으킨다";
                default: return string.Empty;
            }
        }

        public static void Cast(OctopusPlayer player, GameSession session, SpecialId id, int level)
        {
            if (player == null || session == null || !IsSupported(id)) return;
            if(id==SpecialId.DimensionRift)session.CastDimensionRift(player.Pos,player,level);
            else session.CastCelestial(player.Pos, player, id, level);
        }

        public static void CastPair(GameSession session, Vector2 center, SpecialId a, SpecialId b, int level)
        {
            if (session == null || !IsSupported(a) || !IsSupported(b)) return;
            if(a==SpecialId.DimensionRift)session.CastDimensionRift(center,null,level);
            else session.CastCelestial(center, null, a, level);
            if(b==SpecialId.DimensionRift)session.CastDimensionRift(center,null,level);
            else session.CastCelestial(center, null, b, level);
        }
    }
}
