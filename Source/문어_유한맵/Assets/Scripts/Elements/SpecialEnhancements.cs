using System.Collections.Generic;
using UnityEngine;
namespace OctopusSurvivors
{
    public sealed partial class GameSession
    {
        bool specialContactLatched;
        public void CastSpecialConvergence(Vector2 center)
        {
            if (!IsServer || !CombatActive || _actors.Exists(a=>!a.Dead && a.Kind==ActorKind.SpecialConvergence)) return;
            var t=SpecialSkillTuning.Data;
            Effect(ActorKind.SpecialConvergence,center,Vector2.zero,t.convergenceRadius,Mathf.Max(2,t.convergenceDuration),0,null,1);
        }
        void DamageGroundCommas(SimActor source,float dt)
        {
            source.ContactHitTimes ??= new Dictionary<ushort,float>();
            var tuning=SpecialSkillTuning.Data;
            float from=Mathf.Max(0,source.Age-dt);
            int count=_actors.Count;
            for(int n=0;n<count;n++)
            {
                var enemy=_actors[n]; if(!CanTargetActor(enemy))continue;
                if(source.ContactHitTimes.TryGetValue(enemy.Id,out float next) && source.Age<next)continue;
                for(int i=0;i<YinYangVisual.GroundCount;i++)
                {
                    float birth=YinYangVisual.GroundBirth(i);
                    float start=Mathf.Max(from,birth),end=Mathf.Min(source.Age,birth+YinYangVisual.GroundLifetime);
                    if(end<start)continue;
                    Vector2 a=YinYangVisual.GroundAttackPoint(i,start-birth);
                    Vector2 b=YinYangVisual.GroundAttackPoint(i,end-birth);
                    if(SegmentDistanceToActor(a,b,enemy)>tuning.smallCommaHitRadius)continue;
                    source.ContactHitTimes[enemy.Id]=source.Age+Mathf.Max(.08f,tuning.smallCommaHitInterval);
                    ApplyDamage(enemy,source.Damage,StatusMask.None,0,0);
                    ShowYinYangContact(source,enemy,i%2==0);break;
                }
            }
        }
        bool TickSpecialEnhancement(SimActor a,float dt)
        {
            if(a.Kind!=ActorKind.YinYangMote && a.Kind!=ActorKind.SpecialConvergence) return false;
            a.Age+=dt;a.Life-=dt;
            var t=SpecialSkillTuning.Data;
            if(a.Kind==ActorKind.CosmosField)
            {
                while(a.Bounces<a.Mark && SpecialSkillTuning.StarSpawnTime(a.Bounces,a.Mark)<=a.Age)
                {
                    Vector2 pos=FiniteBeach.Clamp(a.Pos+Random.insideUnitCircle*t.cosmosFieldRadius,.3f);
                    float wait=Mathf.Max(.1f,t.cosmosBuildDuration+t.cosmosHoldDuration-a.Age);
                    var star=Effect(ActorKind.CosmicStar,pos,Vector2.zero,(.8f+Random.value*.6f)*t.cosmosStarScale,wait+2.1f,a.Damage,a.Owner,a.EffectLevel);
                    star.Pulse=wait;star.Mark=a.Bounces++;
                }
            }
            else if(a.Kind==ActorKind.YinYangMote)
            {
                Vector2 from=a.Pos;
                a.Pos=SpecialSkillTuning.CommaPosition(a.Anchor,a.Age,a.Mark);
                a.Vel=(a.Pos-from)/Mathf.Max(.001f,dt);
                var group=a.DamageGroup??a;
                if(group.ContactHitTimes==null)group.ContactHitTimes=new Dictionary<ushort,float>();
                int count=_actors.Count;
                for(int i=0;i<count;i++)
                {
                    var enemy=_actors[i];if(!CanTargetActor(enemy))continue;
                    if(group.ContactHitTimes.TryGetValue(enemy.Id,out float next) && a.Age<next)continue;
                    Vector2 segment=a.Pos-from;
                    float u=segment.sqrMagnitude>.00001f?Mathf.Clamp01(Vector2.Dot(enemy.Pos-from,segment)/segment.sqrMagnitude):0;
                    float radius=Mathf.Max(.05f,t.smallCommaHitRadius)+enemy.Size*.3f;
                    if((enemy.Pos-(from+segment*u)).sqrMagnitude>radius*radius)continue;
                    group.ContactHitTimes[enemy.Id]=a.Age+Mathf.Max(.08f,t.smallCommaHitInterval);
                    ApplyDamage(enemy,a.Damage,StatusMask.None,0,0);
                }
            }
            // Convergence intentionally has no hit, damage, or execution calls.
            if(a.Life<=0)a.Dead=true;
            return true;
        }
    }
}
