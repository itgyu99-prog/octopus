using UnityEngine;

namespace OctopusSurvivors
{
    // Prefab/Animator overrides are cosmetic; packed server state drives the default poses.
    public sealed class GiantMonsterVisual : MonoBehaviour
    {
        static Sprite[] sprites;
        static Sprite spike;
        public static Sprite Spike
        {
            get
            {
                if(spike!=null)return spike;
                var t=new Texture2D(32,16,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
                var pixels=new Color[512];
                for(int y=0;y<16;y++)for(int x=0;x<32;x++)
                {
                    float edge=(31-x)*.2f;
                    if(x>2 && Mathf.Abs(y-7.5f)<edge)pixels[y*32+x]=Mathf.Abs(y-7.5f)<edge*.4f?new Color(1,.85f,1):new Color(.56f,.24f,.8f);
                }
                t.SetPixels(pixels);t.Apply();spike=Sprite.Create(t,new Rect(0,0,32,16),new Vector2(.5f,.5f),32);return spike;
            }
        }
        GiantMonsterDefinition definition;
        SpriteRenderer body;
        LineRenderer warning;
        Mesh warningMesh;
        MeshRenderer warningFill;
        Vector3[] vertices=new Vector3[36];
        Animator animator;
        GiantState state;
        float direction, phase;
        byte flags;
        public static Sprite SpriteAt(int id,int frame)
        {
            if(sprites==null)
            {
                var t=Resources.Load<Texture2D>("Monsters/GiantAtlas");if(t==null)return null;
                sprites=new Sprite[20];int w=t.width/4,h=t.height/5;
                for(int i=0;i<20;i++)sprites[i]=Sprite.Create(t,new Rect(i%4*w,(4-i/4)*h,w,h),new Vector2(.5f,.5f),w*.72f);
            }
            return sprites[Mathf.Clamp((id-1)*4+frame,0,19)];
        }
        public void Configure(float packed,byte extra)
        {
            int id=((int)packed-1000)/100;
            if(definition==null)
            {
                definition=GiantMonsterDefinition.Find(id);if(definition==null)return;
                GameObject child=definition.prefab!=null?Instantiate(definition.prefab,transform):new GameObject("Giant body");
                child.transform.SetParent(transform,false);
                body=child.GetComponentInChildren<SpriteRenderer>() ?? child.AddComponent<SpriteRenderer>();
                body.sortingOrder=10;
                if(definition.animator!=null){animator=child.GetComponent<Animator>()??child.AddComponent<Animator>();animator.runtimeAnimatorController=definition.animator;}
                var floor=new GameObject("Server attack telegraph");floor.transform.SetParent(transform,false);
                warning=floor.AddComponent<LineRenderer>();warning.useWorldSpace=false;warning.loop=false;
                warning.sharedMaterial=new Material(Shader.Find("Sprites/Default"));warning.sortingOrder=3;
                warning.startWidth=warning.endWidth=.035f;
                var fill=new GameObject("Translucent attack area");fill.transform.SetParent(transform,false);
                warningFill=fill.AddComponent<MeshRenderer>();warningFill.sharedMaterial=warning.sharedMaterial;warningFill.sortingOrder=2;
                warningMesh=new Mesh();warningMesh.MarkDynamic();fill.AddComponent<MeshFilter>().sharedMesh=warningMesh;
            }
            var next=(GiantState)(((int)packed%100)/10);
            if(state!=next)
            {
                state=next;phase=0;
                if(animator!=null && animator.HasState(0,Animator.StringToHash(state.ToString())))animator.Play(state.ToString(),0,0);
            }
            flags=extra;direction=(packed-Mathf.Floor(packed))*360;
        }
        void LateUpdate()
        {
            if(body==null || definition==null)return;
            phase+=Time.deltaTime;
            int pose=state==GiantState.PrepareAttack?1:state==GiantState.Attack?2:state==GiantState.Recovery?3:0;
            var overrideFrames=pose==0?definition.idleFrames:definition.attackFrames;
            if(animator==null)body.sprite=overrideFrames!=null && overrideFrames.Length>0?overrideFrames[(int)(phase*10)%overrideFrames.Length]:SpriteAt(definition.id,pose);
            body.color=(flags&(byte)EnemyFlag.Hit)!=0?new Color(1,.55f,.55f):(flags&(byte)EnemyFlag.Stunned)!=0?new Color(.65f,.8f,1):Color.white;
            body.transform.localPosition=Vector3.up*(state==GiantState.Chase?Mathf.Sin(Time.time*4+definition.id)*.018f:0);
            if(animator!=null)animator.speed=state==GiantState.Stunned?0:1;
            bool telegraph=state==GiantState.PrepareAttack;
            warning.enabled=telegraph;
            warningFill.enabled=telegraph;
            if(!telegraph)return;
            warning.startColor=warning.endColor=new Color(1,.25f,.12f,.45f+.2f*Mathf.Sin(phase*15));
            float size=Mathf.Max(.1f,transform.localScale.x),range=definition.attackRange/size;
            Vector2 forward=AbilityRunner.Rotate(Vector2.right,direction);
            if(definition.attackPattern==GiantPattern.Hermit)
            {
                Vector2 side=new Vector2(-forward.y,forward.x)*.35f;
                Vector2 end=forward*definition.chargeDistance/size;
                warning.positionCount=5;
                warning.SetPosition(0,side);warning.SetPosition(1,end+side);warning.SetPosition(2,end-side);warning.SetPosition(3,-side);warning.SetPosition(4,side);
            }
            else
            {
                bool cone=definition.attackPattern==GiantPattern.Crab;
                warning.positionCount=35;warning.SetPosition(0,cone?Vector2.zero:forward*range);
                for(int i=1;i<34;i++)warning.SetPosition(i,AbilityRunner.Rotate(forward,cone?-65+(i-1)*130f/32:(i-1)*360f/32)*range);
                warning.SetPosition(34,cone?Vector2.zero:forward*range);
            }
            int count=warning.positionCount;
            vertices[0]=Vector3.zero;
            for(int i=0;i<count;i++)vertices[i+1]=warning.GetPosition(i);
            bool first=warningMesh.vertexCount==0;
            warningMesh.vertices=vertices;
            if(first)
            {
                var triangles=new int[(count-1)*3];var colors=new Color[vertices.Length];
                for(int i=0;i<colors.Length;i++)colors[i]=new Color(1,.22f,.1f,.13f);
                for(int i=0;i<count-1;i++){triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=i+2;}
                warningMesh.colors=colors;warningMesh.triangles=triangles;
            }
        }
        void OnDestroy(){if(warning!=null)Destroy(warning.sharedMaterial);if(warningMesh!=null)Destroy(warningMesh);}
    }
}
