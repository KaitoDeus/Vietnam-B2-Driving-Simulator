using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class VegetationPlacer : EditorWindow
{
    private float areaPerTree = 80f; // 1 tree per 80 sqm
    private float worldMargin = 1.2f; // distance from grass edges
    private float minScale = 0.8f;
    private float maxScale = 1.2f;
    private int maxTreesPerField = 50;

    [MenuItem("Tools/Vietnam B2 Simulator/Vegetation/Open Placer Tool")]
    public static void OpenWindow()
    {
        GetWindow<VegetationPlacer>("Vegetation Placer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Procedural Vegetation Placer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        areaPerTree = EditorGUILayout.FloatField("Area Per Tree (sqm)", areaPerTree);
        worldMargin = EditorGUILayout.FloatField("Edge Margin (meters)", worldMargin);
        minScale = EditorGUILayout.FloatField("Min Scale Multiplier", minScale);
        maxScale = EditorGUILayout.FloatField("Max Scale Multiplier", maxScale);
        maxTreesPerField = EditorGUILayout.IntField("Max Trees Per Field", maxTreesPerField);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Trees on Grass Fields"))
        {
            GenerateTrees();
        }

        if (GUILayout.Button("Clear All Generated Trees"))
        {
            ClearTrees();
        }
    }

    [MenuItem("Tools/Vietnam B2 Simulator/Vegetation/Generate Trees")]
    public static void GenerateTrees()
    {
        string practiceScenePath = "Assets/Scenes/Practice.unity";
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != practiceScenePath)
        {
            Debug.Log($"Opening Practice scene: {practiceScenePath}");
            EditorSceneManager.OpenScene(practiceScenePath);
        }

        GameObject mapGo = GameObject.Find("Map");
        if (mapGo == null)
        {
            Debug.LogError("Map GameObject not found in scene!");
            return;
        }

        // Find or create Trees container (we use the existing TreeLarge child under Map)
        Transform treeContainer = mapGo.transform.Find("TreeLarge");
        if (treeContainer == null)
        {
            GameObject containerGo = new GameObject("TreeLarge");
            containerGo.transform.SetParent(mapGo.transform);
            containerGo.transform.localPosition = Vector3.zero;
            containerGo.transform.localRotation = Quaternion.identity;
            containerGo.transform.localScale = Vector3.one;
            treeContainer = containerGo.transform;
        }

        // 1. Clear existing trees in container
        int deletedCount = 0;
        for (int i = treeContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(treeContainer.GetChild(i).gameObject);
            deletedCount++;
        }
        Debug.Log($"Cleared {deletedCount} existing trees from TreeLarge container.");

        // 2. Find grass container
        Transform grassContainer = mapGo.transform.Find("Grass");
        if (grassContainer == null)
        {
            Debug.LogError("Grass container not found under Map!");
            return;
        }

        // 3. Load tree assets
        string largePath = "Assets/Models/Race/treeLarge.fbx";
        string smallPath = "Assets/Models/Race/treeSmall.fbx";
        GameObject treeLargePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(largePath);
        GameObject treeSmallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(smallPath);

        if (treeLargePrefab == null || treeSmallPrefab == null)
        {
            Debug.LogError($"Failed to load tree models! Large: {treeLargePrefab != null}, Small: {treeSmallPrefab != null}");
            return;
        }

        // Settings
        float areaPerTreeVal = 80f;
        float marginVal = 1.2f;
        float minS = 0.8f;
        float maxS = 1.2f;
        int maxTrees = 50;

        int totalSpawned = 0;
        int activeFields = 0;

        // 4. Iterate over each grass field
        for (int i = 0; i < grassContainer.childCount; i++)
        {
            Transform grassField = grassContainer.GetChild(i);
            if (!grassField.gameObject.activeSelf) continue;

            MeshFilter mf = grassField.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Bounds localBounds = mf.sharedMesh.bounds;
            Vector3 lossyScale = grassField.lossyScale;

            // Calculate world dimensions and area
            float worldWidth = (localBounds.max.x - localBounds.min.x) * lossyScale.x;
            float worldLength = (localBounds.max.z - localBounds.min.z) * lossyScale.z;
            float area = worldWidth * worldLength;

            if (area < 15f) continue; // Skip very small patches

            activeFields++;

            // Calculate local margin boundaries
            float localMarginX = marginVal / lossyScale.x;
            float localMarginZ = marginVal / lossyScale.z;

            float minX = localBounds.min.x + localMarginX;
            float maxX = localBounds.max.x - localMarginX;
            float minZ = localBounds.min.z + localMarginZ;
            float maxZ = localBounds.max.z - localMarginZ;

            // Check if bounds are valid after margin
            if (maxX <= minX || maxZ <= minZ) continue;

            // Determine number of trees to spawn
            int spawnCount = Mathf.FloorToInt(area / areaPerTreeVal);
            spawnCount = Mathf.Min(spawnCount, maxTrees);
            if (spawnCount <= 0) spawnCount = 1; // At least one tree on decent size fields

            for (int t = 0; t < spawnCount; t++)
            {
                // Generate random position in local coordinates
                float localX = Random.Range(minX, maxX);
                float localZ = Random.Range(minZ, maxZ);
                float localY = localBounds.max.y; // Sit on top of the grass

                Vector3 localPos = new Vector3(localX, localY, localZ);
                Vector3 worldPos = grassField.TransformPoint(localPos);

                // Choose tree prefab randomly
                GameObject prefab = (Random.value > 0.5f) ? treeLargePrefab : treeSmallPrefab;

                // Instantiate tree
                GameObject newTree = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (newTree == null) continue;

                newTree.name = prefab.name + "_" + totalSpawned;
                newTree.transform.SetParent(treeContainer);
                newTree.transform.position = worldPos;

                // Random rotation
                float rotY = Random.Range(0f, 360f);
                newTree.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

                // Random scale variation
                float scaleScale = Random.Range(minS, maxS);
                newTree.transform.localScale = Vector3.one * scaleScale;

                totalSpawned++;
            }
        }

        EditorUtility.SetDirty(treeContainer.gameObject);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();

        Debug.Log($"Procedurally spawned {totalSpawned} trees across {activeFields} grass fields.");
    }

    [MenuItem("Tools/Vietnam B2 Simulator/Vegetation/Clear Trees")]
    public static void ClearTrees()
    {
        string practiceScenePath = "Assets/Scenes/Practice.unity";
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != practiceScenePath)
        {
            EditorSceneManager.OpenScene(practiceScenePath);
        }

        GameObject mapGo = GameObject.Find("Map");
        if (mapGo == null) return;

        Transform treeContainer = mapGo.transform.Find("TreeLarge");
        if (treeContainer == null) return;

        int deletedCount = 0;
        for (int i = treeContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(treeContainer.GetChild(i).gameObject);
            deletedCount++;
        }

        EditorUtility.SetDirty(treeContainer.gameObject);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();

        Debug.Log($"Cleared {deletedCount} trees from TreeLarge container.");
    }
}
