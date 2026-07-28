using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

/// <summary>HUD завода: подсказка E, панели станций, смена.</summary>
public class GameplayHud : MonoBehaviour
{
    public static GameplayHud Instance { get; private set; }
    public static bool BlocksWorldInput { get; private set; }

    Font _font;
    Sprite _sprite;
    Canvas _canvas;
    Text _prompt;
    Text _nearbyHint;
    GameObject _promptBg;
    Text _shiftInfo;
    Text _toast;
    float _toastUntil;

    GameObject _panelRoot;
    Transform _activePanel;
    Transform _panelContent;
    readonly List<GameObject> _panelWidgets = new List<GameObject>();

    RawImage _paintImage;
    Texture2D _paintTex;
    bool _painting;
    Vector2Int _lastPaint;
    MetalGrade _draftMetal = MetalGrade.SteelSt3;

    float _cutAlign;
    float _cutTarget = 0.5f;

    PlayerInteractor _user;
    StationInteractable _openStation;
    StationKind _machineKind;

    static readonly Color PanelBg = new Color(0.08f, 0.1f, 0.09f, 0.95f);
    static readonly Color Accent = new Color(0.45f, 0.78f, 0.42f, 1f);

    void Awake()
    {
        Instance = this;
        BlocksWorldInput = false;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        EnsureEventSystem();
        BuildChrome();
        if (GameSessionState.Instance != null)
            GameSessionState.Instance.StateChanged += RefreshShift;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        BlocksWorldInput = false;
        if (GameSessionState.Instance != null)
            GameSessionState.Instance.StateChanged -= RefreshShift;
        if (_paintTex != null)
            Destroy(_paintTex);
    }

    void Update()
    {
        UpdatePrompt();
        RefreshShift();
        if (_toast != null && Time.time > _toastUntil)
            _toast.text = "";

        if (BlocksWorldInput && WasCancelPressed())
            ClosePanel();

        if (_painting && _paintImage != null && IsPointerHeld())
            PaintAtPointer();
    }

