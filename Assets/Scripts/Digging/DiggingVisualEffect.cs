using System.Collections.Generic;
using UnityEngine;

public class DiggingVisualEffect : MonoBehaviour
{
    public const float DigHitDuration = 0.72f;
    public const float FinalRevealDuration = 1.35f;

    private class SandChunk
    {
        public Transform transform;
        public Vector3 startPosition;
        public Vector3 velocity;
        public float spinSpeed;
    }

    private readonly List<SandChunk> sandChunks = new List<SandChunk>();
    private Transform shovelRoot;
    private float timer;
    private float duration;
    private bool finalReveal;

    public static float Play(Vector3 position, bool finalReveal, int itemValue, GameObject shovelPrefab, Vector3 shovelLocalPosition, Vector3 shovelLocalEulerAngles, Vector3 shovelLocalScale)
    {
        GameObject effectObject = new GameObject(finalReveal ? "Final Dig Effect" : "Dig Hit Effect");
        effectObject.transform.position = new Vector3(position.x, position.y + 0.035f, position.z);

        DiggingVisualEffect effect = effectObject.AddComponent<DiggingVisualEffect>();
        effect.Initialize(finalReveal, itemValue, shovelPrefab, shovelLocalPosition, shovelLocalEulerAngles, shovelLocalScale);
        return effect.duration;
    }

    private void Initialize(bool isFinalReveal, int itemValue, GameObject shovelPrefab, Vector3 shovelLocalPosition, Vector3 shovelLocalEulerAngles, Vector3 shovelLocalScale)
    {
        finalReveal = isFinalReveal;
        duration = finalReveal ? FinalRevealDuration : DigHitDuration;
        CreateShovel(itemValue, shovelPrefab, shovelLocalPosition, shovelLocalEulerAngles, shovelLocalScale);
        CreateSandBurst(itemValue);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        AnimateShovel(t);
        AnimateSandChunks();
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    private void CreateShovel(int itemValue, GameObject shovelPrefab, Vector3 shovelLocalPosition, Vector3 shovelLocalEulerAngles, Vector3 shovelLocalScale)
    {
        shovelRoot = new GameObject("Animated Shovel").transform;
        shovelRoot.SetParent(transform, false);
        shovelRoot.localPosition = new Vector3(0f, 0.16f, 0f);

        if (shovelPrefab != null)
        {
            GameObject shovelInstance = Instantiate(shovelPrefab, shovelRoot);
            shovelInstance.name = shovelPrefab.name + " Animated";
            shovelInstance.transform.localPosition = shovelLocalPosition;
            shovelInstance.transform.localRotation = Quaternion.Euler(shovelLocalEulerAngles);
            shovelInstance.transform.localScale = shovelLocalScale;
            DisableCollidersInChildren(shovelInstance);
            ApplyPrefabShovelMaterials(shovelInstance, shovelPrefab.name.ToLowerInvariant().Contains("clean"));
            return;
        }

        Color handleColor = new Color(0.38f, 0.2f, 0.09f, 1f);
        Color metalColor = itemValue > 100
            ? new Color(0.86f, 0.78f, 0.55f, 1f)
            : new Color(0.52f, 0.56f, 0.55f, 1f);

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Shovel Handle";
        handle.transform.SetParent(shovelRoot, false);
        handle.transform.localPosition = new Vector3(0f, 0.48f, 0f);
        handle.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        handle.transform.localScale = new Vector3(0.045f, 0.55f, 0.045f);
        AssignMaterial(handle, handleColor);
        DisableCollider(handle);

        GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grip.name = "Shovel Grip";
        grip.transform.SetParent(shovelRoot, false);
        grip.transform.localPosition = new Vector3(-0.27f, 0.95f, 0f);
        grip.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        grip.transform.localScale = new Vector3(0.28f, 0.055f, 0.055f);
        AssignMaterial(grip, handleColor);
        DisableCollider(grip);

        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        blade.name = "Shovel Blade";
        blade.transform.SetParent(shovelRoot, false);
        blade.transform.localPosition = new Vector3(0.25f, -0.02f, 0f);
        blade.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        blade.transform.localScale = new Vector3(0.18f, 0.12f, 0.055f);
        AssignMaterial(blade, metalColor);
        DisableCollider(blade);
    }

    private void CreateSandBurst(int itemValue)
    {
        int count = finalReveal ? 12 : 7;
        float strength = itemValue > 100 ? 1.25f : itemValue >= 50 ? 1.08f : 0.92f;
        Color sandColor = new Color(0.72f, 0.58f, 0.34f, 1f);

        for (int i = 0; i < count; i++)
        {
            float angle = i / (float)count * Mathf.PI * 2f + Random.Range(-0.18f, 0.18f);
            float distance = Random.Range(0.08f, 0.24f);

            GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            chunk.name = "Sand Chunk";
            chunk.transform.SetParent(transform, false);
            chunk.transform.localPosition = new Vector3(Mathf.Cos(angle) * distance, 0.05f, Mathf.Sin(angle) * distance);
            chunk.transform.localScale = Vector3.one * Random.Range(0.045f, 0.075f);
            AssignMaterial(chunk, sandColor);
            DisableCollider(chunk);

            sandChunks.Add(new SandChunk
            {
                transform = chunk.transform,
                startPosition = chunk.transform.localPosition,
                velocity = new Vector3(Mathf.Cos(angle) * Random.Range(0.35f, 0.7f), Random.Range(0.55f, 0.9f), Mathf.Sin(angle) * Random.Range(0.35f, 0.7f)) * strength,
                spinSpeed = Random.Range(130f, 260f)
            });
        }
    }

    private void AnimateShovel(float t)
    {
        if (shovelRoot == null)
        {
            return;
        }

        float swing = Mathf.Sin(t * Mathf.PI);
        float impact = Mathf.Sin(Mathf.Clamp01(t * 1.5f) * Mathf.PI);
        float strike = 1f - Mathf.Abs(1f - t * 2f);
        shovelRoot.localPosition = new Vector3(0f, 0.08f + impact * 0.36f, Mathf.Lerp(0.16f, -0.04f, strike));
        shovelRoot.localRotation = Quaternion.Euler(Mathf.Lerp(-18f, 18f, swing), 0f, Mathf.Lerp(-4f, 4f, swing));
        shovelRoot.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, Mathf.Clamp01(t * 2f));
    }

