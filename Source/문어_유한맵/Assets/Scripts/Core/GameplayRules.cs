using System;
using UnityEngine;

namespace OctopusSurvivors
{
    [Serializable] public sealed class ComboRecipe
    {
        public ElementId first, second;
        public ComboId result;
    }
    [Serializable] public sealed class AbilityTuning
    {
        public ComboId combo;
        public float damage, range, duration, cooldown;
    }
    [Serializable] public sealed class GameplayData
    {
        public float attributeDropChance;
        public int waterProjectileCount = 8;
        public float attributeRepeatInterval = .22f;
        public int[] requiredPieces;
        public ComboRecipe[] recipes;
        public AbilityTuning[] abilities;
    }
    public static class GameplayRules
    {
        public static readonly ElementId[] BasicElements = { ElementId.Fire, ElementId.Water, ElementId.Grass, ElementId.Electric };
        static GameplayData data;
        public static GameplayData Data
        {
            get
            {
                if (data == null)
                {
                    var source = Resources.Load<TextAsset>("GameplayRules");
                    if (source == null) throw new InvalidOperationException("GameplayRules.json is missing");
                    data = JsonUtility.FromJson<GameplayData>(source.text);
                }
                return data;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => data = null;
        public static bool IsBasic(ElementId id) => Array.IndexOf(BasicElements, id) >= 0;
        public static bool TryRecipe(ElementId a, ElementId b, out ComboId result)
        {
            result = ComboId.None;
            if (a == b || !IsBasic(a) || !IsBasic(b)) return false;
            foreach (var recipe in Data.recipes)
                if ((recipe.first == a && recipe.second == b) || (recipe.first == b && recipe.second == a))
                { result = recipe.result; return result != ComboId.None; }
            return false;
        }
        public static AbilityTuning Tuning(ComboId id)
        {
            foreach (var tuning in Data.abilities) if (tuning.combo == id) return tuning;
            throw new InvalidOperationException("No tuning for combo " + id);
        }
        public static int Pieces(int stage)
        {
            var amounts = Data.requiredPieces;
            return stage > 0 && stage <= amounts.Length ? Mathf.Max(1, amounts[stage - 1]) : Mathf.Max(1, stage) * 2;
        }
    }
}
