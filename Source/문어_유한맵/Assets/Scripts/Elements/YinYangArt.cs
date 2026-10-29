using System.Collections.Generic;
using UnityEngine;

namespace OctopusSurvivors
{
    // Dedicated smooth artwork; the Cosmos pixel atlas remains independent.
    public static class YinYangArt
    {
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static Sprite fog;
        static Sprite[] fogFrames;
        public const int BurstFrames = 24;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { textures.Clear(); sprites.Clear(); fog=null; fogFrames=null; }

        public static Sprite FieldFog(int frame)
        {
            if(fogFrames==null)
            {
                var data=Resources.Load<TextAsset>("Art/YinYang/Fog");
                if(data==null)return Fog;
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,false){name="Approved full field YinYang fog",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                ImageConversion.LoadImage(texture,data.bytes,true);
                fogFrames=new Sprite[64];
                for(int i=0;i<64;i++)fogFrames[i]=Sprite.Create(texture,new Rect((i%8)*512,(7-i/8)*288,512,288),new Vector2(.5f,.5f),512,0,SpriteMeshType.FullRect);
            }
            return fogFrames[Mathf.Clamp(frame,0,63)];
        }

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
                bool contact=name=="CommaHit";
                ImageConversion.LoadImage(texture, data.bytes, !contact);
                if(contact)
                {
                    // The supplied reference is white ink on black. Use its ink
                    // brightness as alpha so one exact silhouette can tint either color.
                    var pixels=texture.GetPixels32();
                    for(int i=0;i<pixels.Length;i++)
                    {
                        var p=pixels[i];byte alpha=(byte)(Mathf.Max(p.r,Mathf.Max(p.g,p.b))*p.a/255);
                        pixels[i]=new Color32(255,255,255,alpha);
                    }
                    texture.SetPixels32(pixels);texture.Apply(false,true);
                }
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
        public static Sprite Contact => Load("CommaHit");
        public static Sprite Shard(int i) => Load("Shards", i % 8, 8);
        public static Sprite Burst(int frame) => Load("Burst", Mathf.Clamp(frame, 0, BurstFrames - 1), 6, 4);
    }
}