    static void EnsureEventSystem()
    {
        var existing = Object.FindAnyObjectByType<EventSystem>();
        if (existing != null)
        {
#if ENABLE_INPUT_SYSTEM
            var module = existing.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                var old = existing.GetComponent<BaseInputModule>();
                if (old != null) Object.Destroy(old);
                module = existing.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            module.AssignDefaultActions();
#endif
            return;
        }

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    void BuildChrome()
    {
        var go = new GameObject("GameplayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        _canvas = go.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 250;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        _prompt = CreateText(go.transform, "Prompt", "", 34, TextAnchor.MiddleCenter);
        var prt = _prompt.rectTransform;
        prt.anchorMin = new Vector2(0.5f, 0f);
        prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0, 70);
        prt.sizeDelta = new Vector2(1000, 70);
        _prompt.fontStyle = FontStyle.Bold;

        var promptBg = new GameObject("PromptBg", typeof(RectTransform), typeof(Image));
        promptBg.transform.SetParent(go.transform, false);
        promptBg.transform.SetSiblingIndex(0);
        var bgImg = promptBg.GetComponent<Image>();
        bgImg.sprite = _sprite;
        bgImg.color = new Color(0f, 0f, 0f, 0.65f);
        bgImg.raycastTarget = false;
        var bgrt = promptBg.GetComponent<RectTransform>();
        bgrt.anchorMin = new Vector2(0.5f, 0f);
        bgrt.anchorMax = new Vector2(0.5f, 0f);
        bgrt.pivot = new Vector2(0.5f, 0f);
        bgrt.anchoredPosition = new Vector2(0, 55);
        bgrt.sizeDelta = new Vector2(920, 90);
        _promptBg = promptBg;
        _prompt.transform.SetAsLastSibling();

        _nearbyHint = CreateText(go.transform, "Nearby", "", 18, TextAnchor.LowerCenter);
        var nrt = _nearbyHint.rectTransform;
        nrt.anchorMin = new Vector2(0.5f, 0f);
        nrt.anchorMax = new Vector2(0.5f, 0f);
        nrt.pivot = new Vector2(0.5f, 0f);
        nrt.anchoredPosition = new Vector2(0, 20);
        nrt.sizeDelta = new Vector2(1000, 30);
        _nearbyHint.color = new Color(0.75f, 0.85f, 0.7f, 0.95f);
        _nearbyHint.raycastTarget = false;

        _shiftInfo = CreateText(go.transform, "Shift", "", 20, TextAnchor.UpperLeft);
        var srt = _shiftInfo.rectTransform;
        srt.anchorMin = new Vector2(0, 1);
        srt.anchorMax = new Vector2(0, 1);
        srt.pivot = new Vector2(0, 1);
        srt.anchoredPosition = new Vector2(20, -20);
        srt.sizeDelta = new Vector2(560, 150);
        _shiftInfo.raycastTarget = false;

        _toast = CreateText(go.transform, "Toast", "", 24, TextAnchor.UpperCenter);
        var trt = _toast.rectTransform;
        trt.anchorMin = new Vector2(0.5f, 1);
        trt.anchorMax = new Vector2(0.5f, 1);
        trt.pivot = new Vector2(0.5f, 1);
        trt.anchoredPosition = new Vector2(0, -80);
        trt.sizeDelta = new Vector2(900, 40);
        _toast.raycastTarget = false;

        _panelRoot = new GameObject("PanelRoot", typeof(RectTransform));
        _panelRoot.transform.SetParent(go.transform, false);
        Stretch(_panelRoot.GetComponent<RectTransform>());
        _panelRoot.SetActive(false);
    }

    void UpdatePrompt()
    {
        if (_prompt == null) return;
        if (BlocksWorldInput)
        {
            _prompt.text = "Esc — закрыть";
            if (_nearbyHint != null) _nearbyHint.text = "";
            if (_promptBg != null) _promptBg.SetActive(true);
            return;
        }

        PlayerInteractor interactor = FindLocalInteractor();
        bool showBg = false;
        if (interactor != null && interactor.Current != null)
        {
            _prompt.text = $"[E]  {interactor.Current.Prompt}";
            showBg = true;
            if (_nearbyHint != null)
                _nearbyHint.text = "Подойди ближе к цветному кубу с подписью и нажми E (можно не целиться точно)";
        }
        else if (interactor != null && interactor.Carry != null && interactor.Carry.HasItem)
        {
            _prompt.text = $"[G] Положить ({interactor.Carry.Held.PartName})";
            showBg = true;
            if (_nearbyHint != null) _nearbyHint.text = "";
        }
        else
        {
            _prompt.text = "";
            if (_nearbyHint != null)
            {
                string hint = FindNearestStationHint(interactor);
                _nearbyHint.text = string.IsNullOrEmpty(hint)
                    ? "Ищи цветные кубы с подписью и жёлтым шариком сверху"
                    : hint;
            }
        }

        if (_promptBg != null)
            _promptBg.SetActive(showBg);
    }

    static string FindNearestStationHint(PlayerInteractor interactor)
    {
        if (interactor == null) return "";
        StationInteractable best = null;
        float bestDist = 25f;
        foreach (var s in Object.FindObjectsByType<StationInteractable>(FindObjectsInactive.Exclude))
        {
            float d = Vector3.Distance(interactor.transform.position, s.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = s;
            }
        }
        if (best == null) return "";
        if (bestDist <= best.UseRadius)
            return $"Рядом: {best.StationLabel} — жми E";
        return $"Ближайшее: {best.StationLabel} (~{bestDist:0} м)";
    }

    void RefreshShift()
    {
        var s = GameSessionState.Instance;
        if (s == null || _shiftInfo == null) return;
        var part = s.ActivePart;
        string partLine = part != null
            ? $"{part.partName} [{part.status}] метал={ProductCatalog.MetalName(part.requiredMetal)}"
            : "—";
        int m = Mathf.FloorToInt(s.ShiftSecondsLeft / 60f);
        int sec = Mathf.FloorToInt(s.ShiftSecondsLeft % 60f);
        _shiftInfo.text =
            $"Смена {(s.ShiftRunning ? "идёт" : s.ShiftFinished ? "конец" : "—")}  {m:00}:{sec:00}\n" +
            $"Квота {s.QuotaDone}/{s.QuotaTarget}  Бюджет {s.Budget:0}  Брак {s.ScrapCount}\n" +
            $"Изделие: {s.CurrentProduct?.displayName}\nДеталь: {partLine}\n" +
            PhoneHudLine();
    }

    static string PhoneHudLine()
    {
        var phone = BossTaskSystem.Instance;
        if (phone == null) return "";
        if (phone.IsRinging) return "☎ Телефон ЗВОНИТ — ответь!";
        if (phone.HasOpenTask) return $"☎ Поручение: {phone.CurrentTask}";
        return "☎ Телефон молчит";
    }

    public void ShowToast(string msg)
    {
        if (_toast == null) return;
        _toast.text = msg;
        _toastUntil = Time.time + 3f;
    }

    public void OpenJobBoard(PlayerInteractor user = null)
    {
        _user = user;
        _machineKind = StationKind.JobBoard;
        var s = GameSessionState.Instance;
        if (s == null) return;
        BeginPanel($"Наряд смены — {s.CurrentProduct?.displayName ?? "—"}");
        AddLabel("Задания выдаёт система. Здесь только статус наряда:");
        AddLabel($"Квота: {s.QuotaDone}/{s.QuotaTarget}  |  Бюджет: {s.Budget:0}");
        var active = s.ActivePart;
        AddLabel(active != null
            ? $"Сейчас в работе: {active.partName} [{active.status}]"
            : "Нет активной детали");
        for (int i = 0; i < s.Parts.Count; i++)
        {
            var p = s.Parts[i];
            string mark = (active != null && p == active) ? " ← система" : "";
            AddLabel($"• {p.partName}: {p.status}{mark}");
        }
        AddButton("Закрыть", () => ClosePanel());
    }

    public void OpenMeasure(PlayerInteractor user)
    {
        _user = user;
        _machineKind = StationKind.Measure;
        var part = GameSessionState.Instance?.ActivePart;
        if (part == null)
        {
            ShowToast("Система ещё не выдала деталь в работу");
            return;
        }

        BeginPanel($"Замер: {part.partName}");
        AddLabel($"Эталон: {part.ideal.lengthMm}×{part.ideal.widthMm}×{part.ideal.heightMm} мм, Ø{part.ideal.holeDiameterMm}");
        AddLabel("Снимите размеры (можно ошибиться — это фича):");

        float l = part.ideal.lengthMm + Random.Range(-3f, 3f);
        float w = part.ideal.widthMm + Random.Range(-3f, 3f);
        float h = part.ideal.heightMm + Random.Range(-1f, 1f);
        float d = part.ideal.holeDiameterMm + Random.Range(-1f, 1f);

        var lField = AddFloatField("Длина мм", l);
        var wField = AddFloatField("Ширина мм", w);
        var hField = AddFloatField("Высота мм", h);
        var dField = AddFloatField("Ø отверстия", d);

        AddButton("Записать замер", () =>
        {
            GameSessionState.Instance.ApplyMeasurement(new PartDimensions
            {
                lengthMm = Parse(lField, l),
                widthMm = Parse(wField, w),
                heightMm = Parse(hField, h),
                holeDiameterMm = Parse(dField, d)
            });
            ShowToast("Замер сохранён");
            ClosePanel();
        });
        AddButton("Отмена", () => ClosePanel());
    }

    public void OpenDraft(PlayerInteractor user)
    {
        _user = user;
        var part = GameSessionState.Instance?.ActivePart;
        if (part == null)
        {
            ShowToast("Нет активной детали");
            return;
        }
        if (!part.hasMeasurement)
        {
            ShowToast("Сначала сделайте замер");
            return;
        }

        BeginPanel($"Чертёж: {part.partName}");
        AddLabel($"Замер: {part.measured.lengthMm:0.#}×{part.measured.widthMm:0.#}×{part.measured.heightMm:0.#}");
        if (PaintUpgradeState.Instance != null && PaintUpgradeState.Instance.StraightLines)
            AddLabel("Прокачка: прямые линии включены (зажми Shift)");

        EnsurePaintTex();
        var paintGo = new GameObject("Paint", typeof(RectTransform), typeof(RawImage));
        paintGo.transform.SetParent(_panelContent != null ? _panelContent : _activePanel, false);
        _paintImage = paintGo.GetComponent<RawImage>();
        _paintImage.texture = _paintTex;
        var le = paintGo.AddComponent<LayoutElement>();
        le.preferredHeight = 280;
        le.minHeight = 280;
        _panelWidgets.Add(paintGo);
        _painting = true;

        _draftMetal = part.requiredMetal;
        AddButton($"Металл сейчас: {ProductCatalog.MetalName(_draftMetal)} → сменить", () =>
        {
            _draftMetal = (MetalGrade)(((int)_draftMetal + 1) % 4);
            ShowToast($"Марка: {ProductCatalog.MetalName(_draftMetal)}");
        });

        AddButton("Отправить в цех", () =>
        {
            GameSessionState.Instance.ApplyDrawing(_draftMetal);
            FactoryItemSpawner.SpawnBlueprint(part);
            ShowToast("Чертёж отправлен");
            ClosePanel();
        });
        AddButton("Очистить холст", () => ClearPaint());
        AddButton("Отмена", () => ClosePanel());
    }

    public void OpenWarehouse(PlayerInteractor user)
    {
        _user = user;
        var part = GameSessionState.Instance?.ActivePart;
        if (part == null)
        {
            ShowToast("Нет активной детали");
            return;
        }

        BeginPanel("Склад / заказ заготовки");
        AddLabel($"Нужно: {part.partName}, {ProductCatalog.MetalName(part.requiredMetal)}, цена {part.blankCost}");
        AddLabel($"В чертеже металл: {ProductCatalog.MetalName(part.orderedMetal)} | Бюджет: {GameSessionState.Instance.Budget:0}");
        AddButton("Заказать заготовку", () =>
        {
            if (GameSessionState.Instance.TryOrderBlank(out var err))
            {
                ShowToast("Заготовка на складе");
                ClosePanel();
            }
            else
                ShowToast(err);
        });
        AddButton("Отмена", () => ClosePanel());
    }

    public void OpenMachine(StationKind kind, PlayerInteractor user)
    {
        _user = user;
        _machineKind = kind;
        var part = GameSessionState.Instance?.ActivePart;
        if (part == null)
        {
            ShowToast("Нет активной детали");
            return;
        }

        MachineType needed = part.requiredMachine;
        MachineType got = kind switch
        {
            StationKind.Cutting => MachineType.Cutting,
            StationKind.Lathe => MachineType.Lathe,
            StationKind.Mill => MachineType.Mill,
            StationKind.Press => MachineType.Press,
            _ => MachineType.Cutting
        };
        if (got != needed)
        {
            ShowToast($"Для «{part.partName}» нужен станок: {needed}");
            return;
        }

        string title = kind switch
        {
            StationKind.Cutting => "Резка по линейке",
            StationKind.Lathe => "Токарный (метка)",
            StationKind.Mill => "Фрезер (попадание)",
            StationKind.Press => "Пресс (совмещение)",
            _ => "Станок"
        };

        BeginPanel(title + $": {part.partName}");
        AddLabel("Поднеси заготовку (опционально) и совмести разметку. A/D или стрелки — сдвиг.");
        _cutTarget = 0.5f;
        _cutAlign = Random.Range(0.2f, 0.8f);

        var bar = new GameObject("CutBar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(_panelContent != null ? _panelContent : _activePanel, false);
        bar.GetComponent<Image>().sprite = _sprite;
        bar.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);
        var ble = bar.AddComponent<LayoutElement>();
        ble.preferredHeight = 36;
        _panelWidgets.Add(bar);

        AddLabel("Зелёная зона = допуск. Пробел — резать.");
        AddButton("Резать / обработать", () => FinishMachine(part));
        AddButton("Отмена", () => ClosePanel());
    }

    void LateUpdate()
    {
        if (!BlocksWorldInput || _panelRoot == null || !_panelRoot.activeSelf)
            return;
        if (_machineKind == StationKind.JobBoard)
            return;

        if (_machineKind == StationKind.Cutting || _machineKind == StationKind.Lathe
            || _machineKind == StationKind.Mill || _machineKind == StationKind.Press)
        {
            float dir = 0f;
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir += 1f;
                if (kb.spaceKey.wasPressedThisFrame)
                {
                    var part = GameSessionState.Instance?.ActivePart;
                    if (part != null) FinishMachine(part);
                }
            }
#endif
            _cutAlign = Mathf.Clamp01(_cutAlign + dir * Time.unscaledDeltaTime * 0.6f);
        }
    }

