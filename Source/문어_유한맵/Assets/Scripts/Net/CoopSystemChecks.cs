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
                            Check(player.SlotElement(3)!=ElementId.None&&player.ComboCount==1,"guest fourth slot and ingredient-preserving synthesis replicated");
                            guestMovementChecks++;guestMoving=false;
                        }
                    }
                    if(finishRequested&&guestMovementChecks==4){Check(sawGiantVisual,"giant visual snapshots received");Debug.Log("COOP CLIENT PASS");done=true;Application.Quit(0);}
                }
            }
            catch(Exception e){Debug.LogError("COOP CHECK FAILED: "+e);done=true;Application.Quit(2);}
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
            Check(p.HasElement(ElementId.Fire)&&p.HasElement(ElementId.Water),"synthesis retains ingredients");
            Check(p.ServerCombine(0,2)&&p.ServerCombine(0,3)&&!p.ServerCombine(1,2),"three combo maximum enforced");
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
