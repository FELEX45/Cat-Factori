using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Рантайм-состояние одной детали в пайплайне.</summary>
[Serializable]
public class PartJob
{
    public string partId;
    public string partName;
    public PartDimensions ideal;
    public float toleranceMm;
    public MetalGrade requiredMetal;
    public MachineType requiredMachine;
    public float blankCost;

    public PartPipelineStatus status = PartPipelineStatus.None;
    public PartDimensions measured;
    public PartDimensions actual;
    public MetalGrade orderedMetal;
    public bool hasDrawing;
    public bool hasMeasurement;
    public bool inTolerance;

    public static PartJob FromDefinition(PartDefinition def, int index)
    {
        return new PartJob
        {
            partId = $"{def.name}_{index}",
            partName = def.displayName,
            ideal = def.ideal,
            toleranceMm = def.toleranceMm,
            requiredMetal = def.requiredMetal,
            requiredMachine = def.requiredMachine,
            blankCost = def.blankCost,
            status = PartPipelineStatus.None
        };
    }
}

/// <summary>Хост/офлайн авторитет смены и пайплайна.</summary>
public class GameSessionState : MonoBehaviour
{
    public static GameSessionState Instance { get; private set; }

    [SerializeField] ProductDefinition starterProduct;

    public ProductDefinition CurrentProduct { get; private set; }
    public readonly List<PartJob> Parts = new List<PartJob>();
    public int ActivePartIndex { get; private set; }
    public PartJob ActivePart => Parts.Count > 0 && ActivePartIndex >= 0 && ActivePartIndex < Parts.Count
        ? Parts[ActivePartIndex]
        : null;

    public float Budget { get; private set; } = 500f;
    public int QuotaTarget { get; private set; } = 1;
    public int QuotaDone { get; private set; }
    public int ScrapCount { get; private set; }
    public float ShiftSecondsLeft { get; private set; } = 900f;
    public bool ShiftRunning { get; private set; }
    public bool ShiftFinished { get; private set; }

