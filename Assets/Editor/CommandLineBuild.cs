using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RingFighter.EditorTools
{
    /// <summary>
    /// Builds the Quest APK without opening the Unity window. Called by tools\build-quest.ps1:
    ///   Unity.exe -batchmode -projectPath . -buildTarget Android -executeMethod RingFighter.EditorTools.CommandLineBuild.BuildQuest
    /// Rebuilds the training scene first (like Ring Fighter > Build Training Scene) unless -skipScene is passed.
    /// Exits Unity with code 0 on success, 1 on failure.
    /// </summary>
    public static class CommandLineBuild
    {
        public const string ApkPath = "Builds/RingFighter.apk";
        private const string ScenePath = "Assets/Scenes/TrainingArena.unity";

        public static void BuildQuest()
        {
            int exitCode = 1;
            try
            {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

                bool skipScene = Array.IndexOf(Environment.GetCommandLineArgs(), "-skipScene") >= 0;
                if (!skipScene)
                {
                    Debug.Log("RingFighterBuild: rebuilding training scene");
                    RingFighterSceneBuilder.BuildTrainingSceneNoPrompt();
                }

                Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = ApkPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None
                };

                Debug.Log("RingFighterBuild: building " + ApkPath);
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                Debug.Log("RingFighterBuild: result " + summary.result + ", " + summary.totalErrors + " errors, "
                          + Mathf.RoundToInt((float)summary.totalTime.TotalSeconds) + " s");
                if (summary.result == BuildResult.Succeeded) exitCode = 0;
            }
            catch (Exception e)
            {
                Debug.LogError("RingFighterBuild: failed with " + e);
            }
            EditorApplication.Exit(exitCode);
        }
    }
}
