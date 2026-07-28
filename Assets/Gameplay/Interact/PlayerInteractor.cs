using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Взаимодействие: толстый SphereCast + запасной поиск ближайшей станции рядом.
/// Не нужно точно целиться в куб.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] float rayDistance = 6f;
    [SerializeField] float sphereRadius = 0.55f;
    [SerializeField] float proximityRadius = 3.2f;
    [SerializeField] LayerMask mask = ~0;

    public Interactable Current { get; private set; }
    public Interactable Nearest { get; private set; }
    public PlayerCarry Carry { get; private set; }

    Camera _cam;
    Interactable _highlighted;

    static readonly Collider[] ProximityBuf = new Collider[32];

    void Awake()
    {
        Carry = GetComponent<PlayerCarry>();
        if (Carry == null)
            Carry = gameObject.AddComponent<PlayerCarry>();
    }

    void OnDisable()
    {
        ClearHighlight();
        Current = null;
        Nearest = null;
    }

    void Update()
    {
        bool uiBlocked = (ChatHud.Instance != null && ChatHud.Instance.IsOpen)
                         || (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen)
                         || GameplayHud.BlocksWorldInput;

        if (uiBlocked)
        {
            ClearHighlight();
            Current = null;
            Nearest = null;
            return;
        }

        Scan();
        UpdateHighlight();

        if (WasInteractPressed())
        {
            var target = Current != null ? Current : Nearest;
            if (target != null && target.CanInteract(this))
                target.Interact(this);
        }
    }

    void Scan()
    {
        Current = null;
        Nearest = null;
        if (_cam == null)
            _cam = Camera.main;
        if (_cam == null)
            return;

        var ray = new Ray(_cam.transform.position, _cam.transform.forward);

        // 1) Толстый луч — прощает промах прицелом
        if (Physics.SphereCast(ray, sphereRadius, out var hit, rayDistance, mask, QueryTriggerInteraction.Collide))
        {
            var fromAim = hit.collider.GetComponentInParent<Interactable>();
            if (fromAim != null && fromAim.CanInteract(this))
                Current = fromAim;
        }

        // 2) Ближайший интерактив в радиусе от игрока (даже если смотришь мимо)
        Nearest = FindNearestInProximity();

        // Если луч ничего не поймал — берём ближайший
        if (Current == null)
            Current = Nearest;
    }

    Interactable FindNearestInProximity()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.9f, proximityRadius,
            ProximityBuf, mask, QueryTriggerInteraction.Collide);

        Interactable best = null;
        float bestScore = float.MaxValue;
        Vector3 eye = _cam != null ? _cam.transform.position : transform.position + Vector3.up;
        Vector3 fwd = _cam != null ? _cam.transform.forward : transform.forward;

        for (int i = 0; i < count; i++)
        {
            var col = ProximityBuf[i];
            if (col == null) continue;
            var interactable = col.GetComponentInParent<Interactable>();
            if (interactable == null || !interactable.CanInteract(this))
                continue;

            Vector3 to = interactable.transform.position - eye;
            float dist = to.magnitude;
            float facing = Vector3.Dot(fwd, to.normalized); // ближе к 1 = смотрим туда
            // Штрафуем то, что за спиной, но не исключаем полностью
            float score = dist - facing * 1.5f;
            if (score < bestScore)
            {
                bestScore = score;
                best = interactable;
            }
        }

        return best;
    }

    void UpdateHighlight()
    {
        if (_highlighted == Current)
            return;
        if (_highlighted != null)
            _highlighted.SetHighlighted(false);
        _highlighted = Current;
        if (_highlighted != null)
            _highlighted.SetHighlighted(true);
    }

    void ClearHighlight()
    {
        if (_highlighted != null)
            _highlighted.SetHighlighted(false);
        _highlighted = null;
    }

    static bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
}
