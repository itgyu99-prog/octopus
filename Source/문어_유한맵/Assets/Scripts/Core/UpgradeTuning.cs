using UnityEngine;

namespace OctopusSurvivors
{
    [CreateAssetMenu(menuName = "Octopus/Upgrade tuning")]
    public sealed class UpgradeTuning : ScriptableObject
    {
        [Range(0, 1)] public float hiddenOfferChance = .2f;
        [Min(0)] public float attributeDamagePerLevel = .22f, elementDamagePerLevel = .3f;
        [Min(.01f)] public float repeatInterval = .22f;
        [Min(0)] public float fireRangePerLevel = .3f;
        [Min(.1f)] public float fireSizeMultiplier = 1.5f;
        [Min(.05f)] public float fireRetargetInterval = .15f;
        [Min(1)] public float fireTurnSpeed = 180f;
        [Min(.1f)] public float fireClusterRadius = 3f;
        [Range(0, 1)] public float fireRecentTargetWeight = .35f;
        [Header("Returning fire orbs / three shots")]
        [Min(.1f)] public float fireOrbDuration = 4.8f, fireOrbThrowTime = .68f, fireOrbReturnTime = .9f;
        [Min(0)] public float fireOrbLaunchGap = .16f, fireOrbSpread = 32f;
        [Min(.1f)] public float fireOrbRadius = .45f, fireOrbDps = 24f;
        [Min(0)] public float fireOrbBurnDps = 4f;
        [Range(.1f, .8f)] public float fireOrbTrailTime = .48f;
        [Min(.1f)] public float fireOrbPatrolDistance = 1.4f, fireOrbPatrolSpeed = 4.2f;
        [Min(0)] public float durationPerLevel = .4f, fogRangePerLevel = .3f;
        [Min(.03f)] public float chainInterval = .08f;
        [Min(0)] public float blueFlameSplashRangePerLevel = .3f;
        [Min(.1f)] public float blueFlameBurstInterval = .6f;
        [Min(1)] public int blueFlameBaseBranches = 3;
        [Min(0)] public int blueFlameBranchesPerLevel = 1;
        [Min(1)] public int blueFlameChainTargets = 20;
        [Min(.1f)] public float blueFlameVisualScale = 5f;
        [Range(1f, 4f)] public float blueFlameImpactIntensity = 1.5f;
        [Min(.5f)] public float blueFlameImpactBodyScale = 1.15f;
        [Min(1f)] public float blueFlameImpactRingScale = 1.55f;
        [Min(.1f)] public float blueFlameCorridorRadius = .65f;
        [Range(0, 1)] public float blueFlameSplashDamageMultiplier = .5f;
        [Min(.1f)] public float thornDuration = 3f, thornTick = .4f;
        [Min(0)] public float thornDamageMultiplier = .4f, splitDamageMultiplier = .4f;
        [Min(.1f)] public float balloonRadius = 2.5f, tornadoDuration = 2f, fireworkRadius = 2f;
        [Min(.1f)] public float lightningWarning = .45f, paralysisDuration = .7f;
        [Min(.1f)] public float chiliFieldDuration = 4f, chiliFieldInterval = .4f;
        [Min(1)] public float masterFogDamageMultiplier = 1.5f, masterSizeMultiplier = 2f;
        [Header("Hidden skill balance")]
        [Min(0)] public float balloonDamage = 27f, tornadoDps = 35f, fireworkDamage = 70f, lightningDamage = 48f;
        [Min(.1f)] public float tornadoRadius = 1.5f, lightningRadius = 1.8f, thornRadius = .9f;
        [Min(.1f)] public float hiddenProjectileLife = 3f, thornProjectileLife = 1.4f;
        [Min(.1f)] public float balloonSpeed = 6f, tornadoSpeed = 5f, fireworkSpeed = 9f, thornSpeed = 8f;
        [Min(.1f)] public float hiddenTickInterval = .25f, lemonWheelTick = .3f;
        static UpgradeTuning instance;
        public static UpgradeTuning Data => instance != null ? instance :
            (instance = Resources.Load<UpgradeTuning>("UpgradeTuning") ?? CreateInstance<UpgradeTuning>());
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => instance = null;
    }
}
