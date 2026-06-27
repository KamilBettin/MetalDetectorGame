using System.Collections.Generic;
using UMA;
using UMA.CharacterSystem;
using UMA.PoseTools;
using UnityEngine;

public static class UmaCharacterFactory
{
    private const string UmaRuntimeResourcePath = "UMA/UMA_GLIB";
    private const string UmaRandomAvatarPrefabResourcePath = "UMA/UMARandomGeneratedCharacter";
    private const string BaseMaleResourcePath = "UMA/Base/UMA_Human_Male";
    private const string BaseFemaleResourcePath = "UMA/Base/UMA_Human_Female";
    private static GameObject umaRuntime;

    public static bool TryCreateCharacter(Transform parent, PlayerCharacterSelection.CharacterProfile profile, out GameObject characterObject)
    {
        characterObject = null;

        if (parent == null || !EnsureRuntime())
        {
            return false;
        }

        if (!TryCreateRandomCharacter(parent, profile, out characterObject))
        {
            return TryCreateBaseCharacter(parent, profile, out characterObject);
        }

        return true;
    }

    private static bool TryCreateRandomCharacter(Transform parent, PlayerCharacterSelection.CharacterProfile profile, out GameObject characterObject)
    {
        characterObject = null;
        GameObject randomGeneratorPrefab = Resources.Load<GameObject>(UmaRandomAvatarPrefabResourcePath);

        if (randomGeneratorPrefab == null)
        {
            return false;
        }

        UMARandomAvatar randomGenerator = randomGeneratorPrefab.GetComponent<UMARandomAvatar>();

        if (randomGenerator == null || randomGenerator.prefab == null)
        {
            return false;
        }

        RandomAvatar randomProfile = FindRandomProfileForRace(randomGenerator, profile.umaRace);

        if (randomProfile == null)
        {
            return false;
        }

        characterObject = Object.Instantiate(randomGenerator.prefab, parent);
        characterObject.name = profile.displayName;
        characterObject.transform.SetParent(parent, false);
        characterObject.transform.localPosition = Vector3.zero;
        characterObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        characterObject.transform.localScale = Vector3.one;

        DynamicCharacterAvatar avatar = characterObject.GetComponent<DynamicCharacterAvatar>();

        if (avatar == null)
        {
            DestroyCharacterObject(characterObject);
            characterObject = null;
            return false;
        }

#if UNITY_EDITOR
        avatar.showPlaceholder = false;
#endif
        avatar.BundleCheck = false;
        avatar.BuildCharacterEnabled = false;
        ConfigureExpressionPlayers(characterObject, false);

        Random.State previousRandomState = Random.state;

        try
        {
            Random.InitState(profile.randomSeed);
            ApplyRandomProfile(randomGenerator, avatar, randomProfile);
        }
        finally
        {
            Random.state = previousRandomState;
        }

        avatar.BuildCharacterEnabled = true;
        avatar.BuildCharacter(false, true);
        avatar.GenerateNow();

        if (!HasRenderableVisual(characterObject))
        {
            DestroyCharacterObject(characterObject);
            characterObject = null;
            return false;
        }

        StabilizeCharacterObject(characterObject);
        ConfigureAnimationDriver(characterObject);
        return true;
    }

    private static RandomAvatar FindRandomProfileForRace(UMARandomAvatar randomAvatar, string raceName)
    {
        if (randomAvatar.Randomizers == null)
        {
            return null;
        }

        for (int i = 0; i < randomAvatar.Randomizers.Count; i++)
        {
            UMARandomizer randomizer = randomAvatar.Randomizers[i];

            if (randomizer == null || randomizer.RandomAvatars == null)
            {
                continue;
            }

            for (int j = 0; j < randomizer.RandomAvatars.Count; j++)
            {
                RandomAvatar randomProfile = randomizer.RandomAvatars[j];

                if (randomProfile != null && randomProfile.RaceName == raceName)
                {
                    return randomProfile;
                }
            }
        }

        return null;
    }

    private static void ApplyRandomProfile(UMARandomAvatar randomAvatar, DynamicCharacterAvatar avatar, RandomAvatar randomProfile)
    {
        avatar.WardrobeRecipes.Clear();
        avatar.ChangeRaceData(randomProfile.RaceName);
        avatar.predefinedDNA = randomProfile.GetRandomDNA();

        ApplyRandomColors(avatar, randomProfile.SharedColors);

        Dictionary<string, List<RandomWardrobeSlot>> randomSlots = randomProfile.GetRandomSlots();

        foreach (KeyValuePair<string, List<RandomWardrobeSlot>> slot in randomSlots)
        {
            RandomWardrobeSlot randomWardrobe = randomAvatar.GetRandomWardrobe(slot.Value);

            if (randomWardrobe == null || randomWardrobe.WardrobeSlot == null)
            {
                continue;
            }

            avatar.SetSlot(randomWardrobe.WardrobeSlot);
            ApplyRandomColors(avatar, randomWardrobe.Colors);
        }
    }

