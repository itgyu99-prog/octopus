using System;
using System.Collections.Generic;
using UnityEngine;

namespace OctopusSurvivors
{
    public static class CosmosSequence
    {
        public const float ReleaseTime=2.75f, RestoreDuration=.55f;
        public static int StarCount(int level)=>90+Mathf.Clamp(level,1,6)*12;
        public static float BirthTime(int index,int count)=>index<8 ? .12f+index*.16f :
            Mathf.Lerp(1.35f,2.05f,(index-8f)/Mathf.Max(1,count-9));
        public static float Hash(int index)=>Mathf.Repeat(Mathf.Sin(index*127.1f+311.7f)*43758.5453f,1);
        public static Vector2 StarOffset(int index,int count)
        {
            var camera=Camera.main;
            float height=camera!=null?camera.orthographicSize*2:GameConfig.CameraSize*2;
            float width=height*(camera!=null?camera.aspect:16f/9);
            int columns=Mathf.CeilToInt(Mathf.Sqrt(count*width/height)),rows=Mathf.CeilToInt(count/(float)columns);
            float x=((index%columns+.12f+Hash(index)*.76f)/columns-.5f)*width;
            float y=((index/columns+.12f+Hash(index+217)*.76f)/rows-.5f)*height;
            return new Vector2(x,y);
        }
    }

    // Smooth light masks keep the high contrast cosmic palette independent of the old atlas.
    public static class CosmosArt
    {
        static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){cache.Clear();}
        static Sprite Make(string key,Func<float,float,float> alpha)
        {
            if(cache.TryGetValue(key,out var sprite) && sprite!=null)return sprite;
            const int resolution=128;
            var texture=new Texture2D(resolution,resolution,TextureFormat.RGBA32,false)
                {name="Cosmos "+key,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[resolution*resolution];
            for(int y=0;y<resolution;y++)for(int x=0;x<resolution;x++)
                pixels[y*resolution+x]=new Color(1,1,1,Mathf.Clamp01(alpha(x/(resolution-1f)*2-1,y/(resolution-1f)*2-1)));
            texture.SetPixels(pixels);texture.Apply(false,true);
            sprite=Sprite.Create(texture,new Rect(0,0,resolution,resolution),new Vector2(.5f,.5f),resolution,0,SpriteMeshType.FullRect);
            cache[key]=sprite;return sprite;
        }
        public static Sprite Star=>Make("pearl star",(x,y)=>Mathf.Exp(-(x*x+y*y)*65)+
            Mathf.Exp(-Mathf.Abs(x)*70)*Mathf.Pow(Mathf.Max(0,1-Mathf.Abs(y)),2)*.9f+
            Mathf.Exp(-Mathf.Abs(y)*70)*Mathf.Pow(Mathf.Max(0,1-Mathf.Abs(x)),2)*.9f);
        public static Sprite Glow=>Make("soft halo",(x,y)=>Mathf.Exp(-(x*x+y*y)*5)*Mathf.Clamp01((1-x*x-y*y)*3));
        public static Sprite Ring=>Make("impact rim",(x,y)=>Mathf.Exp(-Mathf.Pow(Mathf.Sqrt(x*x+y*y)-.7f,2)*180));
        public static Sprite Streak=>Make("comet tail",(x,y)=>Mathf.Pow((x+1)*.5f,1.6f)*
            Mathf.Exp(-y*y/(.006f+.045f*(x+1)))*Mathf.Clamp01((1-Mathf.Abs(x))*8));
        public static Color Palette(int index)
        {
            switch(index&3)
            {
                case 0:return new Color(.95f,.97f,1);
                case 1:return new Color(.13f,.19f,.38f);
                case 2:return new Color(.015f,.018f,.035f);
                default:return new Color(.55f,.63f,.8f);
            }
        }
    }

