using UnityEngine;
using UnityEditor;
using System.IO;
using TMPro;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class ConfigureTrafficLights : EditorWindow
{
    private static bool _setupAttempted = false;

    static ConfigureTrafficLights()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        if (!_setupAttempted)
        {
            _setupAttempted = true;
            // Only queue setup if we are not in play mode or about to play
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
            {
                EditorApplication.delayCall += () => {
                    Setup();
                };
            }
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            Debug.Log("[ConfigureTrafficLights] Entered edit mode, running setup...");
            Setup();
        }
    }

    [MenuItem("Tools/Vietnam B2 Simulator/Setup Traffic Lights")]
    public static void Setup()
    {
        // If we are in play mode or transitioning, abort setup
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Debug.Log("[ConfigureTrafficLights] Starting traffic light setup...");

        // Ensure we are working on the Practice scene
        string practiceScenePath = "Assets/Scenes/Practice.unity";
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != practiceScenePath)
        {
            Debug.Log($"[ConfigureTrafficLights] Opening Practice scene: {practiceScenePath}");
            EditorSceneManager.OpenScene(practiceScenePath);
        }

        // 1. Refresh AssetDatabase to ensure split textures are imported
        AssetDatabase.Refresh();

        // 2. Load the textures
        string texturesDir = "Assets/Tarbo-CITY-TrafficLights/Textures";
        Texture2D redTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texturesDir}/emit_red.png");
        Texture2D yellowTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texturesDir}/emit_yellow.png");
        Texture2D greenTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texturesDir}/emit_green.png");
        Texture2D offTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texturesDir}/emit_off.png");

        if (redTex == null || yellowTex == null || greenTex == null || offTex == null)
        {
            Debug.LogError("[ConfigureTrafficLights] Split textures not found. Make sure python script ran successfully and refreshed AssetDatabase.");
            return;
        }

        // 3. Load original material
        string originalMatPath = "Assets/Tarbo-CITY-TrafficLights/Materials/Tarbo_CITY_TrafficLights.mat";
        Material originalMat = AssetDatabase.LoadAssetAtPath<Material>(originalMatPath);
        if (originalMat == null)
        {
            Debug.LogError($"[ConfigureTrafficLights] Original material not found at {originalMatPath}");
            return;
        }

        // Configure original material to have strong emission and correct emission map
        originalMat.EnableKeyword("_EMISSION");
        originalMat.SetColor("_EmissionColor", Color.white * 3.5f);
        
        string originalEmitTexPath = "Assets/Tarbo-CITY-TrafficLights/Textures/emit.png";
        Texture2D originalEmitTex = AssetDatabase.LoadAssetAtPath<Texture2D>(originalEmitTexPath);
        if (originalEmitTex != null)
        {
            originalMat.SetTexture("_EmissionMap", originalEmitTex);
        }
        EditorUtility.SetDirty(originalMat);

        // 4. Create/Configure split materials
        string materialsDir = "Assets/Tarbo-CITY-TrafficLights/Materials";
        Material redMat = GetOrCreateMaterial(originalMat, $"{materialsDir}/Tarbo_CITY_TrafficLights_Red.mat", redTex);
        redMat.SetColor("_EmissionColor", Color.white * 3.5f);
        EditorUtility.SetDirty(redMat);

        Material yellowMat = GetOrCreateMaterial(originalMat, $"{materialsDir}/Tarbo_CITY_TrafficLights_Yellow.mat", yellowTex);
        yellowMat.SetColor("_EmissionColor", Color.white * 3.5f);
        EditorUtility.SetDirty(yellowMat);

        Material greenMat = GetOrCreateMaterial(originalMat, $"{materialsDir}/Tarbo_CITY_TrafficLights_Green.mat", greenTex);
        greenMat.SetColor("_EmissionColor", Color.white * 3.5f);
        EditorUtility.SetDirty(greenMat);

        Material offMat = GetOrCreateMaterial(originalMat, $"{materialsDir}/Tarbo_CITY_TrafficLights_Off.mat", offTex);
        // Off material should have 0 emission intensity
        offMat.SetColor("_EmissionColor", Color.black);
        EditorUtility.SetDirty(offMat);

        // 5. Configure GameObjects in active scene
        // Find all root objects and traverse
        GameObject[] rootObjects = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in rootObjects)
        {
            SetupTrafficLightsInHierarchy(root.transform, originalMat, redMat, yellowMat, greenMat, offMat);
        }

        // 6. Connect traffic lights to TrafficLightIntersection (create if not found)
        TrafficLightIntersection intersection = FindFirstObjectByType<TrafficLightIntersection>();
        if (intersection == null)
        {
            Debug.Log("[ConfigureTrafficLights] TrafficLightIntersection not found in scene. Creating a new one...");
            GameObject intersectionGo = new GameObject("TrafficLightIntersection");
            intersection = intersectionGo.AddComponent<TrafficLightIntersection>();
            intersection.greenDuration = 15;
            intersection.yellowDuration = 3;
        }

        if (intersection != null)
        {
            Debug.Log("[ConfigureTrafficLights] Found TrafficLightIntersection, synchronizing groups...");
            // Get all TrafficLights configured in the scene
            TrafficLight[] allLights = FindObjectsByType<TrafficLight>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            // Group lights based on their names or positions
            // Group A has lights (TrafficLight_01, 03), Group B has lights (TrafficLight_02, 04)
            System.Collections.Generic.List<TrafficLight> groupAList = new System.Collections.Generic.List<TrafficLight>();
            System.Collections.Generic.List<TrafficLight> groupBList = new System.Collections.Generic.List<TrafficLight>();

            foreach (var light in allLights)
            {
                if (light.name == "TrafficLight_01" || light.name == "TrafficLight_03")
                {
                    groupAList.Add(light);
                }
                else if (light.name == "TrafficLight_02" || light.name == "TrafficLight_04")
                {
                    groupBList.Add(light);
                }
            }

            if (intersection.groupA == null) intersection.groupA = new TrafficLightIntersection.LightGroup() { name = "Group A" };
            if (intersection.groupB == null) intersection.groupB = new TrafficLightIntersection.LightGroup() { name = "Group B" };

            intersection.groupA.lights = groupAList.ToArray();
            intersection.groupB.lights = groupBList.ToArray();
            EditorUtility.SetDirty(intersection);
            Debug.Log($"[ConfigureTrafficLights] Synchronized Intersection: Group A count = {intersection.groupA.lights.Length}, Group B count = {intersection.groupB.lights.Length}");
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[ConfigureTrafficLights] Traffic light setup completed successfully and scene saved!");
    }

    private static Material GetOrCreateMaterial(Material original, string path, Texture2D emissionTex)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(original);
            AssetDatabase.CreateAsset(mat, path);
            Debug.Log($"Created material: {path}");
        }
        else
        {
            // Update properties
            mat.CopyPropertiesFromMaterial(original);
        }

        mat.SetTexture("_EmissionMap", emissionTex);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.white * 2.0f); // Make it bright/glow in HDR
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static MeshRenderer FindLightRenderer(Transform t)
    {
        MeshRenderer[] renderers = t.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in renderers)
        {
            string name = r.name.ToLower();
            if (name.Contains("base") || name.Contains("text") || name.Contains("countdown"))
            {
                continue;
            }
            return r;
        }
        return null;
    }

    private static void SetupTrafficLightsInHierarchy(Transform t, Material originalMat, Material redMat, Material yellowMat, Material greenMat, Material offMat)
    {
        if (t.name.StartsWith("TrafficLight_0"))
        {
            Debug.Log($"Configuring traffic light object: {t.name}");

            // Add TrafficLight script if not present
            TrafficLight lightController = t.GetComponent<TrafficLight>();
            if (lightController == null)
            {
                lightController = t.gameObject.AddComponent<TrafficLight>();
            }

            MeshRenderer meshRenderer = FindLightRenderer(t);
            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = originalMat; // Original material with full emission for editor preview

                lightController.lightRenderer = meshRenderer;
                lightController.redMaterialIndex = 0;
                lightController.yellowMaterialIndex = 0;
                lightController.greenMaterialIndex = 0;

                lightController.redOnMaterial = redMat;
                lightController.redOffMaterial = offMat;
                lightController.yellowOnMaterial = yellowMat;
                lightController.yellowOffMaterial = offMat;
                lightController.greenOnMaterial = greenMat;
                lightController.greenOffMaterial = offMat;

                // Configure Mesh Swapper
                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    string meshName = meshFilter.sharedMesh.name;
                    string basePrefix = "";
                    if (meshName.EndsWith("Black", System.StringComparison.OrdinalIgnoreCase))
                    {
                        basePrefix = meshName.Substring(0, meshName.Length - 5);
                    }
                    else if (meshName.EndsWith("Green", System.StringComparison.OrdinalIgnoreCase))
                    {
                        basePrefix = meshName.Substring(0, meshName.Length - 5);
                    }
                    else if (meshName.EndsWith("Yellow", System.StringComparison.OrdinalIgnoreCase))
                    {
                        basePrefix = meshName.Substring(0, meshName.Length - 6);
                    }
                    else if (meshName.EndsWith("White", System.StringComparison.OrdinalIgnoreCase))
                    {
                        basePrefix = meshName.Substring(0, meshName.Length - 5);
                    }
                    else if (meshName.EndsWith("Whtie", System.StringComparison.OrdinalIgnoreCase))
                    {
                        basePrefix = meshName.Substring(0, meshName.Length - 5);
                    }

                    if (!string.IsNullOrEmpty(basePrefix))
                    {
                        Mesh offMesh = FindMeshAsset(basePrefix + "Black") ?? FindMeshAsset(basePrefix + "black");
                        Mesh greenMesh = FindMeshAsset(basePrefix + "Green") ?? FindMeshAsset(basePrefix + "green");
                        Mesh yellowMesh = FindMeshAsset(basePrefix + "Yellow") ?? FindMeshAsset(basePrefix + "yellow");
                        Mesh redMesh = FindMeshAsset(basePrefix + "White") ?? FindMeshAsset(basePrefix + "white") ?? FindMeshAsset(basePrefix + "Whtie") ?? FindMeshAsset(basePrefix + "whtie");

                        lightController.offMesh = offMesh;
                        lightController.greenMesh = greenMesh;
                        lightController.yellowMesh = yellowMesh;
                        lightController.redMesh = redMesh;

                        Debug.Log($"[ConfigureTrafficLights] {t.name} meshes configured: Off={offMesh?.name}, Red={redMesh?.name}, Yellow={yellowMesh?.name}, Green={greenMesh?.name}");
                    }
                }

                // Remove all countdown text objects in the hierarchy
                Transform[] allChildren = t.GetComponentsInChildren<Transform>(true);
                foreach (var child in allChildren)
                {
                    if (child != null && child.gameObject.name == "CountdownText")
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
                lightController.countdownText = null;
            }
            else
            {
                Debug.LogWarning($"No Light MeshRenderer found under {t.name}");
            }

            EditorUtility.SetDirty(t.gameObject);
        }

        for (int i = 0; i < t.childCount; i++)
        {
            SetupTrafficLightsInHierarchy(t.GetChild(i), originalMat, redMat, yellowMat, greenMat, offMat);
        }
    }

    private static Mesh FindMeshAsset(string meshName)
    {
        string[] extensions = { "FBX", "fbx", "obj", "OBJ" };
        foreach (var ext in extensions)
        {
            string path = $"Assets/Tarbo-CITY-TrafficLights/Meshes/{meshName}.{ext}";
            if (File.Exists(path))
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (Object asset in assets)
                {
                    if (asset is Mesh)
                    {
                        return (Mesh)asset;
                    }
                }
            }
        }
        return null;
    }
}
