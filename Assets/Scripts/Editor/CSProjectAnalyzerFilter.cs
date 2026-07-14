using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class CSProjectAnalyzerFilter : AssetPostprocessor
{
    private static string OnGeneratedCSProject(string path, string content)
    {
        // Match: <Analyzer Include="DLL_PATH" />
        MatchEvaluator evaluator = new MatchEvaluator(match =>
        {
            string dllPath = match.Groups[1].Value;
            
            // If the analyzer DLL does not exist on this machine's disk, strip it out.
            if (!File.Exists(dllPath))
            {
                Debug.Log($"[CSProjectAnalyzerFilter] Stripped missing analyzer reference from project: {dllPath}");
                return ""; // Remove the line entirely
            }
            return match.Value; // Keep the analyzer
        });

        // Regex pattern matches any Analyzer tag with its attributes
        string modifiedContent = Regex.Replace(content, @"\s*<Analyzer Include=""([^""]+)""\s*/>", evaluator);
        return modifiedContent;
    }
}