    void FinishMachine(PartJob part)
    {
        float err = Mathf.Abs(_cutAlign - _cutTarget);
        float tolNorm = Mathf.Clamp01(part.toleranceMm / 5f);
        bool ok = err <= 0.08f + tolNorm * 0.05f;

        var src = part.hasMeasurement ? part.measured : part.ideal;
        float drift = (0.5f - _cutAlign) * 8f;
        var actual = new PartDimensions
        {
            lengthMm = src.lengthMm + drift,
            widthMm = src.widthMm + drift * 0.3f,
            heightMm = src.heightMm,
            holeDiameterMm = src.holeDiameterMm + (_machineKind == StationKind.Mill ? drift * 0.2f : 0f)
        };

        if (_user != null && _user.Carry != null && _user.Carry.HasItem
            && _user.Carry.Held.Kind == FactoryItemKind.Blank)
        {
            var blank = _user.Carry.ConsumeHeld();
            if (blank != null)
                Destroy(blank.gameObject);
        }

        part.status = PartPipelineStatus.AtMachine;
        GameSessionState.Instance.ApplyMachineResult(actual, ok);
        ShowToast(ok ? "Деталь в допуске!" : "Брак / мимо допуска");
        ClosePanel();
    }

    public void OpenAssembly(PlayerInteractor user)
    {
        _user = user;
        BeginPanel("Сборка изделия");
        var s = GameSessionState.Instance;
        foreach (var p in s.Parts)
            AddLabel($"{p.partName}: {p.status} {(p.inTolerance ? "OK" : "")}");
        AddButton("Сдать изделие", () =>
        {
            s.TryAssemble();
            ClosePanel();
        });
        AddButton("Отмена", () => ClosePanel());
    }

