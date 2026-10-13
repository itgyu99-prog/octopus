using System;
using UnityEditor;
using UnityEngine;

namespace OctopusSurvivors.EditorTools
{
    public static class SystemDataChecks
    {
        [MenuItem("문어/2안 시스템 데이터 검증")]
        public static void Run()
        {
            ElementEffectChecks.VerifyCombinations();
            FiniteMapChecks.Verify();
            if (GameplayRules.Data.recipes.Length != 6 || GameplayRules.Data.abilities.Length != 6)
                throw new Exception("Expected six recipes and effects");
            if (Upgrades.All.Length != 6 || Array.IndexOf(Upgrades.All, UpgradeId.MaxHp) < 0)
                throw new Exception("Expected six stage upgrades including health");
            for (int stage=1;stage<=15;stage++) if (GameConfig.RequiredPieces(stage)<=0) throw new Exception("Invalid stage requirement");
            foreach(var tuning in GameplayRules.Data.abilities)
                if(tuning.damage<=0 || tuning.range<=0 || tuning.duration<=0 || tuning.cooldown<=0)
                    throw new Exception("Invalid effect tuning: "+tuning.combo);
            if(ElementRules.TryCombo(ElementId.Stone,ElementId.Fire,out _)) throw new Exception("Stone remains in active recipes");
            if (GameplayRules.Data.attributeDropChance < 0f || GameplayRules.Data.attributeDropChance >= .05f ||
                GameplayRules.Data.waterProjectileCount < 4 || GameplayRules.Data.attributeRepeatInterval <= 0f)
                throw new Exception("Invalid attribute drop or volley configuration");
            Debug.Log("SYSTEM DATA CHECKS PASS");
        }
    }
}
