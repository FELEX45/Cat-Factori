using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>1 слот рук: взять / положить (G).</summary>
public class PlayerCarry : MonoBehaviour
{
    [SerializeField] Vector3 holdOffset = new Vector3(0.35f, -0.2f, 0.7f);

    public FactoryItem Held { get; private set; }
    public bool HasItem => Held != null;

    Transform _holdPoint;

    void Awake()
    {
        var go = new GameObject("HoldPoint");
        _holdPoint = go.transform;
        _holdPoint.SetParent(transform, false);
        _holdPoint.localPosition = holdOffset;
    }

    void LateUpdate()
    {
        if (Held != null)
        {
            Held.transform.position = _holdPoint.position;
            Held.transform.rotation = _holdPoint.rotation;
        }

        if (HasItem && WasDropPressed() && !GameplayHud.BlocksWorldInput)
            Drop();
    }

    public bool TryPickUp(FactoryItem item)
    {
        if (item == null || HasItem || item.IsHeld)
            return false;
        Held = item;
        item.OnPickedUp(GetComponent<PlayerInteractor>());
        GameplayHud.Instance?.ShowToast($"Взято: {item.PartName}");
        return true;
    }

    public FactoryItem Drop()
    {
        if (Held == null) return null;
        var item = Held;
        Held = null;
        item.transform.position = transform.position + transform.forward * 0.8f + Vector3.up * 0.5f;
        item.OnDropped();
        return item;
    }

    public FactoryItem ConsumeHeld()
    {
        if (Held == null) return null;
        var item = Held;
        Held = null;
        return item;
    }

    static bool WasDropPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.gKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.G);
#endif
    }
}
