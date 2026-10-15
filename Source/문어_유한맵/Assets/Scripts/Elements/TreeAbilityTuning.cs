using UnityEngine;

namespace OctopusSurvivors
{
    [CreateAssetMenu(menuName = "Octopus/Tree ability tuning")]
    public sealed class TreeAbilityTuning : ScriptableObject
    {
        [Header("Visual size")]
        [Tooltip("나무 그림 크기. 실행 중 변경하면 즉시 반영됩니다.")]
        [Min(.1f)] public float treeVisualScale = 3f;
        [Tooltip("열매 그림 배율 = 나무 그림 배율 × 이 값. 피해 범위와 별개입니다.")]
        [Min(.1f)] public float fruitVisualScaleRatio = 2.5f;
        [Header("Fruit attack")]
        [Min(.01f)] public float fruitInterval = .05f, masterFruitInterval = .04f;
        [Min(0)] public float fruitDamageMultiplier = .2f, masterFruitDamageMultiplier = .53333336f;
        [Min(.1f)] public float fruitRadius = .5f, fruitFallSeconds = .6f;

        static TreeAbilityTuning instance;
        public static TreeAbilityTuning Data => instance != null ? instance :
            (instance = Resources.Load<TreeAbilityTuning>("TreeAbilityTuning") ?? CreateInstance<TreeAbilityTuning>());

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => instance = null;
    }
}
