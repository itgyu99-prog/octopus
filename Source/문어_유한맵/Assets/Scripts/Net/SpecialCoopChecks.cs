#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
namespace OctopusSurvivors
{
    // Explicit opt-in two-process validation; absent from release players.
    public sealed class SpecialCoopChecks : MonoBehaviour
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        bool host,started,registered,seen,done;float deadline,boot;
        IEnumerator routine;
        ushort port=17991;
        static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,Hidden).Invoke(o,a);
        static List<SimActor> Actors(GameSession s)=>(List<SimActor>)typeof(GameSession).GetField("_actors",Hidden).GetValue(s);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();
            bool h=Array.IndexOf(args,"-special-test-host")>=0;
            if(!h && Array.IndexOf(args,"-special-test-client")<0)return;
            var r=new GameObject("Special cooperative checks").AddComponent<SpecialCoopChecks>();
            DontDestroyOnLoad(r);r.host=h;r.deadline=Time.realtimeSinceStartup+65;r.boot=Time.realtimeSinceStartup;
            int pi=Array.IndexOf(args,"-special-test-port");
            if(pi>=0 && pi+1<args.Length && ushort.TryParse(args[pi+1],out ushort selected))r.port=selected;
            Application.targetFrameRate=30;
        }
        static void Check(bool ok,string message)
        {if(!ok)throw new Exception(message);Debug.Log("SPECIAL COOP PASS: "+message);}
        void Update()
        {
            if(done)return;
            try
            {
                if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Special cooperative validation timed out");
                if(!started)
                {
                    if(Time.realtimeSinceStartup-boot<1)return;
                    started=true;
                    if(host)GameBootstrap.StartHost(port);
                    else
                    {
                        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1",port);
                        Check(NetworkManager.Singleton.StartClient(),"guest client starts on isolated local test port");
                    }
                    return;
                }
                var s=GameSession.Instance;if(s==null||!s.IsSpawned)return;
                if(!registered)
                {
                    registered=true;
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("special.seen",(sender,reader)=>seen=true);
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("special.devrequest",(sender,reader)=>{
                        if(!host){s.RequestDevSpecialConvergence();s.RequestDevSpecialConvergence();}
                    });
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("special.done",(sender,reader)=>{Debug.Log("SPECIAL COOP CLIENT COMPLETE");done=true;Application.Quit(0);});
                }
                if(!s.IsPlaying)
                {
                    if((host?s.HostElement.Value:s.GuestElement.Value)==0){s.SelectStartingElement(host?ElementId.Fire:ElementId.Water);return;}
                    if(!(host?s.HostReady.Value:s.GuestReady.Value))s.ToggleLocalReady();
                    if(host&&s.PlayerCount.Value==2&&s.CanStart())s.RequestStart();return;
                }
                if(host)
                {
                    if(routine==null)routine=RunHost(s);
                    if(!routine.MoveNext()){Debug.Log("SPECIAL COOP HOST COMPLETE");done=true;Application.Quit(0);}
                }
                else if(!seen)
                {
                    var visuals=UnityEngine.Object.FindObjectsByType<SpecialSkillVisual>();
                    bool joint=false;
                    foreach(var v in visuals)
                        if((ActorKind)typeof(SpecialSkillVisual).GetField("kind",Hidden).GetValue(v)==ActorKind.SpecialConvergence)joint=true;
                    if(joint && OctopusPlayer.All.Count==2 && OctopusPlayer.All.TrueForAll(p=>p.IsSpawned&&p.Transformed.Value) && OctopusPlayer.All[0].Special.Value!=OctopusPlayer.All[1].Special.Value)
                    {
                        Check(OctopusPlayer.All.Count==2 && OctopusPlayer.All.TrueForAll(p=>p.IsSpawned&&p.Transformed.Value),"both transformed players are replicated on guest");
                        Check(OctopusPlayer.All[0].Special.Value!=OctopusPlayer.All[1].Special.Value,"different special elements are replicated on guest");
                        Check(visuals.Length>0,"guest reconstructs convergence visual layers from actor snapshots");
                        Send("special.seen",NetworkManager.ServerClientId);seen=true;
                    }
                }
            }
            catch(Exception e){Debug.LogException(e);done=true;Application.Quit(2);}
        }
        void Send(string name,ulong id)
        {using var writer=new FastBufferWriter(1,Allocator.Temp);NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(name,id,writer,NetworkDelivery.ReliableSequenced);}
        IEnumerator RunHost(GameSession s)
        {
            while(OctopusPlayer.All.Count!=2)yield return null;
            s.enabled=false;Actors(s).Clear();
            var a=OctopusPlayer.All[0];var b=OctopusPlayer.All[1];
            foreach(var p in OctopusPlayer.All){p.enabled=false;p.Invincible.Value=true;p.PendingPicks.Value=0;}
            a.Special.Value=b.Special.Value=(byte)SpecialId.YinYang;
            a.Transformed.Value=b.Transformed.Value=true;
            a.transform.position=Vector2.left*.2f;b.transform.position=Vector2.right*.2f;
            a.NetPos.Value=a.Pos;b.NetPos.Value=b.Pos;
            Call(s,"CheckTeamUltimate");Check(Actors(s).Count==0,"same elements do not activate convergence on real server");
            b.Special.Value=(byte)SpecialId.Cosmos;b.Transformed.Value=false;
            Call(s,"CheckTeamUltimate");Check(Actors(s).Count==0,"untransformed guest cannot activate convergence");
            b.Transformed.Value=true;b.transform.position=Vector2.right*4;b.NetPos.Value=b.Pos;
            Call(s,"CheckTeamUltimate");Check(Actors(s).Count==0,"separated players cannot activate convergence");
            b.transform.position=Vector2.right*.2f;b.NetPos.Value=b.Pos;
            Call(s,"CheckTeamUltimate");Check(Actors(s).FindAll(x=>!x.Dead&&x.Kind==ActorKind.SpecialConvergence).Count==1,"touching different transformed players activate exactly one convergence");
            Call(s,"CheckTeamUltimate");Check(Actors(s).Count==1,"active convergence and cooldown block duplicate contact");
            Actors(s)[0].Dead=true;s.TeamUltCooldown.Value=0;
            Call(s,"CheckTeamUltimate");Check(!Actors(s).Exists(x=>!x.Dead&&x.Kind==ActorKind.SpecialConvergence),"held contact remains latched after effect ends");
            b.transform.position=Vector2.right*4;Call(s,"CheckTeamUltimate");
            b.transform.position=Vector2.right*.2f;b.NetPos.Value=b.Pos;Call(s,"CheckTeamUltimate");
            Check(Actors(s).Exists(x=>!x.Dead&&x.Kind==ActorKind.SpecialConvergence),"separating and retouching rearms the contact latch");
            float age=0;
            while(age<SpecialSkillTuning.Data.convergenceDuration+.4f)
            {
                float dt=Mathf.Min(.05f,Time.deltaTime);age+=dt;
                foreach(var actor in Actors(s).ToArray())if(!actor.Dead)Call(s,"TickSpecialEnhancement",actor,dt);
                Call(s,"RenderLocal");Call(s,"BroadcastActors");yield return null;
            }
            float until=Time.realtimeSinceStartup+4;
            while(!seen&&Time.realtimeSinceStartup<until)yield return null;
            Check(seen,"real guest acknowledged receiving and displaying convergence");
            Actors(s).Clear();
            foreach(var id in NetworkManager.Singleton.ConnectedClientsIds)if(id!=NetworkManager.ServerClientId)Send("special.devrequest",id);
            until=Time.realtimeSinceStartup+5;
            while(!Actors(s).Exists(x=>!x.Dead&&x.Kind==ActorKind.SpecialConvergence)&&Time.realtimeSinceStartup<until)yield return null;
            Check(Actors(s).FindAll(x=>!x.Dead&&x.Kind==ActorKind.SpecialConvergence).Count==1,"guest developer button RPC reaches server and rejects overlapping requests");
            foreach(var id in NetworkManager.Singleton.ConnectedClientsIds)if(id!=NetworkManager.ServerClientId)Send("special.done",id);
            float end=Time.realtimeSinceStartup+.6f;while(Time.realtimeSinceStartup<end)yield return null;
        }
    }
}
#endif
