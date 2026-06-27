using UnityEngine;

public class SandGroundAppearance : MonoBehaviour
{
    public Color baseSandColor = new Color(0.86f, 0.72f, 0.46f, 1f);
    public Color lightGrainColor = new Color(0.98f, 0.88f, 0.63f, 1f);
    public Color darkGrainColor = new Color(0.62f, 0.48f, 0.28f, 1f);
    public int textureSize = 128;
    public float textureScale = 22f;

    private void Start()
    {
        ApplySandToGround();
    }

    public void ApplySandToGround()
    {
        GameObject groundObject = GameObject.Find("Ground");

        if (groundObject == null)
        {
            return;
        }

        Renderer groundRenderer = groundObject.GetComponent<Renderer>();

        if (groundRenderer == null)
        {
            return;
        }

        groundRenderer.material = CreateSandMaterial();
    }

    private Material CreateSandMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = "Runtime Sand Material";
        material.color = baseSandColor;

        Texture2D sandTexture = CreateSandTexture();
        sandTexture.wrapMode = TextureWrapMode.Repeat;
        sandTexture.filterMode = FilterMode.Bilinear;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", baseSandColor);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", sandTexture);
            material.SetTextureScale("_BaseMap", new Vector2(textureScale, textureScale));
        }

        material.mainTexture = sandTexture;
        material.mainTextureScale = new Vector2(textureScale, textureScale);

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.18f);
        }

        return material;
    }

    private Texture2D CreateSandTexture()
    {
        int size = Mathf.Max(16, textureSize);
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float broadNoise = Mathf.PerlinNoise(x * 0.055f, y * 0.055f);
                float fineNoise = Mathf.PerlinNoise((x + 37f) * 0.31f, (y + 91f) * 0.31f);
                float grain = Mathf.Clamp01(broadNoise * 0.65f + fineNoise * 0.35f);
                Color grainColor = Color.Lerp(darkGrainColor, lightGrainColor, grain);
                texture.SetPixel(x, y, Color.Lerp(baseSandColor, grainColor, 0.42f));
            }
        }

        texture.Apply();
        return texture;
    }
}
