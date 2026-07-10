using UnityEngine;

[DisallowMultipleComponent]
public class AnimalWanderController : MonoBehaviour
{
    private const string VerticalAnimatorParameter = "Vert";
    private const string StateAnimatorParameter = "State";

    private Animator animator;
    private Terrain terrain;
    private Vector3 homePosition;
    private Vector3 targetPosition;
    private float wanderRadius = 12f;
    private float minDecisionTime = 1.4f;
    private float maxDecisionTime = 4.2f;
    private float pauseChance = 0.18f;
    private float runChance = 0.25f;
    private float groundYOffset = -0.2f;
    private float walkSpeed = 1.2f;
    private float runSpeed = 3.2f;
    private float turnSpeed = 320f;
    private float decisionTimer;
    private bool isPaused;
    private bool isRunning;

    public void Configure(Vector3 home, float radius, float minTime, float maxTime, float pause, float run, float groundOffset, float walk, float runMoveSpeed)
    {
        homePosition = home;
        wanderRadius = Mathf.Max(1f, radius);
        minDecisionTime = Mathf.Max(0.2f, minTime);
        maxDecisionTime = Mathf.Max(minDecisionTime, maxTime);
        pauseChance = Mathf.Clamp01(pause);
        runChance = Mathf.Clamp01(run);
        groundYOffset = groundOffset;
        walkSpeed = Mathf.Max(0.1f, walk);
        runSpeed = Mathf.Max(walkSpeed, runMoveSpeed);
        StickToTerrain();
        PickNextTarget();
    }

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        terrain = Terrain.activeTerrain;
        DisablePrefabMovementScripts();

        if (homePosition == Vector3.zero)
        {
            homePosition = transform.position;
        }
    }

    private void Update()
    {
        decisionTimer -= Time.deltaTime;
        StickToTerrain();

        Vector3 flatOffset = targetPosition - transform.position;
        flatOffset.y = 0f;

        if (decisionTimer <= 0f || flatOffset.sqrMagnitude < 0.9f)
        {
            PickNextTarget();
            flatOffset = targetPosition - transform.position;
            flatOffset.y = 0f;
        }

        if (isPaused || flatOffset.sqrMagnitude < 0.05f)
        {
            Animate(0f, 0f);
            return;
        }

        Vector3 direction = flatOffset.normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        transform.position += direction * (isRunning ? runSpeed : walkSpeed) * Time.deltaTime;
        StickToTerrain();
        Animate(1f, isRunning ? 1f : 0f);
    }

    private void PickNextTarget()
    {
        decisionTimer = Random.Range(minDecisionTime, maxDecisionTime);
        isPaused = Random.value < pauseChance;
        isRunning = !isPaused && Random.value < runChance;

        Vector3 flatFromHome = transform.position - homePosition;
        flatFromHome.y = 0f;

        if (flatFromHome.magnitude > wanderRadius * 1.15f)
        {
            targetPosition = homePosition;
            isPaused = false;
            isRunning = true;
            return;
        }

        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        targetPosition = homePosition + new Vector3(offset.x, 0f, offset.y);
    }

    private void StickToTerrain()
    {
        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
        }

        if (terrain == null || terrain.terrainData == null)
        {
            return;
        }

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 currentPosition = transform.position;

        if (currentPosition.x < terrainPosition.x || currentPosition.x > terrainPosition.x + terrainSize.x || currentPosition.z < terrainPosition.z || currentPosition.z > terrainPosition.z + terrainSize.z)
        {
            return;
        }

        float normalizedX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, currentPosition.x);
        float normalizedZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, currentPosition.z);
        float groundY = terrainPosition.y + terrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
        transform.position = new Vector3(currentPosition.x, groundY + groundYOffset, currentPosition.z);
    }

    private void Animate(float vertical, float state)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat(VerticalAnimatorParameter, vertical, 0.12f, Time.deltaTime);
        animator.SetFloat(StateAnimatorParameter, state, 0.12f, Time.deltaTime);
    }

    private void DisablePrefabMovementScripts()
    {
        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == this)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;

            if (typeName == "MovePlayerInput" || typeName == "PlayerCamera" || typeName == "CreatureMover")
            {
                behaviour.enabled = false;
            }
        }

        CharacterController controller = GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;
        }
    }
}
