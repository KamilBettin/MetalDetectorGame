using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanWaterSurface : MonoBehaviour
{
    public float size = 2900f;
    [Range(32, 240)] public int subdivisions = 180;
    public float waterLevel = 0f;
    public bool animateMeshWaves;
    public float waveHeight = 0.65f;
    public float waveScale = 0.052f;
    public float waveSpeed = 0.7f;
    public Color deepColor = new Color(0.01f, 0.13f, 0.18f, 0.88f);
    public Color shallowColor = new Color(0.11f, 0.25f, 0.29f, 0.78f);
    public Color foamColor = new Color(0.22f, 0.34f, 0.36f, 1f);
    public float foamIntensity = 0f;
    public float sparkle = 0.5f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh waterMesh;
    private Vector3[] baseVertices;
    private Vector3[] animatedVertices;
    private int generatedSubdivisions;
    private float generatedSize;
    private float lastAppliedWaterLevel = float.NaN;
    private bool wasAnimatingMeshWaves;

    private void OnEnable()
    {
        CacheComponents();
        EnsureMesh();
        ConfigureMaterial();
        UpdateWaterSurface();
    }

    private void OnValidate()
    {
        subdivisions = Mathf.Clamp(subdivisions, 32, 240);
        size = Mathf.Max(10f, size);
        waveHeight = Mathf.Max(0f, waveHeight);

        CacheComponents();
        EnsureMesh();
        ConfigureMaterial();
        UpdateWaterSurface();
    }

    private void Update()
    {
        UpdateWaterSurface();
    }

    public void ApplyAtmosphere(bool night, float skyArc)
    {
        CacheComponents();

        if (meshRenderer == null || meshRenderer.sharedMaterial == null)
        {
            return;
        }

        float arc = Mathf.Clamp01(skyArc);

        if (night)
        {
            deepColor = Color.Lerp(new Color(0.008f, 0.03f, 0.075f, 0.9f), new Color(0.012f, 0.055f, 0.12f, 0.88f), arc);
            shallowColor = Color.Lerp(new Color(0.02f, 0.075f, 0.13f, 0.8f), new Color(0.035f, 0.13f, 0.2f, 0.78f), arc);
            foamColor = new Color(0.48f, 0.62f, 0.74f, 1f);
            sparkle = Mathf.Lerp(0.2f, 0.36f, arc);
        }
        else
        {
            deepColor = Color.Lerp(new Color(0.025f, 0.15f, 0.21f, 0.88f), new Color(0.012f, 0.22f, 0.3f, 0.86f), arc);
            shallowColor = Color.Lerp(new Color(0.09f, 0.31f, 0.35f, 0.78f), new Color(0.09f, 0.45f, 0.5f, 0.74f), arc);
            foamColor = Color.Lerp(new Color(0.7f, 0.77f, 0.72f, 1f), new Color(0.76f, 0.92f, 0.9f, 1f), arc);
            sparkle = Mathf.Lerp(0.55f, 0.82f, arc);
        }

        Material material = meshRenderer.sharedMaterial;
        SetColor(material, "_Color", deepColor);
        SetColor(material, "_BaseColor", shallowColor);
        SetColor(material, "_DeepColor", deepColor);
        SetColor(material, "_ShallowColor", shallowColor);
        SetColor(material, "_FoamColor", foamColor);
        SetFloat(material, "_Sparkle", sparkle);

        Light mainLight = RenderSettings.sun;

        if (mainLight != null)
        {
            SetVector(material, "_WorldLightDir", -mainLight.transform.forward);
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying && waterMesh != null)
        {
            Destroy(waterMesh);
        }
        else if (waterMesh != null)
        {
            DestroyImmediate(waterMesh);
        }
    }

    private void CacheComponents()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
    }

    private void EnsureMesh()
    {
        if (meshFilter == null)
        {
            return;
        }

        if (waterMesh != null && generatedSubdivisions == subdivisions && Mathf.Approximately(generatedSize, size))
        {
            return;
        }

        if (waterMesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(waterMesh);
            }
            else
            {
                DestroyImmediate(waterMesh);
            }
        }

        generatedSubdivisions = subdivisions;
        generatedSize = size;
        int vertexCountPerSide = subdivisions + 1;
        int vertexCount = vertexCountPerSide * vertexCountPerSide;
        baseVertices = new Vector3[vertexCount];
        animatedVertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[subdivisions * subdivisions * 6];
        float step = size / subdivisions;
        float halfSize = size * 0.5f;

        int vertexIndex = 0;

        for (int z = 0; z < vertexCountPerSide; z++)
        {
            for (int x = 0; x < vertexCountPerSide; x++)
            {
                Vector3 vertex = new Vector3(-halfSize + x * step, 0f, -halfSize + z * step);
                baseVertices[vertexIndex] = vertex;
                animatedVertices[vertexIndex] = vertex;
                uvs[vertexIndex] = new Vector2(x / (float)subdivisions, z / (float)subdivisions);
                vertexIndex++;
            }
        }

        int triangleIndex = 0;

        for (int z = 0; z < subdivisions; z++)
        {
            for (int x = 0; x < subdivisions; x++)
            {
                int bottomLeft = z * vertexCountPerSide + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + vertexCountPerSide;
                int topRight = topLeft + 1;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
            }
        }

        waterMesh = new Mesh
        {
            name = "Generated Ocean Water Surface",
            hideFlags = HideFlags.DontSave,
            vertices = animatedVertices,
            uv = uvs,
            triangles = triangles
        };

        waterMesh.MarkDynamic();
        waterMesh.RecalculateNormals();
        waterMesh.RecalculateBounds();
        waterMesh.bounds = new Bounds(Vector3.zero, new Vector3(size, Mathf.Max(4f, waveHeight * 8f), size));
        meshFilter.sharedMesh = waterMesh;
    }

    private void ConfigureMaterial()
    {
        if (meshRenderer == null)
        {
            return;
        }

        Material material = meshRenderer.sharedMaterial;
        Shader waterShader = Shader.Find("MetalDetector/Ocean Water");

        if (material == null && waterShader != null)
        {
            material = new Material(waterShader)
            {
                name = "FreeIslandWaterRuntime"
            };
            meshRenderer.sharedMaterial = material;
        }

        if (material == null)
        {
            return;
        }

        if (material.shader != null && material.shader.name != "MetalDetector/Ocean Water")
        {
            return;
        }

        if (waterShader != null && material.shader != waterShader)
        {
            material.shader = waterShader;
        }

        SetColor(material, "_Color", deepColor);
        SetColor(material, "_BaseColor", shallowColor);
        SetColor(material, "_DeepColor", deepColor);
        SetColor(material, "_ShallowColor", shallowColor);
        SetColor(material, "_FoamColor", foamColor);
        SetFloat(material, "_WaveHeight", waveHeight);
        SetFloat(material, "_WaveScale", waveScale);
        SetFloat(material, "_WaveSpeed", waveSpeed);
        SetFloat(material, "_FoamIntensity", foamIntensity);
        SetFloat(material, "_Sparkle", sparkle);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(material);
        }
