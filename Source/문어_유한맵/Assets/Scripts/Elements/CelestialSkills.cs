using UnityEngine;
namespace OctopusSurvivors
{
    public sealed partial class GameSession
    {
        public void CastCelestial(Vector2 center, OctopusPlayer owner, SpecialId id, int level)
        {
            if (!IsServer) return;
            float damage = 1f + .3f * (level - 1);
            if (id == SpecialId.Cosmos)
            {
                var field=YinYangVisual.MistFieldBounds;
                var cast=Effect(ActorKind.CosmicDusk,field.center,Vector2.zero,field.size.x,12f,0,owner,level);
                cast.Pos=field.center;
                if(cast.Dead)return;
                int count=CosmosSequence.StarCount(level);
                float perStar=65f*damage*(48+level*6)/count;
                for (int i=0; i<count; i++)
                {
                    Vector2 position=CelestialField.Point(i,count);
                    var star=Effect(ActorKind.CosmicStar,position,Vector2.zero,
                        .55f+CosmosSequence.Hash(i)*.45f,4f,perStar,owner,level);
                    star.Pos=star.Anchor=position;star.SkillPoint=center;star.CelestialCast=cast;
                    float birth=CosmosSequence.BirthTime(i,count);
                    star.Pulse=CosmosSequence.ReleaseTime-birth+i*.003f;
                    star.Age=-birth; star.Mark=i;
                }
            }
            else if (id == SpecialId.YinYang)
            {
                Effect(ActorKind.YinYangOrbit,center,Vector2.zero,5.5f+level*.35f,YinYangVisual.OrbitDuration,345f*damage,owner,level);
                var field=YinYangVisual.MistFieldBounds;
                Effect(ActorKind.YinYangMist,field.center,Vector2.zero,field.size.x,YinYangVisual.OrbitDuration+YinYangVisual.MergeDuration+.18f,0,owner,level);
            }
        }
        bool TickCelestial(SimActor a,float dt)
        {
            if(a.Kind<ActorKind.CosmicStar || a.Kind>ActorKind.CosmicDusk)return false;
            a.Age+=dt; a.Life-=dt;
            if(a.Kind==ActorKind.CosmicStar)
            {
                if(a.Age>=0)a.Pos=a.Anchor+new Vector2(Mathf.Sin(a.Age*.9f+a.Mark),Mathf.Cos(a.Age*.7f+a.Mark))*.12f;
                if(a.Age>=a.Pulse)
                {
                    var target=CosmosTarget(a);
                    if(target!=null)
                    {
                        a.Kind=ActorKind.CosmicMeteor;a.Target=target;
                        float heading=CosmosSequence.Hash(a.Mark+419)*Mathf.PI*2;
                        a.Vel=new Vector2(Mathf.Cos(heading),Mathf.Sin(heading))*(7f+CosmosSequence.Hash(a.Mark+29)*7f);
                        a.Age=0;a.Life=a.Duration=6f;a.Size=.5f+CosmosSequence.Hash(a.Mark)*.22f;
                    }
                    else {a.Kind=ActorKind.CosmicTrail;a.Age=0;a.Life=a.Duration=.35f;}
                }
            }
            else if(a.Kind==ActorKind.CosmicMeteor)
            {
                if(a.Target==null || !CanTargetActor(a.Target))a.Target=CosmosTarget(a);
                if(a.Target==null){a.Dead=true;return true;}
                int steps=Mathf.Max(1,Mathf.CeilToInt(dt/.025f));float step=dt/steps;
                for(int i=0;i<steps && !a.Dead;i++)
                {
                    Vector2 delta=a.Target.Pos-a.Pos;
                    float age=a.Age-dt+(i+1)*step;
                    float phase=CosmosSequence.Hash(a.Mark+311)*Mathf.PI*2;
                    float freedom=Mathf.Clamp01((delta.magnitude-1.5f)/8f)*Mathf.Clamp01(1.3f-age*.48f);
                    float curl=Mathf.Sin(age*(3.6f+CosmosSequence.Hash(a.Mark+67)*3f)+phase)*.95f
                        +Mathf.Sin(age*8.1f-phase)*.3f;
                    Vector2 sideways=new Vector2(-delta.y,delta.x).normalized;
                    Vector2 direction=(delta.normalized+sideways*curl*freedom).normalized;
                    a.Vel=Vector2.Lerp(a.Vel,direction*Mathf.Min(55f,15f+age*16f),1-Mathf.Exp(-(3.8f+age*2.5f)*step));
                    Vector2 next=a.Pos+a.Vel*step;
                    if(SegmentDistanceToActor(a.Pos,next,a.Target)<=.35f)
                    {
                        a.Pos=a.Target.Pos;
                        DamageCircle(a.Pos,1.3f,a.Damage,StatusMask.None,0,0);
                        float impactSize=Mathf.Clamp(a.Target.Size*1.6f,.7f,2.1f);
                        var impact=Effect(ActorKind.CosmicImpact,a.Pos,Vector2.zero,impactSize,.46f,0,a.Owner,1);
                        impact.Pos=a.Pos;impact.Mark=a.Mark;impact.CelestialCast=a.CelestialCast;a.Dead=true;
                    }
                    else a.Pos=next;
                }
            }
            else if(a.Kind==ActorKind.CosmicDusk)
            {
                bool cancel=a.Owner!=null && (!a.Owner.IsSpawned || a.Owner.Downed.Value) && a.Age<CosmosSequence.ReleaseTime;
                if(cancel)foreach(var child in _actors)if(child.CelestialCast==a)child.Dead=true;
                if(a.Mark==0 && (cancel || a.Age>=CosmosSequence.ReleaseTime))
                {
                    bool active=false;
                    foreach(var child in _actors)if(!child.Dead && child.CelestialCast==a){active=true;break;}
                    if(!active){a.Mark=1;a.Age=0;a.Life=a.Duration=CosmosSequence.RestoreDuration;}
                }
            }
            else if(a.Kind==ActorKind.YinYangOrbit)
            {
                Vector2 previousCenter = a.Pos;
                if(a.Owner!=null)
                {
                    if(!a.Owner.IsSpawned || a.Owner.Downed.Value){a.Dead=true;return true;}
                    a.Pos=a.Owner.Pos;
                }
                DamageYinYangOrbit(a, previousCenter, dt);
                if(a.Life<=0)Effect(ActorKind.YinYangCollision,a.Pos,Vector2.zero,a.Size,YinYangVisual.MergeDuration,a.Damage,a.Owner,a.EffectLevel);
            }
            else if(a.Kind==ActorKind.YinYangCollision && a.Life<=0)
            {
                float radius = a.Size * 1.8f;
                Effect(ActorKind.YinYangBurst,a.Pos,Vector2.zero,radius*2f,YinYangVisual.BurstDuration,a.Damage,a.Owner,a.EffectLevel);
                Effect(ActorKind.YinYangShockwave,a.Pos,Vector2.zero,radius*2.2f,.96f,0,a.Owner,1);
            }
            else if(a.Kind==ActorKind.YinYangBurst)
            {
                // Catch up every scheduled hit even after a slow server frame.
                while(a.Mark<YinYangVisual.BurstHits && a.Age>=a.Mark*YinYangVisual.HitInterval)
                {
                    PulseYinYang(a); a.Mark++;
                }
            }
            else if(a.Kind==ActorKind.YinYangMark || a.Kind==ActorKind.YinYangHitWhite || a.Kind==ActorKind.YinYangHitBlack)
            {
                if(a.Target!=null && !a.Target.Dead)a.Pos=a.Target.Pos;
            }
            else if(a.Kind==ActorKind.YinYangMist && a.Owner!=null)
            {
                if(!a.Owner.IsSpawned || a.Owner.Downed.Value){a.Dead=true;return true;}
            }
            if(a.Life<=0)a.Dead=true;
            return true;
        }

