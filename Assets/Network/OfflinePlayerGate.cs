using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// В онлайн-режиме отключает офлайн-PlayerModel из MainScene,
/// но только когда сетевой игрок реально есть / сеть слушает.
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
        if (scene.name != LobbySessionManager.GameSceneName)
            return;

        var menuCanvas = GameObject.Find("MainMenuCanvas");
        if (menuCanvas != null)
            Object.Destroy(menuCanvas);

        var player = GameObject.Find("PlayerModel");
        if (player == null)
            return;

        bool networkLive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        bool hasLocalNetPlayer = NetworkPlayer.LocalPlayer != null;

        // Флаг Online мог остаться после прошлой сессии (Domain Reload off) —
        // не гасим PlayerModel, пока сети реально нет.
        if (GameSessionMode.IsOnline && (networkLive || hasLocalNetPlayer))
        {
            if (player.activeSelf)
            {
                player.SetActive(false);
                Debug.Log("[Network] Offline PlayerModel отключён — используется NetworkPlayer");
            }
        }
        else
        {
            if (!GameSessionMode.IsOnline || !networkLive)
                GameSessionMode.SetOffline();

            if (!player.activeSelf)
            {
                player.SetActive(true);
                Debug.Log("[Bootstrap] Offline PlayerModel включён обратно");
            }
        }
    }
}
