using UnityEngine;

/// <summary>
/// Цех по ролям из ГДД: слева КБ (задания/замер/чертёж/сборка), справа цех (склад/станки).
/// Это один кооп, не две зеркальные PvP-команды.
/// </summary>
public class FactoryLayoutBuilder : MonoBehaviour
{
    static readonly Color ZoneKb = new Color(0.28f, 0.42f, 0.72f, 1f);
    static readonly Color ZoneAsm = new Color(0.28f, 0.52f, 0.4f, 1f);
    static readonly Color ZoneStore = new Color(0.55f, 0.45f, 0.28f, 1f);
    static readonly Color ZoneShop = new Color(0.42f, 0.38f, 0.35f, 1f);
    static readonly Color BossColor = new Color(0.55f, 0.3f, 0.45f, 1f);

    const float KbX = -12f;
    const float ShopX = 12f;

    public void Build()
    {
        var old = transform.Find("FactoryZones");
        if (old != null)
            Destroy(old.gameObject);

        Vector3 origin = ResolveOrigin();
        float y = origin.y + 0.65f;
        var root = new GameObject("FactoryZones");
        root.transform.SetParent(transform, false);

        // Разделитель: КБ | Цех
        CreateFloorMark(root.transform, "Divider",
            new Vector3(origin.x, y + 0.02f, origin.z),
            new Vector3(1.4f, 1f, 36f),
            new Color(0.85f, 0.85f, 0.7f, 0.55f));
        CreateFloorMark(root.transform, "Floor_KB",
            new Vector3(origin.x + KbX, y + 0.015f, origin.z),
            new Vector3(20f, 1f, 34f),
            new Color(0.15f, 0.25f, 0.45f, 0.28f));
        CreateFloorMark(root.transform, "Floor_Shop",
            new Vector3(origin.x + ShopX, y + 0.015f, origin.z),
            new Vector3(20f, 1f, 34f),
            new Color(0.4f, 0.28f, 0.15f, 0.28f));

        CreateWorldLabel(root.transform, "Label_KB", "КБ — ЗАМЕР / ЧЕРТЁЖ / СБОРКА",
            new Vector3(origin.x + KbX, y + 3.2f, origin.z + 14f), ZoneKb);
        CreateWorldLabel(root.transform, "Label_Shop", "ЦЕХ — СКЛАД / СТАНКИ",
            new Vector3(origin.x + ShopX, y + 3.2f, origin.z + 14f), ZoneStore);

        // —— Левая сторона: КБ ——
        float kx = origin.x + KbX;
        float z = origin.z;

        CreateStation(root.transform, "JobBoard", StationKind.JobBoard, "Табло нарядов", "Смотреть наряд смены",
            new Vector3(kx - 4f, y, z + 8f), ZoneKb, new Vector3(2.0f, 1.2f, 1.1f));
        CreateStation(root.transform, "MeasureDesk", StationKind.Measure, "Стол замера", "Замерить деталь",
            new Vector3(kx, y, z + 8f), ZoneKb, new Vector3(1.7f, 1.05f, 1.05f));
        CreateStation(root.transform, "DraftDesk", StationKind.Draft, "Стол чертежа", "Чертить (Paint)",
            new Vector3(kx + 4f, y, z + 8f), ZoneKb, new Vector3(1.7f, 1.05f, 1.05f));
        CreateStation(root.transform, "Assembly", StationKind.Assembly, "Сборка", "Собрать изделие",
            new Vector3(kx, y, z - 8f), ZoneAsm, new Vector3(3.0f, 1.25f, 2.1f));

        // Стационарный телефон шефа (не стол заданий)
        CreatePhone(root.transform, origin, y, z);

        CreateFloorMark(root.transform, "Zone_KB_desks",
            new Vector3(kx, y + 0.025f, z + 8f), new Vector3(14f, 1f, 5f),
            new Color(ZoneKb.r, ZoneKb.g, ZoneKb.b, 0.32f));
        CreateFloorMark(root.transform, "Zone_Assembly",
            new Vector3(kx, y + 0.025f, z - 8f), new Vector3(12f, 1f, 5f),
            new Color(ZoneAsm.r, ZoneAsm.g, ZoneAsm.b, 0.3f));

        // —— Правая сторона: Цех ——
        float sx = origin.x + ShopX;

        CreateStation(root.transform, "Warehouse", StationKind.Warehouse, "Склад", "Заказать заготовку",
            new Vector3(sx - 5f, y, z + 6f), ZoneStore, new Vector3(2.8f, 1.9f, 1.9f));
        CreateStation(root.transform, "Cutting", StationKind.Cutting, "Резка", "Резать по линейке",
            new Vector3(sx + 2f, y, z + 5f), ZoneShop, new Vector3(2.3f, 1.45f, 1.4f));
        CreateStation(root.transform, "Lathe", StationKind.Lathe, "Токарный", "Токарная обработка",
            new Vector3(sx + 6f, y, z + 5f), ZoneShop, new Vector3(2.1f, 1.45f, 1.4f));
        CreateStation(root.transform, "Mill", StationKind.Mill, "Фрезерный", "Фрезеровка / отверстия",
            new Vector3(sx + 2f, y, z - 1f), ZoneShop, new Vector3(2.1f, 1.45f, 1.4f));
        CreateStation(root.transform, "Press", StationKind.Press, "Пресс", "Пресс / гибка",
            new Vector3(sx + 6f, y, z - 1f), ZoneShop, new Vector3(2.1f, 1.45f, 1.4f));

        CreateFloorMark(root.transform, "Zone_Store",
            new Vector3(sx - 5f, y + 0.025f, z + 6f), new Vector3(8f, 1f, 6f),
            new Color(ZoneStore.r, ZoneStore.g, ZoneStore.b, 0.3f));
        CreateFloorMark(root.transform, "Zone_Machines",
            new Vector3(sx + 4f, y + 0.025f, z + 2f), new Vector3(12f, 1f, 12f),
            new Color(ZoneShop.r, ZoneShop.g, ZoneShop.b, 0.28f));
    }

