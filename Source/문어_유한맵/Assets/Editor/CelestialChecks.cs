using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace OctopusSurvivors.EditorTools
{
    [InitializeOnLoad]
    public static class CelestialChecks
    {
        const string Key="Octopus.CelestialChecks";
        static CelestialChecks()
        {
            EditorApplication.update+=()=>
            {
                if(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Logs/celestial.request"))return;
                File.Delete("Logs/celestial.request");Run();
            };
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
                    new GameObject("Celestial integration checks").AddComponent<CelestialCapture>();
                if(state==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Key,false);
            };
        }
        [MenuItem("문어/우주 음양 적용 검증")]
        public static void Run()
        {
            Directory.CreateDirectory("Logs/Celestial");File.WriteAllText("Logs/Celestial/status.txt","RUNNING");
            SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
        }
    }
    public sealed class CelestialCapture:MonoBehaviour
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        IEnumerator routine;float deadline;int errors;
        readonly List<string> results=new List<string>();
        object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Private).Invoke(o,args);
        void Check(bool ok,string text){if(!ok)throw new Exception(text);results.Add("PASS: "+text);}
        void Start(){Application.logMessageReceived+=OnLog;routine=Run();deadline=Time.realtimeSinceStartup+90;}
        void OnLog(string m,string t,LogType type){if(type==LogType.Error || type==LogType.Exception)errors++;}
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
        void Update()
        {
            if(routine==null)return;
            try{if(Time.realtimeSinceStartup>deadline)throw new TimeoutException();if(routine.MoveNext())return;Finish(true);}
            catch(Exception e){results.Add(e.ToString());Finish(false);}
        }
        void Finish(bool ok){routine=null;File.WriteAllLines("Logs/Celestial/results.txt",results);File.WriteAllText("Logs/Celestial/status.txt",ok?"PASS":"FAIL");EditorApplication.isPlaying=false;}
        void Capture(string name)
        {
            var cam=Camera.main;var rt=new RenderTexture(768,768,24);var old=cam.targetTexture;var active=RenderTexture.active;
            float aspect=cam.aspect;cam.aspect=1;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            var tex=new Texture2D(768,768,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,768,768),0,0);tex.Apply();
            File.WriteAllBytes("Art/Celestial/"+name+".png",tex.EncodeToPNG());cam.targetTexture=old;cam.aspect=aspect;RenderTexture.active=active;Destroy(rt);Destroy(tex);
        }
        IEnumerator Run()
        {
            yield return null;
            Check(Enum.GetValues(typeof(SpecialId)).Length==3,"only None, Cosmos and YinYang special identities");
            for(int row=0;row<11;row++)for(int f=0;f<8;f++)
            {
                var sprite=CelestialArt.Frame(row,f);Check(sprite!=null && sprite.rect.size==new Vector2(128,128) && sprite.texture.filterMode==FilterMode.Point,"RGBA point-filter frame "+row+":"+f);
            }
            Check(!ElementRules.IsSpecialPickup((ActorKind)17) && !ElementRules.IsSpecialPickup((ActorKind)18),"retired special drops rejected");
            ushort port;using(var socket=new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback,0)))port=(ushort)((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
            typeof(GameBootstrap).GetMethod("StartHost",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(ushort)},null).Invoke(null,new object[]{port});
            while(GameSession.Instance==null || !GameSession.Instance.IsSpawned)yield return null;
            var s=GameSession.Instance;s.SelectStartingElement(ElementId.Water);
            while(s.HostElement.Value==0)yield return null;s.ToggleLocalReady();while(!s.CanStart())yield return null;s.RequestStart();
            while(!s.IsPlaying || OctopusPlayer.Local==null)yield return null;
            var p=OctopusPlayer.Local;p.enabled=s.enabled=false;p.Invincible.Value=true;p.transform.position=Vector2.zero;p.NetPos.Value=Vector2.zero;
            var actors=(List<SimActor>)typeof(GameSession).GetField("_actors",Private).GetValue(s);
            actors.Clear();var enemy=(SimActor)Call(s,"Create",ActorKind.Enemy,new Vector2(3,1),.8f);enemy.Hp=enemy.MaxHp=100000;
            s.CastCelestial(Vector2.zero,p,SpecialId.Cosmos,1);Check(actors.FindAll(a=>a.Kind==ActorKind.CosmicStar).Count==54,"54 stars generated sequentially at level 1");
            for(int tick=0;tick<65;tick++)
            {
                foreach(var a in actors.ToArray())if(!a.Dead)Call(s,"TickCelestial",a,.05f);
                Call(s,"RenderLocal");yield return null;
                if(tick%2==0)Capture("runtime_cosmos_"+(tick/2).ToString("D2"));
                if(tick==12)Capture("game_cosmos_stars");if(tick==27)Capture("game_cosmos_tracking");
            }
            Check(enemy.Hp<enemy.MaxHp,"homing stars cause authoritative impact damage");
            actors.RemoveAll(a=>a!=enemy);enemy.Hp=enemy.MaxHp;
            s.CastCelestial(Vector2.zero,p,SpecialId.YinYang,1);
            for(int tick=0;tick<80;tick++)
            {
                foreach(var a in actors.ToArray())if(!a.Dead)Call(s,"TickCelestial",a,.05f);
                Call(s,"RenderLocal");yield return null;
                if(tick%2==0)Capture("runtime_yin_yang_"+(tick/2).ToString("D2"));
                if(tick==15)Capture("game_yin_yang_orbit");if(tick==63)Capture("game_yin_yang_burst");
                if(tick==54)Check(enemy.Hp==enemy.MaxHp,"no premature damage before circular convergence");
            }
            Check(Mathf.Abs(enemy.MaxHp-enemy.Hp-230f)<.1f,"collision applies one 230 damage burst, shockwave is cosmetic");
            Check(actors.FindAll(a=>!a.Dead && CelestialArt.Supports(a.Kind)).Count==0,"all celestial effects expire");
            s.SpecialUnlocked.Value=true;p.DevSetSpecial(4);Check(p.Special.Value==0,"retired special developer grant rejected");
            actors.Clear();Call(s,"DropBossSpecial",Vector2.zero);
            Check(actors.Count==2 && actors.Exists(a=>a.Kind==ActorKind.PickupCosmos) && actors.Exists(a=>a.Kind==ActorKind.PickupYinYang),"boss drops one Cosmos and one YinYang");
            Check(errors==0,"no runtime errors");
        }
    }
}
