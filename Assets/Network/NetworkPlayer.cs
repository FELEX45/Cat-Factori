using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Сетевой игрок: управление только у владельца, чужие модели видны.
/// Камера перепривязывается после загрузки SampleScene (спавн происходит ещё в MainMenu).
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayer : NetworkBehaviour
{
    public static readonly Vector3 SpawnBase = new Vector3(-0.67f, 14.8f, -5.13f);

    [SerializeField] move movement;

    public override void OnNetworkSpawn()
    {
        if (movement == null)
            movement = GetComponent<move>();

        if (IsOwner)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (IsInGameScene())
                SetupLocalPlayer();
            else if (movement != null)
                movement.enabled = false;
        }
        else
        {
            if (movement != null)
                movement.enabled = false;
            SetLayerRecursively(transform, 0);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            SceneManager.sceneLoaded -= OnSceneLoaded;

        if (IsOwner && movement != null)
            movement.enabled = false;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsOwner || !IsSpawned)
            return;
        if (scene.name != LobbySessionManager.GameSceneName)
            return;

        SetupLocalPlayer();
    }

    void SetupLocalPlayer()
    {
        TeleportToSpawn();

        if (movement != null)
        {
            movement.enabled = true;
            // На следующий кадр — камера SampleScene точно существует, Start() уже мог отработать
            movement.StartCoroutine(BindCameraNextFrame());
        }

        Debug.Log($"[Network] Local player готов в {SceneManager.GetActiveScene().name} @ {transform.position}");
    }

    System.Collections.IEnumerator BindCameraNextFrame()
    {
        yield return null;
        if (movement != null)
            movement.BindSceneCamera();
    }

    void TeleportToSpawn()
    {
        Vector3 pos = SpawnBase + new Vector3(OwnerClientId * 2f, 0f, 0f);
        var cc = GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;
        transform.SetPositionAndRotation(pos, Quaternion.identity);
        if (cc != null)
            cc.enabled = true;
    }

    static bool IsInGameScene()
    {
        return SceneManager.GetActiveScene().name == LobbySessionManager.GameSceneName;
    }

    static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }
}
