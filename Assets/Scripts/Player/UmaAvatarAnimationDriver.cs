using UnityEngine;

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

    private Animator animator;
    private CharacterController characterController;
    private Vector3 lastPosition;
    private bool hasLastPosition;
    private int lastRequestedState;

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
        Vector3 localVelocity = source.InverseTransformDirection(horizontalVelocity);
        float direction = normalizedSpeed <= 0f ? 0f : Mathf.Clamp(localVelocity.x / Mathf.Max(worldSpeed, 0.01f), -1f, 1f);

        animator.applyRootMotion = false;
        animator.enabled = true;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        float speedDamping = normalizedSpeed <= 0f ? 0f : moveDamping;
        animator.SetFloat(SpeedParameter, normalizedSpeed, speedDamping, deltaTime);
        animator.SetFloat(DirectionParameter, direction, speedDamping, deltaTime);
        animator.speed = normalizedSpeed <= 0f ? 1f : Mathf.Lerp(0.82f, 1.18f, Mathf.InverseLerp(runSpeed, sprintSpeed, worldSpeed));

        int requestedState = normalizedSpeed > 0f ? RunState : IdleState;

        if (requestedState != lastRequestedState)
        {
            float transition = requestedState == IdleState ? stopTransitionTime : startTransitionTime;
            animator.CrossFadeInFixedTime(requestedState, transition, 0);
            lastRequestedState = requestedState;
        }
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
}
