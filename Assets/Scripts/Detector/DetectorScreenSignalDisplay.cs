using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MetalDetector))]
public class DetectorScreenSignalDisplay : MonoBehaviour
{
    public string screenRendererName = "Plane.003";
    public int textureSize = 128;
    public float refreshRate = 20f;
    public bool forceUnlitScreenMaterial = true;
    [Range(0, 3)] public int screenRotationQuarterTurns = 3;
    public bool flipScreenX = true;
    public bool flipScreenY;
    public Color backgroundColor = new Color(0.015f, 0.03f, 0.025f, 1f);
    public Color panelColor = new Color(0.035f, 0.08f, 0.065f, 1f);
    public Color lineColor = new Color(0.18f, 0.42f, 0.32f, 1f);
    public Color inactiveColor = new Color(0.05f, 0.14f, 0.11f, 1f);
    public Color powerOnColor = new Color(0.18f, 1f, 0.35f, 1f);
    public Color powerOffColor = new Color(0.12f, 0.28f, 0.18f, 1f);
    public Color warningColor = new Color(1f, 0.28f, 0.12f, 1f);
    public Color mediumSignalColor = new Color(1f, 0.78f, 0.16f, 1f);
    public Color strongSignalColor = new Color(0.25f, 1f, 0.42f, 1f);
    public Color textColor = new Color(0.72f, 1f, 0.78f, 1f);

    private const int MinTextureSize = 64;
    private const int MaxTextureSize = 512;

    private readonly List<Renderer> screenRenderers = new List<Renderer>();
    private readonly List<Material> screenMaterials = new List<Material>();
    private MetalDetector metalDetector;
    private Texture2D screenTexture;
    private Color32[] pixels;
    private float nextRefreshTime;

    private void Awake()
    {
        metalDetector = GetComponent<MetalDetector>();
        EnsureTexture();
        DrawScreen(0f, false);
    }

    private void OnEnable()
    {
        metalDetector = metalDetector != null ? metalDetector : GetComponent<MetalDetector>();
        EnsureTexture();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || screenRenderers.Count == 0)
        {
            return;
        }

        float interval = refreshRate > 0f ? 1f / refreshRate : 0f;

        if (interval > 0f && Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + interval;

        float signalStrength = metalDetector != null ? Mathf.Clamp01(metalDetector.CurrentSignal) : 0f;
        DrawScreen(signalStrength, IsScanInputHeld());
    }

    private void OnDestroy()
    {
        for (int i = 0; i < screenMaterials.Count; i++)
        {
            if (screenMaterials[i] != null)
            {
                Destroy(screenMaterials[i]);
            }
        }

        if (screenTexture != null)
        {
            Destroy(screenTexture);
        }
    }

    public void RegisterModelRoot(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.transform.name != screenRendererName)
            {
                continue;
            }

