using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Табличка с ником над головой (world-space), смотрит на камеру.
/// </summary>
public class PlayerNameplate : MonoBehaviour
{
    [SerializeField] float heightOffset = 2.1f;
    [SerializeField] float visibleDistance = 40f;

    Text _label;
    Transform _follow;
    bool _visibleToLocal = true;

    public static PlayerNameplate Create(Transform follow, string nickname, bool visibleToLocal)
    {
        var go = new GameObject("Nameplate");
        go.transform.SetParent(follow, false);
        go.transform.localPosition = new Vector3(0f, 2.1f, 0f);

        var plate = go.AddComponent<PlayerNameplate>();
        plate._follow = follow;
        plate._visibleToLocal = visibleToLocal;
        plate.BuildUi();
        plate.SetNickname(nickname);
        return plate;
    }

    void BuildUi()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        var rt = (RectTransform)transform;
        rt.sizeDelta = new Vector2(2.4f, 0.45f);
        rt.localScale = Vector3.one * 0.02f;

        gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;

        var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(0.05f, 0.07f, 0.06f, 0.65f);
        bgImg.raycastTarget = false;

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(4, 2);
        textRt.offsetMax = new Vector2(-4, -2);

        _label = textGo.GetComponent<Text>();
        _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_label.font == null)
            _label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _label.fontSize = 36;
        _label.alignment = TextAnchor.MiddleCenter;
        _label.color = new Color(0.92f, 0.95f, 0.90f, 1f);
        _label.horizontalOverflow = HorizontalWrapMode.Overflow;
        _label.verticalOverflow = VerticalWrapMode.Overflow;
        _label.raycastTarget = false;
    }

    public void SetNickname(string nickname)
    {
        if (_label != null)
            _label.text = string.IsNullOrEmpty(nickname) ? "Игрок" : nickname;
    }

    public void SetHeight(float height)
    {
        heightOffset = height;
        transform.localPosition = new Vector3(0f, heightOffset, 0f);
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            SetVisible(false);
            return;
        }

        // Свой ник себе не показываем (только другим)
        if (!_visibleToLocal)
        {
            SetVisible(false);
            return;
        }

        float dist = Vector3.Distance(cam.transform.position, transform.position);
        bool show = dist <= visibleDistance;
        SetVisible(show);
        if (!show)
            return;

        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }

    void SetVisible(bool visible)
    {
        if (_label != null)
            _label.enabled = visible;
        var img = GetComponentInChildren<Image>();
        if (img != null)
            img.enabled = visible;
    }
}