    private static void ApplyRandomColors(DynamicCharacterAvatar avatar, List<RandomColors> colors)
    {
        if (colors == null)
        {
            return;
        }

        for (int i = 0; i < colors.Count; i++)
        {
            RandomColors randomColor = colors[i];

            if (randomColor == null || randomColor.ColorTable == null || randomColor.ColorTable.colors == null || randomColor.ColorTable.colors.Length == 0)
            {
                continue;
            }

            int colorIndex = Random.Range(0, randomColor.ColorTable.colors.Length);
            avatar.SetColor(randomColor.ColorName, randomColor.ColorTable.colors[colorIndex], false);
        }
    }

    private static bool TryCreateBaseCharacter(Transform parent, PlayerCharacterSelection.CharacterProfile profile, out GameObject characterObject)
    {
        characterObject = null;
        string resourcePath = profile.gender == PlayerCharacterSelection.CharacterGender.Female
            ? BaseFemaleResourcePath
            : BaseMaleResourcePath;
        GameObject basePrefab = Resources.Load<GameObject>(resourcePath);

        if (basePrefab == null)
        {
            return false;
        }

        characterObject = Object.Instantiate(basePrefab, parent);
        characterObject.name = profile.displayName + " Base";
        characterObject.transform.SetParent(parent, false);
        characterObject.transform.localPosition = Vector3.zero;
        characterObject.transform.localRotation = Quaternion.identity;
        characterObject.transform.localScale = Vector3.one;
        StabilizeCharacterObject(characterObject);
        ConfigureAnimationDriver(characterObject);
        return HasRenderableVisual(characterObject);
    }

    private static bool EnsureRuntime()
    {
        if (UMAContextBase.Instance != null)
        {
            return true;
        }

        if (umaRuntime == null)
        {
            GameObject runtimePrefab = Resources.Load<GameObject>(UmaRuntimeResourcePath);

            if (runtimePrefab == null)
            {
                Debug.LogWarning("UMA runtime prefab was not found in Resources/UMA.");
                return false;
            }

            umaRuntime = Object.Instantiate(runtimePrefab);
            umaRuntime.name = "UMA Runtime";
            Object.DontDestroyOnLoad(umaRuntime);
        }

        return UMAContextBase.Instance != null;
    }

    private static void DestroyCharacterObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(target);
            return;
        }

        Object.DestroyImmediate(target);
    }

    private static void StabilizeCharacterObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        DisableColliders(target);
        DisableCharacterControllers(target);
        FreezeRigidbodies(target);
        DisableRootMotion(target);
        ConfigureExpressionPlayers(target, true);
    }

    private static void ConfigureAnimationDriver(GameObject characterObject)
    {
        if (characterObject == null)
        {
            return;
        }

        UmaAvatarAnimationDriver animationDriver = characterObject.GetComponent<UmaAvatarAnimationDriver>();

        if (animationDriver == null)
        {
            animationDriver = characterObject.AddComponent<UmaAvatarAnimationDriver>();
        }

        Transform parent = characterObject.transform.parent;
        animationDriver.motionSource = parent != null && parent.parent != null
            ? parent.parent
            : parent != null
                ? parent
                : characterObject.transform;
    }

    private static void DisableColliders(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);

        foreach (Collider targetCollider in colliders)
        {
            if (targetCollider != null)
            {
                targetCollider.enabled = false;
            }
        }
    }

    private static void DisableCharacterControllers(GameObject target)
    {
        CharacterController[] controllers = target.GetComponentsInChildren<CharacterController>(true);

        foreach (CharacterController controller in controllers)
        {
            if (controller != null)
            {
                controller.enabled = false;
            }
        }
    }

    private static void FreezeRigidbodies(GameObject target)
    {
        Rigidbody[] rigidbodies = target.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody rigidbody in rigidbodies)
        {
            if (rigidbody == null)
            {
                continue;
            }

            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private static void DisableRootMotion(GameObject target)
    {
        Animator[] animators = target.GetComponentsInChildren<Animator>(true);

        foreach (Animator animator in animators)
        {
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }
        }
    }

    private static void ConfigureExpressionPlayers(GameObject target, bool disableComponent)
    {
        if (target == null)
        {
            return;
        }

        UMAExpressionPlayer[] expressionPlayers = target.GetComponentsInChildren<UMAExpressionPlayer>(true);

        foreach (UMAExpressionPlayer expressionPlayer in expressionPlayers)
        {
            if (expressionPlayer == null)
            {
                continue;
            }

            expressionPlayer.overrideMecanimJaw = false;
            expressionPlayer.overrideMecanimNeck = false;
            expressionPlayer.overrideMecanimHead = false;
            expressionPlayer.logResetErrors = false;

            if (disableComponent)
            {
                expressionPlayer.enabled = false;
            }
        }
    }

    private static bool HasRenderableVisual(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            SkinnedMeshRenderer skinnedMeshRenderer = renderer as SkinnedMeshRenderer;

            if (skinnedMeshRenderer != null)
            {
                if (skinnedMeshRenderer.sharedMesh != null)
                {
                    return true;
                }

                continue;
            }

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();

            if (meshFilter == null || meshFilter.sharedMesh != null)
            {
                return true;
            }
        }

        return false;
    }
}
