using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class GameBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        DayNightCycleBootstrapper.EnsureDayNightCycle();
        DefaultSearchAreaBootstrapper.EnsureDefaultSearchAreas();
        PlayerHomeBootstrapper.EnsurePlayerHome();
        TraderBootstrapper.EnsureTraderAtHome();
        EnsureScanner();
        EnsureDetectorVisual();
        EnsureLocalPlayerAvatarVisual();
        EnsureEventSystem();
        EnsureSandAppearance();
        EnvironmentScatterBootstrapper.EnsureIslandEnvironment();

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
    }

    private static void EnsureScanner()
    {
        if (Object.FindAnyObjectByType<GroundScanner>() != null)
        {
            return;
        }

        MetalDetector metalDetector = Object.FindAnyObjectByType<MetalDetector>();

        if (metalDetector == null)
        {
            return;
        }

        DetectorBattery battery = metalDetector.GetComponent<DetectorBattery>();

        if (battery == null)
        {
            battery = metalDetector.gameObject.AddComponent<DetectorBattery>();
        }

        GroundScanner scanner = metalDetector.gameObject.AddComponent<GroundScanner>();
        scanner.scanOrigin = metalDetector.detectorHead != null ? metalDetector.detectorHead : metalDetector.transform;
        scanner.detectorBattery = battery;

        EnsureDetectorVisual();
    }

    private static void EnsureDetectorVisual()
    {
        MetalDetector metalDetector = Object.FindAnyObjectByType<MetalDetector>();

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