    public void ShowShiftResults(int done, int target, int scrap, float budget, float score)
    {
        BeginPanel("Итоги смены");
        AddLabel($"Квота: {done}/{target}");
        AddLabel($"Брак: {scrap}");
        AddLabel($"Бюджет остаток: {budget:0}");
        AddLabel($"Очки: {score:0}");
        AddButton("Новая смена", () =>
        {
            var product = SeasonProgress.NextProduct();
            int quota = SeasonProgress.GigUnlocked ? 2 : 1;
            GameSessionState.Instance.BeginShift(product, 500f, quota, 900f);
            ClosePanel();
        });
        AddButton("Закрыть", () => ClosePanel());
    }

    void BeginPanel(string title)
    {
        ClosePanel(false);
        BlocksWorldInput = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _panelRoot.SetActive(true);

        if (_user != null)
        {
            _openStation = _user.Current as StationInteractable;
            if (_openStation == null)
                _openStation = _user.Nearest as StationInteractable;
        }

        var bg = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        bg.transform.SetParent(_panelRoot.transform, false);
        Stretch(bg.GetComponent<RectTransform>());
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = _sprite;
        bgImg.color = new Color(0, 0, 0, 0.55f);
        bg.GetComponent<Button>().onClick.AddListener(() => ClosePanel());
        _panelWidgets.Add(bg);

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(_panelRoot.transform, false);
        var img = panel.GetComponent<Image>();
        img.sprite = _sprite;
        img.color = PanelBg;
        img.raycastTarget = true;
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(720, 620);
        var v = panel.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(20, 20, 16, 16);
        v.spacing = 8;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        _panelWidgets.Add(panel);
        _activePanel = panel.transform;

        var titleGo = CreateText(_activePanel, "Title", title, 28, TextAnchor.MiddleCenter);
        Pref(titleGo.gameObject, 36);
        titleGo.fontStyle = FontStyle.Bold;
        titleGo.raycastTarget = false;

        var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement));
        scrollGo.transform.SetParent(_activePanel, false);
        var scrollLe = scrollGo.GetComponent<LayoutElement>();
        scrollLe.flexibleHeight = 1f;
        scrollLe.minHeight = 400f;
        scrollLe.preferredHeight = 520f;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        var vpImg = viewport.GetComponent<Image>();
        vpImg.sprite = _sprite;
        vpImg.color = new Color(1, 1, 1, 0.02f);
        vpImg.raycastTarget = true;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = Vector2.zero;
        var cv = content.GetComponent<VerticalLayoutGroup>();
        cv.spacing = 10;
        cv.padding = new RectOffset(4, 4, 4, 12);
        cv.childControlHeight = false;
        cv.childControlWidth = true;
        cv.childForceExpandWidth = true;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35f;

        _panelContent = content.transform;
    }

    void ClosePanel(bool relock = true)
    {
        _painting = false;
        _paintImage = null;
        if (_openStation != null && _user != null)
        {
            _openStation.Release(_user);
            _openStation = null;
        }

        foreach (var w in _panelWidgets)
        {
            if (w != null) Destroy(w);
        }
        _panelWidgets.Clear();
        if (_panelRoot != null)
            _panelRoot.SetActive(false);
        BlocksWorldInput = false;
        _activePanel = null;
        _panelContent = null;
        if (relock)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void AddLabel(string text)
    {
        var parent = _panelContent != null ? _panelContent : _activePanel;
        if (parent == null) return;
        var t = CreateText(parent, "Lbl", text, 18, TextAnchor.MiddleLeft);
        Pref(t.gameObject, 28);
        t.raycastTarget = false;
    }

    void AddButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        var parent = _panelContent != null ? _panelContent : _activePanel;
        if (parent == null) return;
        var go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        Pref(go, 46);
        var img = go.GetComponent<Image>();
        img.sprite = _sprite;
        img.color = Color.white;
        img.raycastTarget = true;
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.normalColor = new Color(0.16f, 0.22f, 0.18f);
        c.highlightedColor = new Color(0.22f, 0.32f, 0.24f);
        c.pressedColor = new Color(0.12f, 0.18f, 0.14f);
        btn.colors = c;
        btn.onClick.AddListener(onClick);
        var text = CreateText(go.transform, "T", label, 20, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
    }

    InputField AddFloatField(string label, float value)
    {
        AddLabel(label);
        var parent = _panelContent != null ? _panelContent : _activePanel;
        var go = new GameObject(label + "Field", typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        Pref(go, 36);
        go.GetComponent<Image>().sprite = _sprite;
        go.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.09f);
        var text = CreateText(go.transform, "Text", value.ToString("0.##"), 18, TextAnchor.MiddleLeft);
        Stretch(text.rectTransform, 8, 4);
        var field = go.GetComponent<InputField>();
        field.textComponent = text;
        field.contentType = InputField.ContentType.DecimalNumber;
        field.text = value.ToString("0.##");
        return field;
    }

    void EnsurePaintTex()
    {
        if (_paintTex != null) return;
        _paintTex = new Texture2D(256, 160, TextureFormat.RGBA32, false);
        _paintTex.filterMode = FilterMode.Point;
        ClearPaint();
    }

    void ClearPaint()
    {
        if (_paintTex == null) return;
        var fill = new Color(0.92f, 0.92f, 0.88f, 1f);
        var pixels = _paintTex.GetPixels();
        for (int i = 0; i < pixels.Length; i++) pixels[i] = fill;
        _paintTex.SetPixels(pixels);
        _paintTex.Apply();
    }

    void PaintAtPointer()
    {
        if (_paintImage == null || _paintTex == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _paintImage.rectTransform, PointerPos(), null, out var local))
            return;

        var rect = _paintImage.rectTransform.rect;
        float u = (local.x - rect.x) / rect.width;
        float v = (local.y - rect.y) / rect.height;
        if (u < 0 || u > 1 || v < 0 || v > 1) return;
        int x = Mathf.Clamp(Mathf.FloorToInt(u * _paintTex.width), 0, _paintTex.width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(v * _paintTex.height), 0, _paintTex.height - 1);

        bool straight = PaintUpgradeState.Instance != null && PaintUpgradeState.Instance.StraightLines && IsShiftHeld();
        if (straight && _lastPaint.x >= 0)
            DrawLine(_lastPaint.x, _lastPaint.y, x, y, Accent);
        else
        {
            DrawBrush(x, y, Accent);
            _lastPaint = new Vector2Int(x, y);
        }
        if (!straight)
            _lastPaint = new Vector2Int(x, y);
        _paintTex.Apply();
    }

    void DrawBrush(int x, int y, Color col)
    {
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            int px = x + dx, py = y + dy;
            if (px >= 0 && py >= 0 && px < _paintTex.width && py < _paintTex.height)
                _paintTex.SetPixel(px, py, col);
        }
    }

    void DrawLine(int x0, int y0, int x1, int y1, Color col)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            DrawBrush(x0, y0, col);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    static float Parse(InputField f, float fallback)
    {
        if (f == null) return fallback;
        return float.TryParse(f.text.Replace(',', '.'), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;
    }

    static PlayerInteractor FindLocalInteractor()
    {
        if (NetworkPlayer.LocalPlayer != null)
        {
            var p = NetworkPlayer.LocalPlayer.GetComponent<PlayerInteractor>();
            if (p != null) return p;
        }
        return Object.FindAnyObjectByType<PlayerInteractor>();
    }

    Text CreateText(Transform parent, string name, string content, int size, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = _font;
        t.text = content;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = new Color(0.92f, 0.95f, 0.9f);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static void Pref(GameObject go, float h)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredHeight = h;
        le.minHeight = h;
    }

    static void Stretch(RectTransform rt, float padX = 0, float padY = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padX, padY);
        rt.offsetMax = new Vector2(-padX, -padY);
    }

    static Vector2 PointerPos()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
#endif
        return Input.mousePosition;
    }

    static bool IsPointerHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }

    static bool IsShiftHeld()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }

    static bool WasCancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
