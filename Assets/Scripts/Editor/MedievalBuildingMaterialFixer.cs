using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MedievalBuildingMaterialFixer
{
    private const string MaterialsFolder = "Assets/low poly medieval buildings/Materials";
    private const string AutoFixSessionKey = "MetalDetectorGame.FixedMedievalBuildingMaterials";

    [InitializeOnLoadMethod]
    private static void AutoFixAfterScriptReload()
    {
        if (SessionState.GetBool(AutoFixSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoFixSessionKey, true);
        EditorApplication.delayCall += FixMaterials;
    }

    [MenuItem("Tools/Metal Detector Game/Fix Medieval Building Materials")]
    private static void FixMaterials()
    {
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");

        if (urpLitShader == null)
        {
            Debug.LogWarning("Could not find Universal Render Pipeline/Lit shader. Medieval building materials were not changed.");
            return;
        }

        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
        int changedCount = 0;

        foreach (string materialGuid in materialGuids)
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
            {
                continue;
            }

            Texture mainTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            Texture bumpTexture = material.HasProperty("_BumpMap") ? material.GetTexture("_BumpMap") : null;
            Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;

            Undo.RecordObject(material, "Fix medieval building material");
            material.shader = urpLitShader;

            if (mainTexture != null && material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", mainTexture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (bumpTexture != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", bumpTexture);
                material.EnableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(material);
            changedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Fixed " + changedCount + " medieval building materials for URP.");

        RelinkSceneRenderers();
        ReimportMedievalBuildingModels();
        AddSceneColliders();
    }

    [MenuItem("Tools/Metal Detector Game/Add Medieval Building Colliders")]
    private static void AddSceneColliders()
    {
        System.Collections.Generic.HashSet<string> materialNames = GetMedievalMaterialNames();
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int addedColliderCount = 0;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !UsesMedievalBuildingMaterial(renderer, materialNames))
            {
                continue;
            }

            if (renderer.GetComponent<Collider>() != null)
            {
                continue;
            }

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();

            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            Undo.RecordObject(renderer.gameObject, "Add medieval building collider");
            MeshCollider meshCollider = Undo.AddComponent<MeshCollider>(renderer.gameObject);
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
            EditorUtility.SetDirty(renderer.gameObject);
            addedColliderCount++;
        }

        if (addedColliderCount > 0)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
        }

        ReimportMedievalBuildingModels();
        Debug.Log("Added " + addedColliderCount + " medieval building mesh colliders.");
    }

    [MenuItem("Tools/Metal Detector Game/Relink Medieval Building Scene Renderers")]
    private static void RelinkSceneRenderers()
    {
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
        System.Collections.Generic.Dictionary<string, Material> materialsByName = new System.Collections.Generic.Dictionary<string, Material>();

        foreach (string materialGuid in materialGuids)
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material != null && !materialsByName.ContainsKey(material.name))
            {
                materialsByName.Add(material.name, material);
            }
        }

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int changedRendererCount = 0;

        foreach (Renderer renderer in renderers)
        {
            Material[] sharedMaterials = renderer.sharedMaterials;
            bool changedRenderer = false;

            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                Material currentMaterial = sharedMaterials[i];

                if (currentMaterial == null)
                {
                    continue;
                }

                string cleanName = currentMaterial.name.Replace(" (Instance)", string.Empty);

                if (!materialsByName.TryGetValue(cleanName, out Material replacementMaterial) || replacementMaterial == currentMaterial)
                {
                    continue;
                }

                sharedMaterials[i] = replacementMaterial;
                changedRenderer = true;
            }

            if (!changedRenderer)
            {
                continue;
            }

            Undo.RecordObject(renderer, "Relink medieval building materials");
            renderer.sharedMaterials = sharedMaterials;
            EditorUtility.SetDirty(renderer);
            changedRendererCount++;
        }

        if (changedRendererCount > 0)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
        }

        Debug.Log("Relinked medieval building materials on " + changedRendererCount + " scene renderers.");
    }

    private static System.Collections.Generic.HashSet<string> GetMedievalMaterialNames()
    {
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
        System.Collections.Generic.HashSet<string> materialNames = new System.Collections.Generic.HashSet<string>();

        foreach (string materialGuid in materialGuids)
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material != null)
            {
                materialNames.Add(material.name);
            }
        }

        return materialNames;
    }

    private static bool UsesMedievalBuildingMaterial(Renderer renderer, System.Collections.Generic.HashSet<string> materialNames)
    {
        Material[] sharedMaterials = renderer.sharedMaterials;

        foreach (Material material in sharedMaterials)
        {
            if (material == null)
            {
                continue;
            }

            string cleanName = material.name.Replace(" (Instance)", string.Empty);

            if (materialNames.Contains(cleanName))
            {
                return true;
            }
        }

        return false;
    }

    private static void ReimportMedievalBuildingModels()
    {
        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/low poly medieval buildings" });

        foreach (string modelGuid in modelGuids)
        {
            string modelPath = AssetDatabase.GUIDToAssetPath(modelGuid);
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
        }
    }
}
