using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MedievalBuildingMaterialFixer
{
    private const string MaterialsFolder = "Assets/low poly medieval buildings/Materials";
    private const string AlchemistHouseFolder = "Assets/BK_AlchemistHouse";
    private const string AutoFixSessionKey = "MetalDetectorGame.FixedMedievalBuildingMaterials";
    private const string AutoFixAlchemistSessionKey = "MetalDetectorGame.FixedAlchemistHouseMaterials";

    [InitializeOnLoadMethod]
    private static void AutoFixAfterScriptReload()
    {
        if (!SessionState.GetBool(AutoFixSessionKey, false))
        {
            SessionState.SetBool(AutoFixSessionKey, true);
            EditorApplication.delayCall += FixMaterials;
        }

        if (!SessionState.GetBool(AutoFixAlchemistSessionKey, false))
        {
            SessionState.SetBool(AutoFixAlchemistSessionKey, true);
            EditorApplication.delayCall += FixAlchemistHouseMaterials;
        }
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

    [MenuItem("Tools/Metal Detector Game/Fix Alchemist House Materials")]
    private static void FixAlchemistHouseMaterials()
    {
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");

        if (urpLitShader == null)
        {
            Debug.LogWarning("Could not find Universal Render Pipeline/Lit shader. Alchemist house materials were not changed.");
            return;
        }

        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { AlchemistHouseFolder });
        int changedCount = 0;

        foreach (string materialGuid in materialGuids)
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null || !ShouldFixAlchemistMaterial(material))
            {
                continue;
            }

            Texture mainTexture = GetTexture(material, "_BaseMap", "_MainTex", "_Albedo", "_TextureSample0");
            Texture normalTexture = GetTexture(material, "_BumpMap", "_NormalMap");
            Texture metallicTexture = GetTexture(material, "_MetallicGlossMap", "_MetallicMapGlossA", "_MetallicMap");
            Color color = GetColor(material, Color.white, "_BaseColor", "_Color", "_TintColor");
            float metallic = GetFloat(material, 0f, "_Metallic", "_Metalness");
            float smoothness = GetFloat(material, 0.45f, "_Smoothness", "_Glossiness", "_Gloss");
            bool transparent = ShouldBeTransparent(material, color);

            Undo.RecordObject(material, "Fix alchemist house material");
            material.shader = urpLitShader;

            SetTexture(material, mainTexture, "_BaseMap", "_MainTex");
            SetTexture(material, normalTexture, "_BumpMap");
            SetTexture(material, metallicTexture, "_MetallicGlossMap");
            SetColor(material, color, "_BaseColor", "_Color");
            SetFloat(material, metallic, "_Metallic");
            SetFloat(material, smoothness, "_Smoothness");

            if (normalTexture != null)
            {
                material.EnableKeyword("_NORMALMAP");
            }

            if (metallicTexture != null)
            {
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            if (transparent)
            {
                ConfigureTransparentMaterial(material);
            }
            else
            {
                ConfigureOpaqueMaterial(material);
            }

            EditorUtility.SetDirty(material);
            changedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Fixed " + changedCount + " alchemist house materials for URP.");
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

    private static bool ShouldFixAlchemistMaterial(Material material)
    {
        Shader shader = material.shader;

        if (shader == null || !shader.isSupported)
        {
            return true;
        }

        string shaderName = shader.name;

        return shaderName == "Standard"
            || shaderName.StartsWith("Custom/")
            || shaderName.StartsWith("Hidden/")
            || shaderName.Contains("Refraction")
            || shaderName.Contains("Volumetric");
    }

    private static bool ShouldBeTransparent(Material material, Color color)
    {
        string materialName = material.name.ToLowerInvariant();

        return color.a < 0.95f
            || material.renderQueue >= 3000
            || materialName.Contains("glass")
            || materialName.Contains("liquid")
            || materialName.Contains("window")
            || material.IsKeywordEnabled("_ALPHABLEND_ON")
            || material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        SetFloat(material, 1f, "_Surface");
        SetFloat(material, 0f, "_Blend");
        SetFloat(material, 0f, "_ZWrite");
        SetFloat(material, 0f, "_AlphaClip");
        SetFloat(material, (float)UnityEngine.Rendering.BlendMode.SrcAlpha, "_SrcBlend");
        SetFloat(material, (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, "_DstBlend");
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = 3000;
    }

    private static void ConfigureOpaqueMaterial(Material material)
    {
        SetFloat(material, 0f, "_Surface");
        SetFloat(material, 1f, "_ZWrite");
        material.SetOverrideTag("RenderType", "Opaque");
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = -1;
    }

    private static Texture GetTexture(Material material, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (!material.HasProperty(propertyName))
            {
                continue;
            }

            Texture texture = material.GetTexture(propertyName);

            if (texture != null)
            {
                return texture;
            }
        }

        return null;
    }

    private static Color GetColor(Material material, Color fallback, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                return material.GetColor(propertyName);
            }
        }

        return fallback;
    }

    private static float GetFloat(Material material, float fallback, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                return material.GetFloat(propertyName);
            }
        }

        return fallback;
    }

    private static void SetTexture(Material material, Texture texture, params string[] propertyNames)
    {
        if (texture == null)
        {
            return;
        }

        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }
    }

    private static void SetColor(Material material, Color color, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }
    }

    private static void SetFloat(Material material, float value, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }
    }
}
