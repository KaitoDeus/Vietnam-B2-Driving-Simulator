using UnityEngine;
using UnityEditor;

public class DumpPrefabHierarchy
{
    [MenuItem("Tools/Vietnam B2 Simulator/Dump Selected Prefab Hierarchy")]
    public static void DumpSelected()
    {
        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null)
        {
            Debug.LogError("Please select a prefab or GameObject first!");
            return;
        }

        Debug.Log("=== HIERARCHY DUMP FOR: " + selectedObj.name + " ===");
        PrintChildren(selectedObj.transform, 0);
    }

    private static void PrintChildren(Transform t, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 2);
        Debug.Log(indent + "- " + t.name + " (Local Pos: " + t.localPosition + ")");
        for (int i = 0; i < t.childCount; i++)
        {
            PrintChildren(t.GetChild(i), indentLevel + 1);
        }
    }
}
