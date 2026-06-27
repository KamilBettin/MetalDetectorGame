using System;
using UnityEngine;

public static class PlayerCharacterSelection
{
    private const string GenderPrefKey = "MetalDetector.SelectedCharacterGender";
    public const int StyleCount = 1;

    private static readonly int SessionSeed = CreateSessionSeed();

    public enum CharacterGender
    {
        Male = 0,
        Female = 1
    }

    public struct CharacterProfile
    {
        public string displayName;
        public string styleName;
        public CharacterGender gender;
        public string resourcePath;
        public string umaRace;
        public string[] umaWardrobeSlots;
        public string[] umaWardrobeRecipes;
        public Color bodyColor;
        public Color accentColor;
        public int randomSeed;

        public CharacterProfile(CharacterGender gender, int randomSeed)
        {
            this.gender = gender;
            this.randomSeed = randomSeed;
            displayName = gender == CharacterGender.Female ? "Female Hunter" : "Male Hunter";
            styleName = "Random";
            resourcePath = string.Empty;
            umaRace = gender == CharacterGender.Female ? "HumanFemale" : "HumanMale";
            umaWardrobeSlots = Array.Empty<string>();
            umaWardrobeRecipes = Array.Empty<string>();
            bodyColor = gender == CharacterGender.Female
                ? new Color(0.58f, 0.30f, 0.48f, 1f)
                : new Color(0.20f, 0.42f, 0.56f, 1f);
            accentColor = new Color(0.86f, 0.66f, 0.28f, 1f);
        }
    }

    public static readonly CharacterProfile[] Profiles =
    {
        new CharacterProfile(CharacterGender.Male, 1),
        new CharacterProfile(CharacterGender.Female, 1)
    };

    public static CharacterGender SelectedGender => PlayerPrefs.GetInt(GenderPrefKey, 0) == 1
        ? CharacterGender.Female
        : CharacterGender.Male;

    public static int SelectedIndex => SelectedGender == CharacterGender.Female ? 1 : 0;
    public static int SelectedStyleIndex => 0;
    public static int SelectedAvatarToken => EncodeAvatarToken(SelectedGender, SessionSeed);
    public static CharacterProfile SelectedProfile => GetProfile(SelectedAvatarToken);

    public static CharacterProfile GetProfile(int avatarToken)
    {
        if (avatarToken == 0 || avatarToken == 1)
        {
            CharacterGender gender = avatarToken == 1 ? CharacterGender.Female : CharacterGender.Male;
            return new CharacterProfile(gender, SessionSeed);
        }

        CharacterGender decodedGender = (avatarToken & 1) == 1 ? CharacterGender.Female : CharacterGender.Male;
        int decodedSeed = Mathf.Max(1, avatarToken >> 1);
        return new CharacterProfile(decodedGender, decodedSeed);
    }

    public static CharacterProfile GetProfile(int styleIndex, CharacterGender gender)
    {
        return new CharacterProfile(gender, SessionSeed);
    }

    public static string GetStyleName(int styleIndex)
    {
        return "Random";
    }

    public static void SetSelectedIndex(int index)
    {
        SetSelectedGender((index & 1) == 1 ? CharacterGender.Female : CharacterGender.Male);
    }

    public static void SetSelected(int styleIndex, CharacterGender gender)
    {
        SetSelectedGender(gender);
    }

    public static void SetSelectedStyleIndex(int styleIndex)
    {
    }

    public static void SetSelectedGender(CharacterGender gender)
    {
        PlayerPrefs.SetInt(GenderPrefKey, gender == CharacterGender.Female ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static int ToProfileIndex(int styleIndex, CharacterGender gender)
    {
        return gender == CharacterGender.Female ? 1 : 0;
    }

    public static string GetGenderLabel(CharacterGender gender)
    {
        return gender == CharacterGender.Female ? "Female" : "Male";
    }

    private static int EncodeAvatarToken(CharacterGender gender, int seed)
    {
        int safeSeed = Mathf.Clamp(seed & 0x3fffffff, 1, 0x3fffffff);
        return (safeSeed << 1) | (gender == CharacterGender.Female ? 1 : 0);
    }

    private static int CreateSessionSeed()
    {
        unchecked
        {
            int seed = Environment.TickCount ^ Guid.NewGuid().GetHashCode();
            seed &= 0x3fffffff;
            return seed == 0 ? 1 : seed;
        }
    }
}
