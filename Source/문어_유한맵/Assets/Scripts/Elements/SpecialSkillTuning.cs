using UnityEngine;
namespace OctopusSurvivors
{
    [CreateAssetMenu(menuName = "Octopus/Special skill tuning")]
    public sealed class SpecialSkillTuning : ScriptableObject
    {
        [Header("Dimension rift / black particles")]
        [Range(0,1)] public float riftBlackOpacity = .8f;
        [Range(.5f,1.2f)] public float riftBlackScale = 1.035f;
        [Min(.2f)] public float riftPulseInterval = 1.2f;
        [Min(0)] public float riftPulseDamage = 50;
        [Range(.01f,.5f)] public float riftExecutionThreshold = .1f;
        public bool riftExecuteNormalMonsters = true, riftExecuteBosses = false;
        [Range(4,10)] public float riftDuration = 10;
        [Min(1)] public float riftDiameter = 16;
        [Header("YinYang / original large orbit retained")]
        [Min(.3f)] public float orbitDuration = 2.8f;
        [Min(.05f)] public float mergeDuration = .28f;
        [Min(0)] public float burstDamage = 345f;
        [Min(.1f)] public float burstRadiusMultiplier = 1.8f;
        [Min(.1f)] public float burstDuration = 1.4f;
        [Header("Small comma contact attacks / shared per-cast hit interval")]
        [Range(2,256)] public int smallCommaCount = 144;
        [Min(.1f)] public float smallCommaDuration = .46f;
        [Min(0)] public float smallCommaDamage = 28f;
        [Min(.08f)] public float smallCommaHitInterval = .35f;
        [Min(.05f)] public float smallCommaHitRadius = .44f;
        [Min(.05f)] public float smallCommaBurstRadius = 1.15f;
        [Min(.1f)] public float smallCommaFieldRadius = 7.5f;
        [Min(.05f)] public float smallCommaSpeed = .72f;
        [Header("Layered YinYang mist / entities stay above mist")]
        [Range(0,.9f)] public float mistOpacity = .73f;
        [Min(.1f)] public float mistRadius = 8f;
        [Min(.1f)] public float mistDuration = 8f;
        [Min(.01f)] public float mistFlowSpeed = .13f;
        [Header("Cosmos / accelerating star creation")]
        [Range(0,.9f)] public float cosmosDarkness = .8f;
        [Min(8)] public int cosmosBaseStars = 168;
        [Min(.5f)] public float cosmosBuildDuration = 4.8f;
        [Min(0)] public float cosmosHoldDuration = .7f;
        [Min(.1f)] public float cosmosFieldRadius = 13f;
        [Min(.1f)] public float cosmosStarScale = 1.65f;
        [Min(0)] public float cosmosDamage = 65f;
        [Min(.1f)] public float cosmosImpactRadius = 1.3f;
        [Header("Two-player convergence / visual preview only")]
        [Min(2)] public float convergenceDuration = 6f;
        [Min(.1f)] public float convergenceRadius = 12f;
        [Min(1)] public float convergenceCooldown = 30f;
        [Range(0,.9f)] public float convergenceDarkness = .76f;
        [Min(0)] public float convergenceDamage = 0; // Reserved; no damage is applied in the preview.
        static SpecialSkillTuning instance;
        public static SpecialSkillTuning Data => instance != null ? instance :
            (instance = Resources.Load<SpecialSkillTuning>("SpecialSkillTuning") ?? CreateInstance<SpecialSkillTuning>());
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => instance = null;
        public static float StarSpawnTime(int index, int total)
        {
            float unit = Mathf.Max(.5f,Data.cosmosBuildDuration) / 4.8f;
            if (index < 2) return index * .65f * unit;
            if (index < 8) return (1.3f + (index - 2) * .22f) * unit;
            float u = Mathf.Clamp01((index-8f)/Mathf.Max(1,total-9));
            return (2.62f + 2.18f*(1-Mathf.Pow(1-u,2.6f))) * unit;
        }
        public static Vector2 CommaPosition(Vector2 center, float age, int index)
        {
            float a = index*2.399963f + age*Data.smallCommaSpeed*(index%2==0?1:-1);
            float radius = Data.smallCommaFieldRadius*(.38f+.55f*((index*7%13)/12f)) + Mathf.Sin(age*1.2f+index)*.6f;
            return center + new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
        }
        public static bool EligiblePair(int playerCount, bool aUp, bool bUp, bool aForm, bool bForm,
            SpecialId a, SpecialId b, float distance, float contactRadius) => playerCount == 2 && aUp && bUp && aForm && bForm &&
            SpecialAbilities.IsSupported(a) && SpecialAbilities.IsSupported(b) && distance <= contactRadius;
    }
}
