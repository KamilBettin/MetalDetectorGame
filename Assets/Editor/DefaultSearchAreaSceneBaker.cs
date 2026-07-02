using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DefaultSearchAreaSceneBaker
{
    private const string TargetScenePath = "Assets/Scenes/SampleScene.unity";
    private const string BasicAreaRootName = "Search Area - Basic Ground";

    static DefaultSearchAreaSceneBaker()
    {
        EditorApplication.delayCall += BakeDefaultSearchAreaIntoActiveSceneIfNeeded;
    }

    [MenuItem("Tools/Metal Detector/Bake Default Search Area Into Scene")]
    public static void BakeDefaultSearchAreaIntoScene()
    {
        DefaultSearchAreaBootstrapper.EnsureDefaultSearchAreas();
        DefaultSearchAreaBootstrapper.ConvertExistingSignTextsToTmp();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Baked default search area into the open scene.");
    }

    [MenuItem("Tools/Metal Detector/Convert Sign Texts To TMP")]
    public static void ConvertSignTextsToTmpInScene()
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        if (!scene.IsValid() || scene.path != TargetScenePath)
        {
            scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        }

        int convertedCount = DefaultSearchAreaBootstrapper.ConvertExistingSignTextsToTmp();

        if (convertedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log("Converted " + convertedCount + " sign text(s) to TextMeshPro.");
    }

    private static void BakeDefaultSearchAreaIntoActiveSceneIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene activeScene = EditorSceneManager.GetActiveScene();

        if (!activeScene.IsValid() || activeScene.path != TargetScenePath)
        {
            return;
        }

        int convertedCount = DefaultSearchAreaBootstrapper.ConvertExistingSignTextsToTmp();

        if (convertedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
        }

        if (GameObject.Find(BasicAreaRootName) != null)
        {
            return;
        }

        BakeDefaultSearchAreaIntoScene();
    }
}