    private void AnimateSandChunks()
    {
        for (int i = 0; i < sandChunks.Count; i++)
        {
            SandChunk chunk = sandChunks[i];

            if (chunk.transform == null)
            {
                continue;
            }

            float chunkTime = Mathf.Min(timer, 0.8f);
            Vector3 position = chunk.startPosition + chunk.velocity * chunkTime;
            position.y -= 1.85f * chunkTime * chunkTime;
            chunk.transform.localPosition = position;
            chunk.transform.Rotate(Vector3.up, chunk.spinSpeed * Time.deltaTime, Space.Self);

            float fadeStart = duration * 0.55f;
            float fade = timer <= fadeStart ? 1f : Mathf.Clamp01(1f - (timer - fadeStart) / Mathf.Max(0.05f, duration - fadeStart));
            chunk.transform.localScale = Vector3.one * Mathf.Lerp(0.01f, 0.065f, fade);
        }
    }

    private void AssignMaterial(GameObject target, Color color)
    {
        Renderer targetRenderer = target.GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            targetRenderer.material = CreateMaterial(color);
        }
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        return material;
    }

    private void ApplyPrefabShovelMaterials(GameObject shovelInstance, bool isCleanShovel)
    {
        Renderer[] renderers = shovelInstance.GetComponentsInChildren<Renderer>();

        foreach (Renderer targetRenderer in renderers)
        {
            Material[] sourceMaterials = targetRenderer.sharedMaterials;
            Material[] runtimeMaterials = new Material[sourceMaterials.Length];

            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                bool looksLikeHandle = targetRenderer.name.ToLowerInvariant().Contains("handle");
                Color tint = GetShovelTint(isCleanShovel, looksLikeHandle);
                runtimeMaterials[i] = CreateShovelMaterial(sourceMaterials[i], tint, isCleanShovel && !looksLikeHandle);
            }

            targetRenderer.materials = runtimeMaterials;
        }
    }

    private Color GetShovelTint(bool isCleanShovel, bool looksLikeHandle)
    {
        if (looksLikeHandle)
        {
            return isCleanShovel
                ? new Color(0.42f, 0.24f, 0.11f, 1f)
                : new Color(0.28f, 0.14f, 0.065f, 1f);
        }

        return isCleanShovel
            ? new Color(0.82f, 0.86f, 0.88f, 1f)
            : new Color(0.62f, 0.28f, 0.12f, 1f);
    }

    private Material CreateShovelMaterial(Material sourceMaterial, Color tint, bool shinyMetal)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = tint;

        Texture baseTexture = GetMaterialTexture(sourceMaterial, "_BaseMap", "_MainTex");
        Texture normalTexture = GetMaterialTexture(sourceMaterial, "_BumpMap");
        Texture metallicTexture = GetMaterialTexture(sourceMaterial, "_MetallicGlossMap", "_Metallic");

        if (baseTexture != null)
        {
            SetMaterialTexture(material, baseTexture, "_BaseMap", "_MainTex");
        }

        if (normalTexture != null)
        {
            SetMaterialTexture(material, normalTexture, "_BumpMap");
            material.EnableKeyword("_NORMALMAP");
        }

        if (metallicTexture != null)
        {
            SetMaterialTexture(material, metallicTexture, "_MetallicGlossMap");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        SetMaterialColor(material, tint, "_BaseColor", "_Color");
        SetMaterialFloat(material, shinyMetal ? 0.85f : 0.28f, "_Metallic");
        SetMaterialFloat(material, shinyMetal ? 0.72f : 0.38f, "_Smoothness", "_Glossiness");
        return material;
    }

    private Texture GetMaterialTexture(Material material, params string[] names)
    {
        if (material == null)
        {
            return null;
        }

        foreach (string propertyName in names)
        {
            if (material.HasProperty(propertyName))
            {
                Texture texture = material.GetTexture(propertyName);

                if (texture != null)
                {
                    return texture;
                }
            }
        }

        return null;
    }

    private void SetMaterialTexture(Material material, Texture texture, params string[] names)
    {
        foreach (string propertyName in names)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }
    }

    private void SetMaterialColor(Material material, Color color, params string[] names)
    {
        foreach (string propertyName in names)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }
    }

    private void SetMaterialFloat(Material material, float value, params string[] names)
    {
        foreach (string propertyName in names)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }
    }

    private void DisableCollider(GameObject target)
    {
        Collider targetCollider = target.GetComponent<Collider>();

        if (targetCollider != null)
        {
            targetCollider.enabled = false;
        }
    }

    private void DisableCollidersInChildren(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>();

        foreach (Collider targetCollider in colliders)
        {
            targetCollider.enabled = false;
        }
    }
}
