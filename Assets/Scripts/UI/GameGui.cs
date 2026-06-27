using UnityEngine;

public static class GameGui
{
    private static GUIStyle panelStyle;
    private static GUIStyle titleStyle;
    private static GUIStyle labelStyle;
    private static GUIStyle smallLabelStyle;
    private static GUIStyle hintStyle;
    private static GUIStyle buttonStyle;
    private static GUIStyle slotStyle;
    private static Texture2D whiteTexture;

    public static Color PanelColor => new Color(0.07f, 0.08f, 0.07f, 0.78f);
    public static Color PanelBorderColor => new Color(1f, 0.84f, 0.42f, 0.55f);
    public static Color TextColor => new Color(0.98f, 0.94f, 0.82f, 1f);
    public static Color MutedTextColor => new Color(0.78f, 0.73f, 0.62f, 1f);
    public static Color AccentColor => new Color(1f, 0.76f, 0.2f, 1f);
    public static Color GoodColor => new Color(0.35f, 0.92f, 0.62f, 1f);
    public static Color DangerColor => new Color(1f, 0.35f, 0.28f, 1f);

    public static GUIStyle PanelStyle
    {
        get
        {
            EnsureStyles();
            return panelStyle;
        }
    }

    public static GUIStyle TitleStyle
    {
        get
        {
            EnsureStyles();
            return titleStyle;
        }
    }

    public static GUIStyle LabelStyle
    {
        get
        {
            EnsureStyles();
            return labelStyle;
        }
    }

    public static GUIStyle SmallLabelStyle
    {
        get
        {
            EnsureStyles();
            return smallLabelStyle;
        }
    }

    public static GUIStyle HintStyle
    {
        get
        {
            EnsureStyles();
            return hintStyle;
        }
    }

    public static GUIStyle ButtonStyle
    {
        get
        {
            EnsureStyles();
            return buttonStyle;
        }
    }

    public static GUIStyle SlotStyle
    {
        get
        {
            EnsureStyles();
            return slotStyle;
        }
    }

    public static void DrawPanel(Rect rect, string title)
    {
        EnsureStyles();
        DrawRect(rect, PanelColor);
        DrawBorder(rect, PanelBorderColor, 2f);

        if (!string.IsNullOrEmpty(title))
        {
            GUI.Label(new Rect(rect.x + 14f, rect.y + 10f, rect.width - 28f, 24f), title, titleStyle);
        }
    }

    public static void DrawProgressBar(Rect rect, float value, Color fillColor, string label)
    {
        EnsureStyles();
        float clampedValue = Mathf.Clamp01(value);

        DrawRect(rect, new Color(0f, 0f, 0f, 0.42f));
        DrawRect(new Rect(rect.x, rect.y, rect.width * clampedValue, rect.height), fillColor);
        DrawBorder(rect, new Color(1f, 1f, 1f, 0.18f), 1f);

        if (!string.IsNullOrEmpty(label))
        {
            GUI.Label(rect, label, hintStyle);
        }
    }

    public static void DrawToast(Rect rect, string message)
    {
        DrawPanel(rect, "");
        GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, rect.height - 16f), message, HintStyle);
    }

    public static bool Button(Rect rect, string label)
    {
        EnsureStyles();
        return GUI.Button(rect, label, buttonStyle);
    }

    public static void DrawRect(Rect rect, Color color)
    {
        EnsureTexture();
        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = oldColor;
    }

    private static void DrawBorder(Rect rect, Color color, float thickness)
    {
        DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private static void EnsureStyles()
    {
        EnsureTexture();

        if (panelStyle != null)
        {
            return;
        }

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(14, 14, 12, 12)
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = TextColor }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = TextColor }
        };

        smallLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = MutedTextColor },
            wordWrap = true
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            normal = { textColor = TextColor },
            wordWrap = true
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = TextColor },
            hover = { textColor = AccentColor },
            active = { textColor = Color.white }
        };

        slotStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 11,
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(6, 6, 5, 5),
            normal = { textColor = TextColor }
        };
    }

    private static void EnsureTexture()
    {
        if (whiteTexture != null)
        {
            return;
        }

        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }
}
