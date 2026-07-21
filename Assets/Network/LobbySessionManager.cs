using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Создание и подключение к онлайн-лобби (Sessions + Relay + Netcode).
/// </summary>
public class LobbySessionManager : MonoBehaviour
{
    public const int MinPasswordLength = 8;
    public const int DefaultMaxPlayers = 8;
    public const string GameSceneName = "SampleScene";

    public static LobbySessionManager Instance { get; private set; }

    public ISession CurrentSession { get; private set; }
    public bool IsBusy { get; private set; }
    public string StatusMessage { get; private set; } = "";

    public event Action<string> StatusChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static LobbySessionManager EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject("LobbySessionManager");
        return go.AddComponent<LobbySessionManager>();
    }

    void SetStatus(string message)
    {
        StatusMessage = message ?? "";
        StatusChanged?.Invoke(StatusMessage);
        if (!string.IsNullOrEmpty(StatusMessage))
            Debug.Log($"[Lobby] {StatusMessage}");
    }

    public static string ValidateLobbyName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Введите название лобби";
        name = name.Trim();
        if (name.Length < 2)
            return "Название слишком короткое (мин. 2 символа)";
        if (name.Length > 50)
            return "Название слишком длинное (макс. 50)";
        return null;
    }

    public static string ValidatePassword(string password, bool requiredIfNonEmpty = true)
    {
        if (string.IsNullOrEmpty(password))
            return null;
        if (password.Length < MinPasswordLength)
            return $"Пароль: минимум {MinPasswordLength} символов (или оставь пустым)";
        if (password.Length > 64)
            return "Пароль: максимум 64 символа";
        return null;
    }

    public async Task<bool> CreateLobbyAsync(string lobbyName, string password)
    {
        if (IsBusy)
            return false;

        string nameError = ValidateLobbyName(lobbyName);
        if (nameError != null)
        {
            SetStatus(nameError);
            return false;
        }

        string passError = ValidatePassword(password);
        if (passError != null)
        {
            SetStatus(passError);
            return false;
        }

        IsBusy = true;
        try
        {
            SetStatus("Вход в Unity Services…");
            await UnityServicesBootstrap.EnsureSignedInAsync();

            SetStatus("Подготовка сети…");
            GameNetworkBootstrap.EnsureNetworkManager();

            lobbyName = lobbyName.Trim();
            string pass = string.IsNullOrEmpty(password) ? null : password;

            SetStatus($"Создание лобби «{lobbyName}»…");
            var options = new SessionOptions
            {
                Name = lobbyName,
                MaxPlayers = DefaultMaxPlayers,
                IsPrivate = false,
                Password = pass
            }.WithRelayNetwork();

            CurrentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            GameSessionMode.SetOnline();

            SetStatus($"Лобби «{lobbyName}» создано. Загрузка игры…");
            await LoadGameSceneAsHostAsync();
            SetStatus($"Хост лобби «{lobbyName}»");
            return true;
        }
        catch (Exception ex)
        {
            SetStatus("Ошибка: " + UnityServicesBootstrap.DescribeError(ex));
            Debug.LogException(ex);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> JoinLobbyByNameAsync(string lobbyName, string password)
    {
        if (IsBusy)
            return false;

        string nameError = ValidateLobbyName(lobbyName);
        if (nameError != null)
        {
            SetStatus(nameError);
            return false;
        }

        string passError = ValidatePassword(password);
        if (passError != null)
        {
            SetStatus(passError);
            return false;
        }

        IsBusy = true;
        try
        {
            SetStatus("Вход в Unity Services…");
            await UnityServicesBootstrap.EnsureSignedInAsync();

            SetStatus("Подготовка сети…");
            GameNetworkBootstrap.EnsureNetworkManager();

            lobbyName = lobbyName.Trim();
            string pass = string.IsNullOrEmpty(password) ? null : password;

            SetStatus($"Поиск лобби «{lobbyName}»…");
            var query = new QuerySessionsOptions
            {
                Count = 20,
                FilterOptions = new List<FilterOption>
                {
                    new FilterOption(FilterField.Name, lobbyName, FilterOperation.Equal)
                }
            };

            QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(query);
            if (results?.Sessions == null || results.Sessions.Count == 0)
            {
                SetStatus($"Лобби «{lobbyName}» не найдено");
                return false;
            }

            ISessionInfo info = null;
            foreach (var s in results.Sessions)
            {
                if (string.Equals(s.Name, lobbyName, StringComparison.Ordinal))
                {
                    info = s;
                    break;
                }
            }

            if (info == null)
                info = results.Sessions[0];

            SetStatus($"Подключение к «{info.Name}»…");
            var joinOptions = new JoinSessionOptions
            {
                Password = pass
            };

            CurrentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(info.Id, joinOptions);
            GameSessionMode.SetOnline();

            SetStatus("Подключено. Ожидание сцены хоста…");
            // Клиент подтянет сцену через Netcode SceneManager, если хост уже в игре.
            // Если ещё на меню — ждём смену сцены; дополнительно подстрахуемся локальной загрузкой при необходимости.
            await WaitForGameSceneOrLoadAsync();
            SetStatus($"В лобби «{info.Name}»");
            return true;
        }
        catch (Exception ex)
        {
            SetStatus("Ошибка: " + UnityServicesBootstrap.DescribeError(ex));
            Debug.LogException(ex);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    async Task LoadGameSceneAsHostAsync()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening)
            throw new InvalidOperationException("NetworkManager не запущен как Host");

        if (nm.IsServer)
        {
            var status = nm.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started
                && status != SceneEventProgressStatus.SceneEventInProgress)
            {
                Debug.LogWarning($"[Lobby] SceneManager status={status}, fallback LoadScene");
                SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            }
        }

        float timeout = 15f;
        while (timeout > 0f && SceneManager.GetActiveScene().name != GameSceneName)
        {
            timeout -= Time.unscaledDeltaTime;
            await Task.Yield();
        }
    }

    async Task WaitForGameSceneOrLoadAsync()
    {
        float timeout = 20f;
        while (timeout > 0f && SceneManager.GetActiveScene().name != GameSceneName)
        {
            timeout -= Time.unscaledDeltaTime;
            await Task.Yield();
        }

        if (SceneManager.GetActiveScene().name != GameSceneName)
        {
            Debug.LogWarning("[Lobby] Сцена хоста не пришла вовремя — локальная загрузка SampleScene");
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
        }
    }

    public async Task LeaveAsync()
    {
        try
        {
            if (CurrentSession != null)
            {
                await CurrentSession.LeaveAsync();
                CurrentSession = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Lobby] Leave: " + ex.Message);
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        GameSessionMode.SetOffline();
        SetStatus("");
    }
}
