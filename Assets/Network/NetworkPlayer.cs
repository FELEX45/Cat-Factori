using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Сетевой игрок: движение (владелец), ник, nameplate, чат RPC.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayer : NetworkBehaviour
{
    /// <summary>XZ спавна; Y берётся от пола FactoryHall (иначе запасной).</summary>
    public static readonly Vector3 SpawnBase = new Vector3(-0.67f, 10.5f, -5.13f);

    [SerializeField] move movement;

    public readonly NetworkVariable<FixedString64Bytes> Nickname = new NetworkVariable<FixedString64Bytes>(
        new FixedString64Bytes("Игрок"),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    PlayerNameplate _nameplate;
    NetworkTransform _netTransform;
    public static NetworkPlayer LocalPlayer { get; private set; }

    const int MaxMessageLength = 120;
    const int MaxLogLines = 40;
    static readonly List<string> ChatLog = new List<string>();

    public override void OnNetworkSpawn()
    {
        if (movement == null)
            movement = GetComponent<move>();
        _netTransform = GetComponent<NetworkTransform>();

        Nickname.OnValueChanged += OnNicknameChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (IsOwner)
        {
            LocalPlayer = this;
            Nickname.Value = new FixedString64Bytes(PlayerProfile.Nickname);
            ChatHud.EnsureExists().Bind(this);
        }
        else if (movement != null)
        {
            movement.enabled = false;
            SetLayerRecursively(transform, 0);
        }

        EnsureNameplate();

        if (IsInGameScene())
            PlaceInGameScene();
    }

    public override void OnNetworkDespawn()
    {
        Nickname.OnValueChanged -= OnNicknameChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (IsOwner)
        {
            if (LocalPlayer == this)
                LocalPlayer = null;
            if (movement != null)
                movement.enabled = false;
        }
    }

    public override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (LocalPlayer == this)
            LocalPlayer = null;
        base.OnDestroy();
    }

    void OnNicknameChanged(FixedString64Bytes previous, FixedString64Bytes current)
    {
        if (_nameplate != null)
            _nameplate.SetNickname(current.ToString());
    }

    void EnsureNameplate()
    {
        if (_nameplate != null)
            return;

        // Свой ник себе не показываем — только другим игрокам
        bool showToLocalViewer = !IsOwner;
        _nameplate = PlayerNameplate.Create(transform, Nickname.Value.ToString(), showToLocalViewer);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsSpawned)
            return;
        if (scene.name != LobbySessionManager.GameSceneName)
            return;
        PlaceInGameScene();
    }

    void PlaceInGameScene()
    {
        // С Owner-authority позицию выставляет владелец (и она синхронизируется остальным)
        if (IsOwner)
            SetupLocalPlayer();
        else if (IsServer)
            StartCoroutine(ServerEnsureSpawnNextFrame());
    }

    /// <summary>
    /// Если клиент ещё не успел телепортнуться — сервер подстраховывает через кадр.
    /// При Owner-authority Teleport на сервере может не примениться; тогда ждём владельца.
    /// </summary>
    System.Collections.IEnumerator ServerEnsureSpawnNextFrame()
    {
        yield return null;
        if (!IsSpawned || IsOwner)
            yield break;
        // Если игрок всё ещё под полом — форсим позицию (работает при server authority;
        // при owner authority владелец уже должен был телепортнуться)
        if (transform.position.y < 5f)
            TeleportToSpawn();
    }

    void SetupLocalPlayer()
    {
        StartCoroutine(OwnerSpawnRoutine());
    }

    System.Collections.IEnumerator OwnerSpawnRoutine()
    {
        // Даём NetworkTransform инициализироваться после спавна/смены сцены
        yield return null;
        TeleportToSpawn();
        if (movement != null)
        {
            movement.enabled = true;
            movement.BindSceneCamera();
        }
        Debug.Log($"[Network] Local player «{Nickname.Value}» в {SceneManager.GetActiveScene().name} @ {transform.position}");
    }

    public static Vector3 GetSpawnPosition(ulong clientId)
    {
        float y = SpawnBase.y;
        var hall = FindAnyObjectByType<FactoryHall>();
        Vector3 origin = hall != null ? hall.transform.position : SpawnBase;
        if (hall != null)
            y = hall.transform.position.y + 0.2f;

        // Чётные — сторона КБ (слева), нечётные — цех (справа)
        bool kbSide = (clientId % 2UL) == 0UL;
        float sideX = kbSide ? -12f : 12f;
        float lane = (clientId / 2UL) * 2f;
        return new Vector3(origin.x + sideX + (kbSide ? lane : -lane), y, origin.z);
    }

    void TeleportToSpawn()
    {
        Vector3 pos = GetSpawnPosition(OwnerClientId);
        var cc = GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        if (_netTransform == null)
            _netTransform = GetComponent<NetworkTransform>();

        if (_netTransform != null)
            _netTransform.Teleport(pos, Quaternion.identity, Vector3.one);
        else
            transform.SetPositionAndRotation(pos, Quaternion.identity);

        if (cc != null)
            cc.enabled = true;
    }

    public void SubmitChat(string text)
    {
        if (!IsOwner || !IsSpawned)
            return;
        text = SanitizeChat(text);
        if (string.IsNullOrEmpty(text))
            return;
        SubmitChatServerRpc(Nickname.Value, new FixedString512Bytes(text));
    }

    [ServerRpc]
    void SubmitChatServerRpc(FixedString64Bytes nickname, FixedString512Bytes message)
    {
        BroadcastChatClientRpc(nickname, message);
    }

    [ClientRpc]
    void BroadcastChatClientRpc(FixedString64Bytes nickname, FixedString512Bytes message)
    {
        string line = $"[{nickname}] {message}";
        ChatLog.Add(line);
        while (ChatLog.Count > MaxLogLines)
            ChatLog.RemoveAt(0);
        ChatHud.EnsureExists().RefreshLog(ChatLog);
    }

    public static IReadOnlyList<string> GetChatLog() => ChatLog;

    public static string SanitizeChat(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";
        text = text.Trim();
        if (text.Length > MaxMessageLength)
            text = text.Substring(0, MaxMessageLength);
        return text;
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