        void DamageYinYangOrbit(SimActor source, Vector2 previousCenter, float dt)
        {
            float start = Mathf.Max(0, source.Age-dt), end = Mathf.Min(source.Age, source.Duration);
            int steps = Mathf.Max(1, Mathf.CeilToInt((end-start)/.025f));
            for(int step=0;step<steps;step++)
            {
                float t0=Mathf.Lerp(start,end,step/(float)steps), t1=Mathf.Lerp(start,end,(step+1f)/steps);
                Vector2 c0=Vector2.Lerp(previousCenter,source.Pos,step/(float)steps);
                Vector2 c1=Vector2.Lerp(previousCenter,source.Pos,(step+1f)/steps);
                int count=_actors.Count;
                for(int i=0;i<count;i++)
                {
                    var target=_actors[i]; if(!CanTargetActor(target))continue;
                    int hitSide=-1;float nearest=float.PositiveInfinity;
                    for(int side=0;side<2;side++)
                    {
                        Vector2 from=c0+YinYangVisual.OrbitPosition(t0/source.Duration,source.Size,side);
                        Vector2 to=c1+YinYangVisual.OrbitPosition(t1/source.Duration,source.Size,side);
                        float distance=SegmentDistanceToActor(from,to,target);
                        if(distance<=source.Size*.16f+GameConfig.EnemyRadius && distance<nearest)
                        {hitSide=side;nearest=distance;}
                    }
                    if(hitSide>=0)
                    {
                        ShowYinYangContact(source,target,hitSide==1);
                        ApplyDamage(target,source.Damage*.18f*(t1-t0),StatusMask.None,0,0);
                    }
                }
            }
        }

        void ShowYinYangContact(SimActor source,SimActor target,bool white)
        {
            var kind=white?ActorKind.YinYangHitWhite:ActorKind.YinYangHitBlack;
            // Keep sustained contact readable without creating an effect every damage substep.
            foreach(var actor in _actors)
                if(!actor.Dead && actor.Kind==kind && actor.Target==target && actor.Owner==source.Owner)return;
            var hit=Effect(kind,target.Pos,Vector2.zero,Mathf.Clamp(target.Size*1.35f,.8f,1.8f)*1.5f,.18f,0,source.Owner,1);
            hit.Target=target;
        }

        SimActor CosmosTarget(SimActor star)
        {
            var targets=new System.Collections.Generic.List<SimActor>();
            foreach(var actor in _actors)if(CanTargetActor(actor))targets.Add(actor);
            return targets.Count>0?targets[star.Mark%targets.Count]:null;
        }

        void PulseYinYang(SimActor source)
        {
            var targets=new System.Collections.Generic.List<SimActor>();
            foreach(var actor in _actors)
                if(CanTargetActor(actor) && DistanceToActor(source.Pos,actor)<=source.Size*.5f)targets.Add(actor);
            foreach(var target in targets)
            {
                ApplyDamage(target,source.Damage/YinYangVisual.BurstHits,StatusMask.None,0,0);
                // Repeated impacts must not push the victim outside later pulses.
                target.Knockback=Vector2.zero;
            }
        }
    }
}
