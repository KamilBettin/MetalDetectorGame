using System.Collections.Generic;
using UnityEngine;

public class InteractionTargetHighlight : MonoBehaviour
{
    private readonly List<GameObject> outlineObjects = new List<GameObject>();
    private Transform target;
    private Transform builtTarget;
    private Material outlineMaterial;

    public void SetTarget(Transform newTarget)
    {
        if (target == newTarget)
        {
            return;
        }

        target = newTarget;
        RebuildOutline();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            ClearOutlineObjects();
            builtTarget = null;
            return;
        }

        if (builtTarget != target)
        {
            RebuildOutline();
        }

        SetVisible(target.gameObject.activeInHierarchy);
    }

    private void RebuildOutline()
    {
        ClearOutlineObjects();
        builtTarget = target;

        if (target == null)
        {
            return;
        }

        EnsureMaterial();

        if (outlineMaterial == null)
        {
            return;
        }

        CreateMeshOutlines(target);
        CreateSkinnedMeshOutlines(target);
        SetVisible(true);
    }

    private void CreateMeshOutlines(Transform root)
    {
        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter sourceFilter in meshFilters)
        {
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                continue;
            }

            MeshRenderer sourceRenderer = sourceFilter.GetComponent<MeshRenderer>();

            if (sourceRenderer == null || !sourceRenderer.enabled || IsOutlineObject(sourceRenderer.transform))
            {
                continue;
            }

            GameObject outlineObject = new GameObject("Interaction Mesh Outline");
            outlineObject.transform.SetParent(sourceFilter.transform, false);
            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one;

            MeshFilter outlineFilter = outlineObject.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
            outlineRenderer.sharedMaterials = CreateOutlineMaterials(sourceRenderer.sharedMaterials.Length);
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            outlineRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            outlineObjects.Add(outlineObject);
        }
    }

    private void CreateSkinnedMeshOutlines(Transform root)
    {
        SkinnedMeshRenderer[] skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (SkinnedMeshRenderer sourceRenderer in skinnedRenderers)
        {
            if (sourceRenderer == null || sourceRenderer.sharedMesh == null || !sourceRenderer.enabled || IsOutlineObject(sourceRenderer.transform))
            {
                continue;
            }

            GameObject outlineObject = new GameObject("Interaction Skinned Outline");
            outlineObject.transform.SetParent(sourceRenderer.transform, false);
            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one;

            SkinnedMeshRenderer outlineRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>();
            outlineRenderer.sharedMesh = sourceRenderer.sharedMesh;
            outlineRenderer.rootBone = sourceRenderer.rootBone;
            outlineRenderer.bones = sourceRenderer.bones;
            outlineRenderer.localBounds = sourceRenderer.localBounds;
            outlineRenderer.sharedMaterials = CreateOutlineMaterials(sourceRenderer.sharedMaterials.Length);
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.updateWhenOffscreen = true;
            outlineObjects.Add(outlineObject);
        }
    }

    private void SetVisible(bool visible)
    {
        foreach (GameObject outlineObject in outlineObjects)
        {
            if (outlineObject != null && outlineObject.activeSelf != visible)
            {
                outlineObject.SetActive(visible);
            }
        }
    }

    private void ClearOutlineObjects()
    {
        for (int i = outlineObjects.Count - 1; i >= 0; i--)
        {
            GameObject outlineObject = outlineObjects[i];

            if (outlineObject == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(outlineObject);
            }
            else
            {
                DestroyImmediate(outlineObject);
            }
        }

        outlineObjects.Clear();
    }

    private void EnsureMaterial()
    {
        if (outlineMaterial != null)
        {
            return;
        }

        Shader shader = Shader.Find("MetalDetector/Interaction Outline");

        if (shader == null)
        {
            return;
        }

        outlineMaterial = new Material(shader);
        outlineMaterial.name = "Runtime Interaction Mesh Outline";
        SetMaterialColor(outlineMaterial, new Color(0.2f, 1f, 0.34f, 1f));
        SetFloatIfPresent(outlineMaterial, 0.009f, "_OutlineWidth");
        outlineMaterial.renderQueue = 3000;

        if (outlineMaterial.HasProperty("_Surface"))
        {
            outlineMaterial.SetFloat("_Surface", 1f);
        }

        if (outlineMaterial.HasProperty("_SurfaceType"))
        {
            outlineMaterial.SetFloat("_SurfaceType", 1f);
        }

        outlineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        outlineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        outlineMaterial.SetInt("_ZWrite", 0);
        outlineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
        SetFloatIfPresent(outlineMaterial, 1f, "_Cull", "_CullMode", "_CullModeForward");
        SetFloatIfPresent(outlineMaterial, 0f, "_Metallic");
        SetFloatIfPresent(outlineMaterial, 0f, "_Smoothness", "_Glossiness");
        outlineMaterial.DisableKeyword("_ALPHATEST_ON");
        outlineMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }

    private Material[] CreateOutlineMaterials(int sourceCount)
    {
        int materialCount = Mathf.Max(1, sourceCount);
        Material[] materials = new Material[materialCount];

        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = outlineMaterial;
        }

        return materials;
    }

    private static bool IsOutlineObject(Transform candidate)
    {
        return candidate != null && candidate.name.StartsWith("Interaction ", System.StringComparison.Ordinal);
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

    }

    private static void SetFloatIfPresent(Material material, float value, params string[] propertyNames)
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
