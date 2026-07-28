using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Общая форма настроек для Main Menu и Pause Menu.
/// Список в ScrollRect, кнопки Применить/Назад всегда снизу.
/// </summary>
public class SharedSettingsForm
{
    public InputField NicknameField { get; private set; }
    public Slider VolumeSlider { get; private set; }
    public Slider MouseSlider { get; private set; }

    Text _displayModeValue;
    Text _resolutionValue;
    Text _qualityValue;
    Text _vSyncValue;
    Text _frameCapValue;

    List<Resolution> _resolutions;
    int _displayModeIndex;
    int _resolutionIndex;
    int _qualityIndex;
    int _vSyncIndex;
    int _frameCapIndex;

    static readonly int[] FrameCapOptions = { 0, 30, 60, 120, 144 };

    readonly Font _font;
    readonly Sprite _sprite;
    readonly Color _textPrimary;
    readonly Color _textMuted;
    readonly Color _accent;
    readonly Color _btn = new Color(0.16f, 0.22f, 0.18f, 1f);
    readonly Color _btnHover = new Color(0.22f, 0.32f, 0.24f, 1f);
    readonly Color _btnPress = new Color(0.12f, 0.18f, 0.14f, 1f);

    public SharedSettingsForm(Font font, Sprite sprite, Color textPrimary, Color textMuted, Color accent)
    {
        _font = font;
        _sprite = sprite;
        _textPrimary = textPrimary;
        _textMuted = textMuted;
        _accent = accent;
    }

    public void Build(Transform parent, int titleSize = 40, UnityAction onApply = null, UnityAction onBack = null, float scrollHeight = 560f)
    {
        // Фиксированная высота панели — иначе ScrollRect схлопывается в полоску
        var parentRt = parent as RectTransform;
        if (parentRt != null)
        {
            float panelH = Mathf.Clamp(scrollHeight + titleSize + 140f, 640f, 820f);
            parentRt.sizeDelta = new Vector2(Mathf.Max(parentRt.sizeDelta.x, 640f), panelH);
        }

        var layout = parent.gameObject.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 12f;
        layout.padding = new RectOffset(28, 28, 20, 20);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Не сжимаем по контенту — высота задана sizeDelta
        var panelFitter = parent.gameObject.GetComponent<ContentSizeFitter>();
        if (panelFitter != null)
            Object.DestroyImmediate(panelFitter);

        var title = CreateText(parent, "Title", "Настройки", titleSize, _textPrimary, FontStyle.Bold);
        title.alignment = TextAnchor.MiddleCenter;
        PrefHeight(title.gameObject, titleSize + 8);

        Transform content = CreateScrollArea(parent, scrollHeight);

        CreateLabel(content, "Никнейм");
        NicknameField = CreateInput(content, "Nickname", "Ваш никнейм");
        NicknameField.characterLimit = PlayerProfile.MaxNicknameLength;

        var hint = CreateText(content, "NickHint", "Так тебя видят другие игроки и в чате.", 14, _textMuted, FontStyle.Normal);
        hint.alignment = TextAnchor.MiddleLeft;
        PrefHeight(hint.gameObject, 24f);

        Spacer(content, 4f);
        VolumeSlider = CreateSliderRow(content, "Громкость");
        MouseSlider = CreateSliderRow(content, "Чувствительность мыши");

        Spacer(content, 6f);
        var gfx = CreateText(content, "GfxHeader", "Графика", 22, _textPrimary, FontStyle.Bold);
        gfx.alignment = TextAnchor.MiddleLeft;
        PrefHeight(gfx.gameObject, 28f);

        _displayModeValue = CreateCycleRow(content, "Режим экрана", () => CycleDisplay(-1), () => CycleDisplay(+1));
        _resolutionValue = CreateCycleRow(content, "Разрешение", () => CycleResolution(-1), () => CycleResolution(+1));
        _qualityValue = CreateCycleRow(content, "Качество", () => CycleQuality(-1), () => CycleQuality(+1));
        _vSyncValue = CreateCycleRow(content, "Верт. синхронизация", () => CycleVSync(), () => CycleVSync());
        _frameCapValue = CreateCycleRow(content, "Лимит FPS", () => CycleFrameCap(-1), () => CycleFrameCap(+1));

        var tip = CreateText(content, "GfxTip", "Качество — пресеты Quality Settings (URP).", 13, _textMuted, FontStyle.Normal);
        tip.alignment = TextAnchor.MiddleLeft;
        PrefHeight(tip.gameObject, 22f);

        Spacer(content, 8f);

        // Кнопки всегда видны под скроллом
        if (onApply != null)
            CreateFooterButton(parent, "Применить", onApply);
        if (onBack != null)
            CreateFooterButton(parent, "Назад", onBack);

        LoadFromProfile();
    }

