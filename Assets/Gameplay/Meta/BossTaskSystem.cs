using UnityEngine;

/// <summary>
/// Стационарный телефон шефа: иногда звонит, игрок отвечает и получает короткое поручение.
/// </summary>
public class BossTaskSystem : MonoBehaviour
{
    public static BossTaskSystem Instance { get; private set; }

    public string CurrentTask { get; private set; } = "";
    public bool IsRinging { get; private set; }
    public bool HasOpenTask => !string.IsNullOrEmpty(CurrentTask);

    float _nextRingAt;
    float _ringStartedAt;
    float _ringPulse;
    StationInteractable _phone;
    Transform _beacon;
    Vector3 _beaconBaseScale = Vector3.one * 0.28f;
    Renderer _beaconRend;
    Color _beaconBase = new Color(0.7f, 0.35f, 0.55f);

    static readonly string[] Tasks =
    {
        "Принеси лом на склад",
        "Протри стол замера",
        "Проверь свёрла у фрезера",
        "Отнеси чертёж на цех",
        "Убери брак со сборки",
        "Проверь бюджет на табло",
        "Найди пропавший касок"
    };

    void Awake()
    {
        Instance = this;
        ScheduleNextRing(8f, 20f);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        CachePhone();
    }

    void Update()
    {
        var session = GameSessionState.Instance;
        if (session == null || !session.ShiftRunning)
        {
            StopRing(false);
            return;
        }

        if (_phone == null)
            CachePhone();

        if (!IsRinging && !HasOpenTask && Time.time >= _nextRingAt)
            StartRing();

        if (IsRinging)
        {
            _ringPulse += Time.deltaTime * 8f;
            UpdateRingVisual(true);
            // Если долго не берут — сброс и перезвон позже
            if (Time.time - _ringStartedAt > 25f)
            {
                StopRing(false);
                GameplayHud.Instance?.ShowToast("Звонок сброшен… шеф перезвонит");
                ScheduleNextRing(20f, 40f);
            }
        }
        else
        {
            UpdateRingVisual(false);
        }
    }

    public void ResetTasks()
    {
        CurrentTask = "";
        StopRing(false);
        ScheduleNextRing(12f, 25f);
    }

    public string GetPhonePrompt()
    {
        if (IsRinging)
            return "Ответить на звонок";
        if (HasOpenTask)
            return $"Отчитаться: {CurrentTask}";
        return "Телефон молчит";
    }

    public void OnPhoneUsed(PlayerInteractor user)
    {
        if (user == null) return;

        if (IsRinging)
        {
            AnswerCall();
            return;
        }

        if (HasOpenTask)
        {
            CompleteTask();
            return;
        }

        GameplayHud.Instance?.ShowToast("Тишина. Шеф пока не звонит.");
    }

    void AnswerCall()
    {
        StopRing(false);
        CurrentTask = Tasks[Random.Range(0, Tasks.Length)];
        GameplayHud.Instance?.ShowToast($"Шеф: «{CurrentTask}!» — сделай и отчитайся у телефона");
    }

    void CompleteTask()
    {
        string done = CurrentTask;
        CurrentTask = "";
        PaintUpgradeState.Instance?.AddPoints(10);
        GameSessionState.Instance?.AddBudget(15f);
        GameplayHud.Instance?.ShowToast($"Отчёт принят: {done} (+бюджет, +Paint)");
        ScheduleNextRing(35f, 70f);
    }

    void StartRing()
    {
        IsRinging = true;
        _ringStartedAt = Time.time;
        _ringPulse = 0f;
        GameplayHud.Instance?.ShowToast("☎ Телефон шефа звонит!");
        UpdatePhoneLabel();
    }

    void StopRing(bool keepTask)
    {
        IsRinging = false;
        UpdateRingVisual(false);
        UpdatePhoneLabel();
        if (!keepTask) { /* task unchanged */ }
    }

    void ScheduleNextRing(float min, float max)
    {
        _nextRingAt = Time.time + Random.Range(min, max);
    }

    void CachePhone()
    {
        foreach (var s in Object.FindObjectsByType<StationInteractable>(FindObjectsInactive.Exclude))
        {
            if (s.Kind != StationKind.BossTask) continue;
            _phone = s;
            var beacon = s.transform.parent != null
                ? FindBeaconNear(s.transform)
                : null;
            if (beacon == null)
                beacon = s.transform.Find("Beacon");
            // Beacon may be sibling under FactoryZones
            if (beacon == null && s.transform.parent != null)
            {
                foreach (Transform t in s.transform.parent)
                {
                    if (t.name == "Beacon" && Vector3.Distance(t.position, s.transform.position) < 3f)
                    {
                        beacon = t;
                        break;
                    }
                }
            }
            _beacon = beacon;
            if (_beacon != null)
            {
                _beaconBaseScale = _beacon.localScale;
                _beaconRend = _beacon.GetComponent<Renderer>();
                if (_beaconRend != null)
                    _beaconBase = _beaconRend.material.color;
            }
            UpdatePhoneLabel();
            break;
        }
    }

    static Transform FindBeaconNear(Transform station)
    {
        if (station.parent == null) return null;
        Transform best = null;
        float bestD = 3f;
        foreach (Transform t in station.parent)
        {
            if (t.name != "Beacon") continue;
            float d = Vector3.Distance(t.position, station.position);
            if (d < bestD)
            {
                bestD = d;
                best = t;
            }
        }
        return best;
    }

    void UpdateRingVisual(bool ringing)
    {
        if (_beacon == null) return;
        if (ringing)
        {
            float s = 1f + 0.35f * (0.5f + 0.5f * Mathf.Sin(_ringPulse));
            _beacon.localScale = _beaconBaseScale * s;
            if (_beaconRend != null)
            {
                Color c = Color.Lerp(_beaconBase, Color.yellow, 0.5f + 0.5f * Mathf.Sin(_ringPulse * 1.3f));
                if (_beaconRend.material.HasProperty("_BaseColor"))
                    _beaconRend.material.SetColor("_BaseColor", c);
                _beaconRend.material.color = c;
            }
        }
        else
        {
            _beacon.localScale = _beaconBaseScale;
            if (_beaconRend != null)
            {
                if (_beaconRend.material.HasProperty("_BaseColor"))
                    _beaconRend.material.SetColor("_BaseColor", _beaconBase);
                _beaconRend.material.color = _beaconBase;
            }
        }
    }

    void UpdatePhoneLabel()
    {
        // Подпись обновляется через Prompt у StationInteractable
    }

    // Совместимость со старым API
    public void TryCompleteNear(PlayerInteractor user) => OnPhoneUsed(user);
}
