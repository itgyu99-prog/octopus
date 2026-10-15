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
            int l = Mathf.Clamp(level, 1, 6);
            float upgradeMul = 1f + UpgradeTuning.Data.attributeDamagePerLevel * p.UpAttrRank.Value;
            Vector2 dir = p.Facing.Value.sqrMagnitude > 0.01f ? p.Facing.Value.normalized : Vector2.right;
            switch (id)
            {
                case ElementId.Fire:
                    float reach = (4.3f + UpgradeTuning.Data.fireRangePerLevel * (l - 1)) * UpgradeTuning.Data.fireSizeMultiplier;
                    var fire = Effect(ActorKind.FlameAura, p.Pos, DensestFireDirection(p.Pos, dir, reach), reach, 1.25f, 70f * upgradeMul, p, l);
                    fire.SkillPoint = fire.Vel;
                    fire.Pulse = UpgradeTuning.Data.fireRetargetInterval;
                    break;
                case ElementId.Water:
                    var water = Effect(ActorKind.WaterEmitter, p.Pos, p.NextWaterDirection(dir), .1f, l * UpgradeTuning.Data.repeatInterval, 27f * upgradeMul, p, l);
                    water.Bounces = l;
                    break;
                case ElementId.Grass:
                    Effect(ActorKind.ThornEmitter, p.Pos, Vector2.zero, 9f, 4 * .16f, 19f * upgradeMul, p, 3 + (l - 1));
                    break;
                case ElementId.Electric:
                    var current = Effect(ActorKind.CurrentEmitter, p.Pos, dir, 6.1f, l * UpgradeTuning.Data.repeatInterval, 24f * upgradeMul, p, l);
                    current.Bounces = l;
                    break;
            }
            CastHiddenAbilities(p, id, dir, upgradeMul);
        }

        public Vector2 DensestFireDirection(Vector2 origin, Vector2 fallback, float range)
        {
            int best = 0;
            Vector2 result = fallback;
            // Test both cone edges: finds the largest group, even between two targets.
            foreach (var candidate in _actors)
            {
                if (candidate.Dead || (candidate.Kind != ActorKind.Enemy && candidate.Kind != ActorKind.Boss) ||
                    (candidate.Pos-origin).sqrMagnitude > range*range) continue;
                for (int side = -1; side <= 1; side++)
                {
                    Vector2 direction = AbilityRunner.Rotate((candidate.Pos-origin).normalized, side * 29.9f);
                    int count = 0;
                    foreach (var enemy in _actors)
                        if (!enemy.Dead && (enemy.Kind == ActorKind.Enemy || enemy.Kind == ActorKind.Boss) &&
                            (enemy.Pos-origin).sqrMagnitude <= range*range && Vector2.Angle(direction, enemy.Pos-origin) <= 30f) count++;
                    if (count > best) { best = count; result = direction; }
                }
            }
            return result;
        }

        public void CastPlannedCombo(OctopusPlayer p, ComboId combo)
        {
            if (!IsServer || !CombatActive || p == null || p.Downed.Value) return;
            var t = GameplayRules.Tuning(combo);
            int rank = p.ComboUpgradeLevel(combo);
            int l = rank + 1;
            float damage = t.damage * (1f + UpgradeTuning.Data.elementDamagePerLevel * p.UpElemRank.Value);
            Vector2 dir = p.Facing.Value.sqrMagnitude > .01f ? p.Facing.Value.normalized : Vector2.right;
            switch (combo)
            {
                case ComboId.Fog:
                    var mist = Effect(ActorKind.Mist, p.Pos, Vector2.zero, t.range,
                        t.duration + rank * UpgradeTuning.Data.durationPerLevel,
                        damage * (rank == 5 ? UpgradeTuning.Data.masterFogDamageMultiplier : 1), p, l);
                    mist.SkillTime = t.range + rank * UpgradeTuning.Data.fogRangePerLevel;
                    break;
                case ComboId.Chili:
                case ComboId.Lemon:
                    FireComboVolley(p, combo, l, damage, dir);
                    if (rank > 0)
                    {
                        var repeat = Effect(ActorKind.ComboRepeater, p.Pos, dir, .1f,
                            (rank + 1) * UpgradeTuning.Data.repeatInterval, damage, p, l);
                        repeat.Mark = (int)combo; repeat.Bounces = rank;
                        repeat.Pulse = UpgradeTuning.Data.repeatInterval;
                    }
                    if (rank == 5 && combo == ComboId.Chili)
                        Effect(ActorKind.ChiliField, p.Pos, Vector2.zero, 2f, UpgradeTuning.Data.chiliFieldDuration, damage, p, l);
                    if (rank == 5 && combo == ComboId.Lemon)
                        for (int i = 0; i < 2; i++)
                            Effect(ActorKind.LemonWheel, p.Pos, AbilityRunner.Rotate(dir, Random.Range(0f, 360f)) * 6f,
                                t.range * UpgradeTuning.Data.masterSizeMultiplier, t.duration, damage, p, l);
                    break;
                case ComboId.Tree:
                    CastTreeAbility(p, rank, l, damage, dir);
                    break;
                case ComboId.Soda:
                    for (int i = 0; i < 5 + rank + (rank == 5 ? 1 : 0); i++)
                    {
                        float size = t.range * (rank == 5 && i == 10 ? UpgradeTuning.Data.masterSizeMultiplier : 1);
                        Vector2 spot = RandomSodaPosition(p.Pos, size);
                        var bottle = Effect(ActorKind.SodaWave, spot, AbilityRunner.Rotate(Vector2.right, Random.Range(0f, 360f)) * 6f,
                            size, t.duration + rank * UpgradeTuning.Data.durationPerLevel, damage, p, l);
                        bottle.ContactCooldown = Random.Range(.3f, .8f);
                    }
                    break;
                case ComboId.BlueFlame:
                    for (int i = 0; i < (rank == 5 ? 2 : 1); i++)
                    {
                        var chain = Effect(ActorKind.ChainEmitter, p.Pos, Vector2.zero, t.range, t.duration, damage, p, l);
                        chain.SkillPhase = (byte)i;
                    }
                    break;
            }
        }

        void FireComboVolley(OctopusPlayer p, ComboId combo, int level, float damage, Vector2 dir)
        {
            var t = GameplayRules.Tuning(combo);
            for (int i = 0; i < 8; i++)
            {
                Vector2 shot = AbilityRunner.Rotate(dir, i * 45f);
                Effect(combo == ComboId.Chili ? ActorKind.ChiliShot : ActorKind.LemonShot,
                    p.Pos + shot * .3f, shot * 8f, t.range, t.duration, damage, p, level);
            }
        }

        Vector2 RandomSodaPosition(Vector2 fallback, float radius)
        {
            for (int attempt = 0; attempt < 64; attempt++)
            {
                Vector2 spot = new Vector2(Random.Range(-FiniteBeach.Width * .5f, FiniteBeach.Width * .5f),
                    Random.Range(-FiniteBeach.Height * .5f, FiniteBeach.Height * .5f));
                if (FiniteBeach.Walkable(spot, radius)) return spot;
            }
            return GameConfig.ClampToMap(fallback, radius);
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
                    a.Size = Mathf.Lerp(GameplayRules.Tuning(ComboId.Fog).range * .4f, a.SkillTime, Mathf.Clamp01(a.Age / a.Duration));
                    DamageCircle(a.Pos, a.Size, a.Damage * dt, StatusMask.Slow, .3f, 0f);
                    break;
                case ActorKind.FlameAura:
                    a.Pos = a.Owner.Pos;
                    if (a.Pulse <= 0f)
                    {
                        a.Pulse = Mathf.Max(.05f, UpgradeTuning.Data.fireRetargetInterval);
                        a.SkillPoint = TrackedFireDirection(a);
                        a.HitTargets.Clear();
                    }
                    a.Vel = TurnFireDirection(a.Vel, a.SkillPoint, dt);
                    DamageCone(a.Pos, a.Vel, a.Size, 60f, a.Damage * Mathf.Min(dt, Mathf.Max(0f, a.Life + dt)), StatusMask.Burn, 1.2f,
                        8f * (1f + UpgradeTuning.Data.attributeDamagePerLevel * a.Owner.UpAttrRank.Value), a.HitTargets);
                    break;
                case ActorKind.ThornEmitter:
                    a.Pos = a.Owner.Pos;
                    if (a.Pulse <= 0f) { a.Pulse = .16f; StrikeUpgradedVines(a); }
                    break;
                case ActorKind.WaterEmitter:
                case ActorKind.CurrentEmitter:
                    a.Pos = a.Owner.Pos;
                    if (a.Pulse <= 0f && a.Bounces > 0)
                    {
                        a.Pulse = UpgradeTuning.Data.repeatInterval;
                        a.Bounces--;
                        if (a.Kind == ActorKind.WaterEmitter)
                        {
                            int count = Mathf.Max(4, GameplayRules.Data.waterProjectileCount);
                            for (int i = 0; i < count; i++)
                            {
                                Vector2 shot = AbilityRunner.Rotate(a.Vel, i * 360f / count + (a.EffectLevel - a.Bounces - 1) * 12f);
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
                        ApplyDamage(enemy, a.Damage, a.Owner.HasUpgrade(UpgradeId.Paralysis) || Random.value < .2f ? StatusMask.Stun : StatusMask.None, UpgradeTuning.Data.paralysisDuration, 0f);
                    }
                    break;
                case ActorKind.SodaWave:
                    if (a.Pulse <= 0f)
                    {
                        a.Pulse = .25f;
                        DamageCircle(a.Pos, a.Size, a.Damage, StatusMask.None, 0f, 0f);
                    }
                    Vector2 next = a.Pos + a.Vel * dt;
                    if (!FiniteBeach.Walkable(next, a.Size)) a.Vel = AbilityRunner.Rotate(-a.Vel, Random.Range(-45f, 45f)) * 6f;
                    else a.Pos = next;
                    a.ContactCooldown -= dt;
                    if (a.ContactCooldown <= 0f) { a.ContactCooldown = .8f; a.Vel = AbilityRunner.Rotate(a.Vel, Random.Range(-80f, 80f)) * 6f; }
                    break;
                case ActorKind.LemonShot:
                    StepEffectProjectile(a, dt, false);
                    break;
                case ActorKind.FruitTree:
                    TickTreeAbility(a);
                    break;
                case ActorKind.Fruit:
                    LandTreeFruit(a);
                    break;
                case ActorKind.ChainEmitter:
                    a.Pos = a.Owner.Pos;
                    if (a.Pulse <= 0f) CastChain(a);
                    break;
            }
            if (a.Life <= 0f) a.Dead = true;
            return true;
        }

        readonly List<SimActor> fireEnemies = new List<SimActor>(GameConfig.MaxEnemies);
        readonly List<int> fireDensity = new List<int>(GameConfig.MaxEnemies);
        float fireCacheUntil = -1f;

        void RefreshFireCache()
        {
            if (Time.time < fireCacheUntil) return;
            fireCacheUntil = Time.time + Mathf.Max(.05f, UpgradeTuning.Data.fireRetargetInterval);
            fireEnemies.Clear(); fireDensity.Clear();
            foreach (var e in _actors)
                if (!e.Dead && (e.Kind == ActorKind.Enemy || e.Kind == ActorKind.Boss)) fireEnemies.Add(e);
            float radius2 = UpgradeTuning.Data.fireClusterRadius * UpgradeTuning.Data.fireClusterRadius;
            foreach (var e in fireEnemies)
            {
                int count = 0;
                foreach (var other in fireEnemies) if ((e.Pos - other.Pos).sqrMagnitude <= radius2) count++;
                fireDensity.Add(count);
            }
        }

        Vector2 TrackedFireDirection(SimActor flame)
        {
            RefreshFireCache();
            float best = -1f;
            Vector2 result = flame.Vel;
            for (int i = 0; i < fireEnemies.Count; i++)
            {
                var e = fireEnemies[i];
                Vector2 to = e.Pos - flame.Pos;
                if (e.Dead || to.sqrMagnitude > flame.Size * flame.Size || to.sqrMagnitude < .0001f) continue;
                float score = fireDensity[i] * (flame.HitTargets.Contains(e.Id) ? UpgradeTuning.Data.fireRecentTargetWeight : 1f);
                if (score <= best) continue;
                best = score; result = to.normalized;
            }
            return result;
        }

        static Vector2 TurnFireDirection(Vector2 current, Vector2 target, float dt)
        {
            if (target.sqrMagnitude < .0001f) return current;
            float from = Mathf.Atan2(current.y, current.x) * Mathf.Rad2Deg;
            float to = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;
            float angle = Mathf.MoveTowardsAngle(from, to, UpgradeTuning.Data.fireTurnSpeed * dt) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        void CastChain(SimActor source)
        {
            source.Pulse = UpgradeTuning.Data.blueFlameBurstInterval;
            var candidates = TempEnemies(source.Pos, source.Size);
            if (candidates.Count == 0) return;
            candidates.Sort((a, b) => (a.Pos - source.Pos).sqrMagnitude.CompareTo((b.Pos - source.Pos).sqrMagnitude));
            // The second mastery emitter starts from a different nearby target when possible.
            var primary = candidates[source.SkillPhase == 1 && candidates.Count > 1 ? 1 : 0];
            Vector2 center = primary.Pos;
            float radius = source.Size + Mathf.Max(0, source.EffectLevel - 1) * UpgradeTuning.Data.blueFlameSplashRangePerLevel;
            // Snapshot targets before damage can kill the primary or spawn more actors.
            var splashTargets = TempEnemies(center, radius);
            ShowBlueFlameArc(source, source.Pos, center);
            ApplyDamage(primary, source.Damage, StatusMask.None, 0f, 0f);
            foreach (var enemy in splashTargets)
            {
                if (enemy.Id == primary.Id || enemy.Dead) continue;
                ShowBlueFlameArc(source, center, enemy.Pos);
                ApplyDamage(enemy, source.Damage, StatusMask.None, 0f, 0f);
            }
        }

        void ShowBlueFlameArc(SimActor source, Vector2 from, Vector2 to)
        {
            Effect(ActorKind.ChainArc, (from + to) * .5f, to - from,
                Mathf.Max(.1f, Vector2.Distance(from, to) * .5f), .22f, 0f, source.Owner, source.EffectLevel);
        }
    }
}
