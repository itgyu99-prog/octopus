using UnityEngine;

namespace OctopusSurvivors
{
    public enum GiantPattern { Crab, Hermit, Jellyfish, Starfish, Urchin }
    public enum GiantState { Idle, Chase, PrepareAttack, Attack, Recovery, Stunned, Dead }
    [CreateAssetMenu(menuName="문어/Giant monster")]
    public sealed class GiantMonsterDefinition : ScriptableObject
    {
        public int id;
        public string displayName;
        public GiantPattern attackPattern;
        [Min(1)] public int unlockStage=3;
        [Min(0)] public float spawnWeight=1;
        [Min(.1f)] public float hpMultiplier=12, damageMultiplier=2, speedMultiplier=.7f, sizeMultiplier=3;
        [Min(.1f)] public float attackRange=2.6f, attackCooldown=3.5f, prepareTime=.8f, recoveryTime=.8f;
        [Min(.1f)] public float chargeSpeed=12, chargeDistance=8, projectileSpeed=7, projectileDistance=9;
        [Range(1,20)] public int projectileCount=5;
        [Range(0,1)] public float enragedHpRatio=.4f, defenseDamageMultiplier=.3f, knockbackMultiplier=.18f;
        [Min(.1f)] public float defenseDuration=2.5f, counterCooldown=.8f;
        [Min(0)] public int experienceReward=10;
        public GameObject prefab;
        public Sprite[] idleFrames, attackFrames;
        public RuntimeAnimatorController animator;
        static GiantMonsterDefinition[] definitions;
        public static GiantMonsterDefinition[] All => definitions ?? (definitions=Resources.LoadAll<GiantMonsterDefinition>("Monsters"));
        public static GiantMonsterDefinition Find(int id) { foreach(var d in All) if(d.id==id) return d; return null; }
        public static void SpawnBand(int stage,out float probability,out int limit)
        {
            probability=stage<3?0:stage<5?.02f:stage<8?.04f:stage<11?.06f:.08f;
            limit=stage<3?0:stage<5?1:stage<8?2:3;
        }
    }
}
