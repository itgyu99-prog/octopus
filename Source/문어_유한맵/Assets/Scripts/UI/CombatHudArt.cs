using System.Collections.Generic;
using UnityEngine;

namespace OctopusSurvivors
{
    // Source attachments stay byte-for-byte intact. Cropping/background extraction
    // happens once into runtime textures, leaving all existing asset GUIDs untouched.
    public static class CombatHudArt
    {
        static readonly Dictionary<string,Sprite> cache = new Dictionary<string,Sprite>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { foreach(var s in cache.Values) if(s!=null) { Object.Destroy(s.texture); Object.Destroy(s); } cache.Clear(); }
        public static Sprite Frame => Extract("Frame", new RectInt(35,190,1595,550));
        public static Sprite Combo(ComboId id)
        {
            switch(id) {
                case ComboId.Fog: return Extract("Elements",new RectInt(20,35,310,295));
                case ComboId.Chili: return Extract("Elements",new RectInt(410,55,210,265));
                case ComboId.Tree: return Extract("Elements",new RectInt(720,45,270,285));
                case ComboId.Lemon: return Extract("Elements",new RectInt(60,370,235,270));
                case ComboId.Soda: return Extract("Elements",new RectInt(410,345,210,325));
                case ComboId.BlueFlame: return Extract("Elements",new RectInt(730,365,250,280));
                default: return null;
            }
        }
        static Sprite Extract(string source, RectInt rect)
        {
            string key=source+rect;
            if(cache.TryGetValue(key,out var sprite) && sprite!=null) return sprite;
            var data=Resources.Load<TextAsset>("CombatHud/"+source);
            if(data==null) return null;
            var input=new Texture2D(2,2,TextureFormat.RGBA32,false);
            ImageConversion.LoadImage(input,data.bytes);
            var pixels=input.GetPixels(rect.x,input.height-rect.y-rect.height,rect.width,rect.height);
            // Connected background only: never erase isolated highlights or grey art.
            var seen=new bool[pixels.Length]; var queue=new Queue<int>();
            for(int x=0;x<rect.width;x++) { queue.Enqueue(x); queue.Enqueue((rect.height-1)*rect.width+x); }
            for(int y=0;y<rect.height;y++) { queue.Enqueue(y*rect.width); queue.Enqueue(y*rect.width+rect.width-1); }
            while(queue.Count>0) {
                int n=queue.Dequeue(); if(seen[n]) continue; seen[n]=true;
                var c=pixels[n];
                bool background=source=="Frame" ? c.r>.11f && c.r<.36f && Mathf.Abs(c.r-c.g)<.055f && Mathf.Abs(c.b-c.g)<.07f :
                    c.r>.10f && c.r<.23f && c.g>.12f && c.g<.27f && c.b>.15f && c.b<.32f && c.b-c.r<.13f;
                if(!background) continue;
                pixels[n]=Color.clear;
                int x=n%rect.width,y=n/rect.width;
                if(x>0)queue.Enqueue(n-1); if(x+1<rect.width)queue.Enqueue(n+1);
                if(y>0)queue.Enqueue(n-rect.width); if(y+1<rect.height)queue.Enqueue(n+rect.width);
            }
            var texture=new Texture2D(rect.width,rect.height,TextureFormat.RGBA32,false) { filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name=key };
            texture.SetPixels(pixels); texture.Apply(); Object.Destroy(input);
            sprite=Sprite.Create(texture,new Rect(0,0,rect.width,rect.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            cache[key]=sprite; return sprite;
        }
    }
}
