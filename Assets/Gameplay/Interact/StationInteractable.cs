using UnityEngine;

public enum StationKind
{
    JobBoard = 0,
    Measure = 1,
    Draft = 2,
    Warehouse = 3,
    Cutting = 4,
    Lathe = 5,
    Mill = 6,
    Press = 7,
    Assembly = 8,
    BossTask = 9
}

/// <summary>Занимаемая станция (сервер/хост хранит OccupantId).</summary>
public class StationInteractable : Interactable
{
    [SerializeField] StationKind kind;
    [SerializeField] string stationLabel = "Станция";

    public StationKind Kind => kind;
    public string StationLabel => stationLabel;
    public ulong OccupantId { get; private set; }
    public bool IsOccupied => OccupantId != 0;

    public void Configure(StationKind stationKind, string label, string promptText, float radius = 4.5f)
    {
        kind = stationKind;
        stationLabel = label;
        SetPrompt(promptText);
        SetUseRadius(radius);
    }

    public override string Prompt
    {
        get
        {
            if (IsOccupied)
                return $"{stationLabel} (занято)";
            if (kind == StationKind.BossTask && BossTaskSystem.Instance != null)
                return $"{stationLabel}: {BossTaskSystem.Instance.GetPhonePrompt()}";
            return $"{stationLabel}: {base.Prompt}";
        }
    }

    public override bool CanInteract(PlayerInteractor user)
    {
        if (!base.CanInteract(user))
            return false;
        if (IsOccupied && OccupantId != GetUserId(user))
            return false;
        return true;
    }

    public override void Interact(PlayerInteractor user)
    {
        if (user == null)
            return;

        ulong id = GetUserId(user);
        if (!IsOccupied)
            OccupantId = id;

        GameSessionState.Instance?.OnStationUsed(this, user);
    }

    public void Release(PlayerInteractor user)
    {
        if (user == null) return;
        if (OccupantId == GetUserId(user))
            OccupantId = 0;
    }

    public void ForceRelease() => OccupantId = 0;

    static ulong GetUserId(PlayerInteractor user)
    {
        var np = user.GetComponent<NetworkPlayer>();
        if (np != null && np.IsSpawned)
            return np.OwnerClientId;
        return 1;
    }
}
