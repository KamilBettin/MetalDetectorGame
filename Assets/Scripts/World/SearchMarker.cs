using UnityEngine;

public class SearchMarker : MonoBehaviour
{
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
    private Color baseColor;

    private void Awake()
    {
        startScale = transform.localScale;
        markerRenderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        baseColor = GetMarkerColor();

        if (markerRenderer != null)
        {
            markerRenderer.material = CreateTransparentMaterial(baseColor);
        }

        Collider markerCollider = GetComponent<Collider>();

        if (markerCollider != null)
        {
            markerCollider.enabled = false;
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

        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;

        return material;
    }

    private void Update()
    {
        if (markerType != MarkerType.FoundTreasure)
        {
            return;
        }

        float wave = Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed));
        float pulse = 1f + wave * pulseAmount;
        transform.localScale = new Vector3(startScale.x * pulse, startScale.y, startScale.z * pulse);

        if (markerRenderer != null)
        {
            Color dimColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.22f);
            markerRenderer.material.color = Color.Lerp(dimColor, baseColor, wave);
        }
    }

    private Color GetMarkerColor()
    {
        if (markerType == MarkerType.FoundTreasure)
        {
            if (treasureRarity == TreasureRarity.Epic)
            {
                return new Color(0.78f, 0.36f, 1f, 0.68f);
            }

            if (treasureRarity == TreasureRarity.Rare)
            {
                return new Color(0.18f, 0.72f, 1f, 0.6f);
            }

            return new Color(1f, 0.82f, 0.18f, 0.55f);
        }

        if (markerType == MarkerType.DugSpot)
        {
            return new Color(0.23f, 0.14f, 0.08f, 0.38f);
        }

        return new Color(0.2f, 0.2f, 0.2f, 0.18f);
    }
}
