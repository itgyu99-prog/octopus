using UnityEngine;

namespace OctopusSurvivors
{
    public sealed partial class GameSession
    {
        public void CastDimensionRift(Vector2 center, OctopusPlayer owner, int level)
        {
            if (!IsServer || !CombatActive) return;
            var rift = Effect(ActorKind.DimensionRift, center, Vector2.zero,
                DimensionRiftSettings.Diameter, DimensionRiftSettings.Duration,
                Mathf.Max(0, DimensionRiftSettings.Tuning.pulseDamage) * (1 + .3f * (Mathf.Max(1, level) - 1)), owner, level);
            rift.Pulse = DimensionRiftSettings.OpenAt;
        }

        bool TickDimensionRift(SimActor a, float dt)
        {
            if (a.Kind == ActorKind.Enemy || a.Kind == ActorKind.Boss)
            {
                if (!a.RiftExecuting) TryBeginRiftExecution(a);
                if (!a.RiftExecuting) return false;
                a.RiftExecutionAge += dt;
                float t = Mathf.Clamp01(a.RiftExecutionAge / Mathf.Max(.01f, a.RiftExecutionDuration));
                float pull = t * t * (3 - 2 * t);
                Vector2 dest = a.RiftSource.Pos + Vector2.up * DimensionRiftSettings.Lift;
                Vector2 delta = a.RiftStart - dest;
                float angle = t * 1.1f;
                Vector2 spiral = new Vector2(delta.x * Mathf.Cos(angle) - delta.y * Mathf.Sin(angle),
                    delta.x * Mathf.Sin(angle) + delta.y * Mathf.Cos(angle));
                a.Pos = dest + spiral * (1 - pull);
                a.Size = a.RiftOriginalSize * Mathf.Max(.001f, 1 - pull);
                if (t >= 1 || a.RiftSource.Dead)
                {
                    a.Pos = a.RiftStart; // Loot stays on reachable ground, not in the air.
                    KillEnemy(a);
                }
                return true;
            }
            if (a.Kind == ActorKind.RiftWave)
            {
                a.Age += dt; a.Life -= dt;
                if (a.Life <= 0 || a.Age >= a.Duration) a.Dead = true;
                return true;
            }
            if (a.Kind != ActorKind.DimensionRift) return false;
            a.Age = Mathf.Min(a.Duration, a.Age + dt);
            a.Life = Mathf.Max(0, a.Duration - a.Age);
            float interval = Mathf.Max(.2f, DimensionRiftSettings.Tuning.pulseInterval);
            // Catch up missed server ticks; each scheduled pulse is applied exactly once.
            while (a.Pulse <= a.Age + .0001f && a.Pulse < Mathf.Min(8.05f, a.Duration - 1))
            {
                float scheduled = a.Pulse;
                a.Pulse += interval;
                float waveAge = a.Age - scheduled;
                float waveLife = Mathf.Min(DimensionRiftSettings.WaveDuration, a.Duration - scheduled);
                if (waveAge < waveLife)
                {
                    var wave = Effect(ActorKind.RiftWave, a.Pos, Vector2.zero, 40f, waveLife, 0, a.Owner, 1);
                    wave.Age = waveAge; wave.Life = waveLife - waveAge;
                }
                int count = _actors.Count;
                for (int i = 0; i < count; i++)
                {
                    var enemy = _actors[i];
                    if (!CanTargetActor(enemy)) continue;
                    if (DimensionRiftSettings.CanExecute(enemy)) enemy.RiftSource = a;
                    // The entire field is hit; visual ring radius never limits damage.
                    ApplyDamage(enemy, a.Damage, StatusMask.None, 0, 0);
                    if (!enemy.Dead) TryBeginRiftExecution(enemy);
                }
            }
            if (a.Life <= 0) a.Dead = true;
            return true;
        }

        bool TryBeginRiftExecution(SimActor enemy)
        {
            if (enemy.RiftExecuting) return true;
            var source = enemy.RiftSource;
            if (source == null || source.Dead || source.Life <= 0 || !DimensionRiftSettings.CanExecute(enemy) ||
                !CanTargetActor(enemy) || enemy.Hp <= 0 || enemy.MaxHp <= 0) return false;
            float cutoff = enemy.MaxHp * Mathf.Clamp(DimensionRiftSettings.Tuning.executionThreshold, .001f, .99f);
            // Mono may retain extra intermediate precision: exactly 10% is not below it.
            if (enemy.Hp >= cutoff || Mathf.Approximately(enemy.Hp, cutoff)) return false;
            enemy.RiftExecuting = true;
            enemy.RiftExecutionAge = 0;
            enemy.RiftExecutionDuration = Mathf.Min(.7f, Mathf.Max(.01f, source.Life));
            enemy.RiftStart = enemy.Pos; enemy.RiftOriginalSize = enemy.Size;
            enemy.Knockback = enemy.Vel = Vector2.zero;
            return true;
        }
    }
}