    Transform CreateScrollArea(Transform parent, float height)
    {
        var scrollGo = new GameObject("SettingsScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
        scrollGo.transform.SetParent(parent, false);
        var scrollLe = scrollGo.GetComponent<LayoutElement>();
        scrollLe.preferredHeight = height;
        scrollLe.minHeight = height;
        scrollLe.flexibleHeight = 1f;
        var scrollImg = scrollGo.GetComponent<Image>();
        scrollImg.sprite = _sprite;
        scrollImg.color = new Color(0.06f, 0.08f, 0.07f, 0.95f);
        scrollImg.raycastTarget = true;

        const float barW = 18f;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpImg = viewport.GetComponent<Image>();
        vpImg.sprite = _sprite;
        vpImg.color = new Color(1f, 1f, 1f, 0.01f);
        vpImg.raycastTarget = true;
        var vprt = viewport.GetComponent<RectTransform>();
        vprt.anchorMin = Vector2.zero;
        vprt.anchorMax = Vector2.one;
        vprt.offsetMin = new Vector2(4f, 4f);
        vprt.offsetMax = new Vector2(-(barW + 4f), -4f);

        var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(viewport.transform, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);

        var v = contentGo.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(14, 14, 12, 24);
        v.spacing = 12f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = false;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        var fitter = contentGo.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var scrollbarGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarGo.transform.SetParent(scrollGo.transform, false);
        var sbrt = scrollbarGo.GetComponent<RectTransform>();
        sbrt.anchorMin = new Vector2(1f, 0f);
        sbrt.anchorMax = new Vector2(1f, 1f);
        sbrt.pivot = new Vector2(1f, 0.5f);
        sbrt.sizeDelta = new Vector2(barW, -8f);
        sbrt.anchoredPosition = new Vector2(-2f, 0f);
        scrollbarGo.GetComponent<Image>().sprite = _sprite;
        scrollbarGo.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.11f, 1f);

        var sliding = new GameObject("Sliding Area", typeof(RectTransform));
        sliding.transform.SetParent(scrollbarGo.transform, false);
        StretchFull(sliding.GetComponent<RectTransform>(), 2f);

        var handle = CreateImage(sliding.transform, "Handle", _accent);
        StretchFull(handle.rectTransform);
        handle.raycastTarget = true;

        var sb = scrollbarGo.GetComponent<Scrollbar>();
        sb.handleRect = handle.rectTransform;
        sb.direction = Scrollbar.Direction.BottomToTop;
        sb.targetGraphic = handle;
        sb.size = 0.3f;

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.viewport = vprt;
        scroll.content = contentRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        scroll.verticalScrollbar = sb;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        return contentGo.transform;
    }

