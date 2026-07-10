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
        List<TreasureDefinition> iconOnlyTreasures = new List<TreasureDefinition>();

        foreach (string iconPath in iconPaths)
        {
            EnsureSpriteImporter(iconPath);

            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

            if (icon == null)
            {
                continue;
            }

            string rawName = Path.GetFileNameWithoutExtension(iconPath);

            if (IsExcludedTreasureRawName(rawName))
            {
                continue;
            }

            string displayName = ToDisplayName(rawName);
            bool isCraftingIngredient = IsCraftingIngredientRawName(rawName);
            TreasureRarity rarity = isCraftingIngredient ? TreasureRarity.Common : GuessRarity(rawName);
            Vector2Int size = UseAutomaticInventorySizes
                ? GuessInventorySize(rawName, GetOpaqueAspect(iconPath))
                : Vector2Int.one;
            int value = isCraftingIngredient ? GuessCraftingIngredientValue(rawName) : GuessValue(rawName, rarity, size);

            TreasureDefinition treasure = new TreasureDefinition
            {
                treasureName = displayName,
                value = value,
                rarity = rarity,
                icon = icon,
                spawnWeight = isCraftingIngredient ? GuessCraftingIngredientSpawnWeight(rawName) : GuessSpawnWeight(rarity, value),
                width = size.x,
                height = size.y,
                minDigHits = isCraftingIngredient ? 2 : Mathf.Clamp(size.x + size.y, 2, 6),
                maxDigHits = isCraftingIngredient ? 4 : Mathf.Clamp(size.x + size.y + (rarity == TreasureRarity.Epic ? 3 : 2), 3, 9)
            };

            if (IsIconOnlyTreasureRawName(rawName))
            {
                iconOnlyTreasures.Add(treasure);
                continue;
            }

            treasures.Add(treasure);
        }

        database.defaultTreasures = treasures.Where(IsDefaultTreasure).ToArray();
        database.defaultPlusOneUpgradeTreasures = treasures.Where(IsUpgradeOneTreasure).ToArray();
        database.defaultPlusTwoUpgradeTreasures = treasures.Where(IsUpgradeTwoTreasure).ToArray();
        database.defaultPlusThreeUpgradeTreasures = treasures.Where(IsUpgradeThreeTreasure).ToArray();
        database.specialFieldTreasures = treasures.Where(IsSpecialFieldTreasure).ToArray();
        database.specialTreeFieldTreasures = treasures.Where(IsSpecialTreeFieldTreasure).ToArray();
        database.iconOnlyTreasures = iconOnlyTreasures.ToArray();
        database.treasures = treasures.ToArray();
        database.generalTerrainTreasures = database.defaultTreasures;
        database.searchAreaTreasures = database.specialFieldTreasures;
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

    private static bool IsDefaultTreasure(TreasureDefinition treasure)
    {
        return treasure != null && treasure.rarity == TreasureRarity.Common && treasure.value <= 20;
    }

    private static bool IsUpgradeOneTreasure(TreasureDefinition treasure)
    {
        return treasure != null && treasure.value > 20 && treasure.value <= 70 && !IsSpecialFieldTreasure(treasure);
    }

    private static bool IsUpgradeTwoTreasure(TreasureDefinition treasure)
    {
        return treasure != null && treasure.value > 70 && treasure.value <= 140 && !IsSpecialFieldTreasure(treasure);
    }

    private static bool IsUpgradeThreeTreasure(TreasureDefinition treasure)
    {
        return treasure != null && treasure.value > 140 && !IsSpecialFieldTreasure(treasure);
    }

    private static bool IsSpecialFieldTreasure(TreasureDefinition treasure)
    {
        if (treasure == null)
        {
            return false;
        }

        string normalizedName = ToSnakeCase(treasure.treasureName);
        return normalizedName == "medallion"
            || normalizedName == "cross_pendant"
            || normalizedName == "heart_pendant"
            || normalizedName == "metal_map_case_fragment"
            || normalizedName == "small_spyglass_tube"
            || normalizedName == "ornate_document_tube"
            || normalizedName == "time_capsule"
            || normalizedName == "decorative_metal_shell"
            || normalizedName == "old_metal_seal"
            || normalizedName == "wax_seal_with_metal_rim"
            || normalizedName == "jewelry_box";
    }

    private static bool IsSpecialTreeFieldTreasure(TreasureDefinition treasure)
    {
        return treasure != null
            && (treasure.rarity == TreasureRarity.Epic
                || ContainsAny(ToSnakeCase(treasure.treasureName), "amulet", "relic", "idol", "scarab", "runed", "crown", "compass", "token", "pendant"));
    }

    private static bool IsExcludedTreasureRawName(string rawName)
    {
        if (rawName.Contains("key"))
        {
            return true;
        }

        return ContainsAny(
            rawName,
            "fantasy",
            "talisman",
            "rune_amulet",
            "runed_ring",
            "moon_amulet",
            "sun_amulet",
            "star_amulet",
            "scarab",
            "idol",
            "mysterious",
            "bicycle",
            "brake_fragment",
            "metal_sleeve",
            "spring",
            "old_watch_fragment",
            "watch_dial",
            "watch_hands",
            "watch_crown",
            "clock_gear",
            "small_cog",
            "metal_watch_band",
            "pull_tab",
            "aluminum_foil_ball",
            "soda_can_pull_tab",
            "rusty_screw",
            "safety_pin",
            "metal_hair_clip",
            "metal_zipper",
            "metal_washer",
            "nut",
            "sheet_metal_fragment",
            "brass_plaque",
            "numbered_metal_tag",
            "badge_pin",
            "fishing_sinker",
            "fishing_swivel",
            "fishing_bell",
            "metal_fishing_float",
            "beach_umbrella_part",
            "tent_peg",
            "backpack_buckle",
            "leash_carabiner",
            "bag_zipper_piece",
            "bag_metal_logo",
            "sunglasses_part",
            "wallet_metal_plate",
            "jeans_button",
            "pants_rivet",
            "collar_bell",
            "metal_pawn",
            "metal_die",
            "sun_symbol_plate",
            "metal_plate",
            "metal_lighter",
            "small_screwdriver",
            "old_hammer",
            "small_chisel",
            "saw_fragment",
            "metal_file",
            "old_forged_nail",
            "old_hinge",
            "ornate_chest_fitting",
            "small_door_handle",
            "drawer_pull",
            "metal_box_corner",
            "decorative_rosette",
            "old_latch",
            "door_bolt",
            "harness_buckle",
            "horse_bit",
            "old_animal_bell",
            "harness_ring",
            "small_saddle_buckle",
            "decorative_stud",
            "harness_rivet",
            "stable_plaque",
            "chain_fragment",
            "worn_blank_coin",
            "coin_with_hole",
            "crown_token",
            "anchor_token",
            "map_token",
            "cylindrical_metal_container",
            "wing_screw",
            "large_head_screw",
            "metal_ball",
            "small_gear_wheel",
            "old_valve",
            "wing_nut",
            "decorative_button",
            "belt_buckle",
            "metal_shell_casing",
            "dog_tag",
            "old_shoe_buckle",
            "small_padlock",
            "old_scissors",
            "closed_pocket_knife",
            "old_table_knife",
            "small_sheathed_fishing_knife",
            "metal_screw_cap",
            "bottle_opener",
            "cigarette_case",
            "small_flask",
            "old_metal_frame_glasses",
            "crushed_can",
            "metal_mug",
            "old_tin_can",
            "sports_medal",
            "old_commemorative_medal",
            "small_bell",
            "metal_animal_figurine",
            "mini_alarm_clock",
            "old_toy_car",
            "chest_lock",
            "ball_bearing",
            "plain_military_button",
            "lead_seal",
            "merchant_weight",
            "carabiner",
            "metal_clamp",
            "rusty_open_end_wrench",
            "small_adjustable_wrench",
            "gold_ring_with_red_stone",
            "signet_ring",
            "ring_with_green_stone",
            "ring_with_blue_stone",
            "crest_signet_ring"
        );
    }

    private static bool IsIconOnlyTreasureRawName(string rawName)
    {
        string normalizedName = Regex.Replace(rawName, @"^\d+_", "");
        return normalizedName == "pocket_watch"
            || normalizedName == "horseshoe"
            || normalizedName == "compass"
            || normalizedName == "bracelet"
            || normalizedName == "closed_portrait_locket";
    }

    private static bool IsCraftingIngredientRawName(string rawName)
    {
        string normalizedName = Regex.Replace(rawName, @"^\d+_", "");
        return normalizedName == "watch_fragment"
            || normalizedName == "watch_case"
            || normalizedName == "horseshoe_fragment"
            || normalizedName == "metal_rod"
            || normalizedName == "old_cracked_compass"
            || normalizedName == "broken_chain"
            || normalizedName == "chain_piece"
            || normalizedName == "chain_link"
            || normalizedName == "cracked_glass_locket"
            || normalizedName == "metal_photo_frame";
    }

    private static int GuessCraftingIngredientValue(string rawName)
    {
        string normalizedName = Regex.Replace(rawName, @"^\d+_", "");

        if (normalizedName == "watch_case"
            || normalizedName == "old_cracked_compass"
            || normalizedName == "cracked_glass_locket")
        {
            return 14;
        }

        if (normalizedName == "watch_fragment"
            || normalizedName == "horseshoe_fragment"
            || normalizedName == "metal_photo_frame")
        {
            return 10;
        }

        return 8;
    }

    private static int GuessCraftingIngredientSpawnWeight(string rawName)
    {
        string normalizedName = Regex.Replace(rawName, @"^\d+_", "");

        if (normalizedName == "watch_case"
            || normalizedName == "old_cracked_compass"
            || normalizedName == "cracked_glass_locket")
        {
            return 28;
        }

        if (normalizedName == "horseshoe_fragment"
            || normalizedName == "metal_photo_frame")
        {
            return 30;
        }

        return 34;
    }

    private static int ExtractLeadingNumber(string rawName)
    {
        Match match = Regex.Match(rawName, @"^(\d+)_");
        return match.Success && int.TryParse(match.Groups[1].Value, out int number) ? number : 0;
    }

    private static string ToSnakeCase(string displayName)
    {
        return string.IsNullOrEmpty(displayName) ? string.Empty : displayName.ToLowerInvariant().Replace(' ', '_');
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
