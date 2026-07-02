using UnityEngine;

public static class QuestGiverBootstrapper
{
    private const string QuestGiverName = "QuestGiverNPC";
    private const string VisualRootName = "Quest Giver Visual";
    private const string FallbackVisualName = "Fallback Human";
    private const float InteractionDistance = 4.2f;
    private const float GroundOffset = 0.03f;
    private static readonly Vector3 QuestGiverPosition = new Vector3(-677.5f, 68.3f, -733.1f);

    public static void EnsureQuestGiverAtHome()
    {
        PlayerHome home = Object.FindAnyObjectByType<PlayerHome>();
        Transform questGiver = FindOrCreateQuestGiver();

        if (questGiver == null)
        {
            return;
        }

        PlaceQuestGiverNearHome(questGiver, home);
        EnsureQuestGiverComponent(questGiver);
        EnsureQuestGiverVisuals(questGiver);
    }

    private static Transform FindOrCreateQuestGiver()
    {
        GameObject questGiverObject = GameObject.Find(QuestGiverName);

        if (questGiverObject != null)
        {
            return questGiverObject.transform;
        }

        questGiverObject = new GameObject(QuestGiverName);

        CapsuleCollider collider = questGiverObject.AddComponent<CapsuleCollider>();
        collider.height = 2f;
        collider.radius = 0.45f;
        collider.center = new Vector3(0f, 1f, 0f);
        collider.isTrigger = true;

        return questGiverObject.transform;
    }

    private static void PlaceQuestGiverNearHome(Transform questGiver, PlayerHome home)
    {
        Vector3 homePosition = home != null ? home.transform.position : new Vector3(-670f, QuestGiverPosition.y, -728f);
        Vector3 targetPosition = GetGroundedPosition(QuestGiverPosition);
        questGiver.position = targetPosition;

        Vector3 lookDirection = homePosition - targetPosition;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            questGiver.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }

    private static void EnsureQuestGiverComponent(Transform questGiver)
    {
        NpcQuestGiver questGiverComponent = questGiver.GetComponent<NpcQuestGiver>();

        if (questGiverComponent == null)
        {
            questGiverComponent = questGiver.gameObject.AddComponent<NpcQuestGiver>();
        }

        questGiverComponent.npcDisplayName = "Mira";
        questGiverComponent.interactionDistance = InteractionDistance;
        questGiverComponent.player = PlayerRigReferences.FindLocalPlayerTransform();
        questGiverComponent.playerInventory = PlayerRigReferences.FindLocalInventory();

        Transform interactionPoint = questGiver.Find("Interaction Point");

        if (interactionPoint == null)
        {
            GameObject interactionObject = new GameObject("Interaction Point");
            interactionObject.transform.SetParent(questGiver, false);
            interactionObject.transform.localPosition = new Vector3(0f, 1f, 0f);
            interactionPoint = interactionObject.transform;
        }

        questGiverComponent.interactionPoint = interactionPoint;

        Transform promptAnchor = questGiver.Find("Prompt Anchor");

        if (promptAnchor == null)
        {
            GameObject promptObject = new GameObject("Prompt Anchor");
            promptObject.transform.SetParent(questGiver, false);
            promptObject.transform.localPosition = new Vector3(0f, 2.25f, 0f);
            promptAnchor = promptObject.transform;
        }

        questGiverComponent.promptAnchor = promptAnchor;
    }

    private static void EnsureQuestGiverVisuals(Transform questGiver)
    {
        Transform visualRoot = questGiver.Find(VisualRootName);

        if (visualRoot == null)
        {
            GameObject visualObject = new GameObject(VisualRootName);
            visualObject.transform.SetParent(questGiver, false);
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;
            visualObject.transform.localScale = Vector3.one;
            visualRoot = visualObject.transform;
        }

        if (!HasRenderableVisual(visualRoot))
        {
            PlayerCharacterSelection.CharacterProfile profile = new PlayerCharacterSelection.CharacterProfile(PlayerCharacterSelection.CharacterGender.Female, 918271);
            profile.displayName = "Mira Quest Giver";
            UmaCharacterFactory.TryCreateCharacter(visualRoot, profile, out _);
        }

        if (!HasRenderableVisual(visualRoot))
        {
            EnsureFallbackHuman(visualRoot);
        }

        ConfigureRenderers(visualRoot);
        RemoveLabels(questGiver);
        RemoveBlockProps(questGiver);
    }

    private static void EnsureFallbackHuman(Transform visualRoot)
    {
        if (visualRoot.Find(FallbackVisualName) != null)
        {
            return;
        }

        GameObject fallback = new GameObject(FallbackVisualName);
        fallback.transform.SetParent(visualRoot, false);
        fallback.transform.localPosition = Vector3.zero;
        fallback.transform.localRotation = Quaternion.identity;
        fallback.transform.localScale = Vector3.one;

        Material skinMaterial = CreateMaterial(new Color(0.78f, 0.58f, 0.42f, 1f));
        Material clothesMaterial = CreateMaterial(new Color(0.18f, 0.38f, 0.26f, 1f));

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(fallback.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        body.transform.localScale = new Vector3(0.48f, 0.72f, 0.48f);
        body.GetComponent<Renderer>().material = clothesMaterial;
        DisableCollider(body);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(fallback.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.82f, 0f);
        head.transform.localScale = new Vector3(0.34f, 0.34f, 0.34f);
        head.GetComponent<Renderer>().material = skinMaterial;
        DisableCollider(head);
    }

    private static void ConfigureRenderers(Transform visualRoot)
    {
        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
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

    private static void RemoveLabels(Transform questGiver)
    {
        DestroyChild(questGiver, "Quest Label");
    }

    private static void RemoveBlockProps(Transform questGiver)
    {
        DestroyChild(questGiver, "Quest Notice Board");
        DestroyChild(questGiver, "Pinned Quest Paper");
    }

    private static void DestroyChild(Transform parent, string childName)
    {
        Transform child = parent != null ? parent.Find(childName) : null;

        if (child != null)
        {
            DestroyGameObject(child.gameObject);
        }
    }

    private static void DestroyGameObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
    }

    private static void DisableCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();

        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private static float GetGroundY(float worldX, float worldZ)
    {
        Terrain terrain = GetTerrainAt(worldX, worldZ);

        if (terrain == null)
        {
            return 51.08f;
        }

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        float normalizedX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, worldX);
        float normalizedZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, worldZ);
        return terrainPosition.y + terrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
    }

    private static Vector3 GetGroundedPosition(Vector3 position)
    {
        Terrain terrain = GetTerrainAt(position.x, position.z);

        if (terrain != null)
        {
            position.y = GetGroundY(position.x, position.z) + GroundOffset;
        }

        return position;
    }

    private static Terrain GetTerrainAt(float worldX, float worldZ)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain != null && IsInsideTerrain(terrain, worldX, worldZ))
            {
                return terrain;
            }
        }

        Terrain activeTerrain = Terrain.activeTerrain;
        return activeTerrain != null && IsInsideTerrain(activeTerrain, worldX, worldZ) ? activeTerrain : null;
    }

    private static bool IsInsideTerrain(Terrain terrain, float worldX, float worldZ)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        return worldX >= terrainPosition.x
            && worldX <= terrainPosition.x + terrainSize.x
            && worldZ >= terrainPosition.z
            && worldZ <= terrainPosition.z + terrainSize.z;
    }

    private static Material CreateMaterial(Color color)
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

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.22f);
        }

        return material;
    }
}
