#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace OctopusSurvivors
{
    public static class GiantCombatChecks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static object Call(GameSession s,string name,params object[] args)=>typeof(GameSession).GetMethod(name,Hidden).Invoke(s,args);
        static void Check(bool good,string message){if(!good)throw new Exception(message);Debug.Log("GIANT CHECK PASS: "+message);}
        public static void Run(GameSession s,OctopusPlayer player)
        {
            var actors=(List<SimActor>)typeof(GameSession).GetField("_actors",Hidden).GetValue(s);
            Check(GiantMonsterDefinition.All.Length==5,"five definitions loaded");
            for(int stage=1;stage<=15;stage++)
            {
                GiantMonsterDefinition.SpawnBand(stage,out float chance,out int cap);
                Check(chance==(stage<3?0:stage<5?.02f:stage<8?.04f:stage<11?.06f:.08f) && cap==(stage<3?0:stage<5?1:stage<8?2:3),"stage spawn band "+stage);
            }
            int savedStage=s.Stage.Value;bool savedBoss=s.BossAlive.Value;var random=UnityEngine.Random.state;
            UnityEngine.Random.InitState(73493);
            foreach(int stage in new[]{1,3,4,6,8,11})
            {
                actors.Clear();s.Stage.Value=stage;s.BossAlive.Value=false;
                for(int attempt=0;attempt<800;attempt++)
                {
                    var candidate=new SimActor{Kind=ActorKind.Enemy,Pos=player.Pos+Vector2.up*16};
                    Call(s,"TryMakeGiant",candidate);if(candidate.Giant!=null)actors.Add(candidate);
                }
                GiantMonsterDefinition.SpawnBand(stage,out _,out int cap);
                Check(actors.Count==cap && actors.TrueForAll(a=>a.Giant.unlockStage<=stage),"actual spawn cap and unlock selection at "+stage);
            }
            actors.Clear();s.BossAlive.Value=true;
            bool blocked=true;for(int i=0;i<100;i++)
            {var candidate=new SimActor{Kind=ActorKind.Enemy};Call(s,"TryMakeGiant",candidate);blocked &= candidate.Giant==null;}
            Check(blocked,"boss blocks replacement");
            s.Stage.Value=savedStage;s.BossAlive.Value=savedBoss;UnityEngine.Random.state=random;
            foreach(var d in GiantMonsterDefinition.All)
            {
                actors.Clear();
                var e=s.SpawnPersistent(ActorKind.Enemy,player.Pos+Vector2.right*2,Vector2.zero,.8f*d.sizeMultiplier,100,20,null,StatusMask.None);
                e.Giant=d;e.Hp=e.MaxHp=1000;e.Damage=20;e.GiantState=GiantState.Chase;
                Call(s,"ApplyDamage",e,20f,StatusMask.Stun|StatusMask.Burn,2f,3f);
                Check(Mathf.Abs(e.StunTime-1)<.001f && e.BurnTime==2,"half stun and full burn: "+d.displayName);
                Check(e.Knockback.magnitude<=GameConfig.Knockback*d.knockbackMultiplier+.001f,"knockback resistance: "+d.displayName);
                e.StunTime=0;e.BurnTime=0;e.Knockback=Vector2.zero;e.Pulse=0;
                Call(s,"TickGiant",e,.01f,1f);
                Check(e.GiantState==GiantState.PrepareAttack,"attack telegraph: "+d.displayName);
                Vector2 locked=e.SkillPoint;
                Call(s,"TickGiant",e,d.prepareTime+.01f,1f);
                Check(e.GiantState==GiantState.Attack,"attack starts: "+d.displayName);
                if(d.attackPattern==GiantPattern.Hermit)
                {
                    Call(s,"TickGiant",e,.1f,1f);
                    Check(e.SkillPoint==locked && e.ChargeTravel>0,"charge locks direction");
                }
                if(d.attackPattern==GiantPattern.Starfish)
                {
                    Check(actors.FindAll(a=>a.Kind==ActorKind.GiantSpike).Count==d.projectileCount,"starfish fan volley");
                    int before=actors.Count;e.Hp=e.MaxHp*.2f;Call(s,"GiantSpikes",e,false);
                    Check(actors.Count-before==d.projectileCount+3,"low-health starfish adds three spikes");
                    var shot=actors.Find(a=>a.Kind==ActorKind.GiantSpike);shot.Pos=player.Pos+Vector2.up*20;
                    Call(s,"TickGiantSpike",shot,d.projectileDistance/d.projectileSpeed+.01f);Check(shot.Dead,"spikes expire at configured travel distance");
                }
                if(d.attackPattern==GiantPattern.Urchin)
                {
                    float before=e.Hp;
                    Call(s,"ApplyDamage",e,20f,StatusMask.None,0f,0f);
                    Check(Mathf.Abs(before-e.Hp-20*d.defenseDamageMultiplier)<.01f,"defense damage reduction");
                    int count=actors.Count;Call(s,"ApplyDamage",e,20f,StatusMask.None,0f,0f);
                    Check(actors.Count==count,"counter internal cooldown");
                }
                Call(s,"KillEnemy",e);int drops=actors.Count;Call(s,"KillEnemy",e);
                Check(e.Dead && e.GiantState==GiantState.Dead && actors.Count==drops,"single death/drop: "+d.displayName);
            }
            actors.Clear();
            var boss=s.SpawnPersistent(ActorKind.Boss,player.Pos+Vector2.up*6,Vector2.zero,3,100,1,null,StatusMask.None);boss.Hp=boss.MaxHp=1000;
            Call(s,"ApplyDamage",boss,1f,StatusMask.Stun,2f,0f);Check(boss.StunTime==0,"boss remains stun immune");
            actors.Clear();
            var giant=s.SpawnPersistent(ActorKind.Enemy,Vector2.one*8,Vector2.zero,3,100,1,null,StatusMask.None);giant.Giant=GiantMonsterDefinition.All[0];
            Call(s,"ClearFieldForNextStagePreservingGiants",true);Check(!giant.Dead,"boss transition preserves existing giant");
            actors.Clear();Debug.Log("GIANT COMBAT CHECKS PASS");
        }
    }
}
#endif
