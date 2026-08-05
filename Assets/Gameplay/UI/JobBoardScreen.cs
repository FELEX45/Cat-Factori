using System.Text;
using UnityEngine;

/// <summary>Экран табло: тонкий чёрный куб в рамке + белый TextMesh по размеру экрана.</summary>
public class JobBoardScreen : MonoBehaviour
{
    TextMesh _tm;
    float _screenW = 1f;
    float _screenH = 1f;

    public static JobBoardScreen Attach(Transform stationRoot, GameObject visual)
    {
        Bounds b = GetWorldBounds(visual != null ? visual : stationRoot.gameObject);
        float faceW = Mathf.Max(b.size.x, b.size.z);
        float faceH = Mathf.Max(b.size.y, 0.4f);
        float depth = Mathf.Min(b.size.x, b.size.z);

        // Чуть меньше внешней габаритки — закрывает внутренний кант, не вылезает за раму
        float screenW = faceW * 0.92f;
        float screenH = faceH * 0.88f;

        var go = new GameObject("JobBoardScreen");
        go.transform.SetParent(stationRoot, false);

        Vector3 localCenter = stationRoot.InverseTransformPoint(b.center);
        // Выносим плоскость в проём рамки (к игроку вдоль local +Z станции)
        float along = Mathf.Max(depth * 0.5f, 0.08f) + 0.02f;
        go.transform.localPosition = new Vector3(localCenter.x, localCenter.y, localCenter.z + along);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var screen = go.AddComponent<JobBoardScreen>();
        screen._screenW = screenW;
        screen._screenH = screenH;
        screen.BuildVisuals(screenW, screenH);
        screen.Refresh();
        return screen;
    }

    void OnEnable()
    {
        if (GameSessionState.Instance != null)
            GameSessionState.Instance.StateChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (GameSessionState.Instance != null)
            GameSessionState.Instance.StateChanged -= Refresh;
    }

    void Start()
    {
        if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.StateChanged -= Refresh;
            GameSessionState.Instance.StateChanged += Refresh;
        }
        Refresh();
    }

    void BuildVisuals(float worldW, float worldH)
    {
        // Куб виден с обеих сторон — не пропадает из‑за backface cull, в отличие от Quad
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "ScreenBg";
        panel.transform.SetParent(transform, false);
        panel.transform.localPosition = Vector3.zero;
        panel.transform.localRotation = Quaternion.identity;
        panel.transform.localScale = new Vector3(worldW, worldH, 0.025f);
        UnityEngine.Object.Destroy(panel.GetComponent<Collider>());

        var rend = panel.GetComponent<Renderer>();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.material = MakeOpaqueBlack();

        var textGo = new GameObject("ScreenText");
        textGo.transform.SetParent(transform, false);
        // Чуть ближе к игроку, чем панель (local +Z станции смотрит к проёму/игроку)
        textGo.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        textGo.transform.localRotation = Quaternion.identity;
        textGo.transform.localScale = new Vector3(-1f, 1f, 1f);

        _tm = textGo.AddComponent<TextMesh>();
        _tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_tm.font == null)
            _tm.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (_tm.font != null)
        {
            var mr = textGo.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.material = _tm.font.material;
        }

        _tm.anchor = TextAnchor.MiddleCenter;
        _tm.alignment = TextAlignment.Left;
        _tm.color = Color.white;
        _tm.fontSize = 24;
        _tm.characterSize = 0.02f;
        _tm.lineSpacing = 1.05f;
        _tm.richText = false;

        var textRend = textGo.GetComponent<MeshRenderer>();
        if (textRend != null)
        {
            textRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            textRend.receiveShadows = false;
        }
    }

    static Material MakeOpaqueBlack()
    {
        Shader sh =
            Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Lit")
            ?? Shader.Find("Standard")
            ?? Shader.Find("Sprites/Default");

        var mat = new Material(sh);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.black);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.black);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0f);
        if (mat.HasProperty("_Glossiness"))
            mat.SetFloat("_Glossiness", 0f);
        mat.color = Color.black;
        return mat;
    }

    public void Refresh()
    {
        if (_tm == null) return;

        var s = GameSessionState.Instance;
        string text;
        if (s == null)
        {
            text = "ТАБЛО НАРЯДОВ\n\nНет сессии";
        }
        else
        {
            var sb = new StringBuilder(192);
            sb.AppendLine("ТАБЛО НАРЯДОВ");
            sb.AppendLine(s.ShiftRunning ? "Смена идёт" : s.ShiftFinished ? "Смена окончена" : "Ожидание");
            sb.AppendLine($"Квота {s.QuotaDone}/{s.QuotaTarget}  Бюджет {s.Budget:0}");
            sb.AppendLine($"Изделие: {s.CurrentProduct?.displayName ?? "—"}");
            sb.AppendLine("—");
            if (s.Parts.Count == 0)
            {
                sb.Append("Нет задач");
            }
            else
            {
                var active = s.ActivePart;
                for (int i = 0; i < s.Parts.Count; i++)
                {
                    var p = s.Parts[i];
                    string mark = (active != null && p == active) ? " <<" : "";
                    sb.AppendLine($"{i + 1}. {p.partName} [{StatusRu(p.status)}] {ProductCatalog.MetalName(p.requiredMetal)}{mark}");
                }
            }

            text = sb.ToString().TrimEnd();
        }

        _tm.text = text;
        FitText(text);
    }

    void FitText(string text)
    {
        if (_tm == null) return;

        int lines = 1;
        int maxLen = 4;
        int lineLen = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                lines++;
                maxLen = Mathf.Max(maxLen, lineLen);
                lineLen = 0;
            }
            else lineLen++;
        }
        maxLen = Mathf.Max(maxLen, lineLen);

        // TextMesh: итоговый размер ~ characterSize * (примерно высота строки)
        float padH = _screenH * 0.82f;
        float padW = _screenW * 0.88f;
        float byH = padH / (lines * 1.25f);
        float byW = padW / (maxLen * 0.63f);
        _tm.fontSize = 24;
        _tm.characterSize = Mathf.Clamp(Mathf.Min(byH, byW), 0.006f, 0.028f);
    }

    static string StatusRu(PartPipelineStatus status)
    {
        switch (status)
        {
            case PartPipelineStatus.None: return "ожидание";
            case PartPipelineStatus.Measuring: return "замер";
            case PartPipelineStatus.Drawing: return "чертёж";
            case PartPipelineStatus.Ordered: return "заказано";
            case PartPipelineStatus.AtMachine: return "станок";
            case PartPipelineStatus.Ready: return "готово";
            case PartPipelineStatus.Assembled: return "собрано";
            case PartPipelineStatus.Scrap: return "брак";
            default: return status.ToString();
        }
    }

    static Bounds GetWorldBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return new Bounds(go.transform.position + Vector3.up * 1.5f, new Vector3(1.2f, 1.0f, 0.2f));

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                b.Encapsulate(renderers[i].bounds);
        }
        return b;
    }
}
