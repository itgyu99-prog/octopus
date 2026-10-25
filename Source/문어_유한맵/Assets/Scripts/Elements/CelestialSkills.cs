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
                for (int i=0; i<48+level*6; i++)
                {
                    var star=Effect(ActorKind.CosmicStar,center+Random.insideUnitCircle*13f,Vector2.zero,
                        .8f+Random.value*.6f,4.5f,65f*damage,owner,level);
                    star.Pulse=.45f+i*.015f; star.Age=-i*.025f; star.Mark=i;
                }
            }
            else if (id == SpecialId.YinYang)
                Effect(ActorKind.YinYangOrbit,center,Vector2.zero,5.5f+level*.35f,YinYangVisual.OrbitDuration,230f*damage,owner,level);
        }
        bool TickCelestial(SimActor a,float dt)
        {
            if(a.Kind<ActorKind.CosmicStar || a.Kind>ActorKind.YinYangShockwave)return false;
            a.Age+=dt; a.Life-=dt;
            if(a.Kind==ActorKind.CosmicStar)
            {
                if(a.Age>=a.Pulse)
                {
                    var target=NearestEnemy(a.Pos,24f);
                    if(target!=null)
                    {
                        a.Kind=ActorKind.CosmicMeteor;a.Target=target;
                        a.Age=0;a.Life=a.Duration=1.8f;a.Size=2.2f;a.Pulse=0;
                    }
                }
            }
            else if(a.Kind==ActorKind.CosmicMeteor)
            {
                if(a.Target==null || !CanTargetActor(a.Target))a.Target=NearestEnemy(a.Pos,24f);
                if(a.Target!=null)
                {
                    Vector2 delta=a.Target.Pos-a.Pos;a.Vel=delta.normalized*24f;
                    if(delta.magnitude<=dt*24f+.4f)
                    {
                        a.Pos=a.Target.Pos;
                        DamageCircle(a.Pos,1.3f,a.Damage,StatusMask.None,0,0);
                        Effect(ActorKind.CosmicImpact,a.Pos,Vector2.zero,3f,.5f,0,a.Owner,1);a.Dead=true;
                    }
                    else a.Pos+=a.Vel*dt;
                    a.Pulse-=dt;
                    if(a.Pulse<=0 && !a.Dead)
                    {
                        Effect(ActorKind.CosmicTrail,a.Pos,a.Vel,2f,.25f,0,a.Owner,1);a.Pulse=.065f;
                    }
                }
            }
            else if(a.Kind==ActorKind.YinYangOrbit)
            {
                if(a.Owner!=null)
                {
                    if(!a.Owner.IsSpawned || a.Owner.Downed.Value){a.Dead=true;return true;}
                    a.Pos=a.Owner.Pos;
                }
                if(a.Life<=0)Effect(ActorKind.YinYangCollision,a.Pos,Vector2.zero,a.Size,YinYangVisual.MergeDuration,a.Damage,a.Owner,a.EffectLevel);
            }
            else if(a.Kind==ActorKind.YinYangCollision && a.Life<=0)
            {
                DamageCircle(a.Pos,a.Size,a.Damage,StatusMask.Knockback,.5f,0);
                Effect(ActorKind.YinYangBurst,a.Pos,Vector2.zero,a.Size*2.9f,.88f,0,a.Owner,1);
                Effect(ActorKind.YinYangShockwave,a.Pos,Vector2.zero,a.Size*3.4f,.96f,0,a.Owner,1);
            }
            if(a.Life<=0)a.Dead=true;
            return true;
        }
    }
}
