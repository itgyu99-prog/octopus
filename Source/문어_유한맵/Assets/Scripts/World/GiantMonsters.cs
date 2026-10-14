using UnityEngine;

namespace OctopusSurvivors
{
    public sealed partial class GameSession
    {
        static float GiantDefenseFactor(SimActor e) => e.Giant != null && e.Giant.attackPattern == GiantPattern.Urchin && e.GiantState == GiantState.Attack ? e.Giant.defenseDamageMultiplier : 1f;

        void TryMakeGiant(SimActor enemy)
        {
            if(enemy.Dead || BossAlive.Value) return;
            GiantMonsterDefinition.SpawnBand(Stage.Value,out float chance,out int limit);
            int living=0;foreach(var a in _actors) if(!a.Dead && a.Giant!=null) living++;
            if(living>=limit || Random.value>=chance) return;
            float weight=0;
            foreach(var d in GiantMonsterDefinition.All) if(d.unlockStage<=Stage.Value) weight+=d.spawnWeight;
            if(weight<=0) return;
            float roll=Random.value*weight;
            foreach(var d in GiantMonsterDefinition.All)
            {
                if(d.unlockStage>Stage.Value || d.spawnWeight<=0) continue;
                roll-=d.spawnWeight;if(roll>0) continue;
                bool hidden=false;
                for(int attempt=0;attempt<80;attempt++)
                {
                    Vector2 point=attempt==0?enemy.Pos:RandomEdge();
                    bool visible=false;
                    foreach(var player in OctopusPlayer.All)
                    {
                        if(player==null || !player.IsSpawned || player.Downed.Value)continue;
                        Vector2 center=Camera.main!=null?(Vector2)FiniteBeach.ClampCamera(player.Pos,Camera.main):player.Pos;
                        Vector2 delta=point-center;
                        float margin=d.sizeMultiplier*.5f;
                        float aspect=Mathf.Max(2.4f,Camera.main!=null?Camera.main.aspect:2.4f);
                        if(Mathf.Abs(delta.x)<GameConfig.CameraSize*aspect+margin && Mathf.Abs(delta.y)<GameConfig.CameraSize+margin){visible=true;break;}
                    }
                    if(!visible){enemy.Pos=point;hidden=true;break;}
                }
                if(!hidden)return;
                enemy.Giant=d; enemy.Element=ElementId.None;
                enemy.Size=.8f*d.sizeMultiplier;enemy.Hp=enemy.MaxHp=GameConfig.EnemyHp(Elapsed.Value)*d.hpMultiplier;
                enemy.Damage=GameConfig.EnemyContactDamage*d.damageMultiplier;
                enemy.GiantState=GiantState.Chase;enemy.Pulse=d.attackCooldown*.5f;
                return;
            }
        }
        void TickGiant(SimActor e,float dt,float speed)
        {
            var d=e.Giant;e.CounterTime=Mathf.Max(0,e.CounterTime-dt);
            if(e.StunTime>0) { e.GiantState=GiantState.Stunned; return; }
            if(e.GiantState==GiantState.Stunned) { e.GiantState=GiantState.Recovery;e.SkillTime=d.recoveryTime; }
            e.SkillTime-=dt;e.Pulse-=dt;
            // Only two players exist; scan once per tick and always discard downed targets.
            var player=NearestLivingPlayer(e.Pos);
            if(player==null) {e.GiantState=GiantState.Idle;return;}
            Vector2 to=player.Pos-e.Pos;
            if(e.GiantState==GiantState.Recovery)
            {if(e.SkillTime<=0)e.GiantState=GiantState.Chase;return;}
            if(e.GiantState==GiantState.Attack)
            {
                if(d.attackPattern==GiantPattern.Hermit)
                {
                    Vector2 previous=e.Pos;
                    if(e.RootTime<=0) e.Pos=GameConfig.ClampToMap(e.Pos+e.SkillPoint*d.chargeSpeed*(e.SlowTime>0?.45f:1)*dt,e.Size*.3f);
                    e.ChargeTravel+=Vector2.Distance(previous,e.Pos);
                    GiantHitPlayers(e,previous,e.Pos,e.Size*.4f,false);
                    if(e.ChargeTravel<d.chargeDistance && e.SkillTime>0) return;
                }
                else if(e.SkillTime>0) return;
                e.GiantState=GiantState.Recovery;e.SkillTime=d.recoveryTime;e.Pulse=d.attackCooldown;return;
            }
            if(e.GiantState==GiantState.PrepareAttack)
            {
                if(e.SkillTime>0) return;
                e.GiantState=GiantState.Attack;e.HitPlayers=0;
                switch(d.attackPattern)
                {
                    case GiantPattern.Crab:
                        GiantHitPlayers(e,e.Pos,e.Pos,d.attackRange,true);e.SkillTime=.2f;break;
                    case GiantPattern.Hermit:
                        e.ChargeTravel=0;e.SkillTime=d.chargeDistance/d.chargeSpeed*2;break;
                    case GiantPattern.Jellyfish:
                        GiantHitPlayers(e,e.Pos,e.Pos,d.attackRange,false);e.SkillTime=.25f;break;
                    case GiantPattern.Starfish:
                        GiantSpikes(e,false);e.SkillTime=.2f;break;
                    case GiantPattern.Urchin:e.SkillTime=d.defenseDuration;break;
                }
                return;
            }
            e.GiantState=GiantState.Chase;
            float distance=to.magnitude;
            bool ranged=d.attackPattern==GiantPattern.Starfish;
            bool ready=d.attackPattern==GiantPattern.Urchin || distance<= (d.attackPattern==GiantPattern.Hermit?d.chargeDistance:d.attackRange);
            if(e.Pulse<=0 && ready)
            {
                e.GiantState=GiantState.PrepareAttack;e.SkillTime=d.prepareTime;
                e.SkillPoint=to.sqrMagnitude>.001f?to.normalized:Vector2.right;
                e.Vel=e.SkillPoint;return;
            }
            Vector2 movement=to.normalized;
            if(ranged) movement=distance<d.attackRange*.65f?-movement:distance>d.attackRange*.9f?movement:Vector2.zero;
            if(d.attackPattern==GiantPattern.Jellyfish && distance<d.attackRange*.8f) movement=new Vector2(-movement.y,movement.x);
            if(e.SpicyTime>0) movement=e.SpicyDir;
            e.Pos=GameConfig.ClampToMap(e.Pos+movement*speed*d.speedMultiplier*dt,e.Size*.3f);
            e.Vel=movement;
        }
        void GiantHitPlayers(SimActor e,Vector2 from,Vector2 end,float radius,bool cone)
        {
            Vector2 segment=end-from;float length=segment.sqrMagnitude;
            for(int i=0;i<OctopusPlayer.All.Count;i++)
            {
                var p=OctopusPlayer.All[i];if(p==null || !p.IsSpawned || p.Downed.Value || (e.HitPlayers & (1<<i))!=0)continue;
                float t=length>0?Mathf.Clamp01(Vector2.Dot(p.Pos-from,segment)/length):0;
                if((p.Pos-(from+segment*t)).sqrMagnitude>(radius+GameConfig.PlayerRadius)*(radius+GameConfig.PlayerRadius))continue;
                if(cone && Vector2.Angle(e.SkillPoint,p.Pos-e.Pos)>65)continue;
                p.ApplyDamage(e.Damage,DamageSource.NormalEnemy);e.HitPlayers|=1<<i;
            }
        }
        void GiantSpikes(SimActor e,bool radial)
        {
            var d=e.Giant;int count=d.projectileCount+(e.Hp/e.MaxHp<d.enragedHpRatio?3:0);
            for(int i=0;i<count;i++)
            {
                float angle=radial?i*360f/count:(i-(count-1)*.5f)*14f;
                Vector2 dir=AbilityRunner.Rotate(e.SkillPoint.sqrMagnitude>.01f?e.SkillPoint:Vector2.right,angle);
                var shot=SpawnTransient(ActorKind.GiantSpike,e.Pos+dir*.5f,.18f,d.projectileDistance/d.projectileSpeed);
                shot.Vel=dir*d.projectileSpeed;shot.Damage=e.Damage;
            }
        }
        void TickGiantSpike(SimActor shot,float dt)
        {
            Vector2 previous=shot.Pos;shot.Pos+=shot.Vel*dt;shot.Life-=dt;
            Vector2 segment=shot.Pos-previous;float length=segment.sqrMagnitude;
            foreach(var p in OctopusPlayer.All)
            {
                if(p==null || !p.IsSpawned || p.Downed.Value)continue;
                float t=length>0?Mathf.Clamp01(Vector2.Dot(p.Pos-previous,segment)/length):0;
                if((p.Pos-previous-segment*t).sqrMagnitude>Mathf.Pow(GameConfig.PlayerRadius+shot.Size,2))continue;
                p.ApplyDamage(shot.Damage,DamageSource.NormalEnemy);shot.Dead=true;return;
            }
            if(shot.Life<=0)shot.Dead=true;
        }
    }
}
