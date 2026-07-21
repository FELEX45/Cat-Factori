using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// ESC-меню в игре. Весь UI (включая затемнение) прячется при закрытии.
/// Настройки — те же, что в Main Menu (PlayerProfile + SharedSettingsForm).
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }
    public bool IsOpen { get; private set; }

    const string MainMenuScene = "MainMenu";

    Font _font;
    Sprite _sprite;
    GameObject _root;
    GameObject _mainPanel;
    GameObject _settingsPanel;
    SharedSettingsForm _settings;
    bool _busy;

    static readonly Color Bg = new Color(0.04f, 0.05f, 0.05f, 0.72f);
    static readonly Color Panel = new Color(0.10f, 0.14f, 0.12f, 0.96f);
    static readonly Color Accent = new Color(0.45f, 0.78f, 0.42f, 1f);
    static readonly Color Btn = new Color(0.16f, 0.22f, 0.18f, 1f);
    static readonly Color BtnHover = new Color(0.22f, 0.32f, 0.24f, 1f);
    static readonly Color BtnPress = new Color(0.12f, 0.18f, 0.14f, 1f);
    static readonly Color TextPrimary = new Color(0.92f, 0.95f, 0.90f, 1f);
    static readonly Color TextMuted = new Color(0.62f, 0.70f, 0.60f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void HookScenes()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == LobbySessionManager.GameSceneName)
            EnsureExists();
        else if (scene.name == MainMenuScene && Instance != null)
            Destroy(Instance.gameObject);
    }

    public static PauseMenu EnsureExists()
    {
        if (Instance != null)
            return Instance;
        var go = new GameObject("PauseMenu");
        DontDestroyOnLoad(go);
        return go.AddComponent<PauseMenu>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);

        EnsureEventSystem();
        BuildUi();
        // Важно: после BuildUi — всё под _root, скрываем целиком
        SetOpen(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name != LobbySessionManager.GameSceneName)
            return;
        if (_busy)
            return;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame)
            return;
#else
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;
#endif

        if (ChatHud.Instance != null && ChatHud.Instance.IsOpen)
        {
            ChatHud.Instance.SetOpen(false);
            return;
        }

        if (IsOpen && _settingsPanel != null && _settingsPanel.activeSelf)
        {
            ShowMainPause();
            return;
        }

        SetOpen(!IsOpen);
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        if (_root != null)
            _root.SetActive(open);

        if (open)
        {
            ShowMainPause();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (!GameSessionMode.IsOnline)
                Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void ShowMainPause()
    {
        if (_mainPanel != null)
            _mainPanel.SetActive(true);
        if (_settingsPanel != null)
            _settingsPanel.SetActive(false);
    }

    void ShowSettings()
    {
        _settings?.LoadFromProfile();
        if (_mainPanel != null)
            _mainPanel.SetActive(false);
        if (_settingsPanel != null)
            _settingsPanel.SetActive(true);
    }

    void OnContinue() => SetOpen(false);

    void OnExitToMenu()
    {
        if (!_busy)
            StartCoroutine(ExitToMenuRoutine());
    }

    IEnumerator ExitToMenuRoutine()
    {
        _busy = true;
        SetOpen(false);
        Time.timeScale = 1f;

        if (GameSessionMode.IsOnline && LobbySessionManager.Instance != null)
        {
            var leave = LobbySessionManager.Instance.LeaveAsync();
            while (!leave.IsCompleted)
                yield return null;
        }

        if (ChatHud.Instance != null)
            Destroy(ChatHud.Instance.gameObject);

        SceneManager.LoadScene(MainMenuScene);
        if (Instance != null)
            Destroy(Instance.gameObject);
    }

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;
        var es = new GameObject("EventSystem");
        DontDestroyOnLoad(es);
        es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    void BuildUi()
    {
        var canvasGo = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Всё меню — дети _root, чтобы SetActive(false) скрывало и затемнение, и панели
        _root = new GameObject("PauseRoot", typeof(RectTransform), typeof(Image));
        _root.transform.SetParent(canvasGo.transform, false);
        var rootImg = _root.GetComponent<Image>();
        rootImg.sprite = _sprite;
        rootImg.color = Bg;
        rootImg.raycastTarget = true;
        StretchFull(_root.GetComponent<RectTransform>());

        _mainPanel = CreateCenteredPanel(_root.transform, "MainPause");
        BuildMain(_mainPanel.transform);

        _settingsPanel = CreateCenteredPanel(_root.transform, "SettingsPause");
        _settings = new SharedSettingsForm(_font, _sprite, TextPrimary, TextMuted, Accent);
        _settings.Build(_settingsPanel.transform, 36);
        CreateButton(_settingsPanel.transform, "Сохранить", () =>
        {
            _settings.SaveToProfile();
            ShowMainPause();
        });
        CreateButton(_settingsPanel.transform, "Назад", () =>
        {
            _settings.SaveToProfile();
            ShowMainPause();
        });
        _settingsPanel.SetActive(false);
    }

    void BuildMain(Transform parent)
    {
        var v = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(36, 36, 36, 36);
        v.spacing = 12f;
        v.childAlignment = TextAnchor.MiddleCenter;
        v.childControlWidth = true;
        v.childControlHeight = false;
        v.childForceExpandWidth = true;
        var fitter = parent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var title = CreateText(parent, "Title", "Пауза", 42, TextPrimary, FontStyle.Bold);
        title.alignment = TextAnchor.MiddleCenter;
        PrefHeight(title.gameObject, 56f);
        Spacer(parent, 16f);
        CreateButton(parent, "Продолжить", OnContinue);
        CreateButton(parent, "Настройки", ShowSettings);
        CreateButton(parent, "Выйти в меню", OnExitToMenu);
    }

    GameObject CreateCenteredPanel(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = _sprite;
        img.color = Panel;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(440f, 0f);
        rt.anchoredPosition = Vector2.zero;
        var fitter = go.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return go;
    }

    void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = _sprite;
        img.color = Color.white;
        var btn = go.GetComponent<Button>();
        var c = btn.colors;
        c.normalColor = Btn;
        c.highlightedColor = BtnHover;
        c.pressedColor = BtnPress;
        c.selectedColor = BtnHover;
        btn.colors = c;
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        PrefHeight(go, 48f);
        var text = CreateText(go.transform, "Label", label, 22, TextPrimary, FontStyle.Normal);
        StretchFull(text.rectTransform);
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
    }

    Text CreateText(Transform parent, string name, string content, int size, Color color, FontStyle style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = _font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        return text;
    }

    void Spacer(Transform parent, float h)
    {
        var go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        PrefHeight(go, h);
    }

    static void PrefHeight(GameObject go, float h)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredHeight = h;
        le.minHeight = h;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
