using Unity.Netcode;
using UnityEngine;

namespace OctopusSurvivors
{
    public sealed partial class GameSession
    {
        public readonly NetworkVariable<OctopusBossSnapshot> OctopusBoss = new NetworkVariable<OctopusBossSnapshot>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public bool OctopusBossActive => BossAlive.Value && Stage.Value == 5 && OctopusBoss.Value.Pattern != 0;
        readonly SimActor[] octopusArms = new SimActor[6];
        static float OctopusHitPadding(SimActor a) => a.OctopusArm >= 0 ? 1.25f : a.Duration == OctopusBossPattern.VisualMarker ? a.Size * .45f : 0;
        static bool IsOctopusCore(SimActor a) => a != null && a.Duration == OctopusBossPattern.VisualMarker;
        static float CenterDistanceToActor(Vector2 point, SimActor actor) => IsOctopusCore(actor)
            ? OctopusCoreShape.Distance(point, actor.Pos)
            : Vector2.Distance(point, actor.Pos);
        static float DistanceToActor(Vector2 point, SimActor actor) => IsOctopusCore(actor)
            ? OctopusCoreShape.Distance(point, actor.Pos)
            : Mathf.Max(0, Vector2.Distance(point, actor.Pos) - OctopusHitPadding(actor));
        static float SegmentDistanceToActor(Vector2 from, Vector2 to, SimActor actor) => IsOctopusCore(actor)
            ? OctopusCoreShape.SegmentDistance(from, to, actor.Pos)
            : DistToSegment(actor.Pos, from, to);
        bool CanTargetActor(SimActor actor)
        {
            if (actor == null || actor.Dead || actor.RiftExecuting || (actor.Kind != ActorKind.Enemy && actor.Kind != ActorKind.Boss)) return false;
            if (actor.OctopusArm >= 0) return actor.Hp > 0 && OctopusBossPattern.ArmsVisible(OctopusBoss.Value);
            return actor.Duration != OctopusBossPattern.VisualMarker || OctopusBoss.Value.Vulnerable > 0;
        }
        void RestoreOctopusArms(SimActor boss, ref OctopusBossSnapshot state)
        {
            state.ArmMaxHp = Mathf.Max(80, boss.MaxHp * .12f);
            for (int i = 0; i < 6; i++)
            {
                if (octopusArms[i] != null) octopusArms[i].Dead = true;
                var limb = Create(ActorKind.Enemy, OctopusBossPattern.RestRoot(i) + Vector2.up * 3, 6);
                limb.OctopusArm = i; limb.Target = boss; limb.Duration = OctopusBossPattern.ArmMarker;
                limb.Hp = limb.MaxHp = state.ArmMaxHp; octopusArms[i] = limb; state.SetArmHealth(i, 1);
            }
        }
        void TickOctopusArm(SimActor arm, float dt)
        {
            if (arm.Target == null || arm.Target.Dead || !BossAlive.Value) { arm.Dead = true; return; }
            arm.Pos = OctopusBossPattern.ArmTargetPoint(OctopusBoss.Value, arm.OctopusArm);
            arm.Knockback = Vector2.zero; arm.HitFlash = Mathf.Max(0, arm.HitFlash - dt);
            if (arm.BurnTime > 0 && CanTargetActor(arm))
            { arm.BurnTime -= dt; DamageOctopusArm(arm, arm.BurnDps * dt, StatusMask.None, 0, 0); }
            if (arm.SpicyTime > 0 && CanTargetActor(arm))
            { arm.SpicyTime -= dt; DamageOctopusArm(arm, arm.SpicyDps * dt, StatusMask.None, 0, 0); }
        }
        bool DamageOctopusArm(SimActor arm, float damage, StatusMask status, float duration, float burn)
        {
            if (!CanTargetActor(arm)) return false;
            arm.Hp = Mathf.Max(0, arm.Hp - Mathf.Max(0, damage)); arm.HitFlash = GameConfig.HitFlashSeconds;
            if ((status & StatusMask.Burn) != 0) { arm.BurnTime = Mathf.Max(arm.BurnTime, duration); arm.BurnDps = Mathf.Max(arm.BurnDps, burn); }
            var state = OctopusBoss.Value; state.SetArmHealth(arm.OctopusArm, arm.Hp / arm.MaxHp); OctopusBoss.Value = state;
            if (arm.Hp <= 0) { BreakOctopusArm(arm); return true; }
            return false;
        }
        void BreakOctopusArm(SimActor arm)
        {
            arm.Hp = 0; arm.Dead = true;
            var state = OctopusBoss.Value; state.SetArmHealth(arm.OctopusArm, 0);
            if (state.LivingArms == 0 && state.Vulnerable <= 0)
            { state.Pattern = 5; state.Time = 0; state.Vulnerable = OctopusBossPattern.VulnerableSeconds; }
            OctopusBoss.Value = state;
        }
        void TickOctopusCore(SimActor boss, float dt)
        {
            var state = OctopusBoss.Value;
            boss.HitFlash = Mathf.Max(0, boss.HitFlash - dt);
            if (state.Vulnerable > 0 && boss.BurnTime > 0)
            { boss.Hp -= boss.BurnDps * Mathf.Min(dt, state.Vulnerable, boss.BurnTime); boss.BurnTime -= dt; }
            if (boss.Hp <= 0) { KillEnemy(boss); return; }
            TickBossPattern(boss, dt, 0);
        }

