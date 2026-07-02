using UnityEngine;

public class SearchMarker : MonoBehaviour
{
    private const float FoundGroundDiscScale = 0.32f;
    private const float FoundGroundDiscThickness = 0.006f;
    private const float FoundArrowYOffset = 0.54f;
    private const float FoundArrowBobAmount = 0.07f;
    private const float FoundArrowBobSpeed = 3.2f;
    private const float FoundArrowHeadHeight = 0.17f;
    private const float FoundArrowHeadRadius = 0.08f;
    private const float FoundArrowShaftHeight = 0.3f;
    private const float FoundArrowShaftWidth = 0.026f;

    public enum MarkerType
    {
        EmptySpot,
        FoundTreasure,
        DugSpot
    }

    public MarkerType markerType;
    public TreasureRarity treasureRarity = TreasureRarity.Common;
    public float pulseSpeed = 4f;
    public float pulseAmount = 0.25f;

    private Vector3 startScale;
    private Renderer markerRenderer;
    private Renderer[] markerRenderers;
    private Transform floatingArrow;
    private Transform groundPulse;
    private Vector3 groundPulseStartScale;
    private Color baseColor;
    private float animationSeed;

    private void Awake()
    {
        startScale = transform.localScale;
        markerRenderer = GetComponent<Renderer>();
        animationSeed = Mathf.Abs(transform.position.x * 0.137f + transform.position.z * 0.071f);
    }

    private void Start()
    {
        baseColor = GetMarkerColor();
        DisableCollider(gameObject);

        if (markerType == MarkerType.FoundTreasure)
        {
            BuildFoundTreasureVisual();
            return;
        }

        if (markerRenderer != null)
        {
            markerRenderer.material = CreateTransparentMaterial(baseColor);
        }
    }

    private Material CreateTransparentMaterial(Color color)
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

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", 0f);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            Color emissionColor = new Color(color.r, color.g, color.b, 1f) * 0.55f;
            material.SetColor("_EmissionColor", emissionColor);
            material.EnableKeyword("_EMISSION");
        }

        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;

        return material;
    }

    private void SetRendererColor(Renderer renderer, Color color)
    {
        if (renderer == null || renderer.material == null)
        {
            return;
        }

        renderer.material.color = color;

        if (renderer.material.HasProperty("_BaseColor"))
        {
            renderer.material.SetColor("_BaseColor", color);
        }
    }

    private void BuildFoundTreasureVisual()
    {
        if (markerRenderer != null)
        {
            markerRenderer.enabled = false;
        }

        transform.localScale = Vector3.one;
        CreateGroundPulse();
        CreateFloatingArrow();
        markerRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void CreateGroundPulse()
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Dig Target Ground Ring";
        disc.transform.SetParent(transform, false);
        disc.transform.localPosition = Vector3.zero;
        disc.transform.localScale = new Vector3(FoundGroundDiscScale, FoundGroundDiscThickness, FoundGroundDiscScale);
        DisableCollider(disc);

        Renderer discRenderer = disc.GetComponent<Renderer>();

        if (discRenderer != null)
        {
            Color discColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.14f);
            discRenderer.material = CreateTransparentMaterial(discColor);
            discRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            discRenderer.receiveShadows = false;
        }

        groundPulse = disc.transform;
        groundPulseStartScale = disc.transform.localScale;
    }

    private void CreateFloatingArrow()
    {
        GameObject arrowRoot = new GameObject("Floating Dig Arrow");
        arrowRoot.transform.SetParent(transform, false);
        arrowRoot.transform.localPosition = Vector3.up * FoundArrowYOffset;
        floatingArrow = arrowRoot.transform;

        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Dig Arrow Shaft";
        shaft.transform.SetParent(floatingArrow, false);
        shaft.transform.localPosition = Vector3.up * ((FoundArrowHeadHeight + FoundArrowShaftHeight) * 0.5f);
        shaft.transform.localScale = new Vector3(FoundArrowShaftWidth, FoundArrowShaftHeight * 0.5f, FoundArrowShaftWidth);
        DisableCollider(shaft);
        AssignMarkerMaterial(shaft, new Color(baseColor.r, baseColor.g, baseColor.b, 0.42f));

        GameObject head = new GameObject("Dig Arrow Head");
        head.transform.SetParent(floatingArrow, false);
        head.transform.localPosition = Vector3.zero;
        head.transform.localScale = new Vector3(FoundArrowHeadRadius, FoundArrowHeadHeight, FoundArrowHeadRadius);

        MeshFilter meshFilter = head.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = head.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = CreateDownArrowHeadMesh();
        meshRenderer.material = CreateTransparentMaterial(new Color(baseColor.r, baseColor.g, baseColor.b, 0.48f));
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private void AssignMarkerMaterial(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();

        if (renderer == null)
        {
            return;
        }

        renderer.material = CreateTransparentMaterial(color);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void DisableCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();

        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private Mesh CreateDownArrowHeadMesh()
    {
        const int segments = 24;
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 6];

        vertices[0] = new Vector3(0f, -0.5f, 0f);
        vertices[1] = new Vector3(0f, 0.5f, 0f);

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            vertices[i + 2] = new Vector3(Mathf.Cos(angle), 0.5f, Mathf.Sin(angle));
        }

        int triangleIndex = 0;

        for (int i = 0; i < segments; i++)
        {
            int current = i + 2;
            int next = i == segments - 1 ? 2 : current + 1;

            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = current;

            triangles[triangleIndex++] = 1;
            triangles[triangleIndex++] = current;
            triangles[triangleIndex++] = next;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Down Dig Arrow Head";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void Update()
    {
        if (markerType != MarkerType.FoundTreasure)
        {
            return;
        }

        float wave = Mathf.Abs(Mathf.Sin((Time.time + animationSeed) * pulseSpeed));
        float bob = Mathf.Sin((Time.time + animationSeed) * FoundArrowBobSpeed) * FoundArrowBobAmount;
        float pulse = 1f + wave * pulseAmount;

        if (floatingArrow != null)
        {
            floatingArrow.localPosition = Vector3.up * (FoundArrowYOffset + bob);
        }

        if (groundPulse != null)
        {
            groundPulse.localScale = new Vector3(
                groundPulseStartScale.x * pulse,
                groundPulseStartScale.y,
                groundPulseStartScale.z * pulse
            );
        }

        if (markerRenderers != null)
        {
            Color dimColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.16f);
            Color brightColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.52f);

            foreach (Renderer renderer in markerRenderers)
            {
                if (renderer != null && renderer.enabled)
                {
                    SetRendererColor(renderer, Color.Lerp(dimColor, brightColor, wave));
                }
            }
        }
        else if (markerRenderer != null)
        {
            Color dimColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.22f);
            SetRendererColor(markerRenderer, Color.Lerp(dimColor, baseColor, wave));
            transform.localScale = new Vector3(startScale.x * pulse, startScale.y, startScale.z * pulse);
        }
    }

    private Color GetMarkerColor()
    {
        if (markerType == MarkerType.FoundTreasure)
        {
            if (treasureRarity == TreasureRarity.Epic)
            {
                return new Color(0.78f, 0.36f, 1f, 0.42f);
            }

            if (treasureRarity == TreasureRarity.Rare)
            {
                return new Color(0.18f, 0.72f, 1f, 0.38f);
            }

            return new Color(1f, 0.82f, 0.18f, 0.34f);
        }

        if (markerType == MarkerType.DugSpot)
        {
            return new Color(0.23f, 0.14f, 0.08f, 0.38f);
        }

        return new Color(0.2f, 0.2f, 0.2f, 0.18f);
    }
}
