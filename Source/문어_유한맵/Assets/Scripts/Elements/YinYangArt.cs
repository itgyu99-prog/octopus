using System.Collections.Generic;
using UnityEngine;

namespace OctopusSurvivors
{
    // Dedicated smooth artwork; the Cosmos pixel atlas remains independent.
    public static class YinYangArt
    {
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        public const int BurstFrames = 24;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { textures.Clear(); sprites.Clear(); }

        static Sprite Load(string name, int index = 0, int columns = 1, int rows = 1)
        {
            string key = name + ":" + index;
            if (sprites.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            if (!textures.TryGetValue(name, out var texture) || texture == null)
            {
                var data = Resources.Load<TextAsset>("Art/YinYang/" + name);
                if (data == null) { Debug.LogError("Missing YinYang artwork: " + name); return null; }
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                ImageConversion.LoadImage(texture, data.bytes, true);
                textures[name] = texture;
            }
            int w = texture.width / columns, h = texture.height / rows;
            sprite = Sprite.Create(texture, new Rect(index % columns * w, (rows - 1 - index / columns) * h, w, h),
                new Vector2(.5f, .5f), w, 0, SpriteMeshType.FullRect);
            sprite.name = key; sprites[key] = sprite; return sprite;
        }
        public static Sprite Comma(bool white) => Load(white ? "Yang" : "Yin");
        public static Sprite Symbol => Load("Symbol");
        public static Sprite Flash => Load("Flash");
        public static Sprite Ring => Load("Shockwave");
        public static Sprite Shard(int i) => Load("Shards", i % 8, 8);
        public static Sprite Burst(int frame) => Load("Burst", Mathf.Clamp(frame, 0, BurstFrames - 1), 6, 4);
    }
}
