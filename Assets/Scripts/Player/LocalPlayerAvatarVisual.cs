using UnityEngine;

[DefaultExecutionOrder(100)]
[ExecuteAlways]
public class LocalPlayerAvatarVisual : MonoBehaviour
{
    private const string VisualRootName = "Local Character Visual";
    private const string RightHandAnchorName = "Right Hand Anchor";

    public bool showOnlyLocalShadow = true;
    public bool useUmaEditorPreview = false;
    public bool useSimpleEditorFallback = true;

    private int appliedAvatarToken = -1;
    private Transform visualRoot;
    private Transform rightHandAnchor;
    private Renderer originalRenderer;
    private CharacterController characterController;
    private GameObject activeAvatarObject;

    public Transform RightHandAnchor => rightHandAnchor;

    private void OnEnable()
    {
        Initialize();

        if (!Application.isPlaying)
        {
            ApplyCharacter(PlayerCharacterSelection.SelectedAvatarToken);
        }
    }

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        originalRenderer = GetComponent<Renderer>();
        characterController = GetComponent<CharacterController>();
        SetOriginalRendererVisible(false);
    }

    private void Update()
    {
        int avatarToken = PlayerCharacterSelection.SelectedAvatarToken;

        if (avatarToken == appliedAvatarToken)
        {
            return;
        }

        ApplyCharacter(avatarToken);
    }

    private void LateUpdate()
    {
        AnchorVisualRoot();
        UpdateRightHandAnchor();
    }

    private void ApplyCharacter(int avatarToken)
    {
        appliedAvatarToken = avatarToken;
        EnsureVisualRoot();
        ClearVisualRoot();
        activeAvatarObject = null;

        PlayerCharacterSelection.CharacterProfile profile = PlayerCharacterSelection.GetProfile(appliedAvatarToken);

        if (!Application.isPlaying && !useUmaEditorPreview)
        {
            BuildSimpleEditorFallback(profile);
            return;
        }

        bool hasReplacement = UmaCharacterFactory.TryCreateCharacter(visualRoot, profile, out GameObject avatarObject) && HasRenderableVisual(visualRoot);

        if (hasReplacement)
        {
            activeAvatarObject = avatarObject;
            AnchorVisualRoot();
            AnchorAvatarObject(avatarObject);
        }

        if (!hasReplacement)
        {
            ClearVisualRoot();
            if (Application.isPlaying || useSimpleEditorFallback)
            {
                BuildFallbackCharacter(profile);
            }

            hasReplacement = HasRenderableVisual(visualRoot);
        }

        EnsureRightHandAnchor();
        UpdateRightHandAnchor();
        ApplyLocalVisibilityMode();
        SetOriginalRendererVisible(!hasReplacement);
    }

    private void BuildSimpleEditorFallback(PlayerCharacterSelection.CharacterProfile profile)
    {
        ClearVisualRoot();
        BuildFallbackCharacter(profile);
        activeAvatarObject = null;
        EnsureRightHandAnchor();
        UpdateRightHandAnchor();
        ApplyLocalVisibilityMode();
        SetOriginalRendererVisible(false);
    }

    private void EnsureVisualRoot()
    {
        if (visualRoot != null)
        {
            return;
        }

        Transform existingRoot = transform.Find(VisualRootName);

        if (existingRoot != null)
        {
            visualRoot = existingRoot;
            return;
        }

        GameObject rootObject = new GameObject(VisualRootName);
        rootObject.transform.SetParent(transform, false);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        visualRoot = rootObject.transform;
    }

    private void AnchorVisualRoot()
    {
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localPosition = GetFeetLocalOffset();
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;
    }

    private void EnsureRightHandAnchor()
    {
        if (visualRoot == null)
        {
            return;
        }

        if (rightHandAnchor != null)
        {
            return;
        }

        Transform existingAnchor = visualRoot.Find(RightHandAnchorName);

        if (existingAnchor != null)
        {
            rightHandAnchor = existingAnchor;
            return;
        }

        GameObject anchorObject = new GameObject(RightHandAnchorName);
        anchorObject.transform.SetParent(visualRoot, false);
        anchorObject.transform.localPosition = GetFallbackRightHandLocalPosition();
        anchorObject.transform.localRotation = Quaternion.identity;
        anchorObject.transform.localScale = Vector3.one;
        rightHandAnchor = anchorObject.transform;
    }

    private void UpdateRightHandAnchor()
    {
        EnsureRightHandAnchor();

        if (rightHandAnchor == null)
        {
            return;
        }

        Transform humanoidRightHand = FindHumanoidRightHand();

        if (humanoidRightHand != null)
        {
            rightHandAnchor.position = humanoidRightHand.position;
            rightHandAnchor.rotation = humanoidRightHand.rotation;
            return;
        }

        rightHandAnchor.localPosition = GetFallbackRightHandLocalPosition();
        rightHandAnchor.localRotation = Quaternion.Euler(0f, 0f, 8f);
    }

    private Transform FindHumanoidRightHand()
    {
        if (activeAvatarObject == null)
        {
            return null;
        }

        Animator[] animators = activeAvatarObject.GetComponentsInChildren<Animator>(true);

        foreach (Animator animator in animators)
        {
            if (animator == null || !animator.isHuman)
            {
                continue;
            }

            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

            if (rightHand != null)
            {
                return rightHand;
            }
        }

        return null;
    }

    private static Vector3 GetFallbackRightHandLocalPosition()
    {
        return new Vector3(0.46f, 0.72f, 0.08f);
    }

    private Vector3 GetFeetLocalOffset()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (characterController == null)
        {
            return Vector3.zero;
        }

        float feetOffset = characterController.center.y - (characterController.height * 0.5f);
        return new Vector3(0f, feetOffset, 0f);
    }

    private static void AnchorAvatarObject(GameObject avatarObject)
    {
        if (avatarObject == null)
        {
            return;
        }

        avatarObject.transform.localPosition = Vector3.zero;
        avatarObject.transform.localRotation = Quaternion.identity;
        avatarObject.transform.localScale = Vector3.one;
    }

    private void ClearVisualRoot()
    {
        if (visualRoot == null)
        {
            return;
        }

        for (int i = visualRoot.childCount - 1; i >= 0; i--)
        {
            DestroyVisualObject(visualRoot.GetChild(i).gameObject);
        }

        rightHandAnchor = null;
    }

    private static void DestroyVisualObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
            return;
        }

        DestroyImmediate(target);
    }

    private void BuildFallbackCharacter(PlayerCharacterSelection.CharacterProfile profile)
    {
        Material bodyMaterial = CreateMaterial(profile.bodyColor);
        Material accentMaterial = CreateMaterial(profile.accentColor);
        Material skinMaterial = CreateMaterial(new Color(0.86f, 0.65f, 0.48f, 1f));

        CreatePart("Torso", PrimitiveType.Cube, new Vector3(0f, 1.0f, 0f), Quaternion.identity, new Vector3(0.42f, 0.74f, 0.24f), bodyMaterial);
        CreatePart("Head", PrimitiveType.Sphere, new Vector3(0f, 1.64f, 0f), Quaternion.identity, new Vector3(0.30f, 0.30f, 0.30f), skinMaterial);
        CreatePart("Hat Brim", PrimitiveType.Cylinder, new Vector3(0f, 1.82f, 0f), Quaternion.identity, new Vector3(0.34f, 0.035f, 0.34f), accentMaterial);
        CreatePart("Hat Crown", PrimitiveType.Cylinder, new Vector3(0f, 1.9f, 0f), Quaternion.identity, new Vector3(0.22f, 0.10f, 0.22f), accentMaterial);
        CreatePart("Left Arm", PrimitiveType.Cube, new Vector3(-0.34f, 0.96f, 0.02f), Quaternion.Euler(0f, 0f, -8f), new Vector3(0.14f, 0.62f, 0.14f), bodyMaterial);
        CreatePart("Right Arm", PrimitiveType.Cube, new Vector3(0.34f, 0.96f, 0.02f), Quaternion.Euler(0f, 0f, 8f), new Vector3(0.14f, 0.62f, 0.14f), bodyMaterial);
        CreatePart("Left Leg", PrimitiveType.Cube, new Vector3(-0.12f, 0.34f, 0f), Quaternion.identity, new Vector3(0.16f, 0.68f, 0.16f), bodyMaterial);
        CreatePart("Right Leg", PrimitiveType.Cube, new Vector3(0.12f, 0.34f, 0f), Quaternion.identity, new Vector3(0.16f, 0.68f, 0.16f), bodyMaterial);
    }

    private void CreatePart(string partName, PrimitiveType primitiveType, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = partName;
        part.transform.SetParent(visualRoot, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = localScale;

        Collider partCollider = part.GetComponent<Collider>();

        if (partCollider != null)
        {
            partCollider.enabled = false;
        }

        Renderer renderer = part.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material = material;
        }
    }

    private void ApplyLocalVisibilityMode()
    {
        if (visualRoot == null)
        {
            return;
        }

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = true;
            renderer.shadowCastingMode = showOnlyLocalShadow && Application.isPlaying
                ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly
                : UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = !showOnlyLocalShadow || !Application.isPlaying;
        }
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader)
        {
            color = color
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        return material;
    }

    private void SetOriginalRendererVisible(bool visible)
    {
        if (originalRenderer != null)
        {
            originalRenderer.enabled = visible;
        }
    }

    private static bool HasRenderableVisual(Transform root)
    {
        if (root == null)
        {
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

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

    private void OnDestroy()
    {
        SetOriginalRendererVisible(true);
    }
}
