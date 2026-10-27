using System.Collections;
using System.IO;
using UnityEngine;
namespace OctopusSurvivors.EditorTools
{
    public sealed partial class DimensionRiftCapture
    {
        IEnumerator ValidateEnhancements(GameSession s,OctopusPlayer p)
        {
            var ids=new System.Collections.Generic.HashSet<byte>();
            foreach(ActorKind id in System.Enum.GetValues(typeof(ActorKind)))Check(ids.Add((byte)id),"unique actor id: "+id);
            Check(!CelestialArt.Supports(ActorKind.PickupCosmos),"Cosmos pickup is not treated as YinYang mark");
            Check(!SpecialSkillTuning.EligiblePair(1,true,true,true,true,SpecialId.YinYang,SpecialId.Cosmos,0,1),"solo production contact rejected");
            Check(!SpecialSkillTuning.EligiblePair(2,true,true,true,true,SpecialId.Cosmos,SpecialId.Cosmos,0,1),"matching specials rejected");
            Check(SpecialSkillTuning.EligiblePair(2,true,true,true,true,SpecialId.YinYang,SpecialId.Cosmos,0,1),"different transformed pair touching accepted");
            int stars=CosmosSequence.StarCount(1);
            Check(CosmosSequence.BirthTime(2,stars)>1 && CosmosSequence.BirthTime(stars-1,stars)-CosmosSequence.BirthTime(stars-2,stars)<.03f,"slow first stars accelerate into dense field");
            Actors(s).Clear();
            var enemy=Enemy(s,YinYangVisual.GroundAttackPoint(0,.1f),10000,10000);
            s.CastCelestial(Vector2.zero,p,SpecialId.YinYang,1);
            var mist=Actors(s).Find(a=>a.Kind==ActorKind.YinYangMist);
            mist.Age=YinYangVisual.GroundBirth(0)+.1f;
            Call(s,"DamageGroundCommas",mist,.02f);
            Check(enemy.Hp<10000,"existing ground comma pose deals contact damage");
            float hp=enemy.Hp;Call(s,"DamageGroundCommas",mist,.02f);
            Check(enemy.Hp==hp,"shared contact interval blocks duplicate comma damage");
            var camera=Camera.main;
            string output="Art/DimensionRift/Enhancements";Directory.CreateDirectory(output);
            foreach(var special in new[]{SpecialId.YinYang,SpecialId.Cosmos})
            {
                Actors(s).Clear();Enemy(s,new Vector2(4,1),100000,100000);
                s.CastCelestial(Vector2.zero,p,special,1);
                for(int f=0;f<120;f++)
                {
                    foreach(var actor in Actors(s).ToArray())if(!actor.Dead)Call(s,"TickCelestial",actor,1f/15);
                    if(special==SpecialId.Cosmos && f==65)Check(Actors(s).Exists(a=>!a.Dead&&a.Kind==ActorKind.CosmicStar),"stars survive the full configurable buildup");
                    if(special==SpecialId.Cosmos && f==90)Check(Actors(s).Exists(a=>!a.Dead&&a.Kind==ActorKind.CosmicMeteor),"field stars enter existing free homing trajectories");
                    Call(s,"RenderLocal");yield return null;
                    if(f%10==0)Capture(camera,Path.Combine(output,special+"-"+f.ToString("D3")+".png"));
                }
            }
            Actors(s).Clear();s.CastSpecialConvergence(Vector2.zero);s.RequestDevSpecialConvergence();
            Check(Actors(s).Count==1,"solo developer trigger blocks overlapping convergence");
            for(int f=0;f<100;f++)
            {
                foreach(var actor in Actors(s).ToArray())if(!actor.Dead)Call(s,"TickSpecialEnhancement",actor,1f/15);
                Call(s,"RenderLocal");yield return null;
                if(f%10==0)Capture(camera,Path.Combine(output,"Convergence-"+f.ToString("D3")+".png"));
            }
            Check(errors==0,"enhancement gameplay and captures have no runtime errors");
        }
    }
}
