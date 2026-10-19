using Unity.Netcode;
using UnityEngine;

namespace OctopusSurvivors
{
    public sealed partial class GameSession
    {
        public readonly NetworkVariable<OctopusBossSnapshot> OctopusBoss = new NetworkVariable<OctopusBossSnapshot>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public bool OctopusBossActive => BossAlive.Value && Stage.Value == 5 && OctopusBoss.Value.Pattern != 0;

        void StartOctopusBoss(SimActor boss)
        {
            boss.Pos = OctopusBossPattern.Head;
            boss.Size = FiniteBeach.Width * .235f;
            boss.Duration = OctopusBossPattern.VisualMarker;
            boss.Knockback = Vector2.zero;
            OctopusBoss.Value = new OctopusBossSnapshot { Pattern = 2, Time = -1.4f,
                Seed = (uint)Random.Range(1, int.MaxValue) };
        }
        void TickOctopusBoss(SimActor boss, float dt)
        {
            boss.Pos = OctopusBossPattern.Head;
            boss.Knockback = boss.Vel = Vector2.zero;
            var state = OctopusBoss.Value;
            float before = state.Time;
            state.Time += dt;
            if (state.Pattern == 2)
            {
                for (int n = 0; n < OctopusBossPattern.RelayHits; n++)
                {
                    float contact = OctopusBossPattern.RelayContact(n);
                    if (before >= contact || state.Time < contact) continue;
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
                    float contact = OctopusBossPattern.HalfContact(arm, cycle);
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
