using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace OctopusSurvivors
{
    // The trajectory never queries enemies. All three shots keep the cast position/direction.
    public static class FireOrbGeometry
    {
        public const float SampleStep = 1f / 60f;
        public static float Launch(int index) => index * UpgradeTuning.Data.fireOrbLaunchGap;
        public static float ReturnStart(float duration) => duration - UpgradeTuning.Data.fireOrbReturnTime;
        public static Vector2 Position(Vector2 origin, Vector2 direction, float reach, int index,
            float age, float duration, Vector2 owner)
        {
            var t = UpgradeTuning.Data;
            float launch = Launch(index), arrival = launch + t.fireOrbThrowTime;
            float back = ReturnStart(duration);
            Vector2 goal = origin + direction * reach;
            if (age <= launch) return origin;
            if (age < arrival) return Vector2.Lerp(origin, goal, Mathf.SmoothStep(0, 1, (age - launch) / t.fireOrbThrowTime));
            if (age < back) return Patrol(goal, direction, age - arrival);
            Vector2 start = Patrol(goal, direction, back - arrival);
            return Vector2.Lerp(start, owner, Mathf.SmoothStep(0, 1, (age - back) / t.fireOrbReturnTime));
        }
        static Vector2 Patrol(Vector2 goal, Vector2 direction, float age) => goal + direction *
            (Mathf.Sin(age * UpgradeTuning.Data.fireOrbPatrolSpeed) * UpgradeTuning.Data.fireOrbPatrolDistance);
        public static float TrailRadius(float radius, float sampleAge) => radius *
            Mathf.Lerp(.9f, .12f, Mathf.Clamp01(sampleAge / UpgradeTuning.Data.fireOrbTrailTime));
    }

    public sealed partial class GameSession
    {
        sealed class FireTrail
        {
            public struct Sample { public Vector2 Position; public float Age; }
            public readonly List<Sample> Samples = new List<Sample>(40);
            public float Age;
            public Vector2 Owner;
        }
        readonly ConditionalWeakTable<SimActor, FireTrail> fireOrbTrails = new ConditionalWeakTable<SimActor, FireTrail>();

        void CastFireOrbs(OctopusPlayer player, Vector2 direction, int level, float multiplier)
        {
            var tuning = UpgradeTuning.Data;
            float reach = (4.3f + tuning.fireRangePerLevel * (level - 1)) * tuning.fireSizeMultiplier;
            for (int i = 0; i < 3; i++)
            {
                Vector2 heading = AbilityRunner.Rotate(direction, (i - 1) * tuning.fireOrbSpread);
                var orb = Effect(ActorKind.FlameAura, player.Pos, heading, tuning.fireOrbRadius,
                    tuning.fireOrbDuration, tuning.fireOrbDps * multiplier, player, level);
                orb.SkillPoint = heading; orb.SkillTime = reach; orb.Bounces = i;
                fireOrbTrails.Add(orb, new FireTrail { Owner = player.Pos });
            }
        }

        void TickFireOrb(SimActor orb, float dt)
        {
            if (!fireOrbTrails.TryGetValue(orb, out var trail))
            { trail = new FireTrail { Owner = orb.Owner.Pos }; fireOrbTrails.Add(orb, trail); }
            float end = Mathf.Min(orb.Age, orb.Duration);
            float start = trail.Age;
            Vector2 currentOwner = orb.Owner.Pos;
            while (trail.Age < end - .000001f)
            {
                float step = Mathf.Min(FireOrbGeometry.SampleStep, end - trail.Age);
                float nextAge = trail.Age + step;
                Vector2 owner = Vector2.Lerp(trail.Owner, currentOwner, Mathf.InverseLerp(start, end, nextAge));
                Vector2 previous = orb.Pos;
                orb.Pos = FireOrbGeometry.Position(orb.Anchor, orb.SkillPoint, orb.SkillTime, orb.Bounces, nextAge, orb.Duration, owner);
                if ((orb.Pos - previous).sqrMagnitude > .000001f) orb.Vel = (orb.Pos - previous).normalized;
                if (nextAge >= FireOrbGeometry.Launch(orb.Bounces))
                {
                    if (trail.Samples.Count == 0) trail.Samples.Add(new FireTrail.Sample { Position = previous, Age = trail.Age });
                    trail.Samples.Add(new FireTrail.Sample { Position = orb.Pos, Age = nextAge });
                    while (trail.Samples.Count > 1 && nextAge - trail.Samples[1].Age > UpgradeTuning.Data.fireOrbTrailTime)
                        trail.Samples.RemoveAt(0);
                    DamageFireTrail(orb, trail, nextAge, step);
                }
                trail.Age = nextAge;
            }
            trail.Owner = currentOwner;
        }

        void DamageFireTrail(SimActor orb, FireTrail trail, float age, float dt)
        {
            // Union of swept head and tapered trail: each enemy receives DPS once per substep.
            int count = _actors.Count;
            for (int index = 0; index < count; index++)
            {
                var enemy = _actors[index];
                if (enemy.Dead || (enemy.Kind != ActorKind.Enemy && enemy.Kind != ActorKind.Boss)) continue;
                float body = enemy.Giant != null ? enemy.Size * .4f : GameConfig.EnemyRadius;
                bool hit = (enemy.Pos - orb.Pos).sqrMagnitude <= (orb.Size + body) * (orb.Size + body);
                // The sprite itself includes a flame tongue behind its head, even on reversals.
                hit |= DistToSegment(enemy.Pos, orb.Pos - orb.Vel * (orb.Size * 2.3f), orb.Pos) <= orb.Size * .45f + body;
                for (int j = trail.Samples.Count - 1; j > 0 && !hit; j--)
                {
                    var a = trail.Samples[j - 1]; var b = trail.Samples[j];
                    if (age - b.Age > UpgradeTuning.Data.fireOrbTrailTime) break;
                    float radius = FireOrbGeometry.TrailRadius(orb.Size, age - b.Age);
                    hit = DistToSegment(enemy.Pos, a.Position, b.Position) <= radius + body;
                }
                if (hit) ApplyDamage(enemy, orb.Damage * dt, StatusMask.Burn, .5f,
                    UpgradeTuning.Data.fireOrbBurnDps * (1f + UpgradeTuning.Data.attributeDamagePerLevel * orb.Owner.UpAttrRank.Value));
            }
        }
    }
}
