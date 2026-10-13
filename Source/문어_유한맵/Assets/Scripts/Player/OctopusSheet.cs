using UnityEngine;

namespace OctopusSurvivors
{
    public enum OctopusAnim { Idle, Move, Hit, Attack, Think, Death, Revive }

    public static class OctopusSheet
    {
        public const int Columns = 8;
        public const float PixelsPerUnit = 100f;
        public const float BodyRadius = 0.22f;

        public static Sprite Frame(OctopusAnim animation, int index)
        {
            switch (animation)
            {
                case OctopusAnim.Idle: return RuntimeArt.Basic(0, index);
                case OctopusAnim.Move: return RuntimeArt.Basic(1, index);
                case OctopusAnim.Hit: return RuntimeArt.Basic(2, index);
                case OctopusAnim.Think: return RuntimeArt.Basic(3, index);
                case OctopusAnim.Death: return RuntimeArt.Basic(4, index);
                case OctopusAnim.Revive: return RuntimeArt.Basic(5, index);
                default: return RuntimeArt.Basic(0, index);
            }
        }

        public static Sprite AttackFrame(Vector2 facing, int index) => RuntimeArt.Attack(facing, index);
        public static bool AttackHits(int frame) => frame >= 3 && frame <= 5;
    }
}
