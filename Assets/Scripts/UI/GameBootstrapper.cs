using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class GameBootstrapper
{
    private static BootstrapRetryRunner retryRunner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneBootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureSceneObjects();
        EnsureRetryRunner();

        if (Object.FindAnyObjectByType<TutorialQuestSystem>() == null)
        {
            new GameObject("Tutorial Quest System").AddComponent<TutorialQuestSystem>();
        }

        if (Object.FindAnyObjectByType<RuntimeGameUI>() == null)
        {
            new GameObject("Runtime Game UI").AddComponent<RuntimeGameUI>();
        }

        if (Object.FindAnyObjectByType<LocalCoopManager>() == null)
        {
            new GameObject("Local Coop Manager").AddComponent<LocalCoopManager>();
        }

        if (Object.FindAnyObjectByType<PauseMenuUI>() == null)
        {
            new GameObject("Pause Menu UI").AddComponent<PauseMenuUI>();
        }

        if (Object.FindAnyObjectByType<CharacterSelectionUI>() == null)
        {
            new GameObject("Character Selection UI").AddComponent<CharacterSelectionUI>();
        }

        if (Object.FindAnyObjectByType<StartMenuUI>() == null)
        {
            new GameObject("Start Menu UI").AddComponent<StartMenuUI>();
        }

        GameSaveSystem.CaptureInitialDefaults();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSceneObjects();
        EnsureRetryRunner();
    }

    private static void EnsureSceneObjects()
    {
        RunBootstrapStep("UMA runtime", () => UmaCharacterFactory.EnsureRuntime());
        RunBootstrapStep("day night cycle", DayNightCycleBootstrapper.EnsureDayNightCycle);
        RunBootstrapStep("default search areas", DefaultSearchAreaBootstrapper.EnsureDefaultSearchAreas);
        RunBootstrapStep("player home", PlayerHomeBootstrapper.EnsurePlayerHome);
        RunBootstrapStep("trader NPCs", TraderBootstrapper.EnsureTraderAtHome);
        RunBootstrapStep("quest NPC", QuestGiverBootstrapper.EnsureQuestGiverAtHome);
        RunBootstrapStep("scanner", EnsureScanner);
        RunBootstrapStep("detector visual", EnsureDetectorVisual);
        RunBootstrapStep("local player avatar", EnsureLocalPlayerAvatarVisual);
        RunBootstrapStep("event system", EnsureEventSystem);
        RunBootstrapStep("sand appearance", EnsureSandAppearance);
        RunBootstrapStep("island environment", EnvironmentScatterBootstrapper.EnsureIslandEnvironment);
    }

    private static void RunBootstrapStep(string stepName, Action step)
    {
        try
        {
            step?.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogError("Game bootstrap step failed: " + stepName);
            Debug.LogException(exception);
        }
    }

    private static void EnsureRetryRunner()
    {
        if (retryRunner == null)
        {
            retryRunner = Object.FindAnyObjectByType<BootstrapRetryRunner>();
        }

        if (retryRunner == null)
        {
            GameObject runnerObject = new GameObject("Game Bootstrapper Runner");
            retryRunner = runnerObject.AddComponent<BootstrapRetryRunner>();
            Object.DontDestroyOnLoad(runnerObject);
        }

        retryRunner.Restart();
    }

    private class BootstrapRetryRunner : MonoBehaviour
    {
        private const float RetryDuration = 6f;
        private const float RetryInterval = 0.5f;
        private float stopTime;
        private float nextRunTime;

        public void Restart()
        {
            stopTime = Time.unscaledTime + RetryDuration;
            nextRunTime = 0f;
            enabled = true;
        }

        private void Update()
        {
            if (Time.unscaledTime >= stopTime)
            {
                enabled = false;
                return;
            }

            if (Time.unscaledTime < nextRunTime)
            {
                return;
            }

            nextRunTime = Time.unscaledTime + RetryInterval;
            EnsureSceneObjects();
        }
    }

    private static void EnsureScanner()
    {
        MetalDetector metalDetector = PlayerRigReferences.FindLocalMetalDetector();

        if (metalDetector == null)
        {
            return;
        }

        EnsureDetectorVisual();

        DetectorBattery battery = metalDetector.GetComponent<DetectorBattery>();

        if (battery == null)
        {
            battery = metalDetector.gameObject.AddComponent<DetectorBattery>();
        }

        GroundScanner scanner = metalDetector.GetComponent<GroundScanner>();

        if (scanner == null)
        {
            scanner = Object.FindAnyObjectByType<GroundScanner>();
        }

        if (scanner == null)
        {
            scanner = metalDetector.gameObject.AddComponent<GroundScanner>();
        }

        scanner.scanOrigin = metalDetector.detectorHead != null ? metalDetector.detectorHead : metalDetector.transform;
        scanner.detectorBattery = battery;
    }

    private static void EnsureDetectorVisual()
    {
        MetalDetector metalDetector = PlayerRigReferences.FindLocalMetalDetector();

        if (metalDetector == null || metalDetector.GetComponent<DetectorVisualBuilder>() != null)
        {
            return;
        }

        DetectorVisualBuilder visualBuilder = metalDetector.gameObject.AddComponent<DetectorVisualBuilder>();
        visualBuilder.detectorHead = metalDetector.detectorHead;
    }

    private static void EnsureLocalPlayerAvatarVisual()
    {
        FirstPersonController controller = Object.FindAnyObjectByType<FirstPersonController>();

        if (controller == null || controller.GetComponent<LocalPlayerAvatarVisual>() != null)
        {
            return;
        }

        controller.gameObject.AddComponent<LocalPlayerAvatarVisual>();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private static void EnsureSandAppearance()
    {
        if (Object.FindAnyObjectByType<SandGroundAppearance>() != null)
        {
            return;
        }

        new GameObject("Sand Ground Appearance").AddComponent<SandGroundAppearance>();
    }
}

public static class PlayerRigReferences
{
    public static Transform FindLocalPlayerTransform()
    {
        FirstPersonController controller = Object.FindAnyObjectByType<FirstPersonController>();

        if (controller != null)
        {
            return controller.transform;
        }

        PlayerInventory inventory = Object.FindAnyObjectByType<PlayerInventory>();
        return inventory != null ? inventory.transform : null;
    }

    public static PlayerInventory FindLocalInventory()
    {
        Transform player = FindLocalPlayerTransform();

        if (player != null && player.TryGetComponent(out PlayerInventory inventory))
        {
            return inventory;
        }

        return Object.FindAnyObjectByType<PlayerInventory>();
    }

    public static MetalDetector FindLocalMetalDetector()
    {
        Transform player = FindLocalPlayerTransform();

        if (player != null)
        {
            MetalDetector detectorInPlayer = player.GetComponentInChildren<MetalDetector>(true);

            if (detectorInPlayer != null)
            {
                return detectorInPlayer;
            }
        }

        FirstPersonController controller = Object.FindAnyObjectByType<FirstPersonController>();

        if (controller != null && controller.playerCamera != null)
        {
            MetalDetector detectorInCamera = controller.playerCamera.GetComponentInChildren<MetalDetector>(true);

            if (detectorInCamera != null)
            {
                return detectorInCamera;
            }
        }

        return Object.FindAnyObjectByType<MetalDetector>();
    }

    public static DetectorBattery FindLocalDetectorBattery()
    {
        MetalDetector detector = FindLocalMetalDetector();

        if (detector != null && detector.TryGetComponent(out DetectorBattery battery))
        {
            return battery;
        }

        return Object.FindAnyObjectByType<DetectorBattery>();
    }
}
