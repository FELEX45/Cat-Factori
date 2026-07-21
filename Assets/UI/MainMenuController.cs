using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Главное меню при входе в игру. Строит UI в рантайме.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    const string GameScenePath = "Assets/Scenes/SampleScene.unity";

    [SerializeField] string gameTitle = "Cat Factori";
    [SerializeField] string developerName = "FELEX45";
    [SerializeField] string gameSceneName = "SampleScene";

    Font _font;
    Sprite _uiSprite;
    GameObject _mainPanel;
    GameObject _settingsPanel;
    GameObject _joinPanel;
    GameObject _createPanel;

    static readonly Color BgDark = new Color(0.06f, 0.08f, 0.07f, 0.96f);
    static readonly Color PanelBg = new Color(0.10f, 0.14f, 0.12f, 0.98f);
    static readonly Color Accent = new Color(0.45f, 0.78f, 0.42f, 1f);
    static readonly Color ButtonNormal = new Color(0.16f, 0.22f, 0.18f, 1f);
    static readonly Color ButtonHover = new Color(0.22f, 0.32f, 0.24f, 1f);
    static readonly Color ButtonPressed = new Color(0.12f, 0.18f, 0.14f, 1f);
    static readonly Color TextPrimary = new Color(0.92f, 0.95f, 0.90f, 1f);
    static readonly Color TextMuted = new Color(0.62f, 0.70f, 0.60f, 1f);

    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        _uiSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            100f);

        EnsureEventSystem();
        BuildUI();
        ShowMain();
    }

    void EnsureEventSystem()
    {
        var existing = FindAnyObjectByType<EventSystem>();
        if (existing != null)
        {
#if ENABLE_INPUT_SYSTEM
            var module = existing.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                Destroy(existing.GetComponent<BaseInputModule>());
                module = existing.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            module.AssignDefaultActions();
#endif
            return;
        }

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        var uiModule = es.AddComponent<InputSystemUIInputModule>();
        uiModule.AssignDefaultActions();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Фон
        var bg = CreateImage(canvasGo.transform, "Background", BgDark);
        StretchFull(bg.rectTransform);

        // Лёгкий акцент снизу
        var accentBar = CreateImage(canvasGo.transform, "AccentBar", Accent);
        var abRt = accentBar.rectTransform;
        abRt.anchorMin = new Vector2(0f, 0f);
        abRt.anchorMax = new Vector2(1f, 0f);
        abRt.pivot = new Vector2(0.5f, 0f);
        abRt.sizeDelta = new Vector2(0f, 4f);
        abRt.anchoredPosition = Vector2.zero;

        _mainPanel = CreatePanel(canvasGo.transform, "MainPanel");
        BuildMainPanel(_mainPanel.transform);

        _createPanel = CreatePanel(canvasGo.transform, "CreateLobbyPanel");
        BuildPlaceholderPanel(_createPanel.transform, "Создать лобби",
            "Лобби будет доступно после подключения сети.\nПока можно сразу перейти в игру.",
            "В игру", OnCreateLobbyConfirm, ShowMain);

        _joinPanel = CreatePanel(canvasGo.transform, "JoinLobbyPanel");
        BuildJoinPanel(_joinPanel.transform);

        _settingsPanel = CreatePanel(canvasGo.transform, "SettingsPanel");
        BuildSettingsPanel(_settingsPanel.transform);
    }

    void BuildMainPanel(Transform parent)
    {
        var layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 18f;
        layout.padding = new RectOffset(40, 40, 40, 40);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = parent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var title = CreateText(parent, "Title", gameTitle, 64, TextPrimary, FontStyle.Bold);
        title.alignment = TextAnchor.MiddleCenter;
        SetPreferredHeight(title.gameObject, 72f);

        CreateSpacer(parent, 28f);

        CreateMenuButton(parent, $"Игра ({developerName})", () => LoadGameScene());
        CreateMenuButton(parent, "Создать лобби", ShowCreate);
        CreateMenuButton(parent, "Подключиться к лобби", ShowJoin);
        CreateMenuButton(parent, "Настройки", ShowSettings);
        CreateMenuButton(parent, "Выход", OnQuit);
    }

    void BuildPlaceholderPanel(Transform parent, string title, string body, string confirmLabel, UnityEngine.Events.UnityAction onConfirm, UnityEngine.Events.UnityAction onBack)
    {
        var layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 16f;
        layout.padding = new RectOffset(48, 48, 48, 48);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        var fitter = parent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var t = CreateText(parent, "Title", title, 40, TextPrimary, FontStyle.Bold);
        t.alignment = TextAnchor.MiddleCenter;
        SetPreferredHeight(t.gameObject, 48f);

        var b = CreateText(parent, "Body", body, 20, TextMuted, FontStyle.Normal);
        b.alignment = TextAnchor.MiddleCenter;
        SetPreferredHeight(b.gameObject, 80f);

        CreateSpacer(parent, 12f);
        CreateMenuButton(parent, confirmLabel, onConfirm);
        CreateMenuButton(parent, "Назад", onBack);
    }

    void BuildJoinPanel(Transform parent)
    {
        var layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 14f;
        layout.padding = new RectOffset(48, 48, 48, 48);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        var fitter = parent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var t = CreateText(parent, "Title", "Подключиться к лобби", 40, TextPrimary, FontStyle.Bold);
        t.alignment = TextAnchor.MiddleCenter;
        SetPreferredHeight(t.gameObject, 48f);

        var hint = CreateText(parent, "Hint", "Код лобби (пока заглушка)", 18, TextMuted, FontStyle.Normal);
        hint.alignment = TextAnchor.MiddleCenter;
        SetPreferredHeight(hint.gameObject, 24f);

        var inputGo = new GameObject("LobbyCode", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputGo.transform.SetParent(parent, false);
        var inputImg = inputGo.GetComponent<Image>();
        inputImg.sprite = _uiSprite;
        inputImg.type = Image.Type.Sliced;
        inputImg.color = new Color(0.08f, 0.10f, 0.09f, 1f);
        SetPreferredHeight(inputGo, 44f);

        var placeholder = CreateText(inputGo.transform, "Placeholder", "Введите код…", 20, new Color(0.45f, 0.5f, 0.45f, 1f), FontStyle.Italic);
        StretchFull(placeholder.rectTransform);
        placeholder.rectTransform.offsetMin = new Vector2(12, 4);
        placeholder.rectTransform.offsetMax = new Vector2(-12, -4);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.raycastTarget = false;

        var inputText = CreateText(inputGo.transform, "Text", "", 20, TextPrimary, FontStyle.Normal);
        StretchFull(inputText.rectTransform);
        inputText.rectTransform.offsetMin = new Vector2(12, 4);
        inputText.rectTransform.offsetMax = new Vector2(-12, -4);
        inputText.alignment = TextAnchor.MiddleLeft;
        inputText.supportRichText = false;

        var field = inputGo.GetComponent<InputField>();
        field.textComponent = inputText;
        field.placeholder = placeholder;
        field.caretColor = Accent;
        field.selectionColor = new Color(Accent.r, Accent.g, Accent.b, 0.35f);

        CreateSpacer(parent, 8f);
        CreateMenuButton(parent, "Подключиться", () =>
        {
            Debug.Log($"[MainMenu] Подключение к лобби: '{field.text}' (заглушка) → загрузка игры");
            LoadGameScene();
        });
        CreateMenuButton(parent, "Назад", ShowMain);
    }

    void BuildSettingsPanel(Transform parent)
    {
        var layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 14f;
        layout.padding = new RectOffset(48, 48, 48, 48);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        var fitter = parent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var t = CreateText(parent, "Title", "Настройки", 40, TextPrimary, FontStyle.Bold);
        t.alignment = TextAnchor.MiddleCenter;
        SetPreferredHeight(t.gameObject, 48f);

        AddSliderRow(parent, "Громкость", 0.8f);
        AddSliderRow(parent, "Чувствительность мыши", 0.5f);

        var note = CreateText(parent, "Note", "Значения пока не сохраняются — каркас меню.", 16, TextMuted, FontStyle.Normal);
        note.alignment = TextAnchor.MiddleCenter;
        SetPreferredHeight(note.gameObject, 28f);

        CreateSpacer(parent, 8f);
        CreateMenuButton(parent, "Назад", ShowMain);
    }

    void AddSliderRow(Transform parent, string label, float value)
    {
        var row = new GameObject(label, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        SetPreferredHeight(row, 56f);

        var v = row.AddComponent<VerticalLayoutGroup>();
        v.spacing = 4f;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;

        var lbl = CreateText(row.transform, "Label", label, 18, TextMuted, FontStyle.Normal);
        lbl.alignment = TextAnchor.MiddleLeft;

        var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(row.transform, false);
        var le = sliderGo.AddComponent<LayoutElement>();
        le.preferredHeight = 24f;
        le.minHeight = 24f;

        var bg = CreateImage(sliderGo.transform, "Background", new Color(0.08f, 0.10f, 0.09f, 1f));
        StretchFull(bg.rectTransform);
        bg.rectTransform.offsetMin = new Vector2(0, 8);
        bg.rectTransform.offsetMax = new Vector2(0, -8);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        StretchFull(fillArea.GetComponent<RectTransform>());
        fillArea.GetComponent<RectTransform>().offsetMin = new Vector2(0, 8);
        fillArea.GetComponent<RectTransform>().offsetMax = new Vector2(0, -8);

        var fill = CreateImage(fillArea.transform, "Fill", Accent);
        StretchFull(fill.rectTransform);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGo.transform, false);
        StretchFull(handleArea.GetComponent<RectTransform>());

        var handle = CreateImage(handleArea.transform, "Handle", TextPrimary);
        handle.rectTransform.sizeDelta = new Vector2(18f, 18f);

        var slider = sliderGo.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;
    }

    GameObject CreatePanel(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = _uiSprite;
        img.type = Image.Type.Simple;
        img.color = PanelBg;
        img.raycastTarget = true;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, 0f);
        rt.anchoredPosition = Vector2.zero;

        go.SetActive(false);
        return go;
    }

    void CreateMenuButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.sprite = _uiSprite;
        img.type = Image.Type.Simple;
        img.color = Color.white;
        img.raycastTarget = true;

        var btn = go.GetComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        var colors = btn.colors;
        colors.normalColor = ButtonNormal;
        colors.highlightedColor = ButtonHover;
        colors.pressedColor = ButtonPressed;
        colors.selectedColor = ButtonHover;
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        btn.colors = colors;
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 52f;
        le.minHeight = 52f;

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
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = _uiSprite;
        img.type = Image.Type.Simple;
        img.color = color;
        return img;
    }

    void CreateSpacer(Transform parent, float height)
    {
        var go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        SetPreferredHeight(go, height);
    }

    static void SetPreferredHeight(GameObject go, float height)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void ShowMain()
    {
        SetActiveOnly(_mainPanel);
    }

    void ShowCreate()
    {
        SetActiveOnly(_createPanel);
    }

    void ShowJoin()
    {
        SetActiveOnly(_joinPanel);
    }

    void ShowSettings()
    {
        SetActiveOnly(_settingsPanel);
    }

    void SetActiveOnly(GameObject panel)
    {
        _mainPanel.SetActive(panel == _mainPanel);
        _createPanel.SetActive(panel == _createPanel);
        _joinPanel.SetActive(panel == _joinPanel);
        _settingsPanel.SetActive(panel == _settingsPanel);
    }

    void OnCreateLobbyConfirm()
    {
        Debug.Log("[MainMenu] Создание лобби (заглушка) → загрузка игры");
        LoadGameScene();
    }

    void LoadGameScene()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("[MainMenu] Не задано имя игровой сцены");
            return;
        }

#if UNITY_EDITOR
        EnsureSceneInBuildSettings(GameScenePath);
#endif

        Debug.Log($"[MainMenu] Загрузка сцены '{gameSceneName}'…");

        if (Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(GameScenePath))
        {
            SceneManager.LoadScene(GameScenePath, LoadSceneMode.Single);
            return;
        }

        Debug.LogError(
            $"[MainMenu] Сцена '{gameSceneName}' не доступна для загрузки. " +
            "Добавь Assets/Scenes/SampleScene.unity в File → Build Settings.");
    }

#if UNITY_EDITOR
    static void EnsureSceneInBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path == scenePath)
            {
                if (!scenes[i].enabled)
                {
                    scenes[i].enabled = true;
                    EditorBuildSettings.scenes = scenes;
                }
                return;
            }
        }

        var list = new List<EditorBuildSettingsScene>(scenes)
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log($"[MainMenu] Сцена добавлена в Build Settings: {scenePath}");
    }
#endif

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
