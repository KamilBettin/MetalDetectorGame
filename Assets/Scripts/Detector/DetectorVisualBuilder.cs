using UnityEngine;

[DefaultExecutionOrder(150)]
[RequireComponent(typeof(MetalDetector))]
public class DetectorVisualBuilder : MonoBehaviour
{
    private const string ShadowVisualName = "MetalDetector_Shadow";
    private const float ScreenRegistrationRefreshInterval = 0.5f;

    public Transform detectorHead;
    public string modelResourcePath = "Models/MetalDetector/Detector_Player";
    public Vector3 rootOffset = Vector3.zero;
    public Vector3 modelLocalPosition = new Vector3(0.3169f, -1.497f, 0.3378f);
    public Vector3 modelLocalEulerAngles = new Vector3(0f, -64.53f, 0f);
    public Vector3 modelLocalScale = new Vector3(0.15f, 0.15f, 0.15f);
    public string scannerAnchorName = "Cube.009";
    public bool alignScannerAnchorToDetectorHead = false;
    public string gripAnchorName = "Cube.012";
    public bool alignGripAnchorToHand = false;
    public Vector3 gripTargetLocalPosition = new Vector3(0f, -2.24f, 0f);
    public bool stickGripToPlayerRightHand = false;
    public bool matchRightHandRotation = false;
    public bool fitModelToView = false;
    public float fittedModelSize = 1.05f;
    public Vector3 fittedBoundsCenter = new Vector3(0f, -0.18f, 0.48f);
    public bool hideLegacyVisuals = true;
    public bool showOnlyLocalShadow = false;
    public bool preserveExistingWorldDetectorTransform = true;
    public bool showFirstPersonDetector = true;
    public string firstPersonModelResourcePath = "Models/MetalDetector/detector (2)";
    public Vector3 firstPersonModelLocalPosition = new Vector3(0.44f, -0.46f, 0.82f);
    public Vector3 firstPersonModelLocalEulerAngles = new Vector3(8f, -64.53f, 0f);
    public Vector3 firstPersonModelLocalScale = new Vector3(0.07f, 0.07f, 0.07f);
    public bool fitFirstPersonModelToView = true;
    public float firstPersonFittedModelSize = 0.7f;
    public Vector3 firstPersonFittedBoundsCenter = new Vector3(0.34f, -0.24f, 1.35f);
    public bool configureHolderAsFirstPersonOnly = true;
    public bool attachShadowVisualToRightHand = true;
    public bool rotateShadowVisualWithHand = true;
    public bool renderShadowVisualMesh = false;

    private MetalDetector metalDetector;
    private Transform visualRoot;
    private GameObject modelInstance;
    private GameObject firstPersonModelInstance;
    private Transform firstPersonModelParent;
    private bool worldModelTransformApplied;
    private bool firstPersonTransformApplied;
    private Transform gripAnchor;
    private LocalPlayerAvatarVisual localPlayerAvatarVisual;
    private static DetectorVisualBuilder activeHandAttachedDetector;
    private Transform shadowVisual;
    private bool shadowOffsetCaptured;
    private Vector3 shadowHandLocalPositionOffset;
    private Quaternion shadowHandLocalRotationOffset = Quaternion.identity;
    private DetectorScreenSignalDisplay screenSignalDisplay;
    private float nextScreenRegistrationTime;

    private void OnEnable()
    {
        EnsureVisuals();
    }

    private void Awake()
    {
        EnsureVisuals();
    }

    private void OnDisable()
    {
        if (activeHandAttachedDetector == this)
        {
            activeHandAttachedDetector = null;
        }

        shadowOffsetCaptured = false;
    }

    private void EnsureVisuals()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        metalDetector = GetComponent<MetalDetector>();
        detectorHead = detectorHead != null ? detectorHead : metalDetector.detectorHead;

