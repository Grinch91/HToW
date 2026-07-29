using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Repeatable builds, from the Editor menu or from the command line.
///
/// The command-line path requires the Editor to be closed, because Unity takes an
/// exclusive lock on the project. The menu item exists so a build can be made without
/// closing anything, which is usually what you want while working.
///
/// Output defaults to a sibling of the repository rather than a folder inside it. The
/// 2014 project committed its build output — an 11 MB executable plus a 14 MB data
/// folder, about 96% of the repository — and worse, that executable was built from a
/// different revision than the committed source, so it actively misled anyone reading
/// it. See Docs/Architecture.md section 10.
/// </summary>
public static class BuildScript
{
    private const string DefaultOutputDirectory = "../HToW-build";
    private const string ExecutableName = "HToW.exe";

    /// <summary>Environment variable that overrides the output path in CI or scripts.</summary>
    private const string OutputPathVariable = "HTOW_BUILD_PATH";

    [MenuItem("HToW/Build Windows Player %#b")]
    public static void BuildFromMenu()
    {
        BuildReport report = Build();
        if (report == null)
        {
            return;
        }

        if (report.summary.result == BuildResult.Succeeded)
        {
            EditorUtility.RevealInFinder(report.summary.outputPath);
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Build failed",
                "The build did not complete. See the Console for details.",
                "OK");
        }
    }

    /// <summary>
    /// Command-line entry point. Exits with 0 on success and 1 on failure so a script
    /// or CI job can tell the difference:
    ///
    /// Unity.exe -batchmode -nographics -projectPath &lt;project&gt;
    ///           -executeMethod BuildScript.BuildFromCommandLine -logFile build.log
    /// </summary>
    public static void BuildFromCommandLine()
    {
        BuildReport report = Build();
        bool ok = report != null && report.summary.result == BuildResult.Succeeded;
        EditorApplication.Exit(ok ? 0 : 1);
    }

    private static BuildReport Build()
    {
        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("No enabled scenes in Build Profiles; nothing to build.");
            return null;
        }

        string outputPath = ResolveOutputPath();
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        Debug.Log("Building " + scenes.Length + " scene(s) to " + outputPath);
        for (int i = 0; i < scenes.Length; i++)
        {
            Debug.Log("  scene " + i + ": " + scenes[i]);
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        Debug.Log(string.Format(
            "Build {0}: {1} error(s), {2} warning(s), {3:N1} MB, {4:N0}s -> {5}",
            summary.result,
            summary.totalErrors,
            summary.totalWarnings,
            summary.totalSize / (1024f * 1024f),
            summary.totalTime.TotalSeconds,
            summary.outputPath));

        return report;
    }

    private static string[] GetEnabledScenes()
    {
        var enabled = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled && !string.IsNullOrEmpty(scene.path))
            {
                enabled.Add(scene.path);
            }
        }
        return enabled.ToArray();
    }

    private static string ResolveOutputPath()
    {
        string overridePath = Environment.GetEnvironmentVariable(OutputPathVariable);
        if (!string.IsNullOrEmpty(overridePath))
        {
            return overridePath;
        }

        // Application.dataPath is <project>/Assets, so go up one to reach the project.
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.GetFullPath(Path.Combine(projectRoot, DefaultOutputDirectory, ExecutableName));
    }
}
