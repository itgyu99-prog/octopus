using UnityEditor;
using UnityEngine;
namespace OctopusSurvivors.EditorTools
{
    [InitializeOnLoad]
    public static class SpecialSkillTuningSetup
    {
        static SpecialSkillTuningSetup() { EditorApplication.delayCall += Ensure; }
        [MenuItem("문어/특수 스킬 Inspector 설정")]
        public static void Ensure()
        {
            const string path="Assets/Resources/SpecialSkillTuning.asset";
            var asset=AssetDatabase.LoadAssetAtPath<SpecialSkillTuning>(path);
            if (asset != null) return;
            asset=ScriptableObject.CreateInstance<SpecialSkillTuning>();
            AssetDatabase.CreateAsset(asset,path);AssetDatabase.SaveAssets();
        }
    }
}
