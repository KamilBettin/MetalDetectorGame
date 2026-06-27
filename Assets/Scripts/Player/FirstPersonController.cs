using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 1.4f;
    public float mouseSensitivity = 2f;
    public float gravity = -20f;
    public bool limitDeepWater = true;
    public float waterLevel = 0f;
    public OceanWaterSurface waterSurface;
    public float maxWaterDepth = 0.9f;
    public float waterMoveSpeedMultiplier = 0.62f;

    public Transform playerCamera;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraPitch;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        CacheWaterSurface();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (GameUIState.AnyMenuOpen)
        {
            return;
        }

        LookAround();
        MovePlayer();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void LookAround()
    {
        if (playerCamera == null || Mouse.current == null)
        {
            return;
        }

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * 0.05f;
        float mouseY = mouseDelta.y * mouseSensitivity * 0.05f;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);

        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    void MovePlayer()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed)
            {
                input.x -= 1f;
            }

            if (Keyboard.current.dKey.isPressed)
            {
                input.x += 1f;
            }

            if (Keyboard.current.sKey.isPressed)
            {
                input.y -= 1f;
            }

            if (Keyboard.current.wKey.isPressed)
            {
                input.y += 1f;
            }
        }

        input = Vector2.ClampMagnitude(input, 1f);
        bool isSprinting = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        currentSpeed *= GetWaterSpeedMultiplier(transform.position);
        Vector3 horizontalMove = moveDirection * currentSpeed * Time.deltaTime;
        horizontalMove = LimitDeepWaterMove(horizontalMove);

        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 verticalMove = Vector3.up * verticalVelocity * Time.deltaTime;

        controller.Move(horizontalMove + verticalMove);
    }

    private Vector3 LimitDeepWaterMove(Vector3 desiredMove)
    {
        if (!limitDeepWater || desiredMove.sqrMagnitude <= 0.000001f)
        {
            return desiredMove;
        }

        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = currentPosition + desiredMove;

        if (CanStandAtWaterDepth(currentPosition, targetPosition))
        {
            return desiredMove;
        }

        Vector3 xOnlyMove = new Vector3(desiredMove.x, 0f, 0f);

        if (CanStandAtWaterDepth(currentPosition, currentPosition + xOnlyMove))
        {
            return xOnlyMove;
        }

        Vector3 zOnlyMove = new Vector3(0f, 0f, desiredMove.z);

        if (CanStandAtWaterDepth(currentPosition, currentPosition + zOnlyMove))
        {
            return zOnlyMove;
        }

        return Vector3.zero;
    }

    private bool CanStandAtWaterDepth(Vector3 currentPosition, Vector3 targetPosition)
    {
        float targetDepth = GetWaterDepthAt(targetPosition);

        if (targetDepth <= maxWaterDepth)
        {
            return true;
        }

        float currentDepth = GetWaterDepthAt(currentPosition);
        return targetDepth < currentDepth;
    }

    private float GetWaterSpeedMultiplier(Vector3 worldPosition)
    {
        if (!limitDeepWater)
        {
            return 1f;
        }

        float depth = GetWaterDepthAt(worldPosition);

        if (depth <= 0f)
        {
            return 1f;
        }

        float waterT = Mathf.InverseLerp(0f, Mathf.Max(0.01f, maxWaterDepth), depth);
        return Mathf.Lerp(1f, Mathf.Clamp01(waterMoveSpeedMultiplier), waterT);
    }

    private float GetWaterDepthAt(Vector3 worldPosition)
    {
        Terrain terrain = GetTerrainAt(worldPosition);

        if (terrain == null)
        {
            return Terrain.activeTerrains.Length > 0 || Terrain.activeTerrain != null ? maxWaterDepth + 1f : 0f;
        }

        float groundHeight = GetTerrainWorldHeight(terrain, worldPosition.x, worldPosition.z);
        return Mathf.Max(0f, GetCurrentWaterLevel() - groundHeight);
    }

    private float GetCurrentWaterLevel()
    {
        if (waterSurface == null)
        {
            CacheWaterSurface();
        }

        return waterSurface != null ? waterSurface.waterLevel : waterLevel;
    }

    private void CacheWaterSurface()
    {
        if (waterSurface == null)
        {
            waterSurface = FindAnyObjectByType<OceanWaterSurface>();
        }
    }

    private Terrain GetTerrainAt(Vector3 worldPosition)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain != null && IsInsideTerrain(terrain, worldPosition))
            {
                return terrain;
            }
        }

        Terrain activeTerrain = Terrain.activeTerrain;
        return activeTerrain != null && IsInsideTerrain(activeTerrain, worldPosition) ? activeTerrain : null;
    }

    private bool IsInsideTerrain(Terrain terrain, Vector3 worldPosition)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        return worldPosition.x >= terrainPosition.x
            && worldPosition.x <= terrainPosition.x + terrainSize.x
            && worldPosition.z >= terrainPosition.z
            && worldPosition.z <= terrainPosition.z + terrainSize.z;
    }

    private float GetTerrainWorldHeight(Terrain terrain, float worldX, float worldZ)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        float normalizedX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, worldX);
        float normalizedZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, worldZ);
        return terrainPosition.y + terrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
    }
}
