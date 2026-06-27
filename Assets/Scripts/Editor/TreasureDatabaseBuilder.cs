using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class TreasureDatabaseBuilder
{
    private const string IconFolder = "Assets/Art/TreasureIcons/individual";
    private const string DatabasePath = "Assets/Data/DefaultTreasureDatabase.asset";
    private const bool UseAutomaticInventorySizes = false;

    [MenuItem("Tools/Metal Detector/Rebuild Treasure Database From Icons")]
    public static void RebuildDatabaseFromIcons()
    {
        string[] iconGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconFolder });
        List<string> iconPaths = iconGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => Regex.IsMatch(Path.GetFileNameWithoutExtension(path), @"^\d+_"))
            .OrderBy(path => path)
            .ToList();

        if (iconPaths.Count == 0)
        {
            iconPaths = iconGuids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToList();
        }

        TreasureDatabase database = AssetDatabase.LoadAssetAtPath<TreasureDatabase>(DatabasePath);

        if (database == null)
        {
            database = ScriptableObject.CreateInstance<TreasureDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        List<TreasureDefinition> treasures = new List<TreasureDefinition>();

        foreach (string iconPath in iconPaths)
        {
            EnsureSpriteImporter(iconPath);

            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            string rawName = Path.GetFileNameWithoutExtension(iconPath);
            string displayName = ToDisplayName(rawName);
            TreasureRarity rarity = GuessRarity(rawName);
            Vector2Int size = UseAutomaticInventorySizes
                ? GuessInventorySize(rawName, GetOpaqueAspect(iconPath))
                : Vector2Int.one;
            int value = GuessValue(rawName, rarity, size);

            treasures.Add(new TreasureDefinition
            {
                treasureName = displayName,
                value = value,
                rarity = rarity,
                icon = icon,
                spawnWeight = GuessSpawnWeight(rarity, value),
                width = size.x,
                height = size.y,
                minDigHits = Mathf.Clamp(size.x + size.y, 2, 6),
                maxDigHits = Mathf.Clamp(size.x + size.y + (rarity == TreasureRarity.Epic ? 3 : 2), 3, 9)
            });
        }

        database.treasures = treasures.ToArray();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Rebuilt treasure database with " + treasures.Count + " icon-backed treasures.");
    }

    private static void EnsureSpriteImporter(string iconPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        bool changed = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || importer.mipmapEnabled
            || !importer.alphaIsTransparency;

        if (!changed)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = 100f;
        importer.SaveAndReimport();
    }

    private static string ToDisplayName(string rawName)
    {
        string withoutNumber = Regex.Replace(rawName, @"^\d+_", "");
        string[] words = withoutNumber.Split('_');

        for (int i = 0; i < words.Length; i++)
        {
            if (string.IsNullOrEmpty(words[i]))
            {
                continue;
            }

            words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
        }

        return string.Join(" ", words);
    }

    private static TreasureRarity GuessRarity(string rawName)
    {
        if (ContainsAny(rawName, "gold", "jeweled", "jewelry", "ancient", "relic", "crown", "mysterious", "idol", "scarab", "runed", "doubloon", "thaler", "fantasy"))
        {
            return TreasureRarity.Epic;
        }

        if (ContainsAny(rawName, "silver", "ring", "coin", "watch", "compass", "medal", "medallion", "amulet", "pendant", "brooch", "locket", "token", "seal", "casket"))
        {
            return TreasureRarity.Rare;
        }

        return TreasureRarity.Common;
    }

    private static Vector2Int GuessInventorySize(string rawName, float iconAspect)
    {
        if (ContainsAny(rawName, "document_tube", "spyglass_tube", "map_case", "bicycle_pump"))
        {
            return iconAspect > 1.15f ? new Vector2Int(3, 1) : new Vector2Int(1, 3);
        }

        if (ContainsAny(rawName, "knife", "dagger", "screwdriver", "chisel", "file", "rod", "fork", "spoon", "table_knife", "tent_peg", "hinge", "bolt"))
        {
            return iconAspect > 1.15f ? new Vector2Int(2, 1) : new Vector2Int(1, 2);
        }

        if (ContainsAny(rawName, "chain", "glasses", "saw_fragment", "watch_band", "horse_bit", "bicycle_pedal"))
        {
            return new Vector2Int(2, 1);
        }

        if (ContainsAny(rawName, "crushed_can", "tin_can", "metal_mug", "jewelry_box", "casket", "alarm_clock", "bicycle_lock", "compass", "watch", "relic", "animal_figurine"))
        {
            return new Vector2Int(2, 2);
        }

        if (ContainsAny(rawName, "plate", "plaque", "buckle", "lock", "flask", "lighter", "case", "frame", "valve", "bell", "horseshoe"))
        {
            return iconAspect < 0.8f ? new Vector2Int(1, 2) : new Vector2Int(2, 1);
        }

        if (iconAspect > 2.2f)
        {
            return new Vector2Int(2, 1);
        }

        if (iconAspect < 0.45f)
        {
            return new Vector2Int(1, 2);
        }

        return new Vector2Int(1, 1);
    }

    private static float GetOpaqueAspect(string iconPath)
    {
        byte[] bytes = File.ReadAllBytes(iconPath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!texture.LoadImage(bytes))
        {
            Object.DestroyImmediate(texture);
            return 1f;
        }

        Color32[] pixels = texture.GetPixels32();
        int minX = texture.width;
        int minY = texture.height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                if (pixels[y * texture.width + x].a <= 16)
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        Object.DestroyImmediate(texture);

        if (maxX < minX || maxY < minY)
        {
            return 1f;
        }

        return (maxX - minX + 1f) / (maxY - minY + 1f);
    }

    private static int GuessValue(string rawName, TreasureRarity rarity, Vector2Int size)
    {
        int baseValue = rarity switch
        {
            TreasureRarity.Epic => 120,
            TreasureRarity.Rare => 35,
            _ => 4
        };

        int areaBonus = Mathf.Max(0, size.x * size.y - 1) * 8;

        if (ContainsAny(rawName, "trash", "cap", "tab", "foil", "nail", "screw", "wire", "washer", "nut", "rivet"))
        {
            baseValue = Mathf.Min(baseValue, 8);
        }

        if (ContainsAny(rawName, "gold", "crown", "jewel", "doubloon", "relic", "mysterious"))
        {
            baseValue += 80;
        }

        if (ContainsAny(rawName, "silver", "coin", "ring", "watch", "medal", "amulet", "pendant"))
        {
            baseValue += 18;
        }

        int number = ExtractLeadingNumber(rawName);
        return Mathf.Clamp(baseValue + areaBonus + number / 8, 1, 400);
    }

    private static int GuessSpawnWeight(TreasureRarity rarity, int value)
    {
        int weight = rarity switch
        {
            TreasureRarity.Epic => 3,
            TreasureRarity.Rare => 8,
            _ => 22
        };

        return Mathf.Clamp(weight - value / 80, 1, 40);
    }

    private static int ExtractLeadingNumber(string rawName)
    {
        Match match = Regex.Match(rawName, @"^(\d+)_");
        return match.Success && int.TryParse(match.Groups[1].Value, out int number) ? number : 0;
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        foreach (string needle in needles)
        {
            if (value.Contains(needle))
            {
                return true;
            }
        }

        return false;
    }
}
