using System.Collections.Generic;
using UnityEngine;
namespace OctopusSurvivors
{
    public static class CelestialArt
    {
        static readonly Dictionary<int,Sprite> cache=new Dictionary<int,Sprite>();
        static Texture2D texture;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){cache.Clear();texture=null;}
        public static Sprite Frame(int row,int frame)
        {
            int key=row*8+Mathf.Clamp(frame,0,7);
            if(cache.TryGetValue(key,out var sprite) && sprite!=null)return sprite;
            if(texture==null)
            {
                var data=Resources.Load<TextAsset>("Art/CelestialAtlas");
                if(data==null)return null;
                texture=new Texture2D(2,2,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
                ImageConversion.LoadImage(texture,data.bytes,false);
            }
            sprite=Sprite.Create(texture,new Rect((key%8)*128,(10-row)*128,128,128),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect);
            cache[key]=sprite;return sprite;
        }
        public static bool Supports(ActorKind k)=>k>=ActorKind.CosmicStar && k<=ActorKind.YinYangShockwave;
        public static void Show(SpriteRenderer sr,ActorKind kind,float age,float duration,float size,byte direction)
        {
            if (YinYangVisual.Supports(kind))
            {
                sr.enabled = false; // The orbit has no parent ring or trail sprite.
                sr.transform.localScale = Vector3.one;
                sr.transform.rotation = Quaternion.identity;
                var visual = sr.GetComponent<YinYangVisual>() ?? sr.gameObject.AddComponent<YinYangVisual>();
                visual.Configure(kind, age, duration, size);
                return;
            }
            int row=0;float t=duration>0?Mathf.Clamp01(age/duration):0;
            int f=Mathf.Min(7,(int)(t*8));
            switch(kind)
            {
                case ActorKind.CosmicStar:row=age<.35f?0:1;f=age<.35f?Mathf.Min(7,(int)(age/.35f*8)):(int)(age*12)%8;break;
                case ActorKind.CosmicMeteor:row=2;f=(int)(age*18)%8;break;
                case ActorKind.CosmicTrail:row=3;break;
                case ActorKind.CosmicImpact:row=4;break;
                case ActorKind.YinYangOrbit:row=7;f=0;break;
                case ActorKind.YinYangCollision:row=8;break;
                case ActorKind.YinYangBurst:row=9;break;
                case ActorKind.YinYangShockwave:row=10;break;
            }
            sr.sprite=Frame(row,f);sr.color=Color.white;sr.sortingOrder=kind==ActorKind.YinYangShockwave?46:(kind==ActorKind.YinYangBurst?45:42);sr.enabled=age>=0;
            bool directed=kind==ActorKind.CosmicMeteor || kind==ActorKind.CosmicTrail;
            sr.transform.rotation=directed?Quaternion.Euler(0,0,direction/255f*360):Quaternion.identity;
            sr.transform.localScale=Vector3.one*size;
        }
    }
}
