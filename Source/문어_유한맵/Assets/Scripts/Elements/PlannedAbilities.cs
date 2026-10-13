using System.Collections.Generic;
using UnityEngine;

namespace OctopusSurvivors
{
    public sealed partial class GameSession
    {
        // Each cast has its own actor/lifetime so overlapping attributes cannot cancel one another.
        public void CastBasicAbility(OctopusPlayer p, ElementId id, int level)
        {
            if (!IsServer || !CombatActive || p == null || p.Downed.Value) return;
            int l = Mathf.Clamp(level, 1, 5);
            float upgradeMul = 1f + .22f * p.UpAttrRank.Value;
            Vector2 dir = p.Facing.Value.sqrMagnitude > 0.01f ? p.Facing.Value.normalized : Vector2.right;
            switch (id)
            {
                case ElementId.Fire:
                    Effect(ActorKind.FlameAura, p.Pos, Vector2.zero, 1.3f + .3f * l, 2.5f, 35f * upgradeMul, p, l);
                    break;
                case ElementId.Water:
                    var water = Effect(ActorKind.WaterEmitter, p.Pos, dir, .1f, l * GameplayRules.Data.attributeRepeatInterval, 27f * upgradeMul, p, l);
                    water.Bounces = l;
                    break;
                case ElementId.Grass:
                    Effect(ActorKind.ThornEmitter, p.Pos, Vector2.zero, 9f, 4 * .16f, 19f * upgradeMul, p, 1);
                    break;
                case ElementId.Electric:
                    var current = Effect(ActorKind.CurrentEmitter, p.Pos, dir, 6.1f, l * GameplayRules.Data.attributeRepeatInterval, 24f * upgradeMul, p, l);
                    current.Bounces = l;
                    break;
            }
        }

        public void CastPlannedCombo(OctopusPlayer p, ComboId combo)
        {
            if (!IsServer || !CombatActive || p == null || p.Downed.Value) return;
            var t = GameplayRules.Tuning(combo);
            int l = p.ElementLevel;
            float damage = t.damage * (1f + .3f * (l - 1));
            Vector2 dir = p.Facing.Value.sqrMagnitude > .01f ? p.Facing.Value.normalized : Vector2.right;
            switch (combo)
            {
                case ComboId.Fog:
                    Effect(ActorKind.Mist, p.Pos, Vector2.zero, t.range, t.duration, damage, p, l);
                    break;
                case ComboId.Chili:
                case ComboId.Lemon:
                    int count = 8 + 2 * (l - 1);
                    for (int i = 0; i < count; i++)
                    {
                        Vector2 shot = AbilityRunner.Rotate(dir, i * 360f / count);
                        Effect(combo == ComboId.Chili ? ActorKind.ChiliShot : ActorKind.LemonShot,
                            p.Pos + shot * .3f, shot * 8f, t.range, t.duration, damage, p, l);
                    }
                    break;
                case ComboId.Tree:
                    Effect(ActorKind.FruitTree, p.Pos, Vector2.zero, t.range, t.duration, damage, p, l);
                    break;
                case ComboId.Soda:
                    Effect(ActorKind.SodaWave, p.Pos, dir * 6f, t.range, t.duration, damage, p, l);
                    break;
                case ComboId.BlueFlame:
                    Effect(ActorKind.ChainEmitter, p.Pos, Vector2.zero, t.range, t.duration, damage, p, l);
                    break;
            }
        }

