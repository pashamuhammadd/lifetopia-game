using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Lifetopia.EditorTools
{
    /// <summary>
    /// Batch-mode Web build entry point used by <c>Tools/Build-Web.ps1</c> when a specific
    /// main scene is required (for example the public placeholder scene FirstPlayable).
    ///
    /// Usage (Unity Editor must be closed for this project):
    ///   Unity.exe -batchmode -quit -projectPath &lt;project&gt;
    ///     -activeBuildProfile Assets/_Lifetopia/Settings/BuildProfiles/Web.asset
    ///     -executeMethod Lifetopia.EditorTools.WebBuildCommand.Build
    ///     -ltScene Assets/_Lifetopia/Scenes/FirstPlayable.unity -ltOutput Builds/Web/_incoming/Name
    ///     [-ltExtraScenes "Assets/_Lifetopia/Scenes/HomeInterior.unity;..."]
    ///
    /// Extra scenes are loaded at runtime by name (e.g. the house interior, loaded additively).
    ///
    /// The active Web build profile supplies the player settings (template, Brotli compression).
    /// Only the given scene is built. Nothing in the project or the profile asset is modified.
    /// </summary>
    public static class WebBuildCommand
    {
        private const string DefaultProfile = "Assets/_Lifetopia/Settings/BuildProfiles/Web.asset";

        public static void Build()
        {
            int exitCode = 1;
            try
            {
                string scene = Arg("-ltScene");
                string output = Arg("-ltOutput");
                string profilePath = Arg("-ltProfile") ?? DefaultProfile;
                string extra = Arg("-ltExtraScenes");

                if (string.IsNullOrEmpty(scene) || string.IsNullOrEmpty(output))
                    throw new ArgumentException("Both -ltScene and -ltOutput are required.");
                if (!File.Exists(scene))
                    throw new FileNotFoundException("Scene not found: " + scene);

                var sceneList = new System.Collections.Generic.List<string> { scene };
                if (!string.IsNullOrEmpty(extra))
                    foreach (string s in extra.Split(';'))
                    {
                        string e = s.Trim();
                        if (e.Length == 0) continue;
                        if (!File.Exists(e)) throw new FileNotFoundException("Extra scene not found: " + e);
                        if (!sceneList.Contains(e)) sceneList.Add(e);
                    }

                var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(profilePath);
                if (profile == null)
                    throw new FileNotFoundException("Build profile not found: " + profilePath);
                if (BuildProfile.GetActiveBuildProfile() != profile)
                {
                    Debug.Log("[LifetopiaBuild] Activating build profile " + profilePath);
                    BuildProfile.SetActiveBuildProfile(profile);
                }

                var options = new BuildPlayerOptions
                {
                    scenes = sceneList.ToArray(),
                    locationPathName = output,
                    target = BuildTarget.WebGL,
                    targetGroup = BuildTargetGroup.WebGL,
                    options = BuildOptions.None,
                };

                Debug.Log($"[LifetopiaBuild] Building {string.Join(", ", sceneList)} -> {output}");
                BuildReport report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;
                Debug.Log($"[LifetopiaBuild] Result={summary.result} errors={summary.totalErrors} warnings={summary.totalWarnings} size={summary.totalSize} time={summary.totalTime}");
                exitCode = summary.result == BuildResult.Succeeded ? 0 : 1;
            }
            catch (Exception e)
            {
                Debug.LogError("[LifetopiaBuild] Build failed: " + e.Message);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        private static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }
    }
}
