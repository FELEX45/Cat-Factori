using UnityEngine;

/// <summary>Базовый интерактив: подсветка + большая зона.</summary>
public abstract class Interactable : MonoBehaviour
{
    [SerializeField] string prompt = "Взаимодействовать";
    [SerializeField] float useRadius = 4f;

    Renderer[] _renderers;
    Color[] _baseColors;
    bool _highlightReady;

    public virtual string Prompt => prompt;
    public float UseRadius => useRadius;

    public void SetPrompt(string text) => prompt = text;
    public void SetUseRadius(float radius) => useRadius = radius;

    public virtual bool CanInteract(PlayerInteractor user) => user != null && IsInRange(user);

    public bool IsInRange(PlayerInteractor user)
    {
        if (user == null) return false;
        // Считаем от центра станции до игрока (не требуем точного прицела)
        return Vector3.Distance(user.transform.position, transform.position) <= useRadius;
    }

    public abstract void Interact(PlayerInteractor user);

    public void SetHighlighted(bool on)
    {
        EnsureRenderers();
        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;
            var mat = r.material;
            Color baseCol = _baseColors != null && i < _baseColors.Length ? _baseColors[i] : Color.gray;
            Color c = on ? Color.Lerp(baseCol, Color.yellow, 0.55f) : baseCol;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color"))
                mat.color = c;
        }
    }

    void EnsureRenderers()
    {
        if (_highlightReady) return;
        _renderers = GetComponentsInChildren<Renderer>();
        _baseColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            var mat = _renderers[i].material;
            if (mat.HasProperty("_BaseColor"))
                _baseColors[i] = mat.GetColor("_BaseColor");
            else
                _baseColors[i] = mat.color;
        }
        _highlightReady = true;
    }
}