            RegisterScreenRenderer(renderer);
        }
    }

    private void RegisterScreenRenderer(Renderer screenRenderer)
    {
        if (screenRenderers.Contains(screenRenderer))
        {
            return;
        }

        EnsureTexture();

        Material material = screenRenderer.material;
        ConfigureScreenMaterial(material);
        screenRenderer.material = material;
        screenRenderers.Add(screenRenderer);
        screenMaterials.Add(material);
    }

    private void ConfigureScreenMaterial(Material material)
    {
        if (material == null || screenTexture == null)
        {
            return;
        }

        if (forceUnlitScreenMaterial)
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");

            if (unlitShader == null)
            {
                unlitShader = Shader.Find("Unlit/Texture");
            }

            if (unlitShader != null && material.shader != unlitShader)
            {
                material.shader = unlitShader;
            }
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", screenTexture);
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetTextureOffset("_BaseMap", Vector2.zero);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", screenTexture);
            material.SetTextureScale("_MainTex", Vector2.one);
            material.SetTextureOffset("_MainTex", Vector2.zero);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", Color.black);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0f);
        }
    }

    private void EnsureTexture()
    {
        int safeSize = Mathf.Clamp(textureSize, MinTextureSize, MaxTextureSize);

        if (screenTexture != null && screenTexture.width == safeSize && pixels != null && pixels.Length == safeSize * safeSize)
        {
            return;
        }

        if (screenTexture != null)
        {
            Destroy(screenTexture);
        }

        screenTexture = new Texture2D(safeSize, safeSize, TextureFormat.RGBA32, false);
        screenTexture.name = "Detector Signal Panel";
        screenTexture.filterMode = FilterMode.Point;
        screenTexture.wrapMode = TextureWrapMode.Clamp;
        pixels = new Color32[safeSize * safeSize];

        for (int i = 0; i < screenMaterials.Count; i++)
        {
            ConfigureScreenMaterial(screenMaterials[i]);
        }
    }

    private bool IsScanInputHeld()
    {
        return !GameUIState.AnyMenuOpen && Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    private void DrawScreen(float signalStrength, bool powerOn)
    {
        if (screenTexture == null || pixels == null)
        {
            return;
        }

        Fill(backgroundColor);

        int size = screenTexture.width;
        RectInt signalRect = ScaleRect(9, 8, 110, 52, size);
        RectInt powerRect = ScaleRect(10, 76, 48, 34, size);
        RectInt percentRect = ScaleRect(68, 76, 50, 34, size);

        DrawSignalGraph(signalRect, signalStrength, powerOn);
        DrawPowerButton(powerRect, powerOn);
        DrawPercentIndicator(percentRect, signalStrength);

        DrawRectOutline(ScaleRect(5, 4, 118, 116, size), lineColor * 0.72f, Mathf.Max(1, size / 128));
        DrawLine(ScaleX(8, size), ScaleY(68, size), ScaleX(120, size), ScaleY(68, size), lineColor * 0.42f);

        screenTexture.SetPixels32(pixels);
        screenTexture.Apply(false);
    }

    private void DrawSignalGraph(RectInt rect, float signalStrength, bool powerOn)
    {
        DrawRect(rect, panelColor);
        DrawRectOutline(rect, lineColor * 0.72f, 1);
        DrawTinyText("SIGNAL", rect.x + 5, rect.y + 4, 1, textColor * 0.78f);

        int bars = 12;
        int gap = Mathf.Max(1, rect.width / 80);
        int usableWidth = rect.width - 14;
        int barWidth = Mathf.Max(2, (usableWidth - gap * (bars - 1)) / bars);
        int bottom = rect.yMax - 6;
        int maxHeight = rect.height - 22;
        int activeBars = Mathf.RoundToInt(signalStrength * bars);
        Color signalColor = GetSignalColor(signalStrength);
        float pulse = powerOn ? 0.86f + Mathf.Sin(Time.unscaledTime * 10f) * 0.14f : 0.55f;

        for (int i = 0; i < bars; i++)
        {
            float t = (i + 1f) / bars;
            int barHeight = Mathf.Max(3, Mathf.RoundToInt(Mathf.Lerp(maxHeight * 0.22f, maxHeight, t)));
            int x = rect.x + 7 + i * (barWidth + gap);
            int y = bottom - barHeight;
            Color color = i < activeBars ? signalColor * pulse : inactiveColor;
            DrawRect(new RectInt(x, y, barWidth, barHeight), color);
        }
    }

    private void DrawPowerButton(RectInt rect, bool powerOn)
    {
        Color fill = powerOn ? powerOnColor * 0.42f : powerOffColor;
        Color border = powerOn ? powerOnColor : lineColor * 0.65f;
        Color label = powerOn ? Color.white : textColor * 0.55f;

        DrawRect(rect, fill);
        DrawRectOutline(rect, border, 2);
        DrawFilledCircle(new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.28f), rect.height * 0.11f, powerOn ? powerOnColor : inactiveColor);
        DrawTinyTextCentered("POWER", rect, 1, label, 8);
    }

    private void DrawPercentIndicator(RectInt rect, float signalStrength)
    {
        int percent = Mathf.RoundToInt(signalStrength * 100f);
        string percentText = percent + "%";

        DrawRect(rect, panelColor);
        DrawRectOutline(rect, GetSignalColor(signalStrength) * 0.9f, 2);
        DrawTinyTextCentered("SIG", new RectInt(rect.x, rect.y + 3, rect.width, 8), 1, textColor * 0.7f, 0);
        DrawTinyTextCentered(percentText, new RectInt(rect.x, rect.y + 14, rect.width, 16), 2, GetSignalColor(signalStrength), 0);
    }

    private Color GetSignalColor(float signalStrength)
    {
        if (signalStrength <= 0.01f)
        {
            return textColor * 0.42f;
        }

        if (signalStrength < 0.45f)
        {
            return Color.Lerp(warningColor, mediumSignalColor, signalStrength / 0.45f);
        }

        return Color.Lerp(mediumSignalColor, strongSignalColor, Mathf.InverseLerp(0.45f, 1f, signalStrength));
    }

    private void Fill(Color color)
    {
        Color32 pixel = color;

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = pixel;
        }
    }

    private RectInt ScaleRect(int x, int y, int width, int height, int textureSizeValue)
    {
        return new RectInt(
            ScaleX(x, textureSizeValue),
            ScaleY(y, textureSizeValue),
            Mathf.Max(1, Mathf.RoundToInt(width * textureSizeValue / 128f)),
            Mathf.Max(1, Mathf.RoundToInt(height * textureSizeValue / 128f))
        );
    }

    private int ScaleX(int x, int textureSizeValue)
    {
        return Mathf.RoundToInt(x * textureSizeValue / 128f);
    }

    private int ScaleY(int y, int textureSizeValue)
    {
        return Mathf.RoundToInt(y * textureSizeValue / 128f);
    }

    private void DrawRect(RectInt rect, Color color)
    {
        for (int y = rect.y; y < rect.yMax; y++)
        {
            for (int x = rect.x; x < rect.xMax; x++)
            {
                SetPixel(x, y, color);
            }
        }
    }

    private void DrawRectOutline(RectInt rect, Color color, int thickness)
    {
        int safeThickness = Mathf.Max(1, thickness);

        for (int i = 0; i < safeThickness; i++)
        {
            DrawLine(rect.x + i, rect.y + i, rect.xMax - 1 - i, rect.y + i, color);
            DrawLine(rect.x + i, rect.yMax - 1 - i, rect.xMax - 1 - i, rect.yMax - 1 - i, color);
            DrawLine(rect.x + i, rect.y + i, rect.x + i, rect.yMax - 1 - i, color);
            DrawLine(rect.xMax - 1 - i, rect.y + i, rect.xMax - 1 - i, rect.yMax - 1 - i, color);
        }
    }

    private void DrawLine(int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int error = dx - dy;

        while (true)
        {
            SetPixel(x0, y0, color);

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int doubledError = error * 2;

            if (doubledError > -dy)
            {
                error -= dy;
                x0 += sx;
            }

            if (doubledError < dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private void DrawFilledCircle(Vector2 center, float radius, Color color)
    {
        float safeRadius = Mathf.Max(1f, radius);
        float sqrRadius = safeRadius * safeRadius;
        int minX = Mathf.FloorToInt(center.x - safeRadius);
        int maxX = Mathf.CeilToInt(center.x + safeRadius);
        int minY = Mathf.FloorToInt(center.y - safeRadius);
        int maxY = Mathf.CeilToInt(center.y + safeRadius);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if ((new Vector2(x, y) - center).sqrMagnitude <= sqrRadius)
                {
                    SetPixel(x, y, color);
                }
            }
        }
    }

    private void DrawTinyTextCentered(string text, RectInt rect, int scale, Color color, int yOffset)
    {
        int width = GetTinyTextWidth(text, scale);
        int height = 7 * scale;
        int x = rect.x + Mathf.Max(0, (rect.width - width) / 2);
        int y = rect.y + yOffset + Mathf.Max(0, (rect.height - height) / 2);
        DrawTinyText(text, x, y, scale, color);
    }

    private int GetTinyTextWidth(string text, int scale)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return Mathf.Max(0, text.Length * 6 * scale - scale);
    }

    private void DrawTinyText(string text, int startX, int startY, int scale, Color color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        int x = startX;
        int safeScale = Mathf.Max(1, scale);

        for (int i = 0; i < text.Length; i++)
        {
            DrawTinyChar(char.ToUpperInvariant(text[i]), x, startY, safeScale, color);
            x += 6 * safeScale;
        }
    }

    private void DrawTinyChar(char character, int startX, int startY, int scale, Color color)
    {
        string[] rows = GetTinyCharRows(character);

        for (int y = 0; y < rows.Length; y++)
        {
            string row = rows[y];

            for (int x = 0; x < row.Length; x++)
            {
                if (row[x] != '1')
                {
                    continue;
                }

                DrawRect(new RectInt(startX + x * scale, startY + y * scale, scale, scale), color);
            }
        }
    }

    private string[] GetTinyCharRows(char character)
    {
        switch (character)
        {
            case '0':
                return new[] { "111", "101", "101", "101", "101", "101", "111" };
            case '1':
                return new[] { "010", "110", "010", "010", "010", "010", "111" };
            case '2':
                return new[] { "111", "001", "001", "111", "100", "100", "111" };
            case '3':
                return new[] { "111", "001", "001", "111", "001", "001", "111" };
            case '4':
                return new[] { "101", "101", "101", "111", "001", "001", "001" };
            case '5':
                return new[] { "111", "100", "100", "111", "001", "001", "111" };
            case '6':
                return new[] { "111", "100", "100", "111", "101", "101", "111" };
            case '7':
                return new[] { "111", "001", "001", "010", "010", "010", "010" };
            case '8':
                return new[] { "111", "101", "101", "111", "101", "101", "111" };
            case '9':
                return new[] { "111", "101", "101", "111", "001", "001", "111" };
            case '%':
                return new[] { "101", "001", "010", "010", "010", "100", "101" };
            case 'A':
                return new[] { "010", "101", "101", "111", "101", "101", "101" };
            case 'E':
                return new[] { "111", "100", "100", "111", "100", "100", "111" };
            case 'G':
                return new[] { "111", "100", "100", "101", "101", "101", "111" };
            case 'I':
                return new[] { "111", "010", "010", "010", "010", "010", "111" };
            case 'L':
                return new[] { "100", "100", "100", "100", "100", "100", "111" };
            case 'N':
                return new[] { "101", "111", "111", "111", "111", "111", "101" };
            case 'O':
                return new[] { "111", "101", "101", "101", "101", "101", "111" };
            case 'P':
                return new[] { "111", "101", "101", "111", "100", "100", "100" };
            case 'R':
                return new[] { "111", "101", "101", "111", "110", "101", "101" };
            case 'S':
                return new[] { "111", "100", "100", "111", "001", "001", "111" };
            case 'W':
                return new[] { "101", "101", "101", "111", "111", "111", "101" };
            case ' ':
                return new[] { "000", "000", "000", "000", "000", "000", "000" };
            default:
                return new[] { "111", "001", "010", "010", "000", "010", "010" };
        }
    }

    private void SetPixel(int x, int y, Color color)
    {
        int size = screenTexture.width;

        if (x < 0 || y < 0 || x >= size || y >= size)
        {
            return;
        }

        TransformPixel(ref x, ref y, size);
        pixels[y * size + x] = color;
    }

    private void TransformPixel(ref int x, ref int y, int size)
    {
        int max = size - 1;

        if (flipScreenX)
        {
            x = max - x;
        }

        if (flipScreenY)
        {
            y = max - y;
        }

        int turns = ((screenRotationQuarterTurns % 4) + 4) % 4;

        for (int i = 0; i < turns; i++)
        {
            int rotatedX = max - y;
            int rotatedY = x;
            x = rotatedX;
            y = rotatedY;
        }
    }
}
