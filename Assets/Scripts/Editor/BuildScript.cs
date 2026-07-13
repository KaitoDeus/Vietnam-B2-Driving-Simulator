using UnityEditor;
using System.IO;
using UnityEngine;

public class BuildScript
{
    [MenuItem("Build/Build Windows Game (v0.0.1)")]
    public static void BuildWindowsGame()
    {
        string buildFolder = Path.Combine(Directory.GetCurrentDirectory(), "Builds/v0.0.1/Windows");
        if (!Directory.Exists(buildFolder))
        {
            Directory.CreateDirectory(buildFolder);
        }

        // Configuration of scenes included in the build
        string[] scenes = new string[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Practice.unity",
            "Assets/Scenes/TheoryExam.unity"
        };

        string buildPath = Path.Combine(buildFolder, "Vietnam B2 Driving Simulator.exe");

        Debug.Log("Starting Windows Build (StandaloneWindows64)...");
        
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"Build Succeeded! Total size: {summary.totalSize} bytes. Path: {buildPath}");
            EditorUtility.RevealInFinder(buildPath);
        }
        else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
        {
            Debug.LogError("Build Failed! Please check the Console for detailed logs.");
        }
    }
}
