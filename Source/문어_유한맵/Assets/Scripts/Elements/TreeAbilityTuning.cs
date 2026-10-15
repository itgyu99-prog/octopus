using UnityEngine;

namespace OctopusSurvivors
{
    [CreateAssetMenu(menuName = "Octopus/Tree ability tuning")]
    public sealed class TreeAbilityTuning : ScriptableObject
    {
        [Min(.01f)] public float fruitInterval = .1f, masterFruitInterval = .08f;
        [Min(0)] public float fruitDamageMultiplier = .2f, masterFruitDamageMultiplier = .53333336f;
        [Min(.1f)] public float fruitRadius = .5f, fruitFallSeconds = .6f;

        static TreeAbilityTuning instance;
        public static TreeAbilityTuning Data => instance != null ? instance :
            (instance = Resources.Load<TreeAbilityTuning>("TreeAbilityTuning") ?? CreateInstance<TreeAbilityTuning>());

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => instance = null;
    }
}