    void CreateFooterButton(Transform parent, string label, UnityAction onClick)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        PrefHeight(go, 52f);
        var le = go.GetComponent<LayoutElement>();
        le.flexibleHeight = 0f;
        le.minHeight = 52f;
        var img = go.GetComponent<Image>();
        img.sprite = _sprite;
        img.color = Color.white;
        var btn = go.GetComponent<Button>();
        var c = btn.colors;
        c.normalColor = _btn;
        c.highlightedColor = _btnHover;
        c.pressedColor = _btnPress;
        c.selectedColor = _btnHover;
        btn.colors = c;
        btn.onClick.AddListener(onClick);
        var text = CreateText(go.transform, "Label", label, 22, _textPrimary, FontStyle.Normal);
        StretchFull(text.rectTransform);
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
    }

    public void LoadFromProfile()
    {
        if (NicknameField != null)
            NicknameField.text = PlayerProfile.Nickname;
        if (VolumeSlider != null)
            VolumeSlider.SetValueWithoutNotify(PlayerProfile.Volume);
        if (MouseSlider != null)
            MouseSlider.SetValueWithoutNotify(PlayerProfile.MouseSensitivityNormalized);

        _resolutions = PlayerProfile.GetUniqueResolutions();
        _displayModeIndex = (int)PlayerProfile.DisplayMode;
        _qualityIndex = PlayerProfile.QualityLevel;
        _vSyncIndex = PlayerProfile.VSync ? 1 : 0;

        _resolutionIndex = PlayerProfile.ResolutionIndex;
        if (_resolutionIndex < 0 || _resolutionIndex >= _resolutions.Count)
            _resolutionIndex = PlayerProfile.FindClosestResolutionIndex(_resolutions, Screen.width, Screen.height);

        _frameCapIndex = 0;
        for (int i = 0; i < FrameCapOptions.Length; i++)
        {
            if (FrameCapOptions[i] == PlayerProfile.FrameCap)
            {
                _frameCapIndex = i;
                break;
            }
        }

        RefreshGraphicsLabels();
    }

    public void SaveToProfile()
    {
        if (NicknameField != null)
        {
            PlayerProfile.Nickname = NicknameField.text;
            NicknameField.text = PlayerProfile.Nickname;
        }

        if (VolumeSlider != null)
            PlayerProfile.Volume = VolumeSlider.value;

        if (MouseSlider != null)
            PlayerProfile.MouseSensitivityNormalized = MouseSlider.value;

        PlayerProfile.DisplayMode = (PlayerProfile.DisplayModeKind)Mathf.Clamp(_displayModeIndex, 0, 2);
        PlayerProfile.ResolutionIndex = _resolutionIndex;
        PlayerProfile.QualityLevel = _qualityIndex;
        PlayerProfile.VSync = _vSyncIndex == 1;
        PlayerProfile.FrameCap = FrameCapOptions[Mathf.Clamp(_frameCapIndex, 0, FrameCapOptions.Length - 1)];

        PlayerProfile.ApplyAll();

        if (NetworkPlayer.LocalPlayer != null && NetworkPlayer.LocalPlayer.IsSpawned && NetworkPlayer.LocalPlayer.IsOwner)
        {
            NetworkPlayer.LocalPlayer.Nickname.Value =
                new Unity.Collections.FixedString64Bytes(PlayerProfile.Nickname);
        }

        Debug.Log(
            $"[Settings] Ник={PlayerProfile.Nickname}, " +
            $"{PlayerProfile.DisplayModeLabel(PlayerProfile.DisplayMode)}, " +
            $"Q={QualitySettings.names[Mathf.Clamp(PlayerProfile.QualityLevel, 0, QualitySettings.names.Length - 1)]}, " +
            $"VSync={PlayerProfile.VSync}, Cap={PlayerProfile.FrameCap}");
    }

    void CycleDisplay(int delta)
    {
        _displayModeIndex = (_displayModeIndex + delta + 3) % 3;
        RefreshGraphicsLabels();
    }

    void CycleResolution(int delta)
    {
        if (_resolutions == null || _resolutions.Count == 0) return;
        _resolutionIndex = (_resolutionIndex + delta + _resolutions.Count) % _resolutions.Count;
        RefreshGraphicsLabels();
    }

    void CycleQuality(int delta)
    {
        int n = Mathf.Max(1, QualitySettings.names.Length);
        _qualityIndex = (_qualityIndex + delta + n) % n;
        RefreshGraphicsLabels();
    }

    void CycleVSync()
    {
        _vSyncIndex = 1 - _vSyncIndex;
        RefreshGraphicsLabels();
    }

    void CycleFrameCap(int delta)
    {
        _frameCapIndex = (_frameCapIndex + delta + FrameCapOptions.Length) % FrameCapOptions.Length;
        RefreshGraphicsLabels();
    }

    void RefreshGraphicsLabels()
    {
        if (_displayModeValue != null)
            _displayModeValue.text = PlayerProfile.DisplayModeLabel((PlayerProfile.DisplayModeKind)_displayModeIndex);

        if (_resolutionValue != null && _resolutions != null && _resolutions.Count > 0)
        {
            var r = _resolutions[Mathf.Clamp(_resolutionIndex, 0, _resolutions.Count - 1)];
            _resolutionValue.text = PlayerProfile.ResolutionLabel(r);
        }

        if (_qualityValue != null)
        {
            var names = QualitySettings.names;
            _qualityValue.text = names != null && names.Length > 0
                ? names[Mathf.Clamp(_qualityIndex, 0, names.Length - 1)]
                : "—";
        }

        if (_vSyncValue != null)
            _vSyncValue.text = _vSyncIndex == 1 ? "Вкл" : "Выкл";

        if (_frameCapValue != null)
        {
            _frameCapValue.text = PlayerProfile.FrameCapLabel(FrameCapOptions[Mathf.Clamp(_frameCapIndex, 0, FrameCapOptions.Length - 1)]);
            _frameCapValue.color = _vSyncIndex == 1 ? _textMuted : _textPrimary;
        }
    }

    Text CreateCycleRow(Transform parent, string label, UnityAction onPrev, UnityAction onNext)
    {
        var row = new GameObject(label, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        PrefHeight(row, 56f);
        var v = row.AddComponent<VerticalLayoutGroup>();
        v.spacing = 4f;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;

        var lbl = CreateText(row.transform, "Label", label, 16, _textMuted, FontStyle.Normal);
        lbl.alignment = TextAnchor.MiddleLeft;

        var controls = new GameObject("Controls", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        controls.transform.SetParent(row.transform, false);
        var h = controls.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 8f;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandHeight = true;
        h.childForceExpandWidth = false;
        var cle = controls.AddComponent<LayoutElement>();
        cle.preferredHeight = 30f;
        cle.minHeight = 30f;

        CreateSmallButton(controls.transform, "◀", onPrev, 40f);
        var value = CreateText(controls.transform, "Value", "—", 17, _textPrimary, FontStyle.Normal);
        value.alignment = TextAnchor.MiddleCenter;
        var vle = value.gameObject.AddComponent<LayoutElement>();
        vle.flexibleWidth = 1f;
        vle.preferredWidth = 260f;
        CreateSmallButton(controls.transform, "▶", onNext, 40f);
        return value;
    }

    void CreateSmallButton(Transform parent, string label, UnityAction onClick, float width)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = width;
        le.minWidth = width;
        le.preferredHeight = 30f;
        var img = go.GetComponent<Image>();
        img.sprite = _sprite;
        img.color = Color.white;
        var btn = go.GetComponent<Button>();
        var c = btn.colors;
        c.normalColor = _btn;
        c.highlightedColor = _btnHover;
        c.pressedColor = _btnPress;
        btn.colors = c;
        btn.onClick.AddListener(onClick);
        var text = CreateText(go.transform, "T", label, 18, _textPrimary, FontStyle.Bold);
        StretchFull(text.rectTransform);
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
    }

    void CreateLabel(Transform parent, string text)
    {
        var lbl = CreateText(parent, text + "Label", text, 18, _textMuted, FontStyle.Normal);
        lbl.alignment = TextAnchor.MiddleLeft;
        PrefHeight(lbl.gameObject, 22f);
    }

    InputField CreateInput(Transform parent, string name, string placeholder)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = _sprite;
        img.color = new Color(0.08f, 0.10f, 0.09f, 1f);
        PrefHeight(go, 42f);

        var ph = CreateText(go.transform, "Placeholder", placeholder, 18, new Color(0.45f, 0.5f, 0.45f, 1f), FontStyle.Italic);
        Stretch(ph.rectTransform, 10, 4);
        ph.alignment = TextAnchor.MiddleLeft;
        ph.raycastTarget = false;

        var tx = CreateText(go.transform, "Text", "", 18, _textPrimary, FontStyle.Normal);
        Stretch(tx.rectTransform, 10, 4);
        tx.alignment = TextAnchor.MiddleLeft;
        tx.supportRichText = false;

        var field = go.GetComponent<InputField>();
        field.textComponent = tx;
        field.placeholder = ph;
        field.caretColor = _accent;
        return field;
    }

    Slider CreateSliderRow(Transform parent, string label)
    {
        var row = new GameObject(label, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        PrefHeight(row, 52f);
        var v = row.AddComponent<VerticalLayoutGroup>();
        v.spacing = 4f;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;

        var lbl = CreateText(row.transform, "Label", label, 18, _textMuted, FontStyle.Normal);
        lbl.alignment = TextAnchor.MiddleLeft;

        var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(row.transform, false);
        var le = sliderGo.AddComponent<LayoutElement>();
        le.preferredHeight = 22f;
        le.minHeight = 22f;

        var bg = CreateImage(sliderGo.transform, "Background", new Color(0.08f, 0.10f, 0.09f, 1f));
        StretchFull(bg.rectTransform);
        bg.rectTransform.offsetMin = new Vector2(0, 7);
        bg.rectTransform.offsetMax = new Vector2(0, -7);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        StretchFull(fillArea.GetComponent<RectTransform>());
        fillArea.GetComponent<RectTransform>().offsetMin = new Vector2(0, 7);
        fillArea.GetComponent<RectTransform>().offsetMax = new Vector2(0, -7);

        var fill = CreateImage(fillArea.transform, "Fill", _accent);
        StretchFull(fill.rectTransform);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGo.transform, false);
        StretchFull(handleArea.GetComponent<RectTransform>());

        var handle = CreateImage(handleArea.transform, "Handle", _textPrimary);
        handle.rectTransform.sizeDelta = new Vector2(16f, 16f);

        var slider = sliderGo.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        return slider;
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

    Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = _sprite;
        img.color = color;
        return img;
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

    static void Stretch(RectTransform rt, float padX, float padY)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padX, padY);
        rt.offsetMax = new Vector2(-padX, -padY);
    }

    static void StretchFull(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }
}
