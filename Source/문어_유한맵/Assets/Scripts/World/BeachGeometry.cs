using UnityEngine;

namespace OctopusSurvivors
{
    // Traced from the foot of the cliffs and inner shoreline of the supplied
    // 7373x5100 artwork. Coordinates are fractions measured from its top left.
    // Separate outlines are necessary: BossBeach is a different composition.
    public static class BeachGeometry
    {
        static readonly Vector2[] Normal = {
            new Vector2(.19f,.48f), new Vector2(.25f,.445f), new Vector2(.34f,.43f),
            new Vector2(.44f,.435f), new Vector2(.53f,.425f), new Vector2(.59f,.44f),
            new Vector2(.66f,.46f), new Vector2(.72f,.455f), new Vector2(.78f,.49f),
            new Vector2(.82f,.51f), new Vector2(.81f,.55f), new Vector2(.80f,.61f),
            new Vector2(.82f,.66f), new Vector2(.80f,.70f), new Vector2(.77f,.75f),
            new Vector2(.70f,.78f), new Vector2(.62f,.81f), new Vector2(.52f,.82f),
            new Vector2(.43f,.81f), new Vector2(.35f,.79f), new Vector2(.27f,.77f),
            new Vector2(.22f,.73f), new Vector2(.19f,.68f), new Vector2(.18f,.62f),
            new Vector2(.19f,.57f), new Vector2(.18f,.53f)
        };
        static readonly Vector2[] Boss = {
            new Vector2(.19f,.27f), new Vector2(.26f,.22f), new Vector2(.34f,.215f),
            new Vector2(.44f,.22f), new Vector2(.53f,.21f), new Vector2(.60f,.225f),
            new Vector2(.67f,.24f), new Vector2(.73f,.24f), new Vector2(.79f,.28f),
            new Vector2(.82f,.29f), new Vector2(.81f,.34f), new Vector2(.80f,.40f),
            new Vector2(.82f,.44f), new Vector2(.80f,.49f), new Vector2(.77f,.54f),
            new Vector2(.70f,.57f), new Vector2(.62f,.59f), new Vector2(.52f,.61f),
            new Vector2(.43f,.60f), new Vector2(.35f,.58f), new Vector2(.27f,.56f),
            new Vector2(.22f,.52f), new Vector2(.19f,.47f), new Vector2(.18f,.41f),
            new Vector2(.19f,.36f), new Vector2(.18f,.32f)
        };
        public static bool BossMode => PlayerCameraFollow.ShouldUseBossMap(GameSession.Instance);
        public static Vector2 World(Vector2 uv, bool boss)
        {
            Vector2 position = boss ? ArenaBuilder.BossBeachArtworkPosition : ArenaBuilder.BeachArtworkPosition;
            Vector2 scale = boss ? ArenaBuilder.BossBeachArtworkScale : ArenaBuilder.BeachArtworkScale;
            float pivot = boss ? (953f+4147f*.5f)/5100f : .5f;
            return position + Vector2.Scale(new Vector2((uv.x-.5f)*73.73f, (1-uv.y-pivot)*51f),scale);
        }
        public static bool Walkable(Vector2 p, float radius) => Walkable(p, radius, BossMode);
        public static bool Walkable(Vector2 p, float radius, bool boss)
        {
            var outline = boss ? Boss : Normal;
            bool inside = false;
            for(int i=0,j=outline.Length-1;i<outline.Length;j=i++)
            {
                var a=World(outline[j],boss); var b=World(outline[i],boss);
                if((a.y>p.y)!=(b.y>p.y) && p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
                var edge=b-a;
                var nearest=a+edge*Mathf.Clamp01(Vector2.Dot(p-a,edge)/edge.sqrMagnitude);
                if((p-nearest).sqrMagnitude<=radius*radius) return false;
            }
            return inside;
        }

        public static bool BossArenaWalkable(Vector2 p, float radius)
        {
            // The entire sandy boss beach is playable. Walkable still excludes the
            // cliffs, ocean, and rocks, without the old narrow central polygon.
            return Walkable(p, radius, true);
        }

        public static Vector2 MoveBossArena(Vector2 from, Vector2 to, float radius)
        {
            Vector2 p = ClampBossArena(from, radius);
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / .12f));
            Vector2 step = (to - from) / steps;
            for (int i = 0; i < steps; i++)
            {
                if (BossArenaWalkable(p + step, radius)) p += step;
                else
                {
                    var x = p + new Vector2(step.x, 0); if (BossArenaWalkable(x, radius)) p = x;
                    var y = p + new Vector2(0, step.y); if (BossArenaWalkable(y, radius)) p = y;
                }
            }
            return p;
        }

        static Vector2 ClampBossArena(Vector2 p, float radius)
        {
            if (BossArenaWalkable(p, radius)) return p;
            Vector2 best = World(new Vector2(.5f, .5f), true);
            float bestDistance = float.MaxValue;
            for (float y = .20f; y <= .80f; y += .008f)
            for (float x = .18f; x <= .82f; x += .008f)
            {
                var candidate = World(new Vector2(x, y), true);
                if (!BossArenaWalkable(candidate, radius)) continue;
                float distance = (candidate - p).sqrMagnitude;
                if (distance < bestDistance) { bestDistance = distance; best = candidate; }
            }
            return best;
        }
    }
}
