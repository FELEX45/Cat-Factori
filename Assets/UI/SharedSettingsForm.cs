using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Общая форма настроек для Main Menu и Pause Menu.
/// </summary>
public class SharedSettingsForm
{
    public InputField NicknameField { get; private set; }
    public Slider VolumeSlider { get; private set; }
    public Slider MouseSlider { get; private set; }

    readonly Font _font;
    readonly Sprite _sprite;
    readonly Color _textPrimary;
    readonly Color _textMuted;
    readonly Color _accent;

    public SharedSettingsForm(Font font, Sprite sprite, Color textPrimary, Color textMuted, Color accent)
    {
        _font = font;
        _sprite = sprite;
        _textPrimary = textPrimary;
        _textMuted = textMuted;
        _accent = accent;
    }

    public void Build(Transform parent, int titleSize = 40)
    {
        var layout = parent.gameObject.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.padding = new RectOffset(40, 40, 36, 36);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
        }

        if (parent.gameObject.GetComponent<ContentSizeFitter>() == null)
        {
            var fitter = parent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        var title = CreateText(parent, "Title", "Настройки", titleSize, _textPrimary, FontStyle.Bold);
        title.alignment = TextAnchor.MiddleCenter;
        PrefHeight(title.gameObject, titleSize + 12);

        CreateLabel(parent, "Никнейм");
        NicknameField = CreateInput(parent, "Nickname", "Ваш никнейм");
        NicknameField.characterLimit = PlayerProfile.MaxNicknameLength;

        var hint = CreateText(parent, "NickHint", "Так тебя видят другие игроки и в чате.", 14, _textMuted, FontStyle.Normal);
        hint.alignment = TextAnchor.MiddleLeft;
        PrefHeight(hint.gameObject, 26f);

        Spacer(parent, 6f);
        VolumeSlider = CreateSliderRow(parent, "Громкость");
        MouseSlider = CreateSliderRow(parent, "Чувствительность мыши");

        Spacer(parent, 8f);
        LoadFromProfile();
    }

    public void LoadFromProfile()
    {
        if (NicknameField != null)
            NicknameField.text = PlayerProfile.Nickname;
        if (VolumeSlider != null)
            VolumeSlider.SetValueWithoutNotify(PlayerProfile.Volume);
        if (MouseSlider != null)
            MouseSlider.SetValueWithoutNotify(PlayerProfile.MouseSensitivityNormalized);
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

        PlayerProfile.ApplyAudio();

        if (NetworkPlayer.LocalPlayer != null && NetworkPlayer.LocalPlayer.IsSpawned && NetworkPlayer.LocalPlayer.IsOwner)
        {
            NetworkPlayer.LocalPlayer.Nickname.Value =
                new Unity.Collections.FixedString64Bytes(PlayerProfile.Nickname);
        }

        Debug.Log($"[Settings] Ник={PlayerProfile.Nickname}, Vol={PlayerProfile.Volume:0.00}, Sens={PlayerProfile.MouseSensitivity:0.000}");
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

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
