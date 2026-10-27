using System.Collections.Generic;
using UnityEngine;

namespace OctopusSurvivors
{
    public static class DimensionRiftArt
    {
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { textures.Clear(); sprites.Clear(); }
        static Sprite Load(string name, int frame, int columns, int rows)
        {
            string key = name + ":" + frame;
            if (sprites.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            if (!textures.TryGetValue(name, out var tex) || tex == null)
            {
                var asset = Resources.Load<TextAsset>("Art/DimensionRift/" + name);
                if (asset == null) { Debug.LogError("Missing dimension rift artwork: " + name); return null; }
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    { name = "Rift_" + name, filterMode = name == "Smoke" ? FilterMode.Bilinear : FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                ImageConversion.LoadImage(tex, asset.bytes, true); textures[name] = tex;
            }
            int w = tex.width / columns, h = tex.height / rows;
            sprite = Sprite.Create(tex, new Rect(frame % columns * w, (rows - 1 - frame / columns) * h, w, h),
                new Vector2(.5f, .5f), w, 0, SpriteMeshType.FullRect);
            sprites[key] = sprite; return sprite;
        }
        public static Sprite Tear(int frame) => Load("Tear", Mathf.Clamp(frame, 0, 31), 8, 4);
        public static Sprite Wave(float t) => Load("Wave", Mathf.Clamp(Mathf.FloorToInt(t * 48), 0, 47), 8, 6);
        public static Sprite Smoke(int frame) => Load("Smoke", frame % 16, 4, 4);
        public static Sprite Icon => Load("Icon", 0, 1, 1);
    }
}
