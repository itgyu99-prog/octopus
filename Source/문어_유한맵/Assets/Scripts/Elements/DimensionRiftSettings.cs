using System;
using UnityEngine;

namespace OctopusSurvivors
{
    [Serializable]
    public sealed class DimensionRiftTuning
    {
        public float pulseInterval { get => SpecialSkillTuning.Data.riftPulseInterval; set => SpecialSkillTuning.Data.riftPulseInterval=value; }
        public float pulseDamage { get => SpecialSkillTuning.Data.riftPulseDamage; set => SpecialSkillTuning.Data.riftPulseDamage=value; }
        public float executionThreshold { get => SpecialSkillTuning.Data.riftExecutionThreshold; set => SpecialSkillTuning.Data.riftExecutionThreshold=value; }
        public bool executeNormalMonsters { get => SpecialSkillTuning.Data.riftExecuteNormalMonsters; set => SpecialSkillTuning.Data.riftExecuteNormalMonsters=value; }
        public bool executeBosses { get => SpecialSkillTuning.Data.riftExecuteBosses; set => SpecialSkillTuning.Data.riftExecuteBosses=value; }
    }

    public static class DimensionRiftSettings
    {
        public static float Duration => Mathf.Clamp(SpecialSkillTuning.Data.riftDuration,4,10);
        public const float OpenAt = 2.5f;
        public static float CloseAt => Mathf.Min(8.05f,Duration-1);
        public const float Lift = 2.2f, WaveDuration = 2.7f;
        public static float Diameter => Mathf.Max(1,SpecialSkillTuning.Data.riftDiameter);
        static DimensionRiftTuning tuning;
        public static DimensionRiftTuning Tuning
        {
            get
            {
                if (tuning == null)
                {
                    tuning = new DimensionRiftTuning();
                }
                return tuning;
            }
        }
        public static bool CanExecute(SimActor enemy) => enemy != null && enemy.OctopusArm < 0 &&
            (enemy.Kind == ActorKind.Boss ? Tuning.executeBosses : enemy.Kind == ActorKind.Enemy && Tuning.executeNormalMonsters);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => tuning = null;
    }
}
