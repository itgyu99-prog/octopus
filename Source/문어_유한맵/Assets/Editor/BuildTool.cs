using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OctopusSurvivors.EditorTools
{
    public static class BuildTool
    {
        const string OutputDir = "Build/Windows";
        const string ExecutableName = "OctopusSurvivors.exe";

        [MenuItem("문어/Windows 빌드")]
        public static void BuildWindows()
        {
            SystemDataChecks.Run();
            bool validation = Array.IndexOf(Environment.GetCommandLineArgs(), "-coopValidationBuild") >= 0;
            var options = new BuildPlayerOptions
            {
                scenes = ScenePaths(),
                locationPathName = validation ? $"Build/Validation/{ExecutableName}" : $"{OutputDir}/{ExecutableName}",
                target = BuildTarget.StandaloneWindows64,
                options = validation ? BuildOptions.Development : BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
                Debug.Log($"빌드 성공: {summary.outputPath} ({summary.totalSize / 1024 / 1024} MB)");
            else
                Debug.LogError($"빌드 실패: {summary.result}, 에러 {summary.totalErrors}개");

            if (Application.isBatchMode)
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        static string[] ScenePaths()
        {
            var scenes = EditorBuildSettings.scenes;
            var paths = new System.Collections.Generic.List<string>();
            foreach (var scene in scenes)
            {
                if (scene.enabled)
                    paths.Add(scene.path);
            }

            if (paths.Count == 0)
                paths.Add("Assets/Scenes/SampleScene.unity");

            return paths.ToArray();
        }
    }
}
