using UnityEngine;

public enum FactoryItemKind
{
    Blank = 0,
    FinishedPart = 1,
    Scrap = 2,
    Blueprint = 3
}

/// <summary>Переносимый предмет (локальный / упрощённый сетевой).</summary>
public class FactoryItem : Interactable
{
    public FactoryItemKind Kind;
    public string PartId;
    public string PartName;
    public MetalGrade Metal;
    public PartDimensions Dimensions;
    public bool InTolerance;

    PlayerInteractor _heldBy;
    Rigidbody _rb;
    Collider _col;

    public bool IsHeld => _heldBy != null;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        if (_col == null)
            _col = gameObject.AddComponent<BoxCollider>();
    }

    public override string Prompt => IsHeld ? "" : $"Взять: {PartName}";

    public override bool CanInteract(PlayerInteractor user)
    {
        if (IsHeld) return false;
        return base.CanInteract(user) && (user.Carry == null || !user.Carry.HasItem);
    }

    public override void Interact(PlayerInteractor user)
    {
        user.Carry?.TryPickUp(this);
    }

    public void OnPickedUp(PlayerInteractor user)
    {
        _heldBy = user;
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.detectCollisions = false;
        }
        if (_col != null)
            _col.enabled = false;
    }

    public void OnDropped()
    {
        _heldBy = null;
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.detectCollisions = true;
        }
        if (_col != null)
            _col.enabled = true;
    }
}
