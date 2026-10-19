using System;
using Unity.Netcode;
using UnityEngine;

namespace OctopusSurvivors
{
    public struct OctopusBossSnapshot : INetworkSerializable, IEquatable<OctopusBossSnapshot>
    {
        public byte Pattern, Half, Contacts;
        public uint Seed;
        public float Time;
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref Pattern); s.SerializeValue(ref Half); s.SerializeValue(ref Contacts);
            s.SerializeValue(ref Seed); s.SerializeValue(ref Time);
        }
        public bool Equals(OctopusBossSnapshot o) => Pattern == o.Pattern && Half == o.Half &&
            Contacts == o.Contacts && Seed == o.Seed && Time.Equals(o.Time);
    }

    // Both the server and all views use these exact contact times and positions.
    public static class OctopusBossPattern
    {
        public const float VisualMarker = 5005;
        public const int Arms = 6, RelayHits = 30;
        public const float Interval = .16f, ContactOffset = .34f, SwingDuration = .78f;
        public const float RelayFirstContact = .75f, RelayDuration = 6.65f;
        public const float HalfAttackStart = 1.65f, HalfAttackSeconds = 3f, HalfDuration = 7.3f;
        public const float SlamInterval = .24f;
        public static Vector2 Head => new Vector2(0, FiniteBeach.Height * .53f);
        public static Vector2 OverviewCenter => new Vector2(0, FiniteBeach.Height * .11f);
        public static float OverviewSize => FiniteBeach.Height * .64f;
        public static float IdleHeight => FiniteBeach.Height * .245f;
        public static Rect Arena => Rect.MinMaxRect(-FiniteBeach.Width * .25f, -FiniteBeach.Height * .30f,
            FiniteBeach.Width * .25f, FiniteBeach.Height * .335f);
        public static Vector2 RestRoot(int arm) => new Vector2(
            (arm < 3 ? -1 : 1) * FiniteBeach.Width * (arm % 3 == 1 ? .345f : .315f),
            Mathf.Lerp(FiniteBeach.Height * .26f, -FiniteBeach.Height * .245f, arm % 3 / 2f));
        public static int RelayArm(int strike) => (strike % 6) / 2 + (strike % 2) * 3;
        public static float RelayContact(int strike) => RelayFirstContact + strike * Interval;
        public static Rect HalfRect(int half)
        {
            var r = Arena;
            switch (half & 3)
            {
                case 0: return Rect.MinMaxRect(r.xMin, r.center.y, r.xMax, r.yMax);
                case 1: return Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, r.center.y);
                case 2: return Rect.MinMaxRect(r.xMin, r.yMin, r.center.x, r.yMax);
                default: return Rect.MinMaxRect(r.center.x, r.yMin, r.xMax, r.yMax);
            }
        }
        public static Rect HalfCell(int half, int arm)
        {
            var r = HalfRect(half); int columns = half < 2 ? 3 : 2, rows = 6 / columns;
            float w = r.width / columns, h = r.height / rows;
            return new Rect(r.xMin + arm % columns * w, r.yMin + arm / columns * h, w, h);
        }
        public static Vector2 HalfRoot(int half, int arm)
        {
            var c = HalfCell(half, arm);
            return new Vector2(c.center.x, c.yMax - c.height * .08f);
        }
        public static Vector2 RelayTarget(uint seed, int strike)
        {
            var r = Arena;
            return new Vector2(Mathf.Lerp(r.xMin + 1.2f, r.xMax - 1.2f, Sample(seed, strike * 2)),
                Mathf.Lerp(r.yMin + 1.2f, r.yMax - 1.2f, Sample(seed, strike * 2 + 1)));
        }
        public static Vector2 HalfTarget(uint seed, int half, int arm, int cycle)
        {
            var c = HalfCell(half, arm);
            int n = cycle * 12 + arm * 2 + 113;
            return c.center + new Vector2((Sample(seed, n) - .5f) * c.width * .30f,
                -c.height * .22f + (Sample(seed, n + 1) - .5f) * c.height * .16f);
        }
        public static Vector2 HalfRadii(int half) => new Vector2(HalfCell(half, 0).width * .65f,
            HalfCell(half, 0).height * .65f);
        public static float HalfContact(int arm, int cycle) => HalfAttackStart + .09f + arm * .014f + cycle * SlamInterval;
        public static bool InFootprint(Vector2 p, Vector2 center, Vector2 radii, float padding = 0)
        {
            var d = p - center;
            return d.x * d.x / Mathf.Pow(radii.x + padding, 2) + d.y * d.y / Mathf.Pow(radii.y + padding, 2) <= 1;
        }
        public static bool HalfCanHit(Vector2 p, int half, Vector2 target)
        { return HalfRect(half).Contains(p) && InFootprint(p, target, HalfRadii(half), GameConfig.PlayerRadius); }
        public static float Sample(uint seed, int index)
        {
            unchecked
            {
                uint x = seed + (uint)(index + 1) * 0x9e3779b9u;
                x ^= x >> 16; x *= 0x7feb352du; x ^= x >> 15; x *= 0x846ca68bu; x ^= x >> 16;
                return (x & 0xffffff) / 16777216f;
            }
        }
        public static string HalfName(int half) => half == 0 ? "위" : half == 1 ? "아래" : half == 2 ? "왼쪽" : "오른쪽";
        public static string ContactLabel(int completed)
        {
            var counts = new int[6];
            for (int n = 0; n < Mathf.Clamp(completed, 0, 30); n++) counts[RelayArm(n)]++;
            return $"1번 {counts[0]}/5   2번 {counts[1]}/5   3번 {counts[2]}/5   4번 {counts[3]}/5   5번 {counts[4]}/5   6번 {counts[5]}/5";
        }
    }
}
