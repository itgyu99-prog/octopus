using System.Collections.Generic;
using UnityEngine;

namespace OctopusSurvivors
{
    public sealed class HdSpellVisual : MonoBehaviour
    {
        static readonly Dictionary<string,Sprite[]> sheets=new Dictionary<string,Sprite[]>();
        SpriteRenderer image,echo;
        LineRenderer footprint;
        ActorKind kind;
        float age,duration,offset;
        Vector3 previous;
        bool initialized;
        LineRenderer lightningGlow, lightningCore;
        readonly SpriteRenderer[] lightningTextureSegments = new SpriteRenderer[8];
        readonly Vector3[] lightningPoints = new Vector3[9];
        SpriteRenderer lightningImpact;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => sheets.Clear();

        public static Sprite Frame(string name,int rows,int index)
        {
            if(!sheets.TryGetValue(name,out var frames) || frames == null || frames.Length != rows * 4 ||
                frames[Mathf.Clamp(index, 0, frames.Length - 1)] == null ||
                frames[Mathf.Clamp(index, 0, frames.Length - 1)].texture == null)
            {
                var texture=Resources.Load<Texture2D>("Vfx/"+name);if(texture==null)return null;
                frames=new Sprite[rows*4];int w=texture.width/4,h=texture.height/rows;
                // Authored row gutters are measured from the reviewed atlas, not assumed uniform.
                float[] gutters=name=="ComboAtlas"?new[]{0f,.19f,.345f,.51f,.675f,.83f,1f}:
                    name=="LegacyAtlas"?new[]{0f,.178f,.316f,.443f,.596f,.735f,.863f,1f}:
                    name=="DetailAtlas"?new[]{0f,.31f,.515f,.759f,1f}:
                    new[]{0f,.25f,.505f,.769f,1f};
                for(int i=0;i<frames.Length;i++)
                {
                    int r=i/4;float bottom=(1-gutters[r+1])*texture.height, height=(gutters[r+1]-gutters[r])*texture.height;
                    float[] columns=name=="ComboAtlas"?new[]{0f,.2734375f,.5234375f,.7734375f,1f}:new[]{0f,.25f,.5f,.75f,1f};
                    int c=i%4;
                    frames[i]=Sprite.Create(texture,new Rect(columns[c]*texture.width,bottom,(columns[c+1]-columns[c])*texture.width,height),new Vector2(.5f,.5f),Mathf.Max(w,h));
                }
                sheets[name]=frames;
            }
            return frames[Mathf.Clamp(index,0,frames.Length-1)];
        }
        public static bool Supports(ActorKind k)=>Mapping(k,out _,out _,out _);
        static bool Mapping(ActorKind k,out string name,out int rows,out int row)
        {
            name="ElementAtlas";rows=4;row=0;
            switch(k)
            {
                case ActorKind.FlameAura:case ActorKind.Tornado:case ActorKind.Firework:case ActorKind.Explosion:return true;
                case ActorKind.RadialCurrent:case ActorKind.ElectricWave:case ActorKind.LightningBolt:row=1;return true;
                case ActorKind.Vine:case ActorKind.ThornPatch:case ActorKind.ThornShot:row=2;return true;
            }
            name="DetailAtlas";rows=4;
            switch(k)
            {
                case ActorKind.Fruit:row=0;return true;
                case ActorKind.HomingFish:case ActorKind.Fish:row=1;return true;
                case ActorKind.Seed:row=2;return true;
                case ActorKind.VolcanoRock:row=3;return true;
            }
            name="ComboAtlas";rows=6;
            switch(k)
            {
                case ActorKind.Mist:case ActorKind.SteamField:case ActorKind.Fog:row=0;return true;
                case ActorKind.ChiliField:case ActorKind.ChiliShot:case ActorKind.Pepper:row=1;return true;
                case ActorKind.FruitTree:case ActorKind.GrownTree:case ActorKind.Tree:row=2;return true;
                case ActorKind.SodaWave:row=3;return true;
                case ActorKind.LemonWheel:case ActorKind.LemonShot:row=4;return true;
                case ActorKind.ChainArc:row=5;return true;
            }
            name="LegacyAtlas";rows=7;
            switch(k)
            {
                case ActorKind.Volcano:case ActorKind.LavaImpact:case ActorKind.LavaPool:row=0;return true;
                case ActorKind.SeaweedPlant:row=1;return true;
                case ActorKind.MossShot:case ActorKind.BounceRock:row=2;return true;
                case ActorKind.MicrowaveCharge:case ActorKind.MicrowaveBlast:row=3;return true;
                case ActorKind.MowerBlade:row=4;return true;
                case ActorKind.Jellyfish:case ActorKind.CurrentFan:row=5;return true;
                case ActorKind.MagnetPair:case ActorKind.MagnetBlast:row=6;return true;
            }
            return false;
        }
        public void Configure(ActorKind value,float serverAge,float life)
        {
            kind=value;duration=life;
            if(image==null)
            {
                image=Layer("Textured skill",12);echo=Layer("Textured afterglow",11);
                offset=Mathf.Repeat(transform.position.x*1.37f+transform.position.y*.73f,1);
                if (kind == ActorKind.FruitTree || kind == ActorKind.FlameAura)
                {
                    var go = new GameObject("Attack footprint"); go.transform.SetParent(transform, false);
                    footprint = go.AddComponent<LineRenderer>();
                    footprint.useWorldSpace = false;
                    footprint.sharedMaterial = image.sharedMaterial;
                    footprint.sortingOrder = 3;
                    footprint.widthMultiplier = .025f;
                    bool tree = kind == ActorKind.FruitTree;
                    footprint.startColor = footprint.endColor = tree ? new Color(.5f, 1f, .3f, .25f) : new Color(1f, .5f, .15f, .3f);
                    footprint.loop = true;
                    footprint.positionCount = tree ? 65 : 26;
                    for (int i = 0; i < footprint.positionCount; i++)
                    {
                        if (!tree && i == 0) { footprint.SetPosition(i, Vector3.zero); continue; }
                        float angle = (tree ? i * 360f / 65 : -30f + (i - 1) * 60f / 24) * Mathf.Deg2Rad;
                        footprint.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0));
                    }
                }
            }
            if(serverAge>0)age=serverAge;
            if(!initialized)
            {
                initialized=true;previous=transform.position;
                if(kind==ActorKind.MicrowaveBlast || kind==ActorKind.MagnetBlast || kind==ActorKind.LavaImpact)
                    PlayerCameraFollow.StrongImpact(transform.position);
            }
        }
        SpriteRenderer Layer(string name,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);
            var sr=go.AddComponent<SpriteRenderer>();sr.sortingOrder=order;return sr;
        }

        LineRenderer LightningLine(string label, float width, int order)
        {
            var go = new GameObject(label); go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = image.sharedMaterial;
            line.widthMultiplier = width;
            line.sortingOrder = order;
            line.numCapVertices = 2;
            return line;
        }

        void DrawSplashLightning()
        {
            // Actor transform already encodes the midpoint, half-length and direction.
            // Dedicated geometry keeps the bolt endpoints on the actual hit targets.
            image.enabled = echo.enabled = false;
            if (lightningCore == null)
            {
                lightningGlow = LightningLine("Lightning glow", .16f, 14);
                lightningCore = LightningLine("Lightning core", .035f, 16);
                for (int i = 0; i < lightningTextureSegments.Length; i++)
                    lightningTextureSegments[i] = Layer("Original blue flame texture", 15);
                lightningImpact = Layer("Lightning splash impact", 17);
            }
            float fade = Mathf.Clamp01((duration - age) / .25f);
            float visualScale = Mathf.Max(.1f, UpgradeTuning.Data.blueFlameVisualScale);
            lightningGlow.widthMultiplier = .055f * visualScale;
            lightningCore.widthMultiplier = .012f * visualScale;
            float lengthScale = Mathf.Max(.15f, transform.lossyScale.x);
            for (int i = 0; i < lightningPoints.Length; i++)
            {
                float u = i / 8f;
                float jitter = i == 0 || i == 8 ? 0f :
                    Mathf.Sin(i * 8.3f + offset * 13f) * .09f * visualScale / lengthScale;
                lightningPoints[i] = new Vector3(Mathf.Lerp(-1f, 1f, u), jitter, 0f);
            }
            lightningGlow.positionCount = lightningCore.positionCount = lightningPoints.Length;
            lightningGlow.SetPositions(lightningPoints);
            lightningCore.SetPositions(lightningPoints);
            lightningGlow.startColor = lightningGlow.endColor = new Color(.15f, .55f, 1f, fade * .55f);
            lightningCore.startColor = lightningCore.endColor = new Color(.8f, 1f, 1f, fade);
            // Reuse the original ComboAtlas blue-flame animation along the bent path.
            // Length follows the link; transverse thickness stays constant in world units.
            var textureFrame = Frame("ComboAtlas", 6, 20 + (int)((age + offset) * 14f) % 4);
            if (textureFrame == null) return;
            for (int i = 0; i < lightningTextureSegments.Length; i++)
            {
                var piece = lightningTextureSegments[i];
                Vector3 delta = lightningPoints[i + 1] - lightningPoints[i];
                piece.sprite = textureFrame;
                piece.transform.localPosition = (lightningPoints[i] + lightningPoints[i + 1]) * .5f;
                piece.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                piece.transform.localScale = new Vector3(delta.magnitude * 1.08f / textureFrame.bounds.size.x,
                    .24f * visualScale / (lengthScale * textureFrame.bounds.size.y), 1f);
                piece.color = new Color(1f, 1f, 1f, fade * .9f);
            }
            lightningImpact.sprite = textureFrame;
            lightningImpact.transform.localPosition = Vector3.right;
            lightningImpact.transform.localScale = Vector3.one * .35f * visualScale /
                (lengthScale * Mathf.Max(textureFrame.bounds.size.x, textureFrame.bounds.size.y));
            lightningImpact.color = new Color(1f, 1f, 1f, fade);
        }

        void LateUpdate()
        {
            if(image==null || !Mapping(kind,out string name,out int rows,out int row))return;
            age+=Time.deltaTime;
            if (kind == ActorKind.ChainArc) { DrawSplashLightning(); return; }
            int frame=(int)((age+offset)*14)%4;
            bool tree = kind == ActorKind.FruitTree;
            if (tree) frame = 0; // Stable trunk/canopy; only the falling fruit animates.
            bool shortEffect=duration>0 && duration<.65f;
            if(shortEffect || kind==ActorKind.Fruit || kind==ActorKind.Seed)frame=Mathf.Min(3,(int)(age/Mathf.Max(.01f,duration)*4));
            image.sprite=Frame(name,rows,row*4+frame);
            echo.sprite=Frame(name,rows,row*4+(frame+3)%4);
            Vector3 delta=transform.position-previous;previous=transform.position;
            bool directional=ElementEffectVisual.IsDirectional(kind);
            if(!directional && !tree && delta.sqrMagnitude>.00001f)
                image.transform.rotation=echo.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            float scale=2f,alpha=.85f;
            Vector3 center=Vector3.zero;
            if(kind==ActorKind.Fruit)
            {
                center=Vector3.up*Mathf.Max(0,1-age/Mathf.Max(.01f,duration))*3;
                scale=TreeAbilityTuning.Data.treeVisualScale * TreeAbilityTuning.Data.fruitVisualScaleRatio;
            }
            if(tree)image.transform.localRotation=echo.transform.localRotation=Quaternion.identity;
            if(kind==ActorKind.FlameAura){scale=1.05f;center=Vector3.right*.48f;}
            if(kind==ActorKind.ChainArc){scale=2;alpha=1;}
            if(kind==ActorKind.RadialCurrent){scale=2*Mathf.Clamp01(age/Mathf.Max(.01f,duration));alpha=.85f;}
            if(kind==ActorKind.Mist || kind==ActorKind.SteamField || kind==ActorKind.Fog){scale=2;alpha=.3f;image.sortingOrder=2;echo.sortingOrder=1;}
            if(kind==ActorKind.FruitTree || kind==ActorKind.GrownTree)scale=TreeAbilityTuning.Data.treeVisualScale;
            if(kind==ActorKind.ChiliShot || kind==ActorKind.LemonShot || kind==ActorKind.MossShot)scale=2.7f;
            if(kind==ActorKind.Jellyfish || kind==ActorKind.CurrentFan){scale=1.2f;center=Vector3.right*.4f;}
            if(kind==ActorKind.Tornado || kind==ActorKind.LemonWheel)
                image.transform.localRotation=Quaternion.Euler(0,0,age*220);
            if(kind==ActorKind.ThornPatch || kind==ActorKind.ChiliField){image.sortingOrder=3;alpha=.65f;}
            float fade=duration>0?Mathf.Clamp01((duration-age)/Mathf.Min(.2f,duration*.35f)):1;
            image.transform.localPosition=center;image.transform.localScale=Vector3.one*scale;
            image.color=new Color(1,1,1,alpha*fade);
            echo.transform.localPosition=center-Vector3.right*.06f;
            echo.transform.localScale=Vector3.one*scale*1.05f;
            echo.color=new Color(1,1,1,alpha*.15f*fade);
            echo.enabled = !tree;
        }
    }
    public sealed class ElementHitVisual : MonoBehaviour
    {
        SpriteRenderer flash;
        float remaining;
        bool wasHit;
        public void Configure(byte flags,ElementId element)
        {
            bool hit=(flags&(byte)EnemyFlag.Hit)!=0;
            if(hit && !wasHit)
            {
                if(flash==null){var go=new GameObject("Impact fragments");go.transform.SetParent(transform,false);flash=go.AddComponent<SpriteRenderer>();flash.sortingOrder=16;}
                int frame=element==ElementId.Fire?12:element==ElementId.Grass?14:element==ElementId.Water?15:13;
                flash.sprite=HdSpellVisual.Frame("ElementAtlas",4,frame);remaining=.16f;
            }
            wasHit=hit;
        }
        void LateUpdate()
        {
            if(flash==null)return;remaining-=Time.deltaTime;
            flash.enabled=remaining>0;flash.color=new Color(1,1,1,Mathf.Clamp01(remaining/.16f));
            flash.transform.localScale=Vector3.one*(.7f+(1-Mathf.Clamp01(remaining/.16f))*.3f);
        }
    }
}
