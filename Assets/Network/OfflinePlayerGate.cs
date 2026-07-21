using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// В онлайн-режиме отключает офлайн-PlayerModel из SampleScene.
/// </summary>
public static class OfflinePlayerGate
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnSceneLoaded()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        HideIfNeeded(SceneManager.GetActiveScene());
    }

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HideIfNeeded(scene);
    }

    static void HideIfNeeded(Scene scene)
    {
        if (!GameSessionMode.IsOnline)
            return;
        if (scene.name != LobbySessionManager.GameSceneName)
            return;

        // Убрать UI главного меню, если сцена меню ещё висит
        var menuCanvas = GameObject.Find("MainMenuCanvas");
        if (menuCanvas != null)
            Object.Destroy(menuCanvas);

        var player = GameObject.Find("PlayerModel");
        if (player != null)
        {
            player.SetActive(false);
            Debug.Log("[Network] Offline PlayerModel отключён (онлайн-сессия) — используется сетевой NetworkPlayer (та же модель)");
        }
    }
}
