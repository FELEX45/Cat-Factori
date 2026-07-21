#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// MainMenu первой в Build Settings; Play всегда стартует с MainMenu (одна сцена).
/// </summary>
[InitializeOnLoad]
static class MainMenuBuildSettings
{
    const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    const string GameScenePath = "Assets/Scenes/SampleScene.unity";

    static MainMenuBuildSettings()
    {
        EditorApplication.delayCall += Ensure;
    }

    static void Ensure()
    {
        var scenes = EditorBuildSettings.scenes;
        bool hasMenu = false;
        bool hasGame = false;
        bool needsRewrite = scenes.Length == 0;

        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path == MainMenuPath)
            {
                hasMenu = true;
                if (!scenes[i].enabled || i != 0)
                    needsRewrite = true;
            }
            if (scenes[i].path == GameScenePath)
            {
                hasGame = true;
                if (!scenes[i].enabled)
                    needsRewrite = true;
            }
        }

        if (!hasMenu || !hasGame)
            needsRewrite = true;

        if (needsRewrite)
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
        }

        // Иначе при открытых MainMenu + SampleScene в Hierarchy оба грузятся → 2 AudioListener
        var startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
        if (startScene != null && EditorSceneManager.playModeStartScene != startScene)
            EditorSceneManager.playModeStartScene = startScene;
    }
}
#endif
