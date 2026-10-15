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
        bool sawGiantVisual;
        bool blueFlameCheck;
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
#if UNITY_EDITOR
            bool editorBlueFlame = UnityEditor.SessionState.GetBool("Octopus.BlueFlameCheck", false);
            single |= editorBlueFlame;
#endif
            bool server=single||Array.IndexOf(args,"-octopus-test-host")>=0;
            if(!server&&Array.IndexOf(args,"-octopus-test-client")<0) return;
            var go=new GameObject("CoopSystemChecks");DontDestroyOnLoad(go);
            var runner=go.AddComponent<CoopSystemChecks>();
            runner.host=server;runner.solo=single;
            runner.blueFlameCheck=Array.IndexOf(args,"-octopus-blueflame-check")>=0;
#if UNITY_EDITOR
            runner.blueFlameCheck |= editorBlueFlame;
#endif
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
                if (ready && host && run != null && (blueFlameCheck || Array.IndexOf(Environment.GetCommandLineArgs(), "-octopus-balance-check") >= 0))
                {
                    if (!run.MoveNext()) run = null;
                    return; // Continue exit checks even after GameSession despawns.
                }
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
                    else if(host)
                    {
                        ushort port=GameConfig.Port;
#if UNITY_EDITOR
                        if(blueFlameCheck)port=(ushort)UnityEditor.SessionState.GetInt("Octopus.BlueFlamePort",17777);
#endif
                        GameBootstrap.StartHost(port);
                    }
                    else GameBootstrap.StartClient("127.0.0.1");
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
                if(!host && !sawGiantVisual && UnityEngine.Object.FindObjectsByType<GiantMonsterVisual>(FindObjectsInactive.Exclude).Length>=5)
                {sawGiantVisual=true;Capture("giant-monsters-client");Debug.Log("GIANT CLIENT SNAPSHOT PASS: all five species visible");}
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
                            Check(player.SlotElement(3)!=ElementId.None&&player.SlotElement(0)==ElementId.None&&player.SlotElement(1)==ElementId.None&&player.ComboCount==1,"guest fourth slot and consumed synthesis ingredients replicated");
                            guestMovementChecks++;guestMoving=false;
                        }
                    }
                    if(finishRequested&&guestMovementChecks==4){Check(sawGiantVisual,"giant visual snapshots received");Debug.Log("COOP CLIENT PASS");done=true;Application.Quit(0);}
                }
            }
            catch(Exception e){Debug.LogError("COOP CHECK FAILED: "+e);done=true;FinishChecks(2);}
        }
        static void FinishChecks(int code)
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("Octopus.BlueFlameCheck", false))
            {
                UnityEditor.SessionState.SetBool("Octopus.BlueFlameCheck", false);
                System.IO.File.WriteAllText(Application.dataPath+"/../Logs/blueflame-check.status", code==0?"PASS":"FAIL");
                UnityEditor.EditorApplication.isPlaying=false;
                return;
            }
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }
        void Send(string channel)
        {
            using var writer=new FastBufferWriter(1,Allocator.Temp);writer.WriteValueSafe((byte)1);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(channel,host?1ul:NetworkManager.ServerClientId,writer,NetworkDelivery.ReliableSequenced);
        }
        int UpgradeTotal(OctopusPlayer p){int total=p.ExtraDash.Value;foreach(var id in Upgrades.All)total+=p.UpgradeCount(id);return total;}
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
            if (blueFlameCheck || Array.IndexOf(Environment.GetCommandLineArgs(), "-octopus-balance-check") >= 0)
            {
                yield return null;
                var balance = RunBalanceChecks(s, p);
                while (balance.MoveNext()) yield return balance.Current;
                done = true; FinishChecks(0); yield break;
            }
            while(!solo&&!guestReady)yield return null;
            Check(s.PlayerCount.Value==(solo?1:2),"expected player count");
            Check(s.UnlockedComboSlots.Value==3,"three combo slots available from start");
            var texture=RuntimeArt.BeachTexture();
            var arena=GameObject.Find("FiniteBeachArena");
            Check(Mathf.Abs(arena.transform.localScale.x-arena.transform.localScale.y)<.001f,"background uses uniform scale without stretch");
            Check(Mathf.Abs(FiniteBeach.Width/FiniteBeach.Height-(float)texture.width/texture.height)<.001f,"background matches original image aspect");
            Check(Camera.main.orthographicSize==11f&&GameConfig.PlayerScreenHeight==.04f,"wide camera and compact player ratio");
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
            ResetField(s);
            var cooldowns=(float[])typeof(OctopusPlayer).GetField("attributeCooldowns",Hidden).GetValue(p);
            Array.Clear(cooldowns,0,cooldowns.Length);
            Call(p,"TickAttributeCycle",.01f);
            foreach(var kind in new[]{ActorKind.FlameAura,ActorKind.WaterEmitter,ActorKind.ThornEmitter,ActorKind.CurrentEmitter})
                Check(Actors(s).FindAll(a=>a.Kind==kind).Count==1,"all owned attributes cast together: "+kind);
            int castCount=Actors(s).Count;
            Call(p,"TickAttributeCycle",2.9f);
            Check(Actors(s).Count==castCount,"attributes wait their own three-second cooldown");
            cooldowns[1]=.05f; // A newly ready attribute cannot wait for another slot.
            Call(p,"TickAttributeCycle",.06f);
            Check(Actors(s).Count==castCount+1&&p.CastElement==ElementId.Water,"staggered water cooldown fires independently");
            Call(p,"TickAttributeCycle",.05f);
            Check(Actors(s).Count==castCount+4,"remaining three attributes fire without rotating slots");
            Check(Mathf.Abs(p.MoveSpeedValue-4.7f*1.25f)<.0001f,"base movement speed increased 1.25 times");
            p.UpMoveSpeed.Value=10;
            Check(Mathf.Abs(p.MoveSpeedValue-5.875f)<.0001f,"legacy speed picks cannot change fixed movement speed");
            p.UpMoveSpeed.Value=0;
            for(int roll=0;roll<30;roll++)
            {
                Call(p,"RollOffers");
                for(int choice=0;choice<3;choice++)Check(p.Offer(choice)!=UpgradeId.MoveSpeed,"movement speed excluded from offers");
            }
            Check(p.ServerCombine(0,1),"synthesis available at stage one");
            Check(!p.HasElement(ElementId.Fire)&&!p.HasElement(ElementId.Water),"synthesis consumes both ingredients");
            Check(!p.ServerCombine(0,1),"consumed ingredients cannot be reused");
            Check(p.GrantElement(ElementId.Fire)&&p.ServerCombine(0,2),"consumed ingredient can be reacquired and combined");
            Check(p.GrantElement(ElementId.Fire)&&p.ServerCombine(0,3)&&!p.ServerCombine(1,2),"three combo maximum enforced");
            foreach(var element in GameplayRules.BasicElements)p.GrantElement(element);
            ResetField(s);p.transform.position=Vector3.zero;p.NetPos.Value=Vector2.zero;
            foreach(var player in OctopusPlayer.All){player.transform.position=Vector3.zero;player.NetPos.Value=Vector2.zero;}
            var randomState=UnityEngine.Random.state;
            var savedSlots=new byte[]{p.Slot0.Value,p.Slot1.Value,p.Slot2.Value,p.Slot3.Value};
            p.Slot0.Value=p.Slot1.Value=p.Slot2.Value=p.Slot3.Value=0;
            UnityEngine.Random.InitState(4917);
            foreach(var element in GameplayRules.BasicElements)
            {
                ResetField(s);
                for(int trial=0;trial<400;trial++)
                {
                    var enemy=Target(s,new Vector2(5,0)); enemy.Element=element;
                    Call(s,"KillEnemy",enemy);
                    Actors(s).RemoveAll(a=>a.Dead);
                }
                var drops=Actors(s).FindAll(a=>ElementRules.IsAttributePickup(a.Kind));
                Check(drops.Count>0&&drops.TrueForAll(a=>a.Kind==ElementRules.PickupKind(element)),"monster death drops matching element: "+element);
            }
            UnityEngine.Random.state=randomState;
            p.Slot0.Value=savedSlots[0];p.Slot1.Value=savedSlots[1];p.Slot2.Value=savedSlots[2];p.Slot3.Value=savedSlots[3];
            ResetField(s);
            TestAbilities(s,p);
            ResetField(s);
            GiantCombatChecks.Run(s,p);
            ResetField(s);
            for(int i=0;i<5;i++)
            {
                var d=GiantMonsterDefinition.Find(i+1);
                var e=Target(s,p.Pos+new Vector2((i-2)*3.4f,3.5f));
                e.Giant=d;e.Size=.8f*d.sizeMultiplier;e.Hp=e.MaxHp=100000;
                e.StunTime=0;e.GiantState=GiantState.PrepareAttack;e.SkillTime=5;e.SkillPoint=Vector2.down;e.Damage=0;
            }
            yield return Seconds(.6f);Capture("giant-monsters");
            s.CastBasicAbility(p,ElementId.Water,3);
            yield return Seconds(.22f);Capture("water-hd");
            float vfxStart=Time.realtimeSinceStartup;int vfxFrames=0;
            while(Time.realtimeSinceStartup-vfxStart<2f)
            {
                if(vfxFrames%15==0)s.CastBasicAbility(p,ElementId.Water,3);
                vfxFrames++;yield return null;
            }
            Debug.Log("VFX LIVE SAMPLE: "+vfxFrames+" frames / "+(Time.realtimeSinceStartup-vfxStart).ToString("F2")+" seconds, repeated water + five giants");

            ResetField(s);
            for(int i=0;i<GameplayRules.BasicElements.Length;i++)
            {
                var enemy=Target(s,new Vector2(2f+i*1.3f,2f));
                enemy.Element=GameplayRules.BasicElements[i]; enemy.Size=1f; enemy.StunTime=0f;
            }
            s.CastBasicAbility(p,ElementId.Fire,1);yield return null;Capture("fire-field");
            ResetField(s);s.CastPlannedCombo(p,ComboId.Soda);yield return null;Capture("soda-bottles");
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
                    if(pulse>0)Check(Mathf.Abs(Vector2.Angle(shots[shots.Count-2*GameplayRules.Data.waterProjectileCount].Vel,shots[shots.Count-GameplayRules.Data.waterProjectileCount].Vel)-12f)<.01f,"repeat rotates volley by twelve degrees");
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
            ResetField(s);
            p.transform.position=Vector3.zero;p.NetPos.Value=Vector2.zero;
            var inside=Target(s,new Vector2(2f,.2f));Target(s,new Vector2(3f,-.3f));
            var outside=Target(s,new Vector2(-2f,0f));
            s.CastBasicAbility(p,ElementId.Fire,1);
            var fire=Actors(s).Find(a=>a.Kind==ActorKind.FlameAura);
            Call(s,"TickPlannedAbility",fire,.1f);
            Check(inside.Hp<inside.MaxHp&&outside.Hp==outside.MaxHp,"fire breath chooses crowded cone and excludes rear target");
            p.transform.position=Vector3.up;p.NetPos.Value=p.Pos;
            Call(s,"TickPlannedAbility",fire,.1f);
            Check(fire.Pos==p.Pos,"fire breath follows caster");
            p.transform.position=Vector3.zero;p.NetPos.Value=Vector2.zero;
            float baseRadius=fire.Size;s.CastBasicAbility(p,ElementId.Fire,3);
            Check(Actors(s).Exists(a=>a.Kind==ActorKind.FlameAura&&a.Size>baseRadius),"flame range grows with attribute upgrade");
            Call(p,"DashServerRpc",Vector2.right);p.MoveInput.Value=Vector2.zero;
            p.ServerTick(GameConfig.DashDuration);
            Check(p.Pos.x>3f&&p.DashCharges.Value==0,"dash moves quickly and consumes one charge");
            Vector2 dashEnd=p.Pos;Call(p,"DashServerRpc",Vector2.right);p.ServerTick(.01f);
            Check(p.Pos==dashEnd,"empty dash charge rejects repeated use");
            p.ServerTick(GameConfig.DashRecharge);
            Check(p.DashCharges.Value==1,"dash recharges after cooldown");
            Call(p,"ApplyUpgrade",UpgradeId.ExtraDash);Call(p,"ApplyUpgrade",UpgradeId.ExtraDash);
            Check(p.MaxDashCharges==2&&p.DashCharges.Value==2,"rare dash upgrade applies only once");
            for(int roll=0;roll<100;roll++)
            {
                Call(p,"RollOffers");
                Check(p.Offer(0)!=UpgradeId.ExtraDash&&p.Offer(1)!=UpgradeId.ExtraDash&&p.Offer(2)!=UpgradeId.ExtraDash,"owned extra dash excluded from offers");
            }
            Call(p,"DashServerRpc",Vector2.left);p.ServerTick(GameConfig.DashDuration);
            Call(p,"DashServerRpc",Vector2.right);p.ServerTick(GameConfig.DashDuration);
            Check(p.DashCharges.Value==0,"upgraded dash allows two successive uses");
            p.ExtraDash.Value=0;p.DashCharges.Value=1;p.DashRechargeLeft.Value=0;
            p.transform.position=Vector3.zero;p.NetPos.Value=Vector2.zero;
            ResetField(s);s.CastBasicAbility(p,ElementId.Water,1);
            var water=Actors(s).Find(a=>a.Kind==ActorKind.WaterEmitter);Call(s,"TickPlannedAbility",water,.01f);
            var shot=Actors(s).Find(a=>a.Kind==ActorKind.Droplet);var enemy=Target(s,shot.Pos+shot.Vel*.01f);Call(s,"TickProjectile",shot,.01f);
            Check(enemy.Hp<enemy.MaxHp,"water retains projectile damage and collision");
            Vector2 previousDirection=water.Vel;
            s.CastBasicAbility(p,ElementId.Water,1);
            var waterCasts=Actors(s).FindAll(a=>a.Kind==ActorKind.WaterEmitter);
            Check(Mathf.Abs(Vector2.Angle(previousDirection,waterCasts[waterCasts.Count-1].Vel)-12f)<.01f,"next water cast also changes direction");
            ResetField(s);
            var vines=new List<SimActor>();for(int i=0;i<5;i++)vines.Add(Target(s,p.Pos+Vector2.right*(1f+i)));
            s.CastBasicAbility(p,ElementId.Grass,1);
            var grass=Actors(s).Find(a=>a.Kind==ActorKind.ThornEmitter);Call(s,"TickPlannedAbility",grass,.01f);
            Check(vines.FindAll(a=>a.Hp<a.MaxHp).Count==3&&Actors(s).FindAll(a=>a.Kind==ActorKind.Vine).Count==3,"grass pulse attacks three targets with three visible thorns");
            ResetField(s);s.CastPlannedCombo(p,ComboId.Soda);
            var bottles=Actors(s).FindAll(a=>a.Kind==ActorKind.SodaWave);
            Check(bottles.Count==5,"soda spawns five bottles");
            Check(Vector2.Distance(bottles[0].Pos,bottles[1].Pos)>.1f,"soda bottles start at different map positions");
            foreach(var bottle in bottles)
            {
                Vector2 origin=bottle.Pos,velocity=bottle.Vel;
                for(int tick=0;tick<35;tick++)
                {
                    Call(s,"TickPlannedAbility",bottle,.1f);
                    Check(FiniteBeach.Walkable(bottle.Pos,bottle.Size),"soda remains inside map");
                }
                Check(Vector2.Distance(origin,bottle.Pos)>.1f&&Vector2.Angle(velocity,bottle.Vel)>.01f,"soda flies and changes direction");
            }
        }
        IEnumerator RunBalanceChecks(GameSession s, OctopusPlayer p)
        {
            if (blueFlameCheck || Array.IndexOf(Environment.GetCommandLineArgs(), "-octopus-blueflame-check") >= 0)
            {
                var blueFlame = RunBlueFlameChecks(s, p);
                while (blueFlame.MoveNext()) yield return blueFlame.Current;
                yield break;
            }
            s.enabled = false; // Advance simulation explicitly; UI continues through real frames.
            p.transform.position = Vector3.zero; p.NetPos.Value = Vector2.zero;
            p.UpElemRank.Value = p.UpAttrRank.Value = 0;
            p.Combo.Value = (byte)ComboId.Tree;
            foreach (float step in new[] { 1f / 30, 1f / 60, 1f / 144, .25f })
            for (int rank = 0; rank <= 5; rank++)
            {
                ResetField(s);
                p.SkillLevels.Value = (ulong)rank << (((int)UpgradeId.TreeMastery - 10) * 3);
                var target = Target(s, new Vector2(1, 0));
                s.CastPlannedCombo(p, ComboId.Tree);
                var trees = Actors(s).FindAll(a => a.Kind == ActorKind.FruitTree);
                Check(trees.Count == rank + 1, "tree count at mastery " + rank);
                foreach (var tree in trees)
                    while (!tree.Dead) Call(s, "TickPlannedAbility", tree, step);
                var fruits = Actors(s).FindAll(a => a.Kind == ActorKind.Fruit);
                int expected = (rank == 5 ? 44 : 35) * (rank + 1);
                Check(fruits.Count == expected, "fruit budget at mastery " + rank + " step " + step);
                Check(fruits.TrueForAll(a => a.Pos == target.Pos), "fruit aims at live enemy, not empty ground");
                Check(target.Hp == target.MaxHp, "tree itself causes no invisible area damage");
                foreach (var fruit in fruits) Call(s, "TickPlannedAbility", fruit, fruit.Duration);
                float damage = expected * (rank == 5 ? 24f : 9f);
                Check(Mathf.Abs(target.MaxHp - target.Hp - damage) < .1f, "fruit landing damage budget " + damage);
            }
            ResetField(s); p.SkillLevels.Value = 0;
            s.CastPlannedCombo(p, ComboId.Tree);
            var emptyTree = Actors(s).Find(a => a.Kind == ActorKind.FruitTree);
            Call(s, "TickPlannedAbility", emptyTree, .5f);
            Check(Actors(s).FindAll(a => a.Kind == ActorKind.Fruit).TrueForAll(a =>
                Vector2.Distance(a.Pos, emptyTree.Pos) + a.Size <= emptyTree.Size + .001f), "empty tree landings stay within visible footprint");

            ResetField(s);
            var inside = Target(s, new Vector2(6.44f, 0));
            var outside = Target(s, new Vector2(6.46f, 0));
            s.CastBasicAbility(p, ElementId.Fire, 1);
            var flame = Actors(s).Find(a => a.Kind == ActorKind.FlameAura);
            Call(s, "TickPlannedAbility", flame, .01f);
            Check(Mathf.Abs(flame.Size - 6.45f) < .001f && inside.Hp < inside.MaxHp && outside.Hp == outside.MaxHp, "expanded fire boundary matches radius");
            inside.Pos = new Vector2(-3, 0); outside.Pos = new Vector2(-3, .2f);
            typeof(GameSession).GetField("fireCacheUntil", Hidden).SetValue(s, -1f);
            Vector2 before = flame.Vel;
            Call(s, "TickPlannedAbility", flame, .15f);
            float turned = Vector2.Angle(before, flame.Vel);
            Check(turned > 1 && turned <= 27.01f, "fire retargets but obeys 180 degrees per second");
            inside.Dead = outside.Dead = true;
            typeof(GameSession).GetField("fireCacheUntil", Hidden).SetValue(s, -1f);
            flame.Pulse = 0; before = flame.Vel;
            Call(s, "TickPlannedAbility", flame, .15f);
            Check(Vector2.Angle(before, flame.Vel) < .01f, "fire without enemies keeps its current direction");
            var recent = Target(s, new Vector2(3, 0));
            var fresh = Target(s, new Vector2(0, 3));
            flame.HitTargets.Clear(); flame.HitTargets.Add(recent.Id);
            typeof(GameSession).GetField("fireCacheUntil", Hidden).SetValue(s, -1f);
            var aim = (Vector2)Call(s, "TrackedFireDirection", flame);
            Check(Vector2.Angle(aim, Vector2.up) < .01f, "fire prefers an unhit cluster");
            fresh.Dead = true;
            aim = (Vector2)Call(s, "TrackedFireDirection", flame);
            Check(Vector2.Angle(aim, Vector2.right) < .01f, "fire still attacks the only remaining target");
            flame.Vel = Vector2.up;
            byte packed = (byte)typeof(GameSession).GetMethod("PackFlags", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { flame });
            Check(GameConfig.ActorBytes == 25 && Mathf.Abs(packed / 255f * 360 - 90) < 1, "existing 25-byte actor record carries steering angle");
            ResetField(s); s.CastBasicAbility(p, ElementId.Fire, 6);
            Check(Mathf.Abs(Actors(s).Find(a => a.Kind == ActorKind.FlameAura).Size - 8.7f) < .001f, "fire range upgrade remains effective at maximum rank");

            ResetField(s);
            Target(s, new Vector2(3, 1)); Target(s, new Vector2(4, 0)); Target(s, new Vector2(2, -1));
            s.CastPlannedCombo(p, ComboId.Tree); s.CastBasicAbility(p, ElementId.Fire, 1);
            var previewTree = Actors(s).Find(a => a.Kind == ActorKind.FruitTree);
            Call(s, "TickPlannedAbility", previewTree, .31f);
            float warmUntil = Time.realtimeSinceStartup + .8f;
            while (Time.realtimeSinceStartup < warmUntil)
            {
                foreach (var actor in Actors(s)) if (actor.Kind == ActorKind.FlameAura || actor.Kind == ActorKind.Fruit) actor.Age = .15f;
                Call(s, "RenderLocal");
                yield return null;
            }
            Capture("balance-tree-fire");

            ResetField(s);
            GameHud.Show(); var hud = GameHud.Instance;
            var dev = UnityEngine.Object.FindFirstObjectByType<DevMenu>();
            hud.ToggleCombine(); Call(dev, "Toggle");
            hud.HandleBack();
            var combine = (GameObject)typeof(GameHud).GetField("combineRoot", Hidden).GetValue(hud);
            Check(combine.activeSelf && !hud.ExitConfirmationOpen, "Esc closes developer menu before combine");
            hud.HandleBack();
            Check(combine.activeSelf, "same-frame Esc is consumed once");
            yield return null;
            hud.HandleBack(); Check(!combine.activeSelf && !hud.ExitConfirmationOpen, "Esc closes combine without leaving");
            yield return null;
            s.StageFlow.Value = (byte)StageState.Choosing;
            hud.HandleBack(); Check(!hud.ExitConfirmationOpen, "mandatory choice ignores Esc");
            yield return null;
            s.StageFlow.Value = (byte)StageState.Countdown;
            hud.HandleBack(); Check(!hud.ExitConfirmationOpen, "countdown ignores Esc");
            yield return null;
            s.StageFlow.Value = (byte)StageState.Combat;
            hud.HandleBack(); Check(hud.ExitConfirmationOpen && NetworkManager.Singleton.IsListening, "Esc opens confirmation without shutdown");
            yield return null;
            Capture("balance-exit-confirmation");
            hud.HandleBack(); Check(!hud.ExitConfirmationOpen && NetworkManager.Singleton.IsListening, "Esc cancels exit confirmation");
            yield return null;
            hud.HandleBack(); GameHud.Hide(); GameHud.Show();
            Check(!hud.ExitConfirmationOpen, "exit confirmation does not leak into next session");
            GameBootstrap.LeaveToLobby();
            float until = Time.realtimeSinceStartup + 3;
            while (NetworkManager.Singleton.IsListening && Time.realtimeSinceStartup < until) yield return null;
            Check(!NetworkManager.Singleton.IsListening, "explicit host exit shuts down network");
            Debug.Log("BALANCE A-D CHECKS PASS");
        }

        IEnumerator RunBlueFlameChecks(GameSession s, OctopusPlayer p)
        {
            s.enabled = false;
            p.transform.position = Vector3.zero; p.NetPos.Value = Vector2.zero;
            p.UpElemRank.Value = 0; p.SkillLevels.Value = 0;
            p.Combo.Value = (byte)ComboId.BlueFlame;
            foreach (float step in new[] { 1f/30, 1f/60, 1f/144, .25f })
            {
                ResetField(s);
                var targets = new List<SimActor>();
                for (int i=0;i<23;i++)
                {
                    var enemy = Target(s, Vector2.right * 2f);
                    enemy.Pos = new Vector2(2f + i * 6f, 0f); targets.Add(enemy);
                }
                s.CastPlannedCombo(p, ComboId.BlueFlame);
                var emitter = Actors(s).Find(a => a.Kind == ActorKind.ChainEmitter);
                int frames = 0;
                while (!emitter.Dead && frames++ < 1000) Call(s, "TickPlannedAbility", emitter, step);
                Check(emitter.Mark == 20 && emitter.HitTargets.Count == 20, "blue flame hits exactly 20 direct targets at step " + step);
                Check(Vector2.Distance(targets[19].Pos, p.Pos) > emitter.Size * 10f, "chain reaches far beyond initial acquisition range");
                for (int i=0;i<targets.Count;i++)
                    Check(Mathf.Abs((10000-targets[i].Hp) - (i<20 ? emitter.Damage : 0f)) < .01f, "direct damage and untouched target " + i);
                Check(Actors(s).FindAll(a => a.Kind == ActorKind.ChainArc).Count == 20, "one visible link per direct strike");
            }
            ResetField(s);
            var direct = Target(s, new Vector2(8,0));
            var collateral = Target(s, new Vector2(4,.3f));
            var outside = Target(s, new Vector2(4,1.2f));
            var reverse = Target(s, Vector2.zero);
            var source = s.SpawnPersistent(ActorKind.ChainEmitter,Vector2.zero,Vector2.zero,4.5f,3f,40f,p,StatusMask.None);
            source.BlueFlameSplashTargets = new HashSet<ushort>(); source.Duration=3f;source.EffectLevel=1;
            Call(s,"StrikeBlueFlameLink",source,Vector2.zero,direct);
            Check(Mathf.Abs(direct.Hp-9960f)<.01f,"direct target gets full damage");
            Check(Mathf.Abs(collateral.Hp-9980f)<.01f,"corridor target gets exactly half damage");
            Check(Mathf.Abs(outside.Hp-10000f)<.01f,"outside corridor gets no damage");
            Call(s,"StrikeBlueFlameLink",source,direct.Pos,reverse);
            Check(Mathf.Abs(collateral.Hp-9980f)<.01f,"crossing links do not stack collateral damage");
            Call(s,"StrikeBlueFlameLink",source,reverse.Pos,collateral);
            Check(Mathf.Abs(collateral.Hp-9960f)<.01f,"later direct strike tops collateral up to full damage");
            Check(GameSession.InsideBlueFlameCorridor(Vector2.zero,Vector2.zero,Vector2.zero,.65f),"zero length link has safe corridor geometry");
            ResetField(s);
            Target(s,Vector2.right*2f);
            s.CastPlannedCombo(p,ComboId.BlueFlame);
            var single = Actors(s).Find(a=>a.Kind==ActorKind.ChainEmitter);
            for(int i=0;i<10&&!single.Dead;i++) Call(s,"TickPlannedAbility",single,.1f);
            Check(single.Dead&&single.Mark==1,"chain ends safely when no unhit target remains");

            // Real game render, using the same snapshots, camera and enemy art as normal play.
            ResetField(s);
            p.transform.position=new Vector3(-9,-4.5f,0); p.NetPos.Value=new Vector2(-9,-4.5f);
            for(int row=0;row<4;row++)for(int col=0;col<5;col++)
            {
                var enemy=Target(s,new Vector2(-7+col*3.5f,-4.5f+row*3f)); enemy.Element=ElementId.Fire;enemy.Hp=enemy.MaxHp=50f;
            }
            for(int i=0;i<8;i++)
            {
                var enemy=Target(s,new Vector2(-5.3f+(i%4)*3.5f,-4.1f+(i/4)*6f));enemy.Element=ElementId.Grass;enemy.Hp=enemy.MaxHp=50f;
            }
            s.CastPlannedCombo(p,ComboId.BlueFlame);
            var preview = Actors(s).Find(a=>a.Kind==ActorKind.ChainEmitter);
            var camera=Camera.main;
            var follower=camera.GetComponent<PlayerCameraFollow>();if(follower!=null)follower.enabled=false;
            camera.orthographicSize=7f;camera.transform.position=new Vector3(0,0,-10);
            string framesDirectory=System.IO.Path.GetFullPath(Application.dataPath+"/../Build/Validation/BlueFlameFrames");
            System.IO.Directory.CreateDirectory(framesDirectory);
            for(int frame=0;frame<132;frame++)
            {
                const float dt=1f/60;
                if(!preview.Dead)Call(s,"TickPlannedAbility",preview,dt);
                foreach(var actor in Actors(s)) if(actor.Kind==ActorKind.ChainArc)
                {actor.Age+=dt;actor.Life-=dt;if(actor.Life<=0)actor.Dead=true;}
                Call(s,"RenderLocal");
                yield return new WaitForEndOfFrame();
                if(frame%4==0)Capture("frame-"+(frame/4).ToString("D3"),framesDirectory);
                if(frame==14||frame==44||frame==84)Capture("balance-blueflame-"+frame);
                if(frame==44)
                {
                    var tuning=UpgradeTuning.Data;
                    float originalScale=tuning.blueFlameVisualScale;
                    float originalIntensity=tuning.blueFlameImpactIntensity;
                    Vector3 originalCameraPosition=camera.transform.position;
                    float originalCameraSize=camera.orthographicSize;
                    var visuals=UnityEngine.Object.FindObjectsByType<BlueFlameVisual>(FindObjectsSortMode.None);
                    var ages=new float[visuals.Length];
                    var ageField=typeof(BlueFlameVisual).GetField("age",Hidden);
                    for(int i=0;i<visuals.Length;i++)ages[i]=(float)ageField.GetValue(visuals[i]);
                    try
                    {
                        foreach(int multiplier in new[]{3,5,10})
                        {
                            tuning.blueFlameVisualScale=multiplier;
                            for(int i=0;i<visuals.Length;i++)visuals[i].RenderAt(ages[i]);
                            Capture("balance-blueflame-size-"+multiplier);
                        }
                        tuning.blueFlameVisualScale=originalScale;
                        camera.transform.position=new Vector3(preview.Anchor.x,preview.Anchor.y,-10);
                        camera.orthographicSize=1.8f;
                        foreach(float intensity in new[]{1f,1.5f,2f,3f})
                        {
                            tuning.blueFlameImpactIntensity=intensity;
                            for(int i=0;i<visuals.Length;i++)visuals[i].RenderAt(ages[i]);
                            Capture("balance-blueflame-hit-"+intensity.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));
                        }
                    }
                    finally
                    {
                        tuning.blueFlameVisualScale=originalScale;
                        tuning.blueFlameImpactIntensity=originalIntensity;
                        camera.transform.position=originalCameraPosition;
                        camera.orthographicSize=originalCameraSize;
                        for(int i=0;i<visuals.Length;i++)visuals[i].RenderAt(ages[i]);
                    }
                }
            }
            var visual=UnityEngine.Object.FindFirstObjectByType<BlueFlameVisual>();
            Check(visual!=null&&visual.GetComponentInChildren<MeshRenderer>()!=null,"dedicated blue flame meshes rendered in game");
            Debug.Log("BLUE FLAME CHECKS PASS: 20 direct / 50 percent corridor / no duplicate collateral / real game capture");
        }

        void Capture(string name, string outputDirectory = null)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            string directory=Application.dataPath+"/../Screenshots";
#if UNITY_EDITOR
            if (name.StartsWith("balance-", StringComparison.Ordinal))
                directory = System.IO.Path.GetFullPath(Application.dataPath + "/../../../docs/검증");
#endif
            if(outputDirectory!=null)directory=outputDirectory;
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
