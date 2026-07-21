using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Создаёт и настраивает NetworkManager + Unity Transport для Sessions/Relay.
/// </summary>
public static class GameNetworkBootstrap
{
    public const string PlayerPrefabResource = "NetworkPlayer";

    public static void EnsureNetworkManager()
    {
        if (NetworkManager.Singleton != null)
        {
            EnsureConfig(NetworkManager.Singleton);
            EnsurePlayerPrefab(NetworkManager.Singleton);
            return;
        }

        var go = new GameObject("GameNetworkManager");
        Object.DontDestroyOnLoad(go);

        var transport = go.AddComponent<UnityTransport>();
        var nm = go.AddComponent<NetworkManager>();

        EnsureConfig(nm);
        nm.NetworkConfig.NetworkTransport = transport;
        nm.NetworkConfig.EnableSceneManagement = true;
        nm.NetworkConfig.ConnectionApproval = false;

        EnsurePlayerPrefab(nm);
        Debug.Log("[Network] NetworkManager создан");
    }

    static void EnsureConfig(NetworkManager nm)
    {
        if (nm.NetworkConfig == null)
            nm.NetworkConfig = new NetworkConfig();
    }

    static void EnsurePlayerPrefab(NetworkManager nm)
    {
        EnsureConfig(nm);

        var prefab = Resources.Load<GameObject>(PlayerPrefabResource);
        if (prefab == null)
        {
            Debug.LogError(
                $"[Network] Не найден Resources/{PlayerPrefabResource}.prefab. " +
                "В Unity: Tools → Cat Factori → Setup Network Player Prefab");
            return;
        }

        if (prefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("[Network] NetworkPlayer prefab без NetworkObject");
            return;
        }

        nm.NetworkConfig.PlayerPrefab = prefab;

        try
        {
            nm.AddNetworkPrefab(prefab);
        }
        catch (System.Exception)
        {
            // Уже зарегистрирован
        }
    }
}
