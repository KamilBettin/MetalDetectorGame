using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class GameIconConfigurator
{
    private const string IconAssetPath = "Assets/Art/Branding/MetalDetectorGameIcon.png";

    [InitializeOnLoadMethod]
    private static void ScheduleIconSetup()
    {
        EditorApplication.delayCall += ApplyIconIfNeeded;
    }

    [MenuItem("Tools/Metal Detector/Apply Game Icon")]
    public static void ApplyIconFromMenu()
    {
        ApplyIconIfNeeded();
        Debug.Log("Metal Detector game icon is assigned to the Standalone player.");
    }

    private static void ApplyIconIfNeeded()
    {
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconAssetPath);

        if (icon == null)
        {
            AssetDatabase.ImportAsset(IconAssetPath, ImportAssetOptions.ForceUpdate);
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconAssetPath);
        }

        if (icon == null)
        {
            Debug.LogWarning("Could not load the Metal Detector game icon at " + IconAssetPath + ".");
            return;
        }

        NamedBuildTarget target = NamedBuildTarget.Standalone;
        int[] requiredSizes = PlayerSettings.GetIconSizes(target, IconKind.Application);
        int iconCount = Mathf.Max(1, requiredSizes != null ? requiredSizes.Length : 0);
        Texture2D[] assignedIcons = PlayerSettings.GetIcons(target, IconKind.Application);

        if (assignedIcons != null && assignedIcons.Length == iconCount)
        {
            bool alreadyAssigned = true;

            for (int i = 0; i < assignedIcons.Length; i++)
            {
                if (assignedIcons[i] != icon)
                {
                    alreadyAssigned = false;
                    break;
                }
            }

            if (alreadyAssigned)
            {
                return;
            }
        }

        Texture2D[] icons = new Texture2D[iconCount];

        for (int i = 0; i < icons.Length; i++)
        {
            icons[i] = icon;
        }

        PlayerSettings.SetIcons(target, icons, IconKind.Application);
        AssetDatabase.SaveAssets();
    }
}