        bool TickPlannedAbility(SimActor a, float dt)
        {
            if (a.Kind < ActorKind.SodaWave || a.Kind > ActorKind.RadialCurrent) return false;
            if (a.Owner == null || !a.Owner.IsSpawned || a.Owner.Downed.Value)
            { a.Dead = true; return true; }
            a.Age += dt;
            a.Life -= dt;
            a.Pulse -= dt;
            switch (a.Kind)
            {
                case ActorKind.Mist:
                    DamageCircle(a.Pos, a.Size, a.Damage * dt, StatusMask.Slow, .3f, 0f);
                    break;
                case ActorKind.FlameAura:
                    a.Pos = a.Owner.Pos;
                    DamageCircle(a.Pos, a.Size, a.Damage * dt, StatusMask.Burn, 1.2f, 8f);
                    break;
                case ActorKind.ThornEmitter:
                    a.Pos = a.Owner.Pos;
                    if (a.Pulse <= 0f) { a.Pulse = .16f; StrikeNearestVines(a.Pos, a.Size, a.EffectLevel, a.Damage); }
                    break;
                case ActorKind.WaterEmitter:
                case ActorKind.CurrentEmitter:
                    a.Pos = a.Owner.Pos;
                    if (a.Pulse <= 0f && a.Bounces > 0)
                    {
                        a.Pulse = GameplayRules.Data.attributeRepeatInterval;
                        a.Bounces--;
                        if (a.Kind == ActorKind.WaterEmitter)
                        {
                            int count = Mathf.Max(4, GameplayRules.Data.waterProjectileCount);
                            for (int i = 0; i < count; i++)
                            {
                                Vector2 shot = AbilityRunner.Rotate(a.Vel, i * 360f / count);
                                SpawnProjectile(ActorKind.Droplet, a.Pos + shot * .6f, shot * 9.5f, .28f, 2.2f, a.Damage, a.Owner, StatusMask.Slow);
                            }
                        }
                        else Effect(ActorKind.RadialCurrent, a.Pos, Vector2.zero, a.Size, .25f, a.Damage, a.Owner, a.EffectLevel);
                    }
                    if (a.Bounces == 0) a.Dead = true;
                    return true;
                case ActorKind.RadialCurrent:
                    float front = a.Size * Mathf.Clamp01(a.Age / a.Duration);
                    foreach (var enemy in TempEnemies(a.Pos, front))
                    {
                        if (!a.HitTargets.Add(enemy.Id)) continue;
                        ApplyDamage(enemy, a.Damage, Random.value < .2f ? StatusMask.Stun : StatusMask.None, .7f, 0f);
                    }
                    break;
                case ActorKind.SodaWave:
                    if (a.Pulse <= 0f)
                    {
                        a.Pulse = .25f;
                        DamageCircle(a.Pos, a.Size, a.Damage, StatusMask.None, 0f, 0f);
                    }
                    Vector2 next = a.Pos + a.Vel * dt;
                    if (!FiniteBeach.Walkable(next, a.Size)) a.Vel = -a.Vel;
                    else a.Pos = next;
                    a.ContactCooldown -= dt;
                    if (a.ContactCooldown <= 0f) { a.ContactCooldown = .8f; a.Vel = AbilityRunner.Rotate(a.Vel, Random.Range(-80f, 80f)) * 6f; }
                    break;
                case ActorKind.LemonShot:
                    StepEffectProjectile(a, dt, false);
                    break;
                case ActorKind.FruitTree:
                    if (a.Pulse <= 0f)
                    {
                        a.Pulse = .5f;
                        Vector2 landing = GameConfig.ClampToMap(a.Pos + Random.insideUnitCircle * Mathf.Max(0f, a.Size - .5f), .5f);
                        Effect(ActorKind.Fruit, landing, Vector2.zero, .5f, .6f, a.Damage, a.Owner, a.EffectLevel);
                    }
                    break;
                case ActorKind.Fruit:
                    if (a.Life <= 0f) DamageCircle(a.Pos, a.Size, a.Damage, StatusMask.None, 0f, 0f);
                    break;
                case ActorKind.ChainEmitter:
                    a.Pos = a.Owner.Pos;
                    if (a.Pulse <= 0f) { a.Pulse = .6f; CastChain(a); }
                    break;
            }
            if (a.Life <= 0f) a.Dead = true;
            return true;
        }

        void CastChain(SimActor source)
        {
            var used = new HashSet<ushort>();
            Vector2 from = source.Pos;
            for (int jump = 0; jump < 3 + source.EffectLevel && CombatActive; jump++)
            {
                SimActor nearest = null;
                float best = source.Size * source.Size;
                foreach (var enemy in _actors)
                {
                    if (enemy.Dead || used.Contains(enemy.Id) || (enemy.Kind != ActorKind.Enemy && enemy.Kind != ActorKind.Boss)) continue;
                    float dist = (enemy.Pos - from).sqrMagnitude;
                    if (dist <= best) { best = dist; nearest = enemy; }
                }
                if (nearest == null) break;
                used.Add(nearest.Id);
                Vector2 to = nearest.Pos;
                Effect(ActorKind.ChainArc, (from + to) * .5f, to - from, Mathf.Max(.1f, Vector2.Distance(from, to) * .5f), .18f, 0f, source.Owner, source.EffectLevel);
                ApplyDamage(nearest, source.Damage, StatusMask.None, 0f, 0f);
                from = to;
            }
        }
    }
}
