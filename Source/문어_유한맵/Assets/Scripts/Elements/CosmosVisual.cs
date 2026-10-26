using System;
using System.Collections.Generic;
using UnityEngine;

namespace OctopusSurvivors
{
    public static class CosmosSequence
    {
        public const float ReleaseTime=2.75f, RestoreDuration=.55f;
        public static int StarCount(int level)=>168+Mathf.Clamp(level,1,6)*12;
        public static float BirthTime(int index,int count)=>index<8 ? .12f+index*.16f :
            Mathf.Lerp(1.35f,2.05f,(index-8f)/Mathf.Max(1,count-9));
        public static float Hash(int index)=>Mathf.Repeat(Mathf.Sin(index*127.1f+311.7f)*43758.5453f,1);
        public static Vector2 StarOffset(int index,int count)
        {
            return CelestialField.Point(index,count)-(Vector2)CelestialField.Bounds.center;
        }
    }

    // Smooth light masks keep the high contrast cosmic palette independent of the old atlas.
    public static class CosmosArt
    {
        static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
        static Sprite impact;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){cache.Clear();impact=null;}
        public static Sprite Impact
        {
            get
            {
                if(impact!=null)return impact;
                var data=Resources.Load<TextAsset>("Vfx/CosmicImpactV2");
                if(data==null)return Star;
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,false)
                    {filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                texture.LoadImage(data.bytes);
                impact=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),
                    Mathf.Max(texture.width,texture.height),0,SpriteMeshType.FullRect);
                return impact;
            }
        }
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
                case 1:return new Color(.46f,.62f,.92f);
                case 2:return new Color(.68f,.72f,.88f);
                default:return new Color(.8f,.87f,1);
            }
        }
    }

    [DefaultExecutionOrder(150)]
    public sealed class CosmosVisual:MonoBehaviour
    {
        struct TrailSample { public Vector3 Position; public float Time; }
        readonly List<TrailSample> history=new List<TrailSample>(48);
        LineRenderer ribbon;
        static Material lineMaterial;
        const int BackgroundStars=144, Links=36, Echoes=10;
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
                history.Clear();
                if(ribbon!=null){ribbon.enabled=false;Destroy(ribbon.gameObject);ribbon=null;}
                int count=kind==ActorKind.CosmicDusk?7+BackgroundStars+Links:
                    kind==ActorKind.CosmicMeteor?3+Echoes:kind==ActorKind.CosmicImpact?15:kind==ActorKind.CosmicTrail?1:3;
                parts=new SpriteRenderer[count];
                for(int i=0;i<count;i++)
                {
                    var child=new GameObject("Cosmos "+kind+" "+i);child.transform.SetParent(transform,false);
                    parts[i]=child.AddComponent<SpriteRenderer>();
                }
                if(kind==ActorKind.CosmicMeteor)
                {
                    var child=new GameObject("Curved starlight wake");child.transform.SetParent(transform,false);
                    ribbon=child.AddComponent<LineRenderer>();ribbon.useWorldSpace=true;
                    if(lineMaterial==null)lineMaterial=new Material(Shader.Find("Sprites/Default"));
                    ribbon.sharedMaterial=lineMaterial;ribbon.sortingOrder=44;
                    ribbon.numCornerVertices=3;ribbon.numCapVertices=3;
                    ribbon.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.7f,.055f),new Keyframe(1,.11f));
                }
            }
            age=serverAge;duration=Mathf.Max(.001f,lifetime);size=scale;flags=extra;receivedAt=Time.time;enabled=true;
            Sample(age);
        }
        void LateUpdate()
        {
            if(kind==ActorKind.CosmicMeteor)
            {
                if(history.Count==0 || Time.time-history[history.Count-1].Time>=1f/90f)
                    history.Add(new TrailSample{Position=transform.position,Time=Time.time});
                while(history.Count>1 && Time.time-history[1].Time>.36f)history.RemoveAt(0);
            }
            Sample(Mathf.Min(duration,age+Mathf.Clamp(Time.time-receivedAt,0,.1f)));
        }
        void OnDisable(){history.Clear();if(ribbon!=null)ribbon.enabled=false;if(parts!=null)foreach(var part in parts)if(part!=null)part.enabled=false;}
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
                    new Color(.008f,.012f,.04f,.72f*fade),30);
                for(int i=0;i<6;i++)
                {
                    Vector2 p=new Vector2((CosmosSequence.Hash(i+11)-.5f)*bounds.size.x,(CosmosSequence.Hash(i+39)-.5f)*bounds.size.y);
                    Color cloud=i%2==0?new Color(.08f,.12f,.25f):new Color(.3f,.34f,.43f);
                    Draw(i+1,YinYangArt.Fog,p,new Vector2(bounds.size.x*.65f,bounds.size.y*.55f),i*47+time*.4f,Alpha(cloud,.055f*fade),31);
                }
                for(int i=0;i<BackgroundStars;i++)
                {
                    Vector2 p=BackdropPoint(i,bounds);
                    float born=Mathf.SmoothStep(0,1,(time-.2f-p.magnitude*.018f-CosmosSequence.Hash(i)*.35f)/.9f);
                    float twinkle=.72f+.28f*Mathf.Sin(time*2.1f+i*1.7f);
                    float diameter=.10f+CosmosSequence.Hash(i)*.18f;
                    Draw(i+7,CosmosArt.Star,p,Vector2.one*diameter,45,Alpha(CosmosArt.Palette(i),fade*born*twinkle*.6f),40);
                }
                for(int i=0;i<Links;i++)
                {
                    int index=i*3+2;
                    Vector2 a=BackdropPoint(index,bounds),b=BackdropPoint(index+1,bounds),delta=b-a;
                    float born=Mathf.SmoothStep(0,1,(time-.5f-a.magnitude*.018f)/1.1f);
                    Draw(7+BackgroundStars+i,CosmosArt.Streak,(a+b)*.5f,
                        new Vector2(delta.magnitude,.035f+CosmosSequence.Hash(i)*.035f),Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,
                        new Color(.8f,.87f,1,.16f*fade*born),39);
                }
            }
            else if(kind==ActorKind.CosmicStar)
            {
                float visible=Mathf.SmoothStep(0,1,time/.25f),twinkle=.88f+.12f*Mathf.Sin(time*7+size*19);
                Draw(0,CosmosArt.Glow,Vector2.zero,Vector2.one*size*2.4f,0,Alpha(tint,.22f*visible),44);
                Draw(1,CosmosArt.Star,Vector2.zero,Vector2.one*size*twinkle,0,Alpha(tint,visible),46);
                Draw(2,CosmosArt.Star,Vector2.zero,Vector2.one*size*.4f,45,Alpha(Color.white,visible*.9f),47);
            }
            else if(kind==ActorKind.CosmicMeteor)
            {
                float angle=(flags&63)/63f*360,radians=angle*Mathf.Deg2Rad;
                Vector2 direction=new Vector2(Mathf.Cos(radians),Mathf.Sin(radians));float fade=Mathf.Clamp01((duration-time)/.15f);
                Draw(0,CosmosArt.Streak,-direction*size*.5f,new Vector2(size*1.5f,size*.3f),angle,Alpha(tint,.5f*fade),45);
                Draw(1,CosmosArt.Glow,Vector2.zero,Vector2.one*size*2,0,Alpha(tint,.28f*fade),46);
                Draw(2,CosmosArt.Star,Vector2.zero,Vector2.one*size,angle,Alpha(Color.white,fade),49);
                if(ribbon!=null)
                {
                    ribbon.enabled=history.Count>1;ribbon.positionCount=history.Count;
                    ribbon.startColor=Alpha(tint,0);ribbon.endColor=Alpha(tint,.38f*fade);
                    for(int i=0;i<history.Count;i++)ribbon.SetPosition(i,history[i].Position);
                }
                for(int i=0;i<Echoes;i++)
                {
                    float q=(i+1f)/Echoes,at=Time.time-q*.34f;
                    TrailSample sample=history.Count>0?history[0]:new TrailSample{Position=transform.position,Time=Time.time};
                    for(int j=history.Count-1;j>=0;j--)if(history[j].Time<=at){sample=history[j];break;}
                    float visibility=history.Count>1?Mathf.Clamp01((Time.time-history[0].Time)/(q*.34f)):0;
                    Draw(i+3,CosmosArt.Star,transform.InverseTransformPoint(sample.Position),Vector2.one*size*Mathf.Lerp(.65f,.08f,q),
                        angle,Alpha(tint,Mathf.Pow(1-q,1.3f)*.42f*fade*visibility),45);
                }
            }
            else if(kind==ActorKind.CosmicImpact)
            {
                float expand=1-Mathf.Pow(1-u,3),fade=Mathf.Pow(1-u,1.7f);
                float flash=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.02f,.17f,time));
                Draw(0,CosmosArt.Glow,Vector2.zero,Vector2.one*size*.8f,0,Alpha(Color.white,flash*.75f),48);
                Draw(1,CosmosArt.Impact,Vector2.zero,Vector2.one*size*Mathf.Lerp(.35f,1.5f,expand),0,Alpha(Color.white,Mathf.Min(1,fade*1.3f)),50);
                Draw(2,CosmosArt.Star,Vector2.zero,Vector2.one*size*Mathf.Lerp(.6f,.03f,expand),45,Alpha(Color.white,flash),51);
                for(int i=0;i<12;i++)
                {
                    float a=CosmosSequence.Hash(i+31)*Mathf.PI*2;
                    Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                    Draw(i+3,CosmosArt.Streak,d*size*expand*(.25f+CosmosSequence.Hash(i+19)*.2f),
                        new Vector2(size*.3f*(1-u),size*.018f),a*Mathf.Rad2Deg,Alpha(tint,fade*.7f),49);
                }
            }
            else Draw(0,CosmosArt.Glow,Vector2.zero,Vector2.one*size*1.5f,0,Alpha(tint,1-u),45);
        }
        static Vector2 BackdropPoint(int i,Bounds bounds)
        {
            const int columns=18,rows=8;
            return new Vector2(((i%columns+.15f+CosmosSequence.Hash(i+3)*.7f)/columns-.5f)*bounds.size.x,
                ((i/columns+.15f+CosmosSequence.Hash(i+59)*.7f)/rows-.5f)*bounds.size.y);
        }
    }
}