    public sealed class CosmosVisual:MonoBehaviour
    {
        SpriteRenderer[] parts;
        ActorKind kind;
        float age,duration,size,receivedAt;
        byte flags;
        public static bool Supports(ActorKind value)=>(value>=ActorKind.CosmicStar && value<=ActorKind.CosmicImpact) || value==ActorKind.CosmicDusk;
        public void Configure(ActorKind value,float serverAge,float lifetime,float scale,byte extra)
        {
            if(parts==null || kind!=value)
            {
                if(parts!=null)foreach(var part in parts){part.enabled=false;Destroy(part.gameObject);}
                kind=value;
                int count=kind==ActorKind.CosmicDusk?55:kind==ActorKind.CosmicMeteor?6:kind==ActorKind.CosmicTrail?1:3;
                parts=new SpriteRenderer[count];
                for(int i=0;i<count;i++)
                {
                    var child=new GameObject("Cosmos "+kind+" "+i);child.transform.SetParent(transform,false);
                    parts[i]=child.AddComponent<SpriteRenderer>();
                }
            }
            age=serverAge;duration=Mathf.Max(.001f,lifetime);size=scale;flags=extra;receivedAt=Time.time;enabled=true;
            Sample(age);
        }
        void LateUpdate()=>Sample(Mathf.Min(duration,age+Mathf.Clamp(Time.time-receivedAt,0,.1f)));
        void OnDisable(){if(parts!=null)foreach(var part in parts)if(part!=null)part.enabled=false;}
        void Draw(int index,Sprite sprite,Vector2 position,Vector2 scale,float rotation,Color color,int order)
        {
            var part=parts[index];part.sprite=sprite;part.enabled=sprite!=null && color.a>.001f;
            part.color=color;part.sortingOrder=order;part.transform.localPosition=position;
            part.transform.localScale=new Vector3(scale.x,scale.y,1);part.transform.localRotation=Quaternion.Euler(0,0,rotation);
        }
        static Color Alpha(Color color,float alpha){color.a=alpha;return color;}
        void Sample(float time)
        {
            if(time<0){foreach(var part in parts)part.enabled=false;return;}
            float u=Mathf.Clamp01(time/duration);Color tint=CosmosArt.Palette(flags>>6);
            if(kind==ActorKind.CosmicDusk)
            {
                float fade=flags==1?1-Mathf.SmoothStep(0,1,u):Mathf.SmoothStep(0,1,time/.65f);
                var bounds=YinYangVisual.MistFieldBounds;
                Draw(0,SpriteFactory.Square(Color.white),Vector2.zero,new Vector2(bounds.size.x,bounds.size.y)*1.6f,0,
                    new Color(.008f,.012f,.04f,.58f*fade),30);
                for(int i=0;i<6;i++)
                {
                    Vector2 p=new Vector2((CosmosSequence.Hash(i+11)-.5f)*bounds.size.x,(CosmosSequence.Hash(i+39)-.5f)*bounds.size.y);
                    Color cloud=i%2==0?new Color(.08f,.12f,.25f):new Color(.3f,.34f,.43f);
                    Draw(i+1,YinYangArt.Fog,p,new Vector2(bounds.size.x*.65f,bounds.size.y*.55f),i*47+time*.4f,Alpha(cloud,.055f*fade),31);
                }
                for(int i=0;i<48;i++)
                {
                    Vector2 p=new Vector2((CosmosSequence.Hash(i*7+3)-.5f)*bounds.size.x,(CosmosSequence.Hash(i*11+5)-.5f)*bounds.size.y);
                    float born=Mathf.SmoothStep(0,1,(time-CosmosSequence.BirthTime(i,48))/.25f);
                    float launch=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(CosmosSequence.ReleaseTime,CosmosSequence.ReleaseTime+.4f,time));
                    float diameter=.14f+CosmosSequence.Hash(i)*.17f;
                    Draw(i+7,CosmosArt.Star,p,Vector2.one*diameter,45,Alpha(CosmosArt.Palette(i),fade*born*launch*.6f),40);
                }
            }
            else if(kind==ActorKind.CosmicStar)
            {
                float visible=Mathf.SmoothStep(0,1,time/.25f),twinkle=.88f+.12f*Mathf.Sin(time*7+size*19);
                Draw(0,CosmosArt.Glow,Vector2.zero,Vector2.one*size*2.4f,0,Alpha(tint,.22f*visible),44);
                Draw(1,CosmosArt.Star,Vector2.zero,Vector2.one*size*twinkle,0,Alpha(tint,visible),46);
                Draw(2,CosmosArt.Star,Vector2.zero,Vector2.one*size*.4f,45,Alpha((flags>>6)==2?new Color(.22f,.25f,.32f):Color.white,visible*.9f),47);
            }
            else if(kind==ActorKind.CosmicMeteor)
            {
                float angle=(flags&63)/63f*360,radians=angle*Mathf.Deg2Rad;
                Vector2 direction=new Vector2(Mathf.Cos(radians),Mathf.Sin(radians));float fade=Mathf.Clamp01((duration-time)/.15f);
                Draw(0,CosmosArt.Streak,-direction*size*1.7f,new Vector2(size*4.5f,size*.65f),angle,Alpha(tint,.65f*fade),45);
                Draw(1,CosmosArt.Glow,Vector2.zero,Vector2.one*size*2,0,Alpha(tint,.28f*fade),46);
                Draw(2,CosmosArt.Star,Vector2.zero,Vector2.one*size,angle,Alpha((flags>>6)==2?new Color(.04f,.05f,.09f):Color.white,fade),49);
                for(int i=0;i<3;i++)
                {
                    Vector2 side=new Vector2(-direction.y,direction.x)*Mathf.Sin(time*8+i)*size*.15f;
                    Draw(i+3,CosmosArt.Star,-direction*(i+1)*size+side,Vector2.one*size*(.23f-i*.04f),45,Alpha(tint,fade*(.65f-i*.15f)),47);
                }
            }
            else if(kind==ActorKind.CosmicImpact)
            {
                Draw(0,CosmosArt.Glow,Vector2.zero,Vector2.one*size*1.2f,0,Alpha(tint,(1-u)*.3f),48);
                Draw(1,CosmosArt.Ring,Vector2.zero,Vector2.one*size*Mathf.Lerp(.2f,1,u),0,Alpha(tint,Mathf.Pow(1-u,1.5f)*.8f),49);
                Draw(2,CosmosArt.Star,Vector2.zero,Vector2.one*size*Mathf.Lerp(.5f,.1f,u),45,Alpha(Color.white,1-u),50);
            }
            else Draw(0,CosmosArt.Glow,Vector2.zero,Vector2.one*size*1.5f,0,Alpha(tint,1-u),45);
        }
    }
}
