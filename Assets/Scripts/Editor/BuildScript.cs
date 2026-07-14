using UnityEditor;
using System.IO;
using UnityEngine;

public class BuildScript
{
    [MenuItem("Build/Setup Application Icon")]
    public static void SetupApplicationIcon()
    {
        Texture2D logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/logo.png");
        if (logoTexture != null)
        {
            // Set Default Icon (Unknown)
            int[] unknownSizes = PlayerSettings.GetIconSizes(UnityEditor.Build.NamedBuildTarget.Unknown, IconKind.Application);
            if (unknownSizes != null && unknownSizes.Length > 0)
            {
                Texture2D[] unknownIcons = new Texture2D[unknownSizes.Length];
                for (int i = 0; i < unknownIcons.Length; i++)
                {
                    unknownIcons[i] = logoTexture;
                }
                PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, unknownIcons, IconKind.Application);
            }

            // Set Standalone Icons (expects 8 sizes for different resolution modes)
            int[] standaloneSizes = PlayerSettings.GetIconSizes(UnityEditor.Build.NamedBuildTarget.Standalone, IconKind.Application);
            if (standaloneSizes != null && standaloneSizes.Length > 0)
            {
                Texture2D[] standaloneIcons = new Texture2D[standaloneSizes.Length];
                for (int i = 0; i < standaloneIcons.Length; i++)
                {
                    standaloneIcons[i] = logoTexture;
                }
                PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Standalone, standaloneIcons, IconKind.Application);
            }

            Debug.Log("[BuildScript] Successfully set logo.png as application icon.");
        }
        else
        {
            Debug.LogError("[BuildScript] Failed to load logo.png at Assets/Art/logo.png. Make sure it exists.");
        }
    }

    [MenuItem("Build/Build Windows Game (v0.0.1)")]
    public static void BuildWindowsGame()
    {
        // Automatically setup the application icon before building
        SetupApplicationIcon();

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
