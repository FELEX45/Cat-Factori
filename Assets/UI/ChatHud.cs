using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// UI чата. T/Enter — открыть, Esc — закрыть, Enter — отправить.
/// </summary>
public class ChatHud : MonoBehaviour
{
    public static ChatHud Instance { get; private set; }

    public bool IsOpen { get; private set; }

    NetworkPlayer _player;
    Font _font;
    Sprite _sprite;
    GameObject _panel;
    Text _logText;
    InputField _input;
    Text _hint;

    static readonly Color PanelBg = new Color(0.06f, 0.08f, 0.07f, 0.82f);
    static readonly Color TextColor = new Color(0.92f, 0.95f, 0.90f, 1f);
    static readonly Color Muted = new Color(0.7f, 0.78f, 0.7f, 0.9f);

    public static ChatHud EnsureExists()
    {
        if (Instance != null)
            return Instance;
        var go = new GameObject("ChatHud");
        DontDestroyOnLoad(go);
        return go.AddComponent<ChatHud>();
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
        SetOpen(false);
        RefreshLog(NetworkPlayer.GetChatLog());
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Bind(NetworkPlayer player)
    {
        _player = player;
    }

    public void RefreshLog(IReadOnlyList<string> lines)
    {
        if (_logText == null)
            return;
        if (lines == null || lines.Count == 0)
        {
            _logText.text = "";
            return;
        }
        _logText.text = string.Join("\n", lines);
    }

    void Update()
    {
        if (!GameSessionMode.IsOnline)
        {
            if (_hint != null)
                _hint.gameObject.SetActive(false);
            if (_panel != null)
                _panel.SetActive(false);
            return;
        }

        if (_hint != null)
            _hint.gameObject.SetActive(!IsOpen);
        if (_panel != null)
            _panel.SetActive(true);

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null)
            return;

        if (!IsOpen)
        {
            if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen)
                return;
            if (kb.tKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                SetOpen(true);
        }
        else
        {
            if (kb.escapeKey.wasPressedThisFrame)
                SetOpen(false);
            else if ((kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                     && !kb.leftShiftKey.isPressed && !kb.rightShiftKey.isPressed)
                SendCurrent();
        }
#else
        if (!IsOpen && (Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.Return)))
            SetOpen(true);
        else if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            SetOpen(false);
        else if (IsOpen && Input.GetKeyDown(KeyCode.Return))
            SendCurrent();
#endif
    }

    void SendCurrent()
    {
        if (_input == null)
            return;
        string text = _input.text;
        _input.text = "";
        var target = _player != null ? _player : NetworkPlayer.LocalPlayer;
        if (target != null)
            target.SubmitChat(text);
        FocusInput();
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        if (_input != null)
        {
            _input.gameObject.SetActive(open);
            if (open)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                FocusInput();
            }
            else if (GameSessionMode.IsOnline
                     && (PauseMenu.Instance == null || !PauseMenu.Instance.IsOpen))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    void FocusInput()
    {
        if (_input == null)
            return;
        _input.ActivateInputField();
        _input.Select();
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
        var canvasGo = new GameObject("ChatCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        _hint = CreateText(canvasGo.transform, "Hint", "T / Enter — чат", 18, Muted);
        var hintRt = _hint.rectTransform;
        hintRt.anchorMin = new Vector2(0f, 0f);
        hintRt.anchorMax = new Vector2(0f, 0f);
        hintRt.pivot = new Vector2(0f, 0f);
        hintRt.anchoredPosition = new Vector2(24f, 24f);
        hintRt.sizeDelta = new Vector2(280f, 28f);

        _panel = new GameObject("ChatPanel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var panelImg = _panel.GetComponent<Image>();
        panelImg.sprite = _sprite;
        panelImg.color = PanelBg;
        panelImg.raycastTarget = false;

        var panelRt = _panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 0f);
        panelRt.anchorMax = new Vector2(0f, 0f);
        panelRt.pivot = new Vector2(0f, 0f);
        panelRt.anchoredPosition = new Vector2(20f, 56f);
        panelRt.sizeDelta = new Vector2(520f, 260f);

        _logText = CreateText(_panel.transform, "Log", "", 16, TextColor);
        var logRt = _logText.rectTransform;
        logRt.anchorMin = new Vector2(0f, 0f);
        logRt.anchorMax = new Vector2(1f, 1f);
        logRt.offsetMin = new Vector2(12f, 52f);
        logRt.offsetMax = new Vector2(-12f, -12f);
        _logText.alignment = TextAnchor.LowerLeft;
        _logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _logText.verticalOverflow = VerticalWrapMode.Overflow;
        _logText.raycastTarget = false;

        var inputGo = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputGo.transform.SetParent(_panel.transform, false);
        var inputImg = inputGo.GetComponent<Image>();
        inputImg.sprite = _sprite;
        inputImg.color = new Color(0.08f, 0.1f, 0.09f, 1f);

        var inputRt = inputGo.GetComponent<RectTransform>();
        inputRt.anchorMin = new Vector2(0f, 0f);
        inputRt.anchorMax = new Vector2(1f, 0f);
        inputRt.pivot = new Vector2(0.5f, 0f);
        inputRt.anchoredPosition = new Vector2(0f, 8f);
        inputRt.sizeDelta = new Vector2(-24f, 36f);

        var placeholder = CreateText(inputGo.transform, "Placeholder", "Сообщение…", 16, new Color(0.5f, 0.55f, 0.5f, 1f));
        Stretch(placeholder.rectTransform, 8, 4);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.raycastTarget = false;
        placeholder.fontStyle = FontStyle.Italic;

        var inputText = CreateText(inputGo.transform, "Text", "", 16, TextColor);
        Stretch(inputText.rectTransform, 8, 4);
        inputText.alignment = TextAnchor.MiddleLeft;
        inputText.supportRichText = false;

        _input = inputGo.GetComponent<InputField>();
        _input.textComponent = inputText;
        _input.placeholder = placeholder;
        _input.characterLimit = 120;
    }

    Text CreateText(Transform parent, string name, string content, int size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = _font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        return text;
    }

    static void Stretch(RectTransform rt, float padX, float padY)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padX, padY);
        rt.offsetMax = new Vector2(-padX, -padY);
    }
}