    public event Action StateChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!ShiftRunning || ShiftFinished)
            return;
        ShiftSecondsLeft -= Time.deltaTime;
        if (ShiftSecondsLeft <= 0f)
        {
            ShiftSecondsLeft = 0f;
            EndShift();
        }
    }

    public void BeginShift(ProductDefinition product = null, float budget = 500f, int quota = 1, float seconds = 900f)
    {
        CurrentProduct = product != null ? product : starterProduct;
        if (CurrentProduct == null)
            CurrentProduct = ProductCatalog.RelayHousing;

        Parts.Clear();
        if (CurrentProduct.parts != null)
        {
            for (int i = 0; i < CurrentProduct.parts.Length; i++)
            {
                if (CurrentProduct.parts[i] != null)
                    Parts.Add(PartJob.FromDefinition(CurrentProduct.parts[i], i));
            }
        }

        ActivePartIndex = 0;
        Budget = budget;
        QuotaTarget = Mathf.Max(1, quota);
        QuotaDone = 0;
        ScrapCount = 0;
        ShiftSecondsLeft = seconds;
        ShiftRunning = true;
        ShiftFinished = false;
        BossTaskSystem.Instance?.ResetTasks();
        Notify();
        Debug.Log($"[Session] Смена: {CurrentProduct.displayName}, деталей={Parts.Count}, бюджет={Budget}");

        // Задания выдаёт система, не игрок
        string first = ActivePart != null ? ActivePart.partName : "—";
        GameplayHud.Instance?.ShowToast(
            $"Система: смена «{CurrentProduct.displayName}». Текущая деталь — {first}");
    }

    /// <summary>Система сама ведёт активную деталь по пайплайну.</summary>
    public void AdvanceToNextAssignedPart()
    {
        if (Parts.Count == 0) return;

        var cur = ActivePart;
        if (cur != null
            && cur.status != PartPipelineStatus.Ready
            && cur.status != PartPipelineStatus.Assembled
            && cur.status != PartPipelineStatus.Scrap)
            return;

        for (int i = 0; i < Parts.Count; i++)
        {
            var p = Parts[i];
            if (p.status == PartPipelineStatus.Ready
                || p.status == PartPipelineStatus.Assembled
                || p.status == PartPipelineStatus.Scrap)
                continue;

            if (ActivePartIndex != i)
            {
                ActivePartIndex = i;
                GameplayHud.Instance?.ShowToast($"Система: следующая деталь — «{p.partName}»");
                Notify();
            }
            return;
        }
    }

    public void OnStationUsed(StationInteractable station, PlayerInteractor user)
    {
        if (station == null || user == null) return;
        switch (station.Kind)
        {
            case StationKind.JobBoard:
                GameplayHud.Instance?.OpenJobBoard(user);
                station.ForceRelease(); // табло только смотрят, не занимают
                break;
            case StationKind.Measure:
                GameplayHud.Instance?.OpenMeasure(user);
                break;
            case StationKind.Draft:
                GameplayHud.Instance?.OpenDraft(user);
                break;
            case StationKind.Warehouse:
                GameplayHud.Instance?.OpenWarehouse(user);
                break;
            case StationKind.Cutting:
            case StationKind.Lathe:
            case StationKind.Mill:
            case StationKind.Press:
                GameplayHud.Instance?.OpenMachine(station.Kind, user);
                break;
            case StationKind.Assembly:
                GameplayHud.Instance?.OpenAssembly(user);
                break;
            case StationKind.BossTask:
                BossTaskSystem.Instance?.OnPhoneUsed(user);
                station.ForceRelease();
                break;
        }
    }

    public void ApplyMeasurement(PartDimensions measured)
    {
        var part = ActivePart;
        if (part == null) return;
        part.measured = measured;
        part.hasMeasurement = true;
        part.status = PartPipelineStatus.Measuring;
        Notify();
    }

    public void ApplyDrawing(MetalGrade metal)
    {
        var part = ActivePart;
        if (part == null) return;
        part.hasDrawing = true;
        part.orderedMetal = metal;
        part.status = PartPipelineStatus.Drawing;
        Notify();
    }

    public bool TryOrderBlank(out string error)
    {
        error = null;
        var part = ActivePart;
        if (part == null)
        {
            error = "Нет активной детали";
            return false;
        }
        if (!part.hasDrawing)
        {
            error = "Сначала нужен чертёж";
            return false;
        }
        if (Budget < part.blankCost)
        {
            error = "Не хватает бюджета";
            return false;
        }

        Budget -= part.blankCost;
        if (part.orderedMetal != part.requiredMetal)
            Budget -= part.blankCost * 0.25f; // штраф за не ту марку

        part.status = PartPipelineStatus.Ordered;
        FactoryItemSpawner.SpawnBlank(part);
        Notify();
        return true;
    }

    public void ApplyMachineResult(PartDimensions actual, bool success)
    {
        var part = ActivePart;
        if (part == null) return;
        part.actual = actual;
        part.inTolerance = success && WithinTolerance(part.ideal, actual, part.toleranceMm)
                           && part.orderedMetal == part.requiredMetal;
        if (part.inTolerance)
        {
            part.status = PartPipelineStatus.Ready;
            FactoryItemSpawner.SpawnFinishedPart(part);
        }
        else
        {
            part.status = PartPipelineStatus.Scrap;
            ScrapCount++;
            FactoryItemSpawner.SpawnScrap(part);
        }
        Notify();
        AdvanceToNextAssignedPart();
    }

    public void TryAssemble()
    {
        if (Parts.Count == 0) return;
        foreach (var p in Parts)
        {
            if (p.status != PartPipelineStatus.Ready && p.status != PartPipelineStatus.Assembled)
            {
                GameplayHud.Instance?.ShowToast($"Не готова: {p.partName}");
                return;
            }
        }

        foreach (var p in Parts)
            p.status = PartPipelineStatus.Assembled;

        QuotaDone++;
        GameplayHud.Instance?.ShowToast($"Изделие сдано! {QuotaDone}/{QuotaTarget}");
        Notify();

        if (QuotaDone >= QuotaTarget)
            EndShift();
        else
            BeginNextProductInShift();
    }

    void BeginNextProductInShift()
    {
        // Упрощённо: снова то же изделие
        var product = CurrentProduct;
        float budget = Budget;
        int quota = QuotaTarget;
        int done = QuotaDone;
        int scrap = ScrapCount;
        float time = ShiftSecondsLeft;
        BeginShift(product, budget, quota, time);
        QuotaDone = done;
        ScrapCount = scrap;
        ShiftRunning = true;
    }

    public void EndShift()
    {
        ShiftRunning = false;
        ShiftFinished = true;
        float score = QuotaDone * 100f - ScrapCount * 20f + Budget * 0.1f;
        SeasonProgress.OnShiftCompleted();
        GameplayHud.Instance?.ShowShiftResults(QuotaDone, QuotaTarget, ScrapCount, Budget, score);
        Notify();
    }

    public static bool WithinTolerance(PartDimensions ideal, PartDimensions actual, float tol)
    {
        return Mathf.Abs(ideal.lengthMm - actual.lengthMm) <= tol
               && Mathf.Abs(ideal.widthMm - actual.widthMm) <= tol
               && Mathf.Abs(ideal.heightMm - actual.heightMm) <= tol
               && Mathf.Abs(ideal.holeDiameterMm - actual.holeDiameterMm) <= tol;
    }

    public void AddBudget(float amount)
    {
        Budget += amount;
        Notify();
    }

    public void BumpQuota(int delta)
    {
        QuotaTarget = Mathf.Max(1, QuotaTarget + delta);
        Notify();
    }

    public void TightenActiveTolerance(float amount)
    {
        var part = ActivePart;
        if (part == null) return;
        part.toleranceMm = Mathf.Max(0.3f, part.toleranceMm - amount);
        Notify();
    }

    void Notify() => StateChanged?.Invoke();
}