        void StartOctopusBoss(SimActor boss)
        {
            boss.Pos = OctopusBossPattern.CoreCenter(true);
            boss.Size = FiniteBeach.Width * .235f;
            boss.Duration = OctopusBossPattern.VisualMarker;
            boss.Knockback = Vector2.zero;
            var state = new OctopusBossSnapshot { Pattern = 2, Time = -1.4f, Seed = (uint)Random.Range(1, int.MaxValue) };
            RestoreOctopusArms(boss, ref state); OctopusBoss.Value = state;
        }
        void TickOctopusBoss(SimActor boss, float dt)
        {
            boss.Pos = OctopusBossPattern.CoreCenter(true);
            boss.Knockback = boss.Vel = Vector2.zero;
            var state = OctopusBoss.Value;
            if (state.Vulnerable > 0)
            {
                state.Time += dt; state.Vulnerable = Mathf.Max(0, state.Vulnerable - dt);
                if (state.Vulnerable <= 0)
                {
                    boss.BurnTime = boss.SpicyTime = 0; state.Pattern = 2; state.Time = -.8f; state.Contacts = 0;
                    state.Seed = (uint)Random.Range(1, int.MaxValue); RestoreOctopusArms(boss, ref state);
                }
                OctopusBoss.Value = state; return;
            }
            float before = state.Time;
            state.Time += dt;
            if (state.Pattern == 2)
            {
                for (int n = 0; n < OctopusBossPattern.RelayHits; n++)
                {
                    float contact = OctopusBossPattern.RelayContact(n);
                    if (before >= contact || state.Time < contact) continue;
                    if (state.ArmHealth(OctopusBossPattern.RelayArm(n)) <= 0) continue;
                    state.Contacts++; // Actual contact, never the windup/launch.
                    HitOctopusFootprint(OctopusBossPattern.RelayTarget(state.Seed, n), new Vector2(2.6f, 1.9f), -1);
                }
                if (state.Time >= OctopusBossPattern.RelayDuration)
                {
                    state.Pattern = 4; state.Time -= OctopusBossPattern.RelayDuration;
                    state.Half = (byte)Random.Range(0, 4); state.Seed = (uint)Random.Range(1, int.MaxValue);
                }
            }
            else if (state.Pattern == 4)
            {
                for (int cycle = 0; cycle < 13; cycle++)
                for (int arm = 0; arm < 6; arm++)
                {
                    if (state.ArmHealth(arm) <= 0) continue;
                    float contact = OctopusBossPattern.HalfContact(state.Half, arm, cycle);
                    if (contact >= OctopusBossPattern.HalfAttackStart + OctopusBossPattern.HalfAttackSeconds ||
                        before >= contact || state.Time < contact) continue;
                    HitOctopusFootprint(OctopusBossPattern.HalfTarget(state.Seed, state.Half, arm, cycle),
                        OctopusBossPattern.HalfRadii(state.Half), state.Half);
                }
                if (state.Time >= OctopusBossPattern.HalfDuration)
                {
                    state.Pattern = 2; state.Time -= OctopusBossPattern.HalfDuration;
                    state.Contacts = 0; state.Seed = (uint)Random.Range(1, int.MaxValue);
                }
            }
            OctopusBoss.Value = state;
        }
        void HitOctopusFootprint(Vector2 target, Vector2 radii, int half)
        {
            foreach (var player in OctopusPlayer.All)
            {
                if (player == null || !player.IsSpawned || player.Downed.Value) continue;
                if (half >= 0 && !OctopusBossPattern.HalfRect(half).Contains(player.Pos)) continue;
                if (OctopusBossPattern.InFootprint(player.Pos, target, radii, GameConfig.PlayerRadius))
                    player.ApplyDamage(22f, DamageSource.BossSkill);
            }
        }
    }
}
