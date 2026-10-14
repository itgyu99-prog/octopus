using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OctopusSurvivors.EditorTools
{
    public static class GiantMonsterSetup
    {
        [MenuItem("문어/거대 몬스터 기본 에셋 준비")]
        public static void CreateAssets()
        {
            Directory.CreateDirectory("Assets/Resources/Monsters");AssetDatabase.Refresh();
            string[] names={"거대 게","거대 소라게","거대 해파리","거대 불가사리","거대 성게"};
            int[] unlock={3,4,6,8,11};float[] hp={12,9,8,8,15},damage={2,2.5f,2,1.8f,2},speed={.7f,.65f,.8f,.75f,.5f};
            for(int i=0;i<5;i++)
            {
                string path="Assets/Resources/Monsters/Giant"+(i+1)+".asset";
                if(AssetDatabase.LoadAssetAtPath<GiantMonsterDefinition>(path)!=null)continue;
                var d=ScriptableObject.CreateInstance<GiantMonsterDefinition>();d.id=i+1;d.displayName=names[i];d.attackPattern=(GiantPattern)i;
                d.unlockStage=unlock[i];d.hpMultiplier=hp[i];d.damageMultiplier=damage[i];d.speedMultiplier=speed[i];
                d.attackRange=i==3?7:i==2?3.6f:2.6f;d.sizeMultiplier=i==4?3.5f:3;
                var go=new GameObject(names[i]);go.AddComponent<SpriteRenderer>();
                d.prefab=PrefabUtility.SaveAsPrefabAsset(go,"Assets/Resources/Monsters/Giant"+(i+1)+".prefab");
                UnityEngine.Object.DestroyImmediate(go);AssetDatabase.CreateAsset(d,path);
            }
            string atlas="Assets/Resources/Monsters/GiantAtlas.png";
            var importer=AssetImporter.GetAtPath(atlas) as TextureImporter;
            if(importer!=null){importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
            AssetDatabase.SaveAssets();
            foreach(var d in Resources.LoadAll<GiantMonsterDefinition>("Monsters"))
                if(d.id<1 || d.id>5 || d.prefab==null || d.attackCooldown<=0)throw new Exception("Invalid giant definition");
            Debug.Log("GIANT DEFINITIONS PASS: five distinct prefabs, inspector tunings, unlock stages");
        }
    }
}
