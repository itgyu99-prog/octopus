#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace OctopusSurvivors
{
    // Opt-in integration runner; omitted from release players.
    public sealed class CoopSystemChecks : MonoBehaviour
    {
        bool host, solo, relay, started, ready, guestReady, registered, done, finishRequested;
        int guestMovementChecks;
        float deadline, nextPick;
        int observedPending = -1;
        Vector2 guestStart;
        float guestMovementDistance;
        bool guestMoving;
        IEnumerator run;
        static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Hidden).Invoke(target, args);
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Debug.Log("CHECK PASS: " + message); }
        static List<SimActor> Actors(GameSession s) => (List<SimActor>)typeof(GameSession).GetField("_actors", Hidden).GetValue(s);
        static IEnumerator Seconds(float duration) { float end=Time.realtimeSinceStartup+duration; while(Time.realtimeSinceStartup<end) yield return null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();
            bool single=Array.IndexOf(args,"-octopus-test-solo")>=0;
            bool server=single||Array.IndexOf(args,"-octopus-test-host")>=0;
            if(!server&&Array.IndexOf(args,"-octopus-test-client")<0) return;
            var go=new GameObject("CoopSystemChecks");DontDestroyOnLoad(go);
            var runner=go.AddComponent<CoopSystemChecks>();
            runner.host=server;runner.solo=single;
            runner.relay=Array.IndexOf(args,"-octopus-relay-check")>=0;
            runner.deadline=Time.realtimeSinceStartup+150f;
            Application.targetFrameRate=60;
        }
        void Update()
        {
            try
            {
                if(done) return;
                if(Time.realtimeSinceStartup>deadline) throw new Exception("Integration timeout");
                if(!started)
                {
                    if(Time.realtimeSinceStartup<2f) return;
                    string code=Application.dataPath+"/../revision-relay-code.txt";
                    if(relay&&!host&&!System.IO.File.Exists(code)) return;
                    string joinCode=null;
                    if(relay&&!host)
                    {
                        try { joinCode=System.IO.File.ReadAllText(code).Trim(); }
                        catch(System.IO.IOException) { return; }
                        if(string.IsNullOrEmpty(joinCode)) return;
                    }
                    started=true;
                    if(relay) { if(host) GameBootstrap.StartRelayHost(); else GameBootstrap.StartRelayClient(joinCode); }
                    else if(host) GameBootstrap.StartHost(); else GameBootstrap.StartClient("127.0.0.1");
                    return;
                }
                var s=GameSession.Instance;if(s==null||!s.IsSpawned)return;
                if(!registered)
                {
                    registered=true;
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("revision.ready",(sender,reader)=>guestReady=true);
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("revision.done",(sender,reader)=>finishRequested=true);
                    if(relay&&host) System.IO.File.WriteAllText(Application.dataPath+"/../revision-relay-code.txt",RelaySession.JoinCode);
                }
                if(!s.IsPlaying)
                {
                    byte selected=host?s.HostElement.Value:s.GuestElement.Value;
                    if(selected==0){s.SelectStartingElement(host?ElementId.Fire:ElementId.Water);return;}
                    if(!(host?s.HostReady.Value:s.GuestReady.Value))s.ToggleLocalReady();
                    if(host&&(solo||s.PlayerCount.Value==2)&&s.CanStart())s.RequestStart();
                    return;
                }
                var player=OctopusPlayer.Local;if(player==null||!player.IsSpawned)return;
                if(!ready)
                {
                    ready=true;player.enabled=false; // Let the runner provide owner movement input for real NGO replication.
                    Check(player.SlotElement(0)==(host?ElementId.Fire:ElementId.Water),"starting attribute replicated");
                    if(host)run=RunHost(s,player);
                    else Send("revision.ready");
                }
                if(host){if(run!=null&&!run.MoveNext())run=null;}
                else
                {
                    if (s.CombatActive && s.Stage.Value == 1 && player.ComboCount == 0 && player.SlotElement(3) != ElementId.None)
                        player.RequestCombine(0,1);
                    int pending=player.PendingPicks.Value;
                    if(pending!=observedPending){observedPending=pending;nextPick=Time.realtimeSinceStartup+.65f;}
                    if(s.StageFlow.Value==(byte)StageState.Choosing&&pending>0&&Time.realtimeSinceStartup>=nextPick)
                    {
                        Check(player.Offer(0)!=UpgradeId.None&&player.Offer(1)!=UpgradeId.None&&player.Offer(2)!=UpgradeId.None,"guest sees three cards for choice "+(3-pending));
                        player.ChooseUpgrade(0);player.ChooseUpgrade(0);nextPick=Time.realtimeSinceStartup+2f;
                    }
                    if(s.StageFlow.Value==(byte)StageState.Countdown)
                    {
                        if(!guestMoving){guestStart=player.Pos;guestMovementDistance=0f;guestMoving=true;}
                        guestMovementDistance=Mathf.Max(guestMovementDistance,Vector2.Distance(guestStart,player.Pos));
                        player.MoveInput.Value=Vector2.right;
                    }
                    else
                    {
                        player.MoveInput.Value=Vector2.zero;
                        if(guestMoving)
                        {
                            Check(guestMovementDistance>.1f,"guest movement replicated during countdown");
                            Check(player.SlotElement(3)!=ElementId.None&&player.ComboCount==1,"guest fourth slot and ingredient-preserving synthesis replicated");
                            guestMovementChecks++;guestMoving=false;
                        }
                    }
                    if(finishRequested&&guestMovementChecks==4){Debug.Log("COOP CLIENT PASS");done=true;Application.Quit(0);}
                }
            }
            catch(Exception e){Debug.LogError("COOP CHECK FAILED: "+e);done=true;Application.Quit(2);}
        }
        void Send(string channel)
        {
            using var writer=new FastBufferWriter(1,Allocator.Temp);writer.WriteValueSafe((byte)1);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(channel,host?1ul:NetworkManager.ServerClientId,writer,NetworkDelivery.ReliableSequenced);
        }
        int UpgradeTotal(OctopusPlayer p){int total=0;foreach(var id in Upgrades.All)total+=p.UpgradeCount(id);return total;}
        void ResetField(GameSession s){Call(s,"ClearFieldForNextStage");Actors(s).RemoveAll(a=>a.Dead);}
        SimActor Target(GameSession s,Vector2 pos)
        {
            var a=(SimActor)Call(s,"Create",ActorKind.Enemy,pos,.7f);a.Hp=a.MaxHp=10000;a.StunTime=100f;return a;
        }
        IEnumerator PickStage(GameSession s,OctopusPlayer p,int stage)
        {
            foreach(var participant in OctopusPlayer.All)
            {participant.transform.position=Vector3.zero;participant.NetPos.Value=Vector2.zero;}
            int before=UpgradeTotal(p);uint revision=p.ChoiceRevision.Value;
            Check(p.PendingPicks.Value==2,"two sequential choices granted at stage "+stage);
            Check(p.Offer(0)!=UpgradeId.None&&p.Offer(1)!=UpgradeId.None&&p.Offer(2)!=UpgradeId.None,"first choice has three cards");
            p.ChooseUpgrade(0);p.ChooseUpgrade(0);
            Call(p,"ChooseUpgradeServerRpc",(byte)0,revision);
            Check(p.PendingPicks.Value==1&&UpgradeTotal(p)==before+1,"duplicate click and stale RPC apply first reward once");
            Check(p.ChoiceRevision.Value!=revision&&!p.StageChoiceComplete.Value,"second independent offer issued before completion");
            Check(p.Offer(0)!=UpgradeId.None&&p.Offer(1)!=UpgradeId.None&&p.Offer(2)!=UpgradeId.None,"second choice has three cards");
            var hold=Seconds(.4f);while(hold.MoveNext())yield return null;
            Check(s.Stage.Value==stage&&s.StageFlow.Value==(byte)StageState.Choosing,"cannot advance before second choice");
            Capture("choice-two");
            p.ChooseUpgrade(0);
            Check(p.StageChoiceComplete.Value&&UpgradeTotal(p)==before+2,"second choice applies exactly once");
            while(s.StageFlow.Value!=(byte)StageState.Countdown)yield return null;
            Vector2 from=p.Pos;p.MoveInput.Value=Vector2.up;
            float began=Time.realtimeSinceStartup;
            while(s.Stage.Value==stage)yield return null;
            p.MoveInput.Value=Vector2.zero;
            Check(Time.realtimeSinceStartup-began>=2.85f,"three-second countdown retained");
            Check(Vector2.Distance(from,p.Pos)>.1f,"host can move during countdown");
            Check(s.Stage.Value==stage+1&&s.CombatActive,"stage resumes after all players finish two picks");
            foreach(var other in OctopusPlayer.All)Check(other.StageChoiceComplete.Value&&other.PendingPicks.Value==0,"all player choice completion synchronized");
        }
        IEnumerator RunHost(GameSession s,OctopusPlayer p)
        {
            while(!solo&&!guestReady)yield return null;
            Check(s.PlayerCount.Value==(solo?1:2),"expected player count");
            Check(s.UnlockedComboSlots.Value==3,"three combo slots available from start");
            var texture=RuntimeArt.BeachTexture();
            var arena=GameObject.Find("FiniteBeachArena");
            Check(Mathf.Abs(arena.transform.localScale.x-arena.transform.localScale.y)<.001f,"background uses uniform scale without stretch");
            Check(Mathf.Abs(FiniteBeach.Width/FiniteBeach.Height-(float)texture.width/texture.height)<.001f,"background matches original image aspect");
            Check(Camera.main.orthographicSize==8.5f&&FiniteBeach.Height<51f,"background reduced with camera size unchanged");
            Check(GameplayRules.Data.attributeDropChance==.02f,"attribute drop chance reduced to two percent");
            Check(!s.SpecialUnlocked.Value,"specials unavailable before boss five");
            p.DevSetSpecial((byte)SpecialId.Meteor);Check(p.Special.Value==0,"developer grant also respects special gate");
            foreach(var player in OctopusPlayer.All)
            {
                foreach(var element in GameplayRules.BasicElements)player.GrantElement(element);
                foreach(var element in GameplayRules.BasicElements)Check(player.HasElement(element),"owns attribute "+element);
                byte rank=player.UpAttrRank.Value;Check(!player.GrantElement(ElementId.Fire)&&player.UpAttrRank.Value==rank,"duplicate attribute adds no rank or slot");
            }
            foreach(var element in GameplayRules.BasicElements)Check(!s.AttributeNeeded(element),"no redundant team-wide attribute drops: "+element);
            // Rotation uses the same timer for four consecutive casts; an early tick must not skip ahead.
            p.ActiveSlot.Value=0;typeof(OctopusPlayer).GetField("attributeCycleLeft",Hidden).SetValue(p,0f);
            for(byte i=0;i<4;i++)
            {
                Call(p,"TickAttributeCycle",GameConfig.AttributeCycle);
                Check(p.CastElement==p.SlotElement(i),"rotation casts slot "+i);
                var last=p.CastElement;Call(p,"TickAttributeCycle",.01f);Check(p.CastElement==last,"rotation does not fire extra attribute in same interval");
            }
            Check(p.ServerCombine(0,1),"synthesis available at stage one");
            Check(p.HasElement(ElementId.Fire)&&p.HasElement(ElementId.Water),"synthesis retains ingredients");
            Check(p.ServerCombine(0,2)&&p.ServerCombine(0,3)&&!p.ServerCombine(1,2),"three combo maximum enforced");
            ResetField(s);p.transform.position=Vector3.zero;p.NetPos.Value=Vector2.zero;
            foreach(var player in OctopusPlayer.All){player.transform.position=Vector3.zero;player.NetPos.Value=Vector2.zero;}
            TestAbilities(s,p);
            ResetField(s);s.CastBasicAbility(p,ElementId.Fire,1);yield return null;Capture("fire-field");
            GameHud.Instance.ToggleCombine();yield return null;Capture("four-attributes-combine");GameHud.Instance.ToggleCombine();
            while(!solo&&OctopusPlayer.All.Exists(other=>other!=p&&other.ComboCount==0))yield return null;
            ResetField(s);
            Call(s,"SpawnMarker",ActorKind.Statue,Vector2.zero,1.55f);
            for(int i=0;i<GameConfig.RequiredPieces(1);i++)
            {
                Call(s,"SpawnMarker",ActorKind.ArtifactPiece,Vector2.zero,.42f);
                while(s.ArtifactDelivered.Value<=i)yield return null;
            }
            var leftovers=new List<ushort>();
            foreach(var kind in new[]{ActorKind.XpOrb,ActorKind.ArtifactPiece,ActorKind.HealthPotion,ElementRules.PickupKind(ElementId.Fire)})
            {var a=(SimActor)Call(s,"Create",kind,new Vector2(15,0),.4f);leftovers.Add(a.Id);}
            var picks=PickStage(s,p,1);while(picks.MoveNext())yield return null;
            Check(!Actors(s).Exists(a=>leftovers.Contains(a.Id)),"all previous-stage drops including XP removed");
            Check(p.ComboCount==3&&p.HasElement(ElementId.Electric),"loadout persists across field cleanup");
            for(int milestone=5;milestone<=15;milestone+=5)
            {
                p.transform.position=Vector3.zero;p.NetPos.Value=Vector2.zero;
                s.RequestDevStage(milestone);s.DamageAll(10000000f);
                var specials=Actors(s).FindAll(a=>!a.Dead&&ElementRules.IsSpecialPickup(a.Kind));
                Check(specials.Count==(milestone==5?2:0),"special drop count at boss "+milestone);
                ushort unclaimed=0;
                if(milestone==5)
                {
                    p.transform.position=specials[0].Pos;p.NetPos.Value=p.Pos;
                    Call(p,"PressServerRpc",(byte)2);
                    Check(p.Special.Value!=0&&specials[0].Dead,"special collectible during clear before cleanup");
                    unclaimed=specials[1].Id;
                }
                var advance=PickStage(s,p,milestone);while(advance.MoveNext())yield return null;
                if(milestone==5)Check(!Actors(s).Exists(a=>a.Id==unclaimed)&&p.Special.Value!=0,"unclaimed special removed but owned special retained");
            }
            Debug.Log(solo?"SOLO TEST PASS":"COOP HOST PASS");
            if(!solo)Send("revision.done");
            var end=Seconds(1f);while(end.MoveNext())yield return null;
            done=true;Application.Quit(0);
        }
        void TestAbilities(GameSession s,OctopusPlayer p)
        {
            for(int level=1;level<=3;level++)
            {
                ResetField(s);s.CastBasicAbility(p,ElementId.Water,level);
                var emitter=Actors(s).Find(a=>a.Kind==ActorKind.WaterEmitter);
                for(int pulse=0;pulse<level;pulse++)
                {
                    Call(s,"TickPlannedAbility",emitter,pulse==0?.001f:GameplayRules.Data.attributeRepeatInterval+.001f);
                    var shots=Actors(s).FindAll(a=>a.Kind==ActorKind.Droplet);
                    Check(shots.Count==(pulse+1)*GameplayRules.Data.waterProjectileCount,"water radial volley count at level "+level+" pulse "+pulse);
                    Vector2 sum=Vector2.zero;
                    for(int i=0;i<GameplayRules.Data.waterProjectileCount;i++)sum+=shots[shots.Count-1-i].Vel.normalized;
                    Check(sum.sqrMagnitude<.001f,"water directions evenly cover full circle");
                    if(pulse>0)Check(Vector2.Angle(shots[0].Vel,shots[shots.Count-GameplayRules.Data.waterProjectileCount].Vel)<.01f,"repeat retains volley direction");
                }
                Check(emitter.Dead,"water emitter stops after requested volleys");
                ResetField(s);s.CastBasicAbility(p,ElementId.Electric,level);
                emitter=Actors(s).Find(a=>a.Kind==ActorKind.CurrentEmitter);
                for(int i=0;i<level;i++)Call(s,"TickPlannedAbility",emitter,GameplayRules.Data.attributeRepeatInterval+.001f);
                var waves=Actors(s).FindAll(a=>a.Kind==ActorKind.RadialCurrent);
                Check(waves.Count==level&&emitter.Dead,"electric emission repeat count at level "+level);
                var targets=new List<SimActor>();for(int i=0;i<8;i++)targets.Add(Target(s,p.Pos+AbilityRunner.Rotate(Vector2.right,i*45)*2f));
                Call(s,"TickPlannedAbility",waves[0],.25f);
                foreach(var target in targets)Check(target.Hp<target.MaxHp,"electric wave damages all directions");
            }
            ResetField(s);s.CastBasicAbility(p,ElementId.Fire,1);
            var fire=Actors(s).Find(a=>a.Kind==ActorKind.FlameAura);
            var inside=Target(s,p.Pos);var outside=Target(s,p.Pos+Vector2.right*(fire.Size+1f));
            Call(s,"TickPlannedAbility",fire,.1f);float hp=inside.Hp;Call(s,"TickPlannedAbility",fire,.1f);
            Check(inside.Hp<hp&&outside.Hp==outside.MaxHp,"flame field damages entire interior over time only");
            float baseRadius=fire.Size;s.CastBasicAbility(p,ElementId.Fire,3);
            Check(Actors(s).Exists(a=>a.Kind==ActorKind.FlameAura&&a.Size>baseRadius),"flame range grows with attribute upgrade");
            ResetField(s);var enemy=Target(s,p.Pos+Vector2.right*.7f);s.CastBasicAbility(p,ElementId.Water,1);
            var water=Actors(s).Find(a=>a.Kind==ActorKind.WaterEmitter);Call(s,"TickPlannedAbility",water,.01f);
            var shot=Actors(s).Find(a=>a.Kind==ActorKind.Droplet);Call(s,"TickProjectile",shot,.01f);
            Check(enemy.Hp<enemy.MaxHp,"water retains projectile damage and collision");
        }
        void Capture(string name)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            string directory=Application.dataPath+"/../Screenshots";
            System.IO.Directory.CreateDirectory(directory);
            var camera=Camera.main;
            if(camera==null) throw new Exception("Capture camera missing");
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var changed=new List<Canvas>();
            var previous=RenderTexture.active;
            var target=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                foreach(var canvas in canvases)
                    if(canvas.isRootCanvas && canvas.renderMode==RenderMode.ScreenSpaceOverlay)
                    {
                        changed.Add(canvas);
                        canvas.renderMode=RenderMode.ScreenSpaceCamera;
                        canvas.worldCamera=camera;
                        canvas.planeDistance=1f;
                    }
                Canvas.ForceUpdateCanvases();
                var request=new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination=target };
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1280,720),0,0);
                texture.Apply();
                System.IO.File.WriteAllBytes(directory+"/"+(host?"host-":"guest-")+name+".png",texture.EncodeToPNG());
            }
            finally
            {
                foreach(var canvas in changed) { canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null; }
                RenderTexture.active=previous;
                RenderTexture.ReleaseTemporary(target);
                Destroy(texture);
            }
        }
    }
}
#endif
