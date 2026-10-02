#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DrivingSim.EditorTools
{
    /// <summary>
    /// Produces a Safari-compatible Web build for quick iPhone testing from Windows.
    /// The first build runs automatically after scripts compile and Play Mode is stopped.
    /// </summary>
    [InitializeOnLoad]
    public static class WebBuildAutomation
    {
        private const string BuildKey = "DrivingSim.WebBuild.v1";
        private const string DefaultBuildDirectory = "Builds/Web";
        private static bool buildInProgress;

        static WebBuildAutomation()
        {
            if (!Application.isBatchMode) EditorApplication.delayCall += TryAutomaticBuild;
        }

        [MenuItem("Driving Sim/Build Web for iPhone", priority = 60)]
        public static void BuildWeb()
        {
            if (buildInProgress) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode first. The Web build will start automatically afterwards.");
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                return;
            }

            buildInProgress = true;
            try
            {
                string buildDirectory = Environment.GetEnvironmentVariable("DRIVING_SIM_WEB_OUTPUT");
                if (string.IsNullOrWhiteSpace(buildDirectory)) buildDirectory = DefaultBuildDirectory;

                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL &&
                    !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                {
                    Debug.LogError("DRIVING_SIM_WEB_BUILD_FAILED: Could not switch to the Web platform.");
                    return;
                }

                // Uncompressed output works with simple local/static servers without
                // requiring special Content-Encoding response headers.
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.runInBackground = true;

                string[] scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray();
                if (scenes.Length == 0)
                {
                    Debug.LogError("DRIVING_SIM_WEB_BUILD_FAILED: No enabled scenes are in Build Profiles.");
                    return;
                }

                Directory.CreateDirectory(buildDirectory);
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = buildDirectory,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None
                });

                if (report.summary.result == BuildResult.Succeeded)
                {
                    EditorPrefs.SetBool(BuildKey, true);
                    Debug.Log($"DRIVING_SIM_WEB_BUILD_SUCCEEDED: {Path.GetFullPath(buildDirectory)} " +
                              $"({report.summary.totalSize / (1024f * 1024f):0.0} MB)");
                }
                else
                {
                    Debug.LogError($"DRIVING_SIM_WEB_BUILD_FAILED: {report.summary.result}, " +
                                   $"{report.summary.totalErrors} errors.");
                }
            }
            finally
            {
                buildInProgress = false;
            }
        }

        private static void TryAutomaticBuild()
        {
            if (EditorPrefs.GetBool(BuildKey, false) || buildInProgress) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutomaticBuild;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                return;
            }

            BuildWeb();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.delayCall += TryAutomaticBuild;
        }
    }
}
#endif