    static Vector3 ResolveOrigin()
    {
        var hall = Object.FindAnyObjectByType<FactoryHall>();
        if (hall != null)
            return hall.transform.position;
        return NetworkPlayer.SpawnBase;
    }

    static void CreatePhone(Transform parent, Vector3 origin, float y, float z)
    {
        // Корпус аппарата
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "BossPhone";
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(origin.x, y, origin.z + 10f);
        go.transform.localScale = new Vector3(0.55f, 0.9f, 0.45f);

        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            Color c = BossColor;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            mat.color = c;
            rend.material = mat;
        }

        var station = go.AddComponent<StationInteractable>();
        station.Configure(StationKind.BossTask, "Телефон шефа", "Ответить", 4f);

        var triggerGo = new GameObject("UseTrigger");
        triggerGo.transform.SetParent(go.transform, false);
        var sphere = triggerGo.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = 2.2f;

        // Трубка
        var handset = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handset.name = "Handset";
        handset.transform.SetParent(parent, false);
        handset.transform.position = go.transform.position + new Vector3(0.35f, 0.55f, 0f);
        handset.transform.localScale = new Vector3(0.7f, 0.18f, 0.22f);
        Object.Destroy(handset.GetComponent<Collider>());
        var hr = handset.GetComponent<Renderer>();
        if (hr != null)
        {
            var hm = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            Color hc = new Color(0.15f, 0.15f, 0.18f);
            if (hm.HasProperty("_BaseColor"))
                hm.SetColor("_BaseColor", hc);
            hm.color = hc;
            hr.material = hm;
        }

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(parent, false);
        labelGo.transform.position = go.transform.position + Vector3.up * 1.35f;
        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = "Телефон шефа\n[E]";
        tm.characterSize = 0.04f;
        tm.fontSize = 36;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        tm.fontStyle = FontStyle.Bold;
        var billboard = labelGo.AddComponent<BillboardLabel>();
        billboard.Follow = go.transform;
        billboard.HeightOffset = 1.35f;

