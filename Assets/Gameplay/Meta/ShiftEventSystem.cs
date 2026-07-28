using UnityEngine;

/// <summary>Случайные события смены (§6 ГДД).</summary>
public class ShiftEventSystem : MonoBehaviour
{
    public static ShiftEventSystem Instance { get; private set; }

    float _nextEventAt;
    public string LastEvent { get; private set; }

    void Awake()
    {
        Instance = this;
        _nextEventAt = Time.time + 60f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        var s = GameSessionState.Instance;
        if (s == null || !s.ShiftRunning || s.ShiftFinished)
            return;
        if (Time.time < _nextEventAt)
            return;

        FireRandom();
        _nextEventAt = Time.time + Random.Range(70f, 140f);
    }

    void FireRandom()
    {
        string[] events =
        {
            "Отключился станок резки на 10 сек — перегруз",
            "На складе пересорт металла — проверьте марку!",
            "Проверка от шефа — не AFK'айте у станков",
            "Заказчик прислал кривой эталон (допуск ужесточён)",
            "Кто-то утащил свёрла с фрезера",
            "Внеплановый срочный заказ — квота +1",
            "Короткое замыкание в КБ — свет моргнул"
        };

        LastEvent = events[Random.Range(0, events.Length)];
        GameplayHud.Instance?.ShowToast($"Событие: {LastEvent}");

        if (LastEvent.Contains("квота"))
            GameSessionState.Instance.BumpQuota(1);
        if (LastEvent.Contains("допуск"))
            GameSessionState.Instance.TightenActiveTolerance(0.5f);
    }
}
