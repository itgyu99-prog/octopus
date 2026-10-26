using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OctopusSurvivors.EditorTools
{
    [InitializeOnLoad]
    public static class CelestialFieldChecks
    {
        const string Key="Octopus.CelestialFieldChecks";
        static CelestialFieldChecks()
        {
            EditorApplication.playModeStateChanged+=s=>
            {
                if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
                    new GameObject("Celestial field gameplay review").AddComponent<CelestialFieldCapture>();
                if(s==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key,false))
                {
                    SessionState.SetBool(Key,false);
                    if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Key+".Exit",2));
                }
            };
        }
        [MenuItem("문어/우주 전필드 개선 검증 및 화면 저장")]
        public static void Run()
        {
            Directory.CreateDirectory("Logs/CelestialField");File.WriteAllText("Logs/CelestialField/status.txt","RUNNING");
            if(Application.isBatchMode)EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
        }
        internal static void Finish(bool pass)
        {
            File.WriteAllText("Logs/CelestialField/status.txt",pass?"PASS":"FAIL");
            SessionState.SetInt(Key+".Exit",pass?0:2);EditorApplication.isPlaying=false;
        }
    }
    public sealed class CelestialFieldCapture:MonoBehaviour
    {
        const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
        readonly List<string> results=new List<string>();
        IEnumerator run;float deadline;bool done;int errors,assertions;
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Hidden).Invoke(o,args);
        static List<SimActor> Actors(GameSession s)=>(List<SimActor>)typeof(GameSession).GetField("_actors",Hidden).GetValue(s);
        void Check(bool ok,string label){assertions++;if(!ok)throw new Exception(label);}
        void Start(){Application.logMessageReceived+=OnLog;deadline=Time.realtimeSinceStartup+300;run=Run();}
        void OnLog(string m,string stack,LogType t){if(t==LogType.Error || t==LogType.Exception)errors++;}
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
        void Update()
        {
            if(done || run==null)return;
            try
            {
                if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Celestial capture timed out");
                if(run.MoveNext())return;Finish(true);
            }
            catch(Exception e){results.Add(e.ToString());Debug.LogException(e);Finish(false);}
        }
        void Finish(bool pass)
        {
            done=true;Time.captureDeltaTime=0;results.Add("Assertions: "+assertions);
            File.WriteAllLines("Logs/CelestialField/results.txt",results);CelestialFieldChecks.Finish(pass);
        }
        void Tick(GameSession s,float dt)
        {
            foreach(var a in Actors(s).ToArray())if(!a.Dead && CelestialArt.Supports(a.Kind))Call(s,"TickCelestial",a,dt);
        }
        SimActor Enemy(GameSession s,Vector2 p,float size=.75f)
        {
            var a=(SimActor)Call(s,"Create",ActorKind.Enemy,p,size);
            a.Hp=a.MaxHp=200000;a.Element=ElementId.Water;return a;
        }
        IEnumerator Run()
        {
            yield return null;
            ushort port;
            using(var socket=new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback,0)))
                port=(ushort)((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
            typeof(GameBootstrap).GetMethod("StartHost",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(ushort)},null).Invoke(null,new object[]{port});
            while(GameSession.Instance==null || !GameSession.Instance.IsSpawned)yield return null;
            var s=GameSession.Instance;s.SelectStartingElement(ElementId.Water);
            while(s.HostElement.Value==0)yield return null;
            s.ToggleLocalReady();while(!s.CanStart())yield return null;s.RequestStart();
            while(!s.IsPlaying || OctopusPlayer.Local==null)yield return null;
            var p=OctopusPlayer.Local;s.enabled=p.enabled=false;p.Invincible.Value=true;
            p.transform.position=Vector3.zero;p.NetPos.Value=Vector2.zero;
            p.Slot0.Value=p.Slot1.Value=p.Slot2.Value=p.Slot3.Value=0;
            Call(s,"ClearFieldForNextStage");Actors(s).Clear();
            var camera=Camera.main;camera.GetComponent<PlayerCameraFollow>().enabled=false;
            Time.captureDeltaTime=1f/30f;
            foreach(bool boss in new[]{false,true})
            {
                var bounds=CelestialField.BoundsFor(boss);var used=new Bounds();bool first=true;
                for(int i=0;i<CosmosSequence.StarCount(6);i++)
                {
                    var point=CelestialField.Point(i,CosmosSequence.StarCount(6),boss);
                    Check(BeachGeometry.Walkable(point,.1f,boss),"star position must be on sand, boss="+boss+" index="+i);
                    if(first){used=new Bounds(point,Vector3.zero);first=false;}else used.Encapsulate(point);
                }
                Check(used.size.x>bounds.size.x*.8f && used.size.y>bounds.size.y*.8f,"stars span the full normal/boss field");
                for(int i=0;i<YinYangVisual.GroundCount;i++)
                    Check(BeachGeometry.Walkable(CelestialField.Point(i,YinYangVisual.GroundCount,boss),.1f,boss),"ground commas stay on sand");
            }
            results.Add("PASS: normal and boss field coverage; stars and ground commas stay on sand.");
            s.CastBasicAbility(p,ElementId.Fire,1);
            var orb=Actors(s).Find(a=>a.Kind==ActorKind.FlameAura);
            Check(Mathf.Abs(orb.Damage-31.2f)<.001f,"fire direct damage increased by 30 percent");
            var fireTarget=Enemy(s,Vector2.zero);orb.Age=.02f;Call(s,"TickFireOrb",orb,.02f);
            Check(fireTarget.Hp<fireTarget.MaxHp,"fire applies server damage");
            Check(Mathf.Abs(UpgradeTuning.Data.fireOrbBurnDps-5.2f)<.001f,"fire burn damage increased by 30 percent");
            results.Add("PASS: fire server projectile damage 31.2 DPS; burn tuning 5.2 DPS.");
            Actors(s).Clear();s.CastCelestial(Vector2.zero,p,SpecialId.Cosmos,1);
            var positions=Actors(s).FindAll(a=>a.Kind==ActorKind.CosmicStar).ConvertAll(a=>a.Anchor);
            Actors(s).Clear();camera.transform.position=new Vector3(100,-50,-10);camera.orthographicSize=4;
            s.CastCelestial(new Vector2(15,-10),p,SpecialId.Cosmos,1);
            var stars=Actors(s).FindAll(a=>a.Kind==ActorKind.CosmicStar);
            for(int i=0;i<stars.Count;i++)Check(stars[i].Anchor==positions[i],"star position independent of camera and caster");
            Actors(s).Clear();var target=Enemy(s,CelestialField.Bounds.center);
            s.CastCelestial(Vector2.zero,p,SpecialId.Cosmos,1);
            var dusk=Actors(s).Find(a=>a.Kind==ActorKind.CosmicDusk);bool impact=false,flight=false;
            var inspected=new HashSet<SimActor>();var headings=new HashSet<int>();
            for(int tick=0;tick<420;tick++)
            {
                Tick(s,.025f);
                foreach(var a in Actors(s))
                {
                    flight|=a.Kind==ActorKind.CosmicMeteor && !a.Dead;
                    if(a.Kind==ActorKind.CosmicMeteor && a.Age<.026f)
                        headings.Add(Mathf.FloorToInt(Mathf.Repeat(Mathf.Atan2(a.Vel.y,a.Vel.x),Mathf.PI*2)/(Mathf.PI*.25f)));
                    if(a.Kind==ActorKind.CosmicImpact && inspected.Add(a)){impact=true;Check(a.Size<=2.1f,"impact is monster sized");}
                }
            }
            Check(flight && impact,"stars launch and impact");
            Check(headings.Count>=6,"stars launch freely in multiple directions instead of sharing a fixed arc");
            Check(Mathf.Abs(target.MaxHp-target.Hp-3510)<2,"all field-wide stars preserve total damage and hit a target");
            Check(dusk.Dead && !Actors(s).Exists(a=>!a.Dead && CosmosVisual.Supports(a.Kind)),"field brightness restores and every cosmic actor expires");
            Check(CosmosArt.Impact!=CosmosArt.Star,"dedicated impact asset loaded");
            results.Add("PASS: camera-independent positions; all stars hit; total damage 3510; small impacts; darkness restores.");
            Actors(s).Clear();
            for(int i=0;i<32;i++)Enemy(s,CelestialField.Point(i,32));
            var field=CelestialField.Bounds;
            camera.aspect=16f/9;camera.orthographicSize=Mathf.Max(field.extents.y,field.extents.x/camera.aspect)+2;
            camera.transform.position=new Vector3(field.center.x,field.center.y,-10);
            Call(s,"RenderLocal");yield return null;
            Capture(camera,"baseline");s.CastCelestial(Vector2.zero,p,SpecialId.Cosmos,1);
            Directory.CreateDirectory("Logs/CelestialField/Frames");
            bool trail=false;
            for(int frame=0;frame<255;frame++)
            {
                Tick(s,1f/30);Call(s,"RenderLocal");yield return null;
                if(frame==25)Capture(camera,"cosmos-soft-spread");
                if(frame==73)Capture(camera,"cosmos-whole-field");
                if(frame==94)Capture(camera,"cosmos-flight");
                if(frame==139)Capture(camera,"cosmos-impacts");
                if(frame%3==0 && frame<180)Capture(camera,"Frames/frame-"+(frame/3).ToString("D3"),960,540);
                foreach(var line in UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))
                    if(line.gameObject.name=="Curved starlight wake" && line.enabled && line.positionCount>3)trail=true;
            }
            Check(trail,"actual world-space trail history renders during gameplay");
            Capture(camera,"cosmos-restored");
            Actors(s).Clear();Call(s,"RenderLocal");yield return null;
            var smallEnemy=Enemy(s,field.center,.75f);
            var burstPreview=new GameObject("Dedicated cosmic impact size review");burstPreview.transform.position=smallEnemy.Pos;
            var burstRenderer=burstPreview.AddComponent<SpriteRenderer>();
            camera.orthographicSize=4;camera.transform.position=new Vector3(field.center.x,field.center.y,-10);
            Call(s,"RenderLocal");yield return null;
            for(int i=0;i<6;i++)
            {
                CelestialArt.Show(burstRenderer,ActorKind.CosmicImpact,.02f+i*.065f,.46f,1.2f,0);
                yield return null;Capture(camera,"impact-detail-"+i);
            }
            Destroy(burstPreview);Actors(s).Clear();Call(s,"RenderLocal");yield return null;
            camera.orthographicSize=Mathf.Max(field.extents.y,field.extents.x/camera.aspect)+2;
            s.CastCelestial(p.Pos,p,SpecialId.YinYang,1);
            var mist=Actors(s).Find(a=>a.Kind==ActorKind.YinYangMist);var fixedCenter=mist.Pos;
            for(int frame=0;frame<104;frame++)
            {
                p.transform.position=new Vector3(frame*.12f,0,0);p.NetPos.Value=p.transform.position;
                Tick(s,1f/30);Call(s,"RenderLocal");yield return null;
                Check(mist.Pos==fixedCenter,"field ground eruption origin never follows caster");
                if(frame==29)Capture(camera,"yin-yang-field-1");
                if(frame==47)Capture(camera,"yin-yang-whole-field");
                if(frame==65)Capture(camera,"yin-yang-field-3");
            }
            var preview=new GameObject("Ground eruption review");preview.transform.position=YinYangVisual.MistFieldBounds.center;
            var sr=preview.AddComponent<SpriteRenderer>();float t=YinYangVisual.GroundBirth(0)+.1f;
            CelestialArt.Show(sr,ActorKind.YinYangMist,t,3.26f,YinYangVisual.MistFieldBounds.size.x,0);
            int start=YinYangVisual.MistColumns*YinYangVisual.MistRows;
            var rising=preview.transform.GetChild(start+1).GetComponent<SpriteRenderer>();
            Check(rising.enabled && rising.sprite==YinYangArt.Comma(true),"world comma rises from ground");
            var old=rising.transform.position;p.transform.position=Vector3.right*30;
            CelestialArt.Show(sr,ActorKind.YinYangMist,t,3.26f,YinYangVisual.MistFieldBounds.size.x,0);
            Check(Vector3.Distance(old,rising.transform.position)<.001f,"ground comma ignores caster movement");
            CelestialArt.Show(sr,ActorKind.YinYangMist,YinYangVisual.GroundBirth(0)+.31f,3.26f,YinYangVisual.MistFieldBounds.size.x,0);
            Check(!rising.enabled && preview.transform.GetChild(start+2).GetComponent<SpriteRenderer>().enabled,"ground comma bursts naturally after rising");
            Destroy(preview);Actors(s).Clear();Call(s,"RenderLocal");yield return null;
            Check(errors==0,"no Unity runtime errors");
            results.Add("PASS: actual gameplay rendering and trails; field-wide comma rising/burst phases; zero runtime errors.");
            GameBootstrap.LeaveToLobby();yield return null;
        }
        static void Capture(Camera camera,string name,int width=1600,int height=900)
        {
            var old=RenderTexture.active;var rt=RenderTexture.GetTemporary(width,height,24);
            var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();
                File.WriteAllBytes("Logs/CelestialField/"+name+".png",tex.EncodeToPNG());
            }
            finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Destroy(tex);}
        }
    }
}