        var beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        beacon.name = "Beacon";
        beacon.transform.SetParent(parent, false);
        beacon.transform.position = go.transform.position + Vector3.up * 1.7f;
        beacon.transform.localScale = Vector3.one * 0.22f;
        Object.Destroy(beacon.GetComponent<Collider>());
        var br = beacon.GetComponent<Renderer>();
        if (br != null)
        {
            var bm = new Material(Shader.Find("Universal Render Pipeline/Unlit")
                                  ?? Shader.Find("Unlit/Color")
                                  ?? Shader.Find("Standard"));
            Color glow = Color.Lerp(BossColor, Color.yellow, 0.35f);
            if (bm.HasProperty("_BaseColor"))
                bm.SetColor("_BaseColor", glow);
            bm.color = glow;
            br.material = bm;
        }
        var follow = beacon.AddComponent<StationBeaconFollow>();
        follow.Target = go.transform;
        follow.HeightOffset = 1.7f;
    }

    static void CreateStation(Transform parent, string name, StationKind kind, string label, string prompt,
        Vector3 pos, Color color, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        Vector3 worldScale = scale * 1.15f;
        go.transform.localScale = worldScale;

        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            mat.color = color;
            rend.material = mat;
        }

        var station = go.AddComponent<StationInteractable>();
        station.Configure(kind, label, prompt, 4.5f);

        var triggerGo = new GameObject("UseTrigger");
        triggerGo.transform.SetParent(go.transform, false);
        triggerGo.transform.localPosition = Vector3.zero;
        var sphere = triggerGo.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = 1.15f;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(parent, false);
        labelGo.transform.position = pos + Vector3.up * (worldScale.y * 0.55f + 0.55f);
        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = label + "\n[E]";
        tm.characterSize = 0.04f;
        tm.fontSize = 36;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        tm.fontStyle = FontStyle.Bold;
        var billboard = labelGo.AddComponent<BillboardLabel>();
        billboard.Follow = go.transform;
        billboard.HeightOffset = worldScale.y * 0.55f + 0.55f;

        var beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        beacon.name = "Beacon";
        beacon.transform.SetParent(parent, false);
        beacon.transform.position = pos + Vector3.up * (worldScale.y * 0.55f + 0.95f);
        beacon.transform.localScale = Vector3.one * 0.28f;
        Object.Destroy(beacon.GetComponent<Collider>());
        var br = beacon.GetComponent<Renderer>();
        if (br != null)
        {
            var bm = new Material(Shader.Find("Universal Render Pipeline/Unlit")
                                  ?? Shader.Find("Unlit/Color")
                                  ?? Shader.Find("Standard"));
            Color glow = Color.Lerp(color, Color.yellow, 0.45f);
            if (bm.HasProperty("_BaseColor"))
                bm.SetColor("_BaseColor", glow);
            bm.color = glow;
            br.material = bm;
        }
        var follow = beacon.AddComponent<StationBeaconFollow>();
        follow.Target = go.transform;
        follow.HeightOffset = worldScale.y * 0.55f + 0.95f;
    }

    static void CreateWorldLabel(Transform parent, string name, string text, Vector3 pos, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.characterSize = 0.07f;
        tm.fontSize = 42;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        tm.fontStyle = FontStyle.Bold;
        go.AddComponent<BillboardLabel>();
    }

    static void CreateFloorMark(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale.x, 0.05f, scale.z);
        Object.Destroy(go.GetComponent<Collider>());
        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            rend.material = mat;
        }
    }
}

/// <summary>Маяк держится над станцией без наследования её scale.</summary>
public class StationBeaconFollow : MonoBehaviour
{
    public Transform Target;
    public float HeightOffset = 1.5f;

    void LateUpdate()
    {
        if (Target == null) return;
        transform.position = Target.position + Vector3.up * HeightOffset;
    }
}
