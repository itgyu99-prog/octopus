using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AshenOath.Editor
{
    public static class OathBuild
    {
        [MenuItem("잿빛 서약/Windows 빌드")]
        public static void Windows()
        {
            bool validation=Array.IndexOf(Environment.GetCommandLineArgs(),"-oath-validation")>=0;
            PlayerSettings.companyName="Ashen Oath";PlayerSettings.productName="Ashen Oath";PlayerSettings.bundleVersion="0.1.0";
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            const string scene="Assets/Scenes/Sanctum.unity";
            if(!File.Exists(scene))
            {
                var empty=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.SaveScene(empty,scene);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scene,true)};
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{scene},target=BuildTarget.StandaloneWindows64,
                locationPathName=validation?"Build/Validation/AshenOath.exe":"Build/Windows/AshenOath.exe",
                options=validation?BuildOptions.Development:BuildOptions.None});
            Debug.Log("OATH_BUILD_RESULT: "+report.summary.result+" errors="+report.summary.totalErrors);
            if(Application.isBatchMode)EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
        }
    }
}
