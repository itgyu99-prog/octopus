using UnityEngine;
namespace OctopusSurvivors
{
    public sealed class SpecialSkillVisual : MonoBehaviour
    {
        SpriteRenderer[] parts;
        ActorKind kind;
        float age,duration,size,receivedAt,last=-1;
        byte extra;
        public static bool Supports(ActorKind k) => k==ActorKind.YinYangMote || k==ActorKind.SpecialConvergence;
        public void Configure(ActorKind k,float a,float d,float s,byte flags)
        {
            if(parts==null)
            {
                kind=k;int count=k==ActorKind.YinYangMote?1:k==ActorKind.CosmosField?1:k==ActorKind.YinYangMist?24:54;
                parts=new SpriteRenderer[count];
                for(int i=0;i<count;i++)
                {
                    var go=new GameObject(k+" layer "+i);go.transform.SetParent(transform,false);
                    parts[i]=go.AddComponent<SpriteRenderer>();
                }
            }
            age=a;duration=Mathf.Max(.01f,d);size=s;extra=flags;receivedAt=Time.time;Sample(age);
        }
        void LateUpdate()
        {
            bool running=GameSession.Instance!=null && GameSession.Instance.CombatActive;
            Sample(Mathf.Min(duration,age+(running?Mathf.Clamp(Time.time-receivedAt,0,.1f):0)));
        }
        void Draw(int i,Sprite sprite,Vector2 p,Vector2 scale,float rotation,Color color,int order)
        {
            var sr=parts[i];sr.sprite=sprite;sr.enabled=sprite!=null && color.a>.002f;
            sr.color=color;sr.sortingOrder=order;sr.transform.localPosition=p;
            sr.transform.localScale=new Vector3(scale.x,scale.y,1);sr.transform.localRotation=Quaternion.Euler(0,0,rotation);
        }
        static float Hash(int x)=>Mathf.Repeat(Mathf.Sin(x*127.1f+311.7f)*43758.5453f,1);
        static float Envelope(float age,float life,float attack=.4f,float release=.8f) => Mathf.SmoothStep(0,1,age/attack)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(life-release,life,age)));
        void Sample(float t)
        {
            foreach(var part in parts)part.enabled=false;
            if(t<0||t>=duration)return;
            var tuning=SpecialSkillTuning.Data;float visibility=Envelope(t,duration);
            if(kind==ActorKind.YinYangMote)
            {
                Draw(0,YinYangArt.Comma(extra>=128),Vector2.zero,Vector2.one*size,(extra&127)/127f*360-45,new Color(1,1,1,visibility),48);
            }
            else if(kind==ActorKind.CosmosField)
            {
                Draw(0,SpriteFactory.Square(Color.white),Vector2.zero,Vector2.one*200,0,new Color(.015f,.02f,.055f,visibility*tuning.cosmosDarkness),-10);
            }
            else if(kind==ActorKind.YinYangMist)
            {
                float burstAt=tuning.orbitDuration+tuning.mergeDuration;
                float push=Mathf.Max(0,t-burstAt)*4;
                for(int i=0;i<parts.Length;i++)
                {
                    float a=i*2.39996f+t*tuning.mistFlowSpeed*(i%2==0?1:-1);
                    float r=size*(.15f+.66f*Hash(i+5))+push;
                    Vector2 p=new Vector2(Mathf.Cos(a),Mathf.Sin(a)*.76f)*r;
                    float density=(.55f+.45f*Mathf.Sin(t*.5f+i))*visibility*tuning.mistOpacity;
                    Color c=i%3==0?new Color(.07f,.06f,.09f,density):new Color(.88f,.87f,.9f,density);
                    Draw(i,DimensionRiftArt.Smoke((int)(t*7+i)%16),p,new Vector2(4.5f+Hash(i)*3,2.7f+Hash(i+2)*2),a*Mathf.Rad2Deg,c,-4+i%2);
                }
            }
            else
            {
                float u=t/duration;
                Draw(0,SpriteFactory.Square(Color.white),Vector2.zero,Vector2.one*200,0,new Color(.015f,.018f,.045f,visibility*tuning.convergenceDarkness),-10);
                float gather=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.48f,u));
                for(int side=0;side<2;side++)
                {
                    float a=t*1.7f+t*t*.18f+side*Mathf.PI;
                    Vector2 p=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(size*.55f*gather);
                    Draw(side+1,YinYangArt.Comma(side==1),p,Vector2.one*size*.20f*(.3f+.7f*gather),a*Mathf.Rad2Deg-45,new Color(1,1,1,visibility*(1-Mathf.InverseLerp(.47f,.6f,u))),7);
                }
                for(int i=0;i<24;i++)
                {
                    float a=i*2.39996f-t*(1.2f+i%3*.2f);
                    float r=size*(.12f+.5f*Hash(i+7))*Mathf.Lerp(.35f,1,gather);
                    Vector2 p=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
                    float twinkle=.7f+.3f*Mathf.Sin(t*4+i);
                    Draw(i+3,CelestialArt.CosmosIcon,p,Vector2.one*(.65f+.9f*Hash(i+2)),a*Mathf.Rad2Deg,new Color(.75f,.78f,1,visibility*twinkle),6);
                }
                float tearIn=Mathf.InverseLerp(.35f,.64f,u);
                float close=1-Mathf.InverseLerp(.82f,1,u);
                int frame=tearIn<1?Mathf.Min(23,(int)(tearIn*24)):24+(int)(t*8)%8;
                Draw(27,DimensionRiftArt.Tear(frame),Vector2.up*1.2f,Vector2.one*size*1.7f,0,new Color(1,1,1,visibility*close),8);
                for(int i=0;i<24;i++)
                {
                    float a=i*2.3999f+t*.2f;
                    float r=size*(.15f+.35f*Hash(i+51));
                    Color c=i%2==0?new Color(.06f,.05f,.08f,visibility*.25f):new Color(.83f,.82f,.87f,visibility*.13f);
                    Draw(i+28,DimensionRiftArt.Smoke((int)(t*7+i)%16),new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,Vector2.one*(2.5f+Hash(i)*2),a*Mathf.Rad2Deg,c,-3);
                }
                float waveU=Mathf.InverseLerp(.57f,.98f,u);
                if(u>=.57f)
                {
                    Draw(52,DimensionRiftArt.Wave(waveU),Vector2.up*1.2f,Vector2.one*size*3.5f,0,new Color(.025f,.02f,.035f,visibility*.9f),5);
                    Draw(53,DimensionRiftArt.Wave(waveU),Vector2.up*1.2f,Vector2.one*size*3.38f,2,new Color(1,1,1,visibility),6);
                }
                if(last<duration*.57f && t>=duration*.57f && t-duration*.57f<.2f)PlayerCameraFollow.ConvergenceImpact();
            }
            last=t;
        }
        void OnDisable(){if(parts!=null)foreach(var part in parts)if(part!=null)part.enabled=false;}
    }
}
