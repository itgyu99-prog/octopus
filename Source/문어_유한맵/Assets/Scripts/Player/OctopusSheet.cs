using UnityEngine;
using System.Collections.Generic;

namespace OctopusSurvivors
{
    public enum OctopusAnim { Idle, Move, Hit, Attack, Think, Death, Revive, Stop, Victory }

    public static class OctopusSheet
    {
        public const int Columns = 8;
        public const float PixelsPerUnit = 100f;
        public const float BodyRadius = 0.22f;

        public static Sprite Frame(OctopusAnim animation, int index)
        {
            switch (animation)
            {
                case OctopusAnim.Idle:
                case OctopusAnim.Stop:
                case OctopusAnim.Victory:
                case OctopusAnim.Move:
                case OctopusAnim.Hit:
                    var frames = Frames(animation);
                    return frames[Mathf.Clamp(index, 0, frames.Length - 1)];
                case OctopusAnim.Think: return RuntimeArt.Basic(3, index);
                case OctopusAnim.Death: return RuntimeArt.Basic(4, index);
                case OctopusAnim.Revive: return RuntimeArt.Basic(5, index);
                default: return RuntimeArt.Basic(0, index);
            }
        }

        static readonly Dictionary<OctopusAnim, Sprite[]> Cache = new Dictionary<OctopusAnim, Sprite[]>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Cache.Clear();
        public static int FrameCount(OctopusAnim animation) => Frames(animation).Length;
        static Sprite[] Frames(OctopusAnim animation)
        {
            if (!Cache.TryGetValue(animation, out var frames)) Cache[animation] = frames = LoadAnimation(animation);
            return frames;
        }
        static Sprite[] LoadAnimation(OctopusAnim animation)
        {
            switch (animation)
            {
                case OctopusAnim.Idle: return Load("CharacterIdle", new RectInt[] { new RectInt(98,733,236,254), new RectInt(437,733,238,254), new RectInt(771,733,239,254), new RectInt(1108,733,242,254), new RectInt(95,409,239,250), new RectInt(435,409,240,250), new RectInt(771,409,239,250), new RectInt(1108,409,243,250), new RectInt(99,80,236,238), new RectInt(439,80,236,238), new RectInt(771,80,239,238), new RectInt(1112,80,237,238) });
                case OctopusAnim.Move: return Load("CharacterMove", new RectInt[] { new RectInt(34,602,266,303), new RectInt(345,602,269,303), new RectInt(650,602,265,303), new RectInt(961,602,264,303), new RectInt(35,233,267,287), new RectInt(345,233,267,287), new RectInt(650,233,264,287), new RectInt(962,233,264,287) });
                case OctopusAnim.Stop: return Load("CharacterStop", new RectInt[] { new RectInt(109,86,552,543), new RectInt(811,86,548,543), new RectInt(1489,86,546,543) });
                case OctopusAnim.Hit: return Load("CharacterHit", new RectInt[] { new RectInt(103,43,556,589), new RectInt(857,43,539,589), new RectInt(1527,43,546,589) });
                case OctopusAnim.Victory: return Load("CharacterVictory", new RectInt[] { new RectInt(172,808,223,237), new RectInt(467,808,222,237), new RectInt(760,808,222,237), new RectInt(1057,808,221,237), new RectInt(173,548,222,236), new RectInt(467,548,222,236), new RectInt(760,548,222,236), new RectInt(1055,548,221,236), new RectInt(172,288,223,237), new RectInt(466,288,224,237), new RectInt(757,288,223,237), new RectInt(1054,288,224,237), new RectInt(173,30,222,237), new RectInt(466,30,222,237), new RectInt(760,30,223,237), new RectInt(1056,30,222,237) });
                default: throw new System.ArgumentOutOfRangeException(nameof(animation));
            }
        }
        static Sprite[] Load(string name, RectInt[] cells)
        {
            var source = Resources.Load<TextAsset>("Art/" + name);
            if (source == null) throw new System.InvalidOperationException("Missing character sheet: " + name);
            var sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(sheet, source.bytes)) throw new System.InvalidOperationException(name);
            int width = 0, height = 0;
            foreach (var cell in cells) { width = Mathf.Max(width, cell.width); height = Mathf.Max(height, cell.height); }
            var frames = new Sprite[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];
                var texture = new Texture2D(width + 16, height + 16, TextureFormat.RGBA32, false)
                    { name = name + "_" + i, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                texture.SetPixels32(new Color32[texture.width * texture.height]);
                texture.SetPixels((texture.width - cell.width) / 2, 8, cell.width, cell.height,
                    sheet.GetPixels(cell.x, cell.y, cell.width, cell.height));
                texture.Apply();
                frames[i] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(.5f, .5f), height);
            }
            Object.Destroy(sheet);
            return frames;
        }

        public static Sprite AttackFrame(Vector2 facing, int index) => RuntimeArt.Attack(facing, index);
        public static bool AttackHits(int frame) => frame >= 3 && frame <= 5;
    }
}
