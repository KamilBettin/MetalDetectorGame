using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(50)]
public class UmaAvatarAnimationDriver : MonoBehaviour
{
    private const string FallbackControllerResourcePath = "UMA/Locomotion";

    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int DirectionParameter = Animator.StringToHash("Direction");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int RunState = Animator.StringToHash("Base Layer.Run");
    private static RuntimeAnimatorController fallbackController;

    public Transform motionSource;
    public float movingThreshold = 0.08f;
    public float runSpeed = 5f;
    public float sprintSpeed = 8f;
    public float moveDamping = 0.06f;
    public float startTransitionTime = 0.06f;
    public float stopTransitionTime = 0.015f;
    public bool reduceArmAnimation = true;
    [Range(0f, 1f)] public float armAnimationWeight = 0.58f;
    [Range(0f, 1f)] public float upperBodyAnimationWeight = 0.74f;
    public bool steadyUpperBodyWhileScanning = true;
    public bool useLocalScanInputForSteadyPose = true;
    public bool useWalkAnimationWhileScanning = true;
    [Range(0.1f, 1f)] public float scanningWalkSpeedParameter = 0.22f;
    [Range(0.1f, 1.5f)] public float scanningWalkAnimationSpeed = 0.58f;
    [Range(0f, 1f)] public float scanningArmAnimationWeight = 0.22f;
    [Range(0f, 1f)] public float scanningUpperBodyAnimationWeight = 0.48f;
    public float scanningPoseBlendSpeed = 10f;

    private Animator animator;
    private CharacterController characterController;
    private Vector3 lastPosition;
    private bool hasLastPosition;
    private int lastRequestedState;
    private BonePose[] armBindPoses;
    private BonePose[] upperBodyBindPoses;
    private Animator cachedPoseAnimator;
    private float scanningPoseBlend;

    private static readonly HumanBodyBones[] UpperBodyBones =
    {
        HumanBodyBones.Spine,
        HumanBodyBones.Chest,
        HumanBodyBones.UpperChest
    };

    private static readonly HumanBodyBones[] ArmBones =
    {
        HumanBodyBones.LeftShoulder,
        HumanBodyBones.LeftUpperArm,
        HumanBodyBones.LeftLowerArm,
        HumanBodyBones.LeftHand,
        HumanBodyBones.LeftThumbProximal,
        HumanBodyBones.LeftThumbIntermediate,
        HumanBodyBones.LeftThumbDistal,
        HumanBodyBones.LeftIndexProximal,
        HumanBodyBones.LeftIndexIntermediate,
        HumanBodyBones.LeftIndexDistal,
        HumanBodyBones.LeftMiddleProximal,
        HumanBodyBones.LeftMiddleIntermediate,
        HumanBodyBones.LeftMiddleDistal,
        HumanBodyBones.LeftRingProximal,
        HumanBodyBones.LeftRingIntermediate,
        HumanBodyBones.LeftRingDistal,
        HumanBodyBones.LeftLittleProximal,
        HumanBodyBones.LeftLittleIntermediate,
        HumanBodyBones.LeftLittleDistal,
        HumanBodyBones.RightShoulder,
        HumanBodyBones.RightUpperArm,
        HumanBodyBones.RightLowerArm,
        HumanBodyBones.RightHand,
        HumanBodyBones.RightThumbProximal,
        HumanBodyBones.RightThumbIntermediate,
        HumanBodyBones.RightThumbDistal,
        HumanBodyBones.RightIndexProximal,
        HumanBodyBones.RightIndexIntermediate,
        HumanBodyBones.RightIndexDistal,
        HumanBodyBones.RightMiddleProximal,
        HumanBodyBones.RightMiddleIntermediate,
        HumanBodyBones.RightMiddleDistal,
        HumanBodyBones.RightRingProximal,
        HumanBodyBones.RightRingIntermediate,
        HumanBodyBones.RightRingDistal,
        HumanBodyBones.RightLittleProximal,
        HumanBodyBones.RightLittleIntermediate,
        HumanBodyBones.RightLittleDistal
    };

    private void Awake()
    {
        CacheAnimator();
    }

    private void OnEnable()
    {
        Transform source = GetMotionSource();
        lastPosition = source.position;
        hasLastPosition = true;
        CacheAnimator();
    }

    private void LateUpdate()
    {
        Transform source = GetMotionSource();

        if (animator == null)
        {
            CacheAnimator();

            if (animator == null)
            {
                return;
            }
        }

        if (!hasLastPosition)
        {
            lastPosition = source.position;
            hasLastPosition = true;
            return;
        }

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 delta = source.position - lastPosition;
        lastPosition = source.position;

        Vector3 velocity = characterController != null ? characterController.velocity : delta / deltaTime;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float worldSpeed = horizontalVelocity.magnitude;
        float normalizedSpeed = worldSpeed <= movingThreshold ? 0f : Mathf.Clamp01(worldSpeed / Mathf.Max(0.01f, runSpeed));
        bool isScanning = IsScanInputHeld();
        bool useScanningLocomotion = useWalkAnimationWhileScanning
            && isScanning
            && normalizedSpeed > 0f;

        if (useScanningLocomotion)
        {
            normalizedSpeed = Mathf.Min(normalizedSpeed, scanningWalkSpeedParameter);
        }

        Vector3 localVelocity = source.InverseTransformDirection(horizontalVelocity);
        float direction = normalizedSpeed <= 0f ? 0f : Mathf.Clamp(localVelocity.x / Mathf.Max(worldSpeed, 0.01f), -1f, 1f);

        animator.applyRootMotion = false;
        animator.enabled = true;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        float speedDamping = normalizedSpeed <= 0f ? 0f : moveDamping;
        animator.SetFloat(SpeedParameter, normalizedSpeed, speedDamping, deltaTime);
        animator.SetFloat(DirectionParameter, direction, speedDamping, deltaTime);
        animator.speed = GetAnimatorPlaybackSpeed(normalizedSpeed, worldSpeed, useScanningLocomotion);

        int requestedState = normalizedSpeed <= 0f ? IdleState : RunState;

        if (requestedState != lastRequestedState)
        {
            float transition = requestedState == IdleState ? stopTransitionTime : startTransitionTime;
            animator.CrossFadeInFixedTime(requestedState, transition, 0);
            lastRequestedState = requestedState;
        }

        UpdateScanningPoseBlend(deltaTime);
        ReduceUpperBodyAnimation();
    }

