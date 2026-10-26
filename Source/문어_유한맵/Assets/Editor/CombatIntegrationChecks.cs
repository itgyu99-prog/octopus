using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace OctopusSurvivors.EditorTools
{
    [InitializeOnLoad]
    public static class CombatIntegrationChecks
    {
        const string Key="Octopus.CombatIntegrationChecks";
        static CombatIntegrationChecks()
        {
            EditorApplication.update+=()=> {
                if(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Logs/CombatIntegration.request"))return;
                File.Delete("Logs/CombatIntegration.request"); Run();
            };
            EditorApplication.playModeStateChanged+=state=> {
                if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
                    new GameObject("Combat integration checks").AddComponent<CombatIntegrationCapture>();
                if(state==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Key,false);
            };
        }
        [MenuItem("문어/통합 전투 개선 검증")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            Directory.CreateDirectory("Logs/CombatIntegration");
            File.WriteAllText("Logs/CombatIntegration/status.txt","RUNNING");
            SessionState.SetBool(Key,true); EditorApplication.EnterPlaymode();
        }
    }
    public sealed class CombatIntegrationCapture : MonoBehaviour
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        IEnumerator routine;
        float deadline;
        readonly List<string> results=new List<string>();
        int runtimeErrors;
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Private).Invoke(o,args);
        void Check(bool condition,string label) { if(!condition)throw new Exception(label);results.Add("PASS: "+label); }
        void Start() {Application.logMessageReceived+=OnLog;Debug.Log("COMBAT_INTEGRATION_BEGIN");routine=Run();deadline=Time.realtimeSinceStartup+180;}
        void OnLog(string message,string trace,LogType type) {if(type==LogType.Error || type==LogType.Exception)runtimeErrors++;}
        void OnDestroy() {Application.logMessageReceived-=OnLog;}
        void Update()
        {
            if(routine==null)return;
            try {
                if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Integration checks exceeded 180 seconds");
                if(routine.MoveNext())return;
                Finish(true);
            } catch(Exception e) {results.Add(e.ToString());Debug.LogException(e);Finish(false);}
        }
        void Finish(bool pass)
        {
            routine=null;
            File.WriteAllLines("Logs/CombatIntegration/results.txt",results);
            File.WriteAllText("Logs/CombatIntegration/status.txt",pass?"PASS":"FAIL");
            EditorApplication.isPlaying=false;
            if(Application.isBatchMode)EditorApplication.Exit(pass?0:2);
        }
        IEnumerator Run()
        {
            yield return null;
            SystemDataChecks.Run();Check(true,"existing system data and finite map regression checks");
            foreach(bool boss in new[]{false,true}) {
                Check(BeachGeometry.Walkable(Vector2.zero,.42f,boss),"player origin on beach, boss="+boss);
                Check(!BeachGeometry.Walkable(BeachGeometry.World(new Vector2(.5f,.1f),boss),.32f,boss),"cliff excluded, boss="+boss);
                Check(!BeachGeometry.Walkable(BeachGeometry.World(new Vector2(.5f,.95f),boss),.32f,boss),"ocean excluded, boss="+boss);
            }
            Check(BeachGeometry.BossArenaWalkable(Vector2.zero,GameConfig.PlayerRadius),"boss arena includes the central player start");
            var bossEdge=BeachGeometry.MoveBossArena(Vector2.zero,new Vector2(100,0),GameConfig.PlayerRadius);
            Check(BeachGeometry.BossArenaWalkable(bossEdge,GameConfig.PlayerRadius),"boss movement stays on sand near the outer beach edge");
            foreach(var beachPoint in new[]{BeachGeometry.World(new Vector2(.30f,.43f),true),
                BeachGeometry.World(new Vector2(.70f,.43f),true),BeachGeometry.World(new Vector2(.50f,.57f),true),
                new Vector2(0,7)}) {
                Check(BeachGeometry.BossArenaWalkable(beachPoint,GameConfig.PlayerRadius),"boss arena allows movement across all sand including in front of boss "+beachPoint);
                var reached=BeachGeometry.MoveBossArena(Vector2.zero,beachPoint,GameConfig.PlayerRadius);
                Check(Vector2.Distance(reached,beachPoint)<.25f,"player can cross open boss beach to "+beachPoint);
            }
            foreach(var recipe in GameplayRules.Data.recipes)Check(CombatHudArt.Combo(recipe.result)!=null,"icon for actual recipe "+recipe.result);
            for(int n=0;n<=4;n++)Check(Mathf.Abs(OctopusPlayer.ReadyFraction(10-2.5f*n,10)-.25f*n)<.001f,"cooldown quarter "+n);
            ushort port;
            using(var socket=new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback,0)))
                port=(ushort)((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
            typeof(GameBootstrap).GetMethod("StartHost",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(ushort)},null).Invoke(null,new object[]{port});
            while(GameSession.Instance==null || !GameSession.Instance.IsSpawned)yield return null;
            var s=GameSession.Instance;s.SelectStartingElement(ElementId.Water);
            while(s.HostElement.Value==0)yield return null;
            s.ToggleLocalReady();while(!s.CanStart())yield return null;s.RequestStart();
            while(!s.IsPlaying || OctopusPlayer.Local==null)yield return null;
            var p=OctopusPlayer.Local;p.enabled=s.enabled=false;p.Invincible.Value=true;
            p.transform.position=Vector2.zero;p.NetPos.Value=Vector2.zero;
            var actors=(List<SimActor>)typeof(GameSession).GetField("_actors",Private).GetValue(s);
            Call(s,"ClearFieldForNextStage");
            int spawned=0;
            for(int batch=0;batch<20;batch++) {
                Call(s,"SpawnWave",10);
                foreach(var a in actors)if(!a.Dead && a.Kind==ActorKind.Enemy) {
                    Check(FiniteBeach.Walkable(a.Pos,GameConfig.EnemyRadius),"spawn on current beach "+spawned);
                    Check(Vector2.Distance(a.Pos,p.Pos)>=6,"spawn distance "+spawned);a.Dead=true;spawned++;
                }
            }
            Check(spawned>=150,"normal spawn density retained over 200 attempts");
            Call(p,"TickAttributeCycle",.02f);
            Check(p.AttributeCooldownTotal.Value.x>0 && p.AttributeReadyFraction(0)<.001f,"real server attribute cooldown published");
            Call(p,"TickAttributeCycle",.25f);
            Check(p.AttributeReadyFraction(0)>0,"real cooldown recovery advances");
            var hud=GameHud.Instance;Check(hud!=null,"HUD created by game flow");
            var root=(GameObject)typeof(GameHud).GetField("_root",Private).GetValue(hud);
            var images=root.GetComponentsInChildren<Image>(true);
            string[] sockets={"Attribute 0","Attribute 1","Attribute 2","Attribute 3","Element 0","Element 1","Element 2","Cosmos or YinYang"};
            foreach(var socket in sockets) {
                Image frame=null;foreach(var image in images)if(image.name==socket){frame=image;break;}
                Check(frame!=null,"HUD socket exists: "+socket);
                Check(frame.GetComponent<Mask>()!=null && frame.transform.Find("Full color icon")!=null,"circular icon mask: "+socket);
            }
            p.Slot0.Value=(byte)ElementId.Water;p.Slot1.Value=(byte)ElementId.Fire;p.Slot2.Value=(byte)ElementId.Grass;p.Slot3.Value=(byte)ElementId.Electric;
            p.Combo.Value=(byte)ComboId.Fog;p.Combo1.Value=(byte)ComboId.Chili;p.Combo2.Value=(byte)ComboId.Tree;p.Special.Value=(byte)SpecialId.Cosmos;
            var comboTimers=(float[])typeof(OctopusPlayer).GetField("comboCooldowns",Private).GetValue(p);
            for(int i=0;i<comboTimers.Length;i++)comboTimers[i]=5f;
            Call(p,"TickComboCycle",.02f);
            for(int i=0;i<3;i++)Check(Mathf.Abs(p.ComboCooldownTotal.Value[i]-4.98f)<.001f && p.ComboReadyFraction(i)>0,"combo cooldown recovery "+i);
            p.Hp.Value=50;
            Call(hud,"Update");
            var hpImage=(Image)typeof(GameHud).GetField("combatHp",Private).GetValue(hud);
            Check(Mathf.Abs(hpImage.fillAmount-.5f)<.001f,"HUD health follows actual 50/100 HP");
            var fieldCamera=Camera.main??FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            Check(fieldCamera!=null,"field camera remains available after starting the run");
            var cameraFollow=fieldCamera.GetComponent<PlayerCameraFollow>();
            Check(cameraFollow!=null,"field camera follow component remains available");
            foreach(var edge in new[]{new Vector2(0,-100),new Vector2(-100,-10),new Vector2(100,-10),new Vector2(0,100)}) {
                var point=FiniteBeach.Clamp(edge,GameConfig.PlayerRadius);
                p.transform.position=point;p.NetPos.Value=point;
                var blocked=FiniteBeach.Move(point,point+edge.normalized*100,GameConfig.PlayerRadius);
                Check(FiniteBeach.Walkable(blocked,GameConfig.PlayerRadius),"player movement still respects beach boundary "+edge);
                float settleUntil=Time.realtimeSinceStartup+3;
                do { Call(cameraFollow,"LateUpdate");yield return null; }
                while(Vector2.Distance(fieldCamera.transform.position,point)>.1f && Time.realtimeSinceStartup<settleUntil);
                var viewport=fieldCamera.WorldToViewportPoint(point);
                bool bottomEdge=edge.y<-50;
                bool visible=viewport.x>=.05f && viewport.x<=.95f && viewport.y>=.18f && viewport.y<=.95f;
                Check(visible && (!bottomEdge || Mathf.Abs(viewport.y-.595f)<.035f),"camera keeps shoreline player visible above the bottom HUD "+edge+" viewport="+viewport);
                Call(hud,"Update");Call(s,"RenderLocal");yield return null;
                ScreenCapture.CaptureScreenshot("Logs/CombatIntegration/camera-edge-"+(edge.y<-50?"bottom":edge.y>50?"top":edge.x<0?"left":"right")+".png");yield return null;
            }
            var combatRect=(RectTransform)typeof(GameHud).GetField("combatRoot",Private).GetValue(hud);
            Check(Mathf.Abs(combatRect.anchoredPosition.y-14)<.001f && combatRect.localScale.x>0 && combatRect.localScale.x<=1,"HUD anchored above the bottom screen edge and scaled to fit");
            p.transform.position=Vector2.zero;p.NetPos.Value=Vector2.zero;
            fieldCamera.transform.position=new Vector3(0,0,-10);
            Call(s,"RenderLocal");yield return null;
            ScreenCapture.CaptureScreenshot("Logs/CombatIntegration/field-hud.png");yield return null;
            p.Hp.Value=100;
            Call(s,"ClearFieldForNextStage");s.Stage.Value=5;Call(s,"SpawnBoss",5);
            var bossActor=actors.Find(a=>!a.Dead && a.Kind==ActorKind.Boss);
            float hp=bossActor.Hp;Call(s,"ApplyDamage",bossActor,10f,StatusMask.Burn,1f,10f);
            Check(bossActor.Hp==hp,"protected body rejects damage");
            var limbs=(SimActor[])typeof(GameSession).GetField("octopusArms",Private).GetValue(s);
            for(int i=0;i<6;i++) {
                Check(BeachGeometry.Walkable(OctopusBossPattern.RestRoot(i),2.8f,true),"grounded rest root "+i);
                Call(s,"ApplyDamage",limbs[i],100000f,StatusMask.None,0f,0f);
            }
            Check(s.OctopusBoss.Value.Vulnerable==10,"all six arms expose body for 10 seconds");
            Call(s,"TickBossPattern",bossActor,.01f,0f);
            Check(Vector2.Distance(bossActor.Pos,OctopusBossPattern.CoreCenter(true))<.001f,"body simulation matches lowered portrait");
            Call(s,"ApplyDamage",bossActor,13f,StatusMask.None,0f,0f);Check(Mathf.Abs(bossActor.Hp-hp+13)<.01f,"exposed body takes damage");
            Check(OctopusCoreShape.Distance(bossActor.Pos,bossActor.Pos)<1,"portrait center hit mask");
            Check(OctopusCoreShape.Distance(bossActor.Pos+Vector2.one*30,bossActor.Pos)>20,"transparent outside portrait rejects hits");
            Call(s,"TickBossPattern",bossActor,10f,0f);Check(s.OctopusBoss.Value.LivingArms==6 && s.OctopusBoss.Value.Vulnerable==0,"arms restore and body relocks");
            for(int seed=1;seed<=10;seed++)for(int strike=0;strike<30;strike++) {
                var target=OctopusBossPattern.RelayTarget((uint)seed,strike);
                for(int n=0;n<16;n++) {
                    var edge=target+Vector2.Scale(new Vector2(Mathf.Cos(n*Mathf.PI/8),Mathf.Sin(n*Mathf.PI/8)),new Vector2(2.6f,1.9f));
                    Check(BeachGeometry.Walkable(edge,0,true),"relay footprint on sand "+seed+"/"+strike+"/"+n);
                }
            }
            for(int half=0;half<4;half++)for(int arm=0;arm<6;arm++) {
                var target=OctopusBossPattern.HalfTarget(12345,half,arm,0);var radii=OctopusBossPattern.HalfRadii(half);
                for(int n=0;n<16;n++) {
                    var edge=target+Vector2.Scale(new Vector2(Mathf.Cos(n*Mathf.PI/8),Mathf.Sin(n*Mathf.PI/8)),radii);
                    Check(BeachGeometry.Walkable(edge,0,true),"half footprint on sand "+half+"/"+arm+"/"+n);
                }
            }
            s.OctopusBoss.Value=new OctopusBossSnapshot {Pattern=2,Time=1.2f,Seed=12345};Call(s,"RenderLocal");yield return null;
            var visual=FindAnyObjectByType<OctopusBossVisual>();Check(visual!=null,"boss visual active");
            visual.Preview(s.OctopusBoss.Value);Call(fieldCamera.GetComponent<PlayerCameraFollow>(),"LateUpdate");
            Call(hud,"Update");yield return null;
            ScreenCapture.CaptureScreenshot("Logs/CombatIntegration/boss-hud.png");yield return null;
            p.Combo.Value=(byte)ComboId.Lemon;p.Combo1.Value=(byte)ComboId.Soda;p.Combo2.Value=(byte)ComboId.BlueFlame;
            Call(hud,"Update");yield return null;
            ScreenCapture.CaptureScreenshot("Logs/CombatIntegration/other-elements.png");yield return null;
            var sizeChecks=(IEnumerator)typeof(OctopusBossCapture).GetMethod("VerifyLiveGameViewSizes",BindingFlags.Static|BindingFlags.NonPublic)
                .Invoke(null,new object[]{ArenaBuilder.BeachRenderer,fieldCamera});
            try { while(sizeChecks.MoveNext()) {
                int previousWidth=Screen.width, previousHeight=Screen.height;
                yield return null;
                if(Screen.width!=previousWidth || Screen.height!=previousHeight) continue;
                Canvas.ForceUpdateCanvases();Call(hud,"Update");Canvas.ForceUpdateCanvases();
                var hudRect=(RectTransform)typeof(GameHud).GetField("combatRoot",Private).GetValue(hud);
                var corners=new Vector3[4];hudRect.GetWorldCorners(corners);
                Check(corners[0].x>=-1 && corners[2].x<=Screen.width+1 && corners[0].y>=-1 && corners[2].y<=Screen.height+1,
                    "combat HUD stays inside screen "+Screen.width+"x"+Screen.height+" corners="+corners[0]+"/"+corners[2]);
            }} finally { (sizeChecks as IDisposable)?.Dispose(); }
            Check(true,"live 4:3, 16:9, 21:9 and portrait camera and HUD checks");
            for(int half=0;half<4;half++)for(int f=0;f<6;f++) {
                var snap=new OctopusBossSnapshot {Pattern=4,Half=(byte)half,Time=1.65f+f*.16f,Seed=12345};
                s.OctopusBoss.Value=snap;visual.Preview(snap);
                yield return null;
                if(f==2)ScreenCapture.CaptureScreenshot("Logs/CombatIntegration/half-"+half+".png");
            }
            Call(s,"KillEnemy",bossActor);Check(!s.BossAlive.Value && s.SpecialUnlocked.Value,"defeat cleanup and original special unlock");
            int rewards=0;
            foreach(var actor in actors)if(!actor.Dead && ElementRules.IsSpecialPickup(actor.Kind)) {
                rewards++;Check(FiniteBeach.Walkable(actor.Pos,.4f),"special reward reachable after return to normal beach");
            }
            Check(rewards==2,"two original boss rewards retained");
            Check(runtimeErrors==0,"no runtime error or exception during integration");
            GameBootstrap.LeaveToLobby();
            yield return null;
        }
    }
}