#endif
    }

    private void UpdateWaterSurface()
    {
        if (waterMesh == null || baseVertices == null || animatedVertices == null)
        {
            return;
        }

        SyncWaterLevelWithTransform();

        if (animateMeshWaves)
        {
            AnimateMeshWaves();
            wasAnimatingMeshWaves = true;
        }
        else if (wasAnimatingMeshWaves)
        {
            waterMesh.vertices = baseVertices;
            waterMesh.RecalculateNormals();
            wasAnimatingMeshWaves = false;
        }

        waterMesh.bounds = new Bounds(Vector3.zero, new Vector3(size, Mathf.Max(4f, waveHeight * 8f), size));
    }

    private void AnimateMeshWaves()
    {
        float time = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;

        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 vertex = baseVertices[i];
            float wave = GetCombinedWave(vertex.x, vertex.z, time);
            vertex.y = wave * waveHeight;
            animatedVertices[i] = vertex;
        }

        waterMesh.vertices = animatedVertices;
        waterMesh.RecalculateNormals();
    }

    private float GetCombinedWave(float x, float z, float time)
    {
        float speedTime = time * waveSpeed;
        float longSwell = Mathf.Sin(x * waveScale + z * waveScale * 0.37f + speedTime) * 0.42f;
        float crossingSwell = Mathf.Sin((x * 0.62f + z * 0.78f) * waveScale * 1.65f - speedTime * 1.38f) * 0.28f;
        float shortRipple = Mathf.Sin((-x * 0.74f + z * 0.52f) * waveScale * 3.15f + speedTime * 2.1f) * 0.18f;
        float tightRipple = Mathf.Sin((x * 0.25f - z * 0.97f) * waveScale * 4.4f - speedTime * 2.75f) * 0.12f;
        return longSwell + crossingSwell + shortRipple + tightRipple;
    }

    private void SyncWaterLevelWithTransform()
    {
        Vector3 position = transform.position;
        bool hasAppliedLevel = !float.IsNaN(lastAppliedWaterLevel);

        if (!Application.isPlaying
            && hasAppliedLevel
            && !Mathf.Approximately(position.y, waterLevel)
            && Mathf.Approximately(waterLevel, lastAppliedWaterLevel))
        {
            waterLevel = position.y;
        }
        else if (!Mathf.Approximately(position.y, waterLevel))
        {
            transform.position = new Vector3(position.x, waterLevel, position.z);
        }

        lastAppliedWaterLevel = waterLevel;
    }

    private static void SetColor(Material material, string propertyName, Color value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, value);
        }
    }

    private static void SetFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static void SetVector(Material material, string propertyName, Vector4 value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetVector(propertyName, value);
        }
    }
}