    private float GetAnimatorPlaybackSpeed(float normalizedSpeed, float worldSpeed, bool useScanningLocomotion)
    {
        if (normalizedSpeed <= 0f)
        {
            return 1f;
        }

        if (useScanningLocomotion)
        {
            return scanningWalkAnimationSpeed;
        }

        return Mathf.Lerp(0.82f, 1.18f, Mathf.InverseLerp(runSpeed, sprintSpeed, worldSpeed));
    }

    private Transform GetMotionSource()
    {
        if (motionSource != null)
        {
            return motionSource;
        }

        if (transform.parent != null)
        {
            motionSource = transform.parent;
            return motionSource;
        }

        motionSource = transform;
        return motionSource;
    }

    private void CacheAnimator()
    {
        animator = GetComponentInChildren<Animator>(true);

        if (animator != null)
        {
            if (animator.runtimeAnimatorController == null)
            {
                animator.runtimeAnimatorController = GetFallbackController();
            }

            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;

            if (cachedPoseAnimator != animator)
            {
                armBindPoses = null;
                upperBodyBindPoses = null;
                cachedPoseAnimator = animator;
            }
        }

        Transform source = GetMotionSource();
        characterController = source.GetComponentInParent<CharacterController>();
    }

    private static RuntimeAnimatorController GetFallbackController()
    {
        if (fallbackController == null)
        {
            fallbackController = Resources.Load<RuntimeAnimatorController>(FallbackControllerResourcePath);
        }

        return fallbackController;
    }

    private void ReduceUpperBodyAnimation()
    {
        if (!reduceArmAnimation || animator == null || !animator.isHuman)
        {
            return;
        }

        EnsureBindPoses();
        float effectiveUpperBodyWeight = Mathf.Lerp(upperBodyAnimationWeight, scanningUpperBodyAnimationWeight, scanningPoseBlend);
        float effectiveArmWeight = Mathf.Lerp(armAnimationWeight, scanningArmAnimationWeight, scanningPoseBlend);
        ApplyPoseBlend(upperBodyBindPoses, effectiveUpperBodyWeight);
        ApplyPoseBlend(armBindPoses, effectiveArmWeight);
    }

    private void UpdateScanningPoseBlend(float deltaTime)
    {
        float targetBlend = steadyUpperBodyWhileScanning && IsScanInputHeld() ? 1f : 0f;
        float step = Mathf.Max(0.01f, scanningPoseBlendSpeed) * deltaTime;
        scanningPoseBlend = Mathf.MoveTowards(scanningPoseBlend, targetBlend, step);
    }

    private bool IsScanInputHeld()
    {
        return useLocalScanInputForSteadyPose && !GameUIState.AnyBlockingUIOpen && Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    private static void ApplyPoseBlend(BonePose[] poses, float animationWeight)
    {
        if (poses == null)
        {
            return;
        }

        float weight = Mathf.Clamp01(animationWeight);

        foreach (BonePose pose in poses)
        {
            if (pose.Bone == null)
            {
                continue;
            }

            pose.Bone.localPosition = Vector3.Lerp(pose.LocalPosition, pose.Bone.localPosition, weight);
            pose.Bone.localRotation = Quaternion.Slerp(pose.LocalRotation, pose.Bone.localRotation, weight);
        }
    }

    private void EnsureBindPoses()
    {
        if (animator == null || !animator.isHuman)
        {
            return;
        }

        if (upperBodyBindPoses == null)
        {
            upperBodyBindPoses = CaptureBindPoses(UpperBodyBones);
        }

        if (armBindPoses == null)
        {
            armBindPoses = CaptureBindPoses(ArmBones);
        }
    }

    private BonePose[] CaptureBindPoses(HumanBodyBones[] bones)
    {
        System.Collections.Generic.List<BonePose> poses = new System.Collections.Generic.List<BonePose>();

        foreach (HumanBodyBones bone in bones)
        {
            Transform boneTransform = animator.GetBoneTransform(bone);

            if (boneTransform == null)
            {
                continue;
            }

            poses.Add(new BonePose(boneTransform));
        }

        return poses.Count > 0 ? poses.ToArray() : null;
    }

    private readonly struct BonePose
    {
        public readonly Transform Bone;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;

        public BonePose(Transform bone)
        {
            Bone = bone;
            LocalPosition = bone.localPosition;
            LocalRotation = bone.localRotation;
            LocalScale = bone.localScale;
        }
    }
}