        EnsureScreenSignalDisplay();
        EnsureDetectorHead();
        CacheShadowVisual();
        ApplyFirstPersonHolderVisibility();
        ApplyShadowVisibility();
        RegisterDetectorScreens(true);
    }

    private void LateUpdate()
    {
        AttachShadowVisualToRightHand();
        RegisterDetectorScreens(false);
    }

    private void EnsureScreenSignalDisplay()
    {
        if (screenSignalDisplay == null)
        {
            screenSignalDisplay = GetComponent<DetectorScreenSignalDisplay>();
        }

        if (screenSignalDisplay == null)
        {
            screenSignalDisplay = gameObject.AddComponent<DetectorScreenSignalDisplay>();
        }
    }

    private void RegisterDetectorScreens(bool force)
    {
        if (screenSignalDisplay == null)
        {
            return;
        }

        if (!force && Time.unscaledTime < nextScreenRegistrationTime)
        {
            return;
        }

        nextScreenRegistrationTime = Time.unscaledTime + ScreenRegistrationRefreshInterval;

        screenSignalDisplay.RegisterModelRoot(transform);
        RegisterDetectorTierVisualScreens();

        if (modelInstance != null)
        {
            screenSignalDisplay.RegisterModelRoot(modelInstance.transform);
        }

        if (firstPersonModelInstance != null)
        {
            screenSignalDisplay.RegisterModelRoot(firstPersonModelInstance.transform);
        }

        if (shadowVisual != null)
        {
            screenSignalDisplay.RegisterModelRoot(shadowVisual);
        }
    }

    private void RegisterDetectorTierVisualScreens()
    {
        if (metalDetector == null || metalDetector.detectorTierVisuals == null)
        {
            return;
        }

        foreach (Transform tierVisual in metalDetector.detectorTierVisuals)
        {
            screenSignalDisplay.RegisterModelRoot(tierVisual);
        }
    }

    private void EnsureDetectorHead()
    {
        if (detectorHead != null)
        {
            return;
        }

        GameObject headObject = new GameObject("DetectorHead");
        headObject.transform.SetParent(transform, false);
        headObject.transform.localPosition = new Vector3(0f, -0.45f, 0.95f);
        detectorHead = headObject.transform;
        metalDetector.detectorHead = detectorHead;
    }

    private void CacheShadowVisual()
    {
        if (shadowVisual != null)
        {
            return;
        }

        FirstPersonController ownerController = GetOwnerController();

        if (ownerController == null)
        {
            return;
        }

        Transform foundShadow = FindChildByName(ownerController.transform, ShadowVisualName);

        if (foundShadow == null)
        {
            foundShadow = FindUniqueSceneTransform(ShadowVisualName);
        }

        if (foundShadow != null && foundShadow != transform)
        {
            shadowVisual = foundShadow;
        }
    }

    private void AttachShadowVisualToRightHand()
    {
        if (!Application.isPlaying || !attachShadowVisualToRightHand)
        {
            return;
        }

        if (activeHandAttachedDetector != null && activeHandAttachedDetector != this)
        {
            return;
        }

        CacheShadowVisual();

        if (shadowVisual == null)
        {
            return;
        }

        Transform rightHand = GetPlayerRightHandAnchor();

        if (rightHand == null)
        {
            return;
        }

        if (!shadowOffsetCaptured)
        {
            shadowHandLocalPositionOffset = rightHand.InverseTransformPoint(shadowVisual.position);
            shadowHandLocalRotationOffset = Quaternion.Inverse(rightHand.rotation) * shadowVisual.rotation;
            shadowOffsetCaptured = true;
        }

        activeHandAttachedDetector = this;

        Vector3 targetPosition = rightHand.TransformPoint(shadowHandLocalPositionOffset);

        if (rotateShadowVisualWithHand)
        {
            Quaternion targetRotation = rightHand.rotation * shadowHandLocalRotationOffset;
            shadowVisual.SetPositionAndRotation(targetPosition, targetRotation);
            return;
        }

        shadowVisual.position = targetPosition;
    }

    private void ApplyFirstPersonHolderVisibility()
    {
        if (!configureHolderAsFirstPersonOnly)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || IsShadowVisualRenderer(renderer))
            {
                continue;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private void ApplyShadowVisibility()
    {
        if (shadowVisual == null)
        {
            return;
        }

        Renderer[] renderers = shadowVisual.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = true;
            renderer.shadowCastingMode = renderShadowVisualMesh
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            renderer.receiveShadows = renderShadowVisualMesh;
        }
    }

    private bool IsShadowVisualRenderer(Renderer renderer)
    {
        return shadowVisual != null && renderer.transform.IsChildOf(shadowVisual);
    }

    private FirstPersonController GetOwnerController()
    {
        return GetComponentInParent<FirstPersonController>();
    }

    private static Transform FindUniqueSceneTransform(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        Transform uniqueTransform = null;

        foreach (Transform candidate in transforms)
        {
            if (candidate == null
                || candidate.name != objectName
                || candidate.gameObject == null
                || !candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            if (uniqueTransform != null)
            {
                return null;
            }

            uniqueTransform = candidate;
        }

        return uniqueTransform;
    }

    private void CreateVisualRoot()
    {
        if (visualRoot == null)
        {
            Transform existingRoot = transform.Find("Metal Detector Visual");

            if (existingRoot != null)
            {
                visualRoot = existingRoot;
            }
        }

        if (visualRoot != null)
        {
            visualRoot.SetParent(transform, false);

            if (!preserveExistingWorldDetectorTransform)
            {
                visualRoot.localPosition = rootOffset;
                visualRoot.localRotation = Quaternion.identity;
                visualRoot.localScale = Vector3.one;
            }

            return;
        }

        GameObject rootObject = new GameObject("Metal Detector Visual");
        rootObject.transform.SetParent(transform, false);
        rootObject.transform.localPosition = rootOffset;
        visualRoot = rootObject.transform;
    }

    private void HideLegacyVisuals()
    {
        if (!hideLegacyVisuals)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer targetRenderer in renderers)
        {
            if (visualRoot != null && targetRenderer.transform.IsChildOf(visualRoot))
            {
                continue;
            }

            targetRenderer.enabled = false;
        }
    }

    private void LoadDetectorModel()
    {
        modelInstance = FindExistingDetectorModel();
        RemoveDuplicateDetectorModels(modelInstance);

        if (modelInstance != null)
        {
            worldModelTransformApplied = preserveExistingWorldDetectorTransform;
            DisableModelColliders();
            ApplyLocalVisibilityMode();
            EnsureFirstPersonModelLoaded();
            RegisterDetectorScreens(true);
            return;
        }

        GameObject detectorModel = Resources.Load<GameObject>(modelResourcePath);

        if (detectorModel == null)
        {
            Debug.LogWarning("Could not load metal detector model from Resources/" + modelResourcePath + ".");
            return;
        }

        modelInstance = Instantiate(detectorModel, visualRoot);
        modelInstance.name = "MetalDetector_Player";
        worldModelTransformApplied = false;
        ApplyWorldModelDefaultTransform();

        DisableModelColliders();
        ApplyLocalVisibilityMode();
        AlignGripAnchorToHand();
        FitModelToView();
        EnsureFirstPersonModelLoaded();
        RegisterDetectorScreens(true);
    }

    private void EnsureFirstPersonModelLoaded()
    {
        if (!showFirstPersonDetector || firstPersonModelInstance != null)
        {
            return;
        }

        string resourcePath = string.IsNullOrWhiteSpace(firstPersonModelResourcePath)
            ? modelResourcePath
            : firstPersonModelResourcePath;
        GameObject detectorModel = Resources.Load<GameObject>(resourcePath);

        if (detectorModel == null)
        {
            Debug.LogWarning("Could not load first-person detector model from Resources/" + resourcePath + ".");
            return;
        }

        LoadFirstPersonModel(detectorModel);
    }

    private void ClearModelInstance()
    {
        gripAnchor = null;

        if (firstPersonModelParent != null)
        {
            RemoveDuplicateFirstPersonModels(firstPersonModelParent, firstPersonModelInstance);
        }

        if (modelInstance != null)
        {
            DestroyDetectorObject(modelInstance);
            modelInstance = null;
            worldModelTransformApplied = false;
        }

        if (visualRoot == null)
        {
            Transform existingRoot = transform.Find("Metal Detector Visual");
            visualRoot = existingRoot;
        }

        if (visualRoot == null)
        {
            return;
        }

        for (int i = visualRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = visualRoot.GetChild(i);

            if (child.name.StartsWith("MetalDetector_"))
            {
                DestroyDetectorObject(child.gameObject);
            }
        }
    }

    private static void DestroyDetectorObject(GameObject target)
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

    private void LoadFirstPersonModel(GameObject detectorModel)
    {
        if (!showFirstPersonDetector || detectorModel == null)
        {
            return;
        }

        Transform cameraTransform = GetFirstPersonCameraTransform();

        if (cameraTransform == null)
        {
            return;
        }

        firstPersonModelParent = cameraTransform;
        firstPersonModelInstance = FindExistingFirstPersonModel(cameraTransform);

        if (firstPersonModelInstance != null)
        {
            firstPersonTransformApplied = true;
            RemoveDuplicateFirstPersonModels(cameraTransform, firstPersonModelInstance);
            DisableModelColliders(firstPersonModelInstance);
            ApplyFirstPersonVisibilityMode();
            RegisterDetectorScreens(true);
            return;
        }

        firstPersonModelInstance = Instantiate(detectorModel, cameraTransform);
        firstPersonModelInstance.name = "MetalDetector_FirstPerson";
        firstPersonTransformApplied = false;

        RemoveDuplicateFirstPersonModels(cameraTransform, firstPersonModelInstance);
        KeepFirstPersonModelAttachedToCamera();
        DisableModelColliders(firstPersonModelInstance);
        ApplyFirstPersonVisibilityMode();
        RegisterDetectorScreens(true);
    }

    private GameObject FindExistingDetectorModel()
    {
        if (visualRoot == null)
        {
            return null;
        }

        for (int i = 0; i < visualRoot.childCount; i++)
        {
            Transform child = visualRoot.GetChild(i);

            if (child.name == "MetalDetector_Player")
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private void RemoveDuplicateDetectorModels(GameObject modelToKeep)
    {
        if (visualRoot == null)
        {
            return;
        }

        for (int i = visualRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = visualRoot.GetChild(i);

            if (child.name == "MetalDetector_Player" && child.gameObject != modelToKeep)
            {
                DestroyDetectorObject(child.gameObject);
            }
        }
    }

    private void AlignGripAnchorToHand()
    {
        if (!alignGripAnchorToHand || modelInstance == null || visualRoot == null || string.IsNullOrEmpty(gripAnchorName))
        {
            return;
        }

        Transform gripAnchor = FindChildByName(modelInstance.transform, gripAnchorName);

        if (gripAnchor == null)
        {
            Debug.LogWarning("Could not attach detector grip. Child '" + gripAnchorName + "' was not found in " + modelResourcePath + ".");
            return;
        }

        Vector3 handTargetPosition = visualRoot.TransformPoint(gripTargetLocalPosition);
        modelInstance.transform.position += handTargetPosition - gripAnchor.position;
    }

    private void StickGripToPlayerRightHand()
    {
        if (!Application.isPlaying || !stickGripToPlayerRightHand || modelInstance == null || string.IsNullOrEmpty(gripAnchorName))
        {
            return;
        }

        if (activeHandAttachedDetector != null && activeHandAttachedDetector != this)
        {
            return;
        }

        if (gripAnchor == null)
        {
            gripAnchor = FindChildByName(modelInstance.transform, gripAnchorName);
        }

        if (gripAnchor == null)
        {
            return;
        }

        Transform rightHand = GetPlayerRightHandAnchor();

        if (rightHand == null)
        {
            return;
        }

        activeHandAttachedDetector = this;

        if (matchRightHandRotation)
        {
            Quaternion rotationDelta = rightHand.rotation * Quaternion.Inverse(gripAnchor.rotation);
            modelInstance.transform.rotation = rotationDelta * modelInstance.transform.rotation;
        }

        modelInstance.transform.position += rightHand.position - gripAnchor.position;
    }

    private void KeepModelAttachedToVisualRoot()
    {
        if (stickGripToPlayerRightHand || alignGripAnchorToHand || modelInstance == null || worldModelTransformApplied)
        {
            return;
        }

        ApplyWorldModelDefaultTransform();
    }

    private void ApplyWorldModelDefaultTransform()
    {
        if (modelInstance == null)
        {
            return;
        }

        modelInstance.transform.localPosition = modelLocalPosition;
        modelInstance.transform.localRotation = Quaternion.Euler(modelLocalEulerAngles);
        modelInstance.transform.localScale = modelLocalScale;
        worldModelTransformApplied = true;
    }

    private void KeepFirstPersonModelAttachedToCamera()
    {
        if (!showFirstPersonDetector || firstPersonModelInstance == null)
        {
            return;
        }

        Transform cameraTransform = GetFirstPersonCameraTransform();

        if (cameraTransform != null && firstPersonModelInstance.transform.parent != cameraTransform)
        {
            firstPersonModelParent = cameraTransform;
            firstPersonModelInstance.transform.SetParent(cameraTransform, false);
        }

        if (firstPersonTransformApplied)
        {
            return;
        }

        firstPersonModelInstance.transform.localPosition = firstPersonModelLocalPosition;
        firstPersonModelInstance.transform.localRotation = Quaternion.Euler(firstPersonModelLocalEulerAngles);
        firstPersonModelInstance.transform.localScale = firstPersonModelLocalScale;
        FitFirstPersonModelToView();
        firstPersonTransformApplied = true;
    }

    private Transform GetFirstPersonCameraTransform()
    {
        FirstPersonController controller = FindAnyObjectByType<FirstPersonController>();

        if (controller != null && controller.playerCamera != null)
        {
            return controller.playerCamera;
        }

        Camera mainCamera = Camera.main;
        return mainCamera != null ? mainCamera.transform : null;
    }

    private GameObject FindExistingFirstPersonModel(Transform cameraTransform)
    {
        if (cameraTransform == null)
        {
            return null;
        }

        for (int i = cameraTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = cameraTransform.GetChild(i);

            if (child.name == "MetalDetector_FirstPerson")
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private void RemoveDuplicateFirstPersonModels(Transform cameraTransform, GameObject modelToKeep)
    {
        if (cameraTransform == null)
        {
            return;
        }

        for (int i = cameraTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = cameraTransform.GetChild(i);

            if (child.name == "MetalDetector_FirstPerson" && child.gameObject != modelToKeep)
            {
                DestroyDetectorObject(child.gameObject);
            }
        }
    }

    private Transform GetPlayerRightHandAnchor()
    {
        FirstPersonController ownerController = GetComponentInParent<FirstPersonController>();

        if (ownerController == null)
        {
            localPlayerAvatarVisual = null;
            return null;
        }

        LocalPlayerAvatarVisual ownerAvatarVisual = ownerController.GetComponent<LocalPlayerAvatarVisual>();

        if (ownerAvatarVisual == null)
        {
            localPlayerAvatarVisual = null;
            return null;
        }

        if (localPlayerAvatarVisual != ownerAvatarVisual)
        {
            localPlayerAvatarVisual = ownerAvatarVisual;
        }

        return localPlayerAvatarVisual.RightHandAnchor;
    }

    private void AlignScannerAnchorToDetectorHead()
    {
        if (!alignScannerAnchorToDetectorHead || detectorHead == null || modelInstance == null || string.IsNullOrEmpty(scannerAnchorName))
        {
            return;
        }

        Transform scannerAnchor = FindChildByName(modelInstance.transform, scannerAnchorName);

        if (scannerAnchor == null)
        {
            Debug.LogWarning("Could not align detector scanner. Child '" + scannerAnchorName + "' was not found in " + modelResourcePath + ".");
            return;
        }

        modelInstance.transform.position += detectorHead.position - scannerAnchor.position;
    }

    private static Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), targetName);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void FitModelToView()
    {
        if (!fitModelToView || modelInstance == null || fittedModelSize <= 0f)
        {
            return;
        }

        Bounds localBounds;

        if (!TryGetLocalBounds(out localBounds))
        {
            return;
        }

        float largestAxis = Mathf.Max(localBounds.size.x, Mathf.Max(localBounds.size.y, localBounds.size.z));

        if (largestAxis <= Mathf.Epsilon)
        {
            return;
        }

        float scaleMultiplier = fittedModelSize / largestAxis;
        modelInstance.transform.localScale *= scaleMultiplier;

        if (!TryGetLocalBounds(out localBounds))
        {
            return;
        }

        Vector3 centerDelta = fittedBoundsCenter - localBounds.center;
        modelInstance.transform.localPosition += centerDelta;
    }

    private void FitFirstPersonModelToView()
    {
        if (!fitFirstPersonModelToView
            || firstPersonModelInstance == null
            || firstPersonModelParent == null
            || firstPersonFittedModelSize <= 0f)
        {
            return;
        }

        Bounds localBounds;

        if (!TryGetLocalBounds(firstPersonModelInstance, firstPersonModelParent, out localBounds))
        {
            return;
        }

        float largestAxis = Mathf.Max(localBounds.size.x, Mathf.Max(localBounds.size.y, localBounds.size.z));

        if (largestAxis <= Mathf.Epsilon)
        {
            return;
        }

        float scaleMultiplier = firstPersonFittedModelSize / largestAxis;
        firstPersonModelInstance.transform.localScale *= scaleMultiplier;

        if (!TryGetLocalBounds(firstPersonModelInstance, firstPersonModelParent, out localBounds))
        {
            return;
        }

        Vector3 centerDelta = firstPersonFittedBoundsCenter - localBounds.center;
        firstPersonModelInstance.transform.localPosition += centerDelta;
    }

    private bool TryGetLocalBounds(out Bounds localBounds)
    {
        return TryGetLocalBounds(modelInstance, visualRoot, out localBounds);
    }

    private static bool TryGetLocalBounds(GameObject target, Transform boundsRoot, out Bounds localBounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        localBounds = new Bounds();
        bool hasBounds = false;

        foreach (Renderer modelRenderer in renderers)
        {
            Bounds rendererBounds = modelRenderer.bounds;
            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;

            EncapsulateLocalPoint(boundsRoot.InverseTransformPoint(new Vector3(min.x, min.y, min.z)), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(boundsRoot.InverseTransformPoint(new Vector3(min.x, min.y, max.z)), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(boundsRoot.InverseTransformPoint(new Vector3(min.x, max.y, min.z)), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(boundsRoot.InverseTransformPoint(new Vector3(min.x, max.y, max.z)), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(boundsRoot.InverseTransformPoint(new Vector3(max.x, min.y, min.z)), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(boundsRoot.InverseTransformPoint(new Vector3(max.x, min.y, max.z)), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(boundsRoot.InverseTransformPoint(new Vector3(max.x, max.y, min.z)), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(boundsRoot.InverseTransformPoint(new Vector3(max.x, max.y, max.z)), ref localBounds, ref hasBounds);
        }

        return hasBounds;
    }

    private static void EncapsulateLocalPoint(Vector3 point, ref Bounds localBounds, ref bool hasBounds)
    {
        if (!hasBounds)
        {
            localBounds = new Bounds(point, Vector3.zero);
            hasBounds = true;
            return;
        }

        localBounds.Encapsulate(point);
    }

    private void DisableModelColliders()
    {
        DisableModelColliders(modelInstance);
    }

    private void DisableModelColliders(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);

        foreach (Collider modelCollider in colliders)
        {
            modelCollider.enabled = false;
        }
    }

    private void ApplyLocalVisibilityMode()
    {
        if (modelInstance == null)
        {
            return;
        }

        Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = true;
            renderer.shadowCastingMode = showOnlyLocalShadow
                ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly
                : UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = !showOnlyLocalShadow;
        }
    }

    private void ApplyFirstPersonVisibilityMode()
    {
        if (firstPersonModelInstance == null)
        {
            return;
        }

        Renderer[] renderers = firstPersonModelInstance.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
