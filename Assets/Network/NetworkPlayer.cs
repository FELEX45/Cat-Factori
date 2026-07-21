using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Сетевой игрок: движение (владелец), ник, nameplate, чат RPC.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayer : NetworkBehaviour
{
    public static readonly Vector3 SpawnBase = new Vector3(-0.67f, 14.8f, -5.13f);

    [SerializeField] move movement;

    public readonly NetworkVariable<FixedString64Bytes> Nickname = new NetworkVariable<FixedString64Bytes>(
        new FixedString64Bytes("Игрок"),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    PlayerNameplate _nameplate;
    public static NetworkPlayer LocalPlayer { get; private set; }

    const int MaxMessageLength = 120;
    const int MaxLogLines = 40;
    static readonly List<string> ChatLog = new List<string>();

    public override void OnNetworkSpawn()
    {
        if (movement == null)
            movement = GetComponent<move>();

        Nickname.OnValueChanged += OnNicknameChanged;

        if (IsOwner)
        {
            LocalPlayer = this;
            Nickname.Value = new FixedString64Bytes(PlayerProfile.Nickname);
            ChatHud.EnsureExists().Bind(this);
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

        EnsureNameplate();
    }

    public override void OnNetworkDespawn()
    {
        Nickname.OnValueChanged -= OnNicknameChanged;

        if (IsOwner)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (LocalPlayer == this)
                LocalPlayer = null;
        }

        if (IsOwner && movement != null)
            movement.enabled = false;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (LocalPlayer == this)
            LocalPlayer = null;
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
            movement.StartCoroutine(BindCameraNextFrame());
        }
        Debug.Log($"[Network] Local player «{Nickname.Value}» в {SceneManager.GetActiveScene().name}");
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
