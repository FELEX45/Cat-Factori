using UnityEngine;

/// <summary>
/// Цех по ролям: КБ слева, станки справа.
/// Где есть FBX в Resources/Stations — ставит модель, иначе цветной куб.
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

        SoftenSceneLights();

        Vector3 origin = ResolveOrigin();
        float floorY = origin.y; // пол зала
        var root = new GameObject("FactoryZones");
        root.transform.SetParent(transform, false);

        CreateFloorMark(root.transform, "Divider",
            new Vector3(origin.x, floorY + 0.08f, origin.z),
            new Vector3(1.4f, 1f, 36f),
            new Color(0.85f, 0.85f, 0.7f, 0.4f));
        CreateFloorMark(root.transform, "Floor_KB",
            new Vector3(origin.x + KbX, floorY + 0.06f, origin.z),
            new Vector3(20f, 1f, 34f),
            new Color(0.15f, 0.25f, 0.45f, 0.2f));
        CreateFloorMark(root.transform, "Floor_Shop",
            new Vector3(origin.x + ShopX, floorY + 0.06f, origin.z),
            new Vector3(20f, 1f, 34f),
            new Color(0.4f, 0.28f, 0.15f, 0.2f));

        CreateWorldLabel(root.transform, "Label_KB", "КБ — ЗАМЕР / ЧЕРТЁЖ / СБОРКА",
            new Vector3(origin.x + KbX, floorY + 3.2f, origin.z + 14f), ZoneKb);
        CreateWorldLabel(root.transform, "Label_Shop", "ЦЕХ — СКЛАД / СТАНКИ",
            new Vector3(origin.x + ShopX, floorY + 3.2f, origin.z + 14f), ZoneStore);

        float kx = origin.x + KbX;
        float z = origin.z;

        // КБ
        // RuletTable = стол замера и стол чертежа (+90° по часовой, x1.5)
        CreateStation(root.transform, "MeasureDesk", StationKind.Measure, "Стол замера", "Замерить деталь",
            new Vector3(kx - 0.5f, floorY, z + 8f), ZoneKb,
            fallbackScale: new Vector3(2.7f, 1.4f, 1.65f),
            modelResource: "Stations/RuletTable",
            targetHeight: 1.0f * 1.5f, yawDegrees: -90f, maxFootprint: 2.4f * 1.5f);

        CreateStation(root.transform, "DraftDesk", StationKind.Draft, "Стол чертежа", "Чертить (Paint)",
            new Vector3(kx + 3.5f, floorY, z + 8f), ZoneKb,
            fallbackScale: new Vector3(2.7f, 1.4f, 1.65f),
            modelResource: "Stations/RuletTable",
            targetHeight: 1.0f * 1.5f, yawDegrees: -90f, maxFootprint: 2.4f * 1.5f);

        // Monitor = табло нарядов на столбе за столами замера (центр КБ), x3, экран с задачами
        CreateStation(root.transform, "JobBoard", StationKind.JobBoard, "Табло нарядов", "Смотреть наряд смены",
            new Vector3(kx, floorY, z + 11f), ZoneKb,
            fallbackScale: new Vector3(1.4f, 1.6f, 0.35f),
            modelResource: "Stations/Monitor",
            targetHeight: 1.7f, yawDegrees: 180f, maxFootprint: 1.6f,
            scaleMul: 3f, mountOnPole: true);

        CreateStation(root.transform, "Assembly", StationKind.Assembly, "Сборка", "Собрать изделие",
            new Vector3(kx, floorY, z - 8f), ZoneAsm,
            fallbackScale: new Vector3(2.6f * 1.5f, 1.1f * 1.5f, 1.8f * 1.5f),
            modelResource: null,
            targetHeight: 1.1f * 1.5f, yawDegrees: -90f);

        CreatePhone(root.transform, origin, floorY, z, yawDegrees: -90f, scaleMul: 1.5f);

        CreateFloorMark(root.transform, "Zone_KB_desks",
            new Vector3(kx, floorY + 0.08f, z + 8f), new Vector3(14f, 1f, 5f),
            new Color(ZoneKb.r, ZoneKb.g, ZoneKb.b, 0.22f));
        CreateFloorMark(root.transform, "Zone_Assembly",
            new Vector3(kx, floorY + 0.08f, z - 8f), new Vector3(12f, 1f, 5f),
            new Color(ZoneAsm.r, ZoneAsm.g, ZoneAsm.b, 0.22f));

        // Цех
        float sx = origin.x + ShopX;

        CreateStation(root.transform, "Warehouse", StationKind.Warehouse, "Склад", "Заказать заготовку",
            new Vector3(sx - 5f, floorY, z + 6f), ZoneStore,
            fallbackScale: new Vector3(2.6f * 1.5f, 1.8f * 1.5f, 1.8f * 1.5f),
            modelResource: null,
            targetHeight: 1.8f * 1.5f, yawDegrees: -90f);

        // TableSaw = резка (+90° по часовой от текущего, x1.5)
        CreateStation(root.transform, "Cutting", StationKind.Cutting, "Резка", "Резать по линейке",
            new Vector3(sx + 2f, floorY, z + 5f), ZoneShop,
            fallbackScale: new Vector3(2.0f * 1.5f, 1.3f * 1.5f, 1.3f * 1.5f),
            modelResource: "Stations/TableSaw",
            targetHeight: 1.25f * 1.5f, yawDegrees: -180f, maxFootprint: 2.2f * 1.5f);

        CreateStation(root.transform, "Lathe", StationKind.Lathe, "Токарный", "Токарная обработка",
            new Vector3(sx + 6f, floorY, z + 5f), ZoneShop,
            fallbackScale: new Vector3(2.0f * 1.5f, 1.35f * 1.5f, 1.2f * 1.5f),
            modelResource: null,
            targetHeight: 1.35f * 1.5f, yawDegrees: -90f);

        CreateStation(root.transform, "Mill", StationKind.Mill, "Фрезерный", "Фрезеровка / отверстия",
            new Vector3(sx + 2f, floorY, z - 1f), ZoneShop,
            fallbackScale: new Vector3(2.0f * 1.5f, 1.35f * 1.5f, 1.2f * 1.5f),
            modelResource: null,
            targetHeight: 1.35f * 1.5f, yawDegrees: -90f);

        CreateStation(root.transform, "Press", StationKind.Press, "Пресс", "Пресс / гибка",
            new Vector3(sx + 6f, floorY, z - 1f), ZoneShop,
            fallbackScale: new Vector3(2.0f * 1.5f, 1.5f * 1.5f, 1.2f * 1.5f),
            modelResource: null,
            targetHeight: 1.5f * 1.5f, yawDegrees: -90f);

        CreateFloorMark(root.transform, "Zone_Store",
            new Vector3(sx - 5f, floorY + 0.08f, z + 6f), new Vector3(8f, 1f, 6f),
            new Color(ZoneStore.r, ZoneStore.g, ZoneStore.b, 0.22f));
        CreateFloorMark(root.transform, "Zone_Machines",
            new Vector3(sx + 4f, floorY + 0.08f, z + 2f), new Vector3(12f, 1f, 12f),
            new Color(ZoneShop.r, ZoneShop.g, ZoneShop.b, 0.2f));
    }

    static void SoftenSceneLights()
    {
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (light == null || light.type != LightType.Directional)
                continue;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = Mathf.Min(light.shadowStrength, 0.7f);
            light.shadowBias = Mathf.Max(light.shadowBias, 0.05f);
            light.shadowNormalBias = Mathf.Max(light.shadowNormalBias, 0.4f);
            if (light.intensity > 1.6f)
                light.intensity = 1.35f;
        }
    }

    static Vector3 ResolveOrigin()
    {
        var hall = Object.FindAnyObjectByType<FactoryHall>();
        if (hall != null)
            return hall.transform.position;
        return NetworkPlayer.SpawnBase;
    }

    static void CreatePhone(Transform parent, Vector3 origin, float floorY, float z,
        float yawDegrees = 0f, float scaleMul = 1f)
    {
        var go = new GameObject("BossPhone");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(origin.x, floorY, origin.z + 10f);
        go.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);

        float s = scaleMul;
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(go.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.45f * s, 0f);
        body.transform.localScale = new Vector3(0.55f * s, 0.9f * s, 0.45f * s);
        TintPrimitive(body, BossColor);

        var handset = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handset.name = "Handset";
        handset.transform.SetParent(go.transform, false);
        handset.transform.localPosition = new Vector3(0.35f * s, 0.85f * s, 0f);
        handset.transform.localScale = new Vector3(0.7f * s, 0.18f * s, 0.22f * s);
        Object.Destroy(handset.GetComponent<Collider>());
        TintPrimitive(handset, new Color(0.15f, 0.15f, 0.18f));

        var station = go.AddComponent<StationInteractable>();
        station.Configure(StationKind.BossTask, "Телефон шефа", "Ответить", 4f * s);

        var triggerGo = new GameObject("UseTrigger");
        triggerGo.transform.SetParent(go.transform, false);
        triggerGo.transform.localPosition = new Vector3(0f, 0.5f * s, 0f);
        var sphere = triggerGo.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = 1.8f * s;

        AttachLabelAndBeacon(parent, go.transform, "Телефон шефа", BossColor, 1.55f * s);
    }

    static GameObject CreateStation(Transform parent, string name, StationKind kind, string label, string prompt,
        Vector3 floorPos, Color color, Vector3 fallbackScale,
        string modelResource, float targetHeight, float yawDegrees = 0f, float maxFootprint = 2.8f,
        float scaleMul = 1f, bool mountOnPole = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = floorPos;
        go.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);

        GameObject visual = null;
        float visualHeight = fallbackScale.y * scaleMul;
        float poleHeight = 0f;

        if (mountOnPole)
        {
            // Столб под монитор/табло
            poleHeight = 1.35f;
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(go.transform, false);
            pole.transform.localPosition = new Vector3(0f, poleHeight * 0.5f, 0f);
            pole.transform.localScale = new Vector3(0.18f, poleHeight * 0.5f, 0.18f);
            Object.Destroy(pole.GetComponent<Collider>());
            TintPrimitive(pole, new Color(0.25f, 0.25f, 0.28f));

            var basePlate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePlate.name = "PoleBase";
            basePlate.transform.SetParent(go.transform, false);
            basePlate.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            basePlate.transform.localScale = new Vector3(0.55f, 0.04f, 0.55f);
            Object.Destroy(basePlate.GetComponent<Collider>());
            TintPrimitive(basePlate, new Color(0.2f, 0.2f, 0.22f));
        }

        if (!string.IsNullOrEmpty(modelResource))
        {
            var prefab = Resources.Load<GameObject>(modelResource);
            if (prefab != null)
            {
                visual = Object.Instantiate(prefab, go.transform);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;

                StripEmbeddedSceneJunk(visual);

                foreach (var col in visual.GetComponentsInChildren<Collider>())
                    Object.Destroy(col);

                float fitHeight = targetHeight * Mathf.Max(0.01f, scaleMul);
                float fitFoot = maxFootprint * Mathf.Max(0.01f, scaleMul);
                visualHeight = FitAndSeatOnFloor(visual, go.transform, fitHeight, fitFoot);

                if (mountOnPole)
                {
                    // Поднять монитор на столб: низ модели на верхушке столба
                    Bounds b = GetWorldBounds(visual);
                    float lift = (go.transform.position.y + poleHeight) - b.min.y;
                    visual.transform.position += Vector3.up * lift;
                    visualHeight = poleHeight + GetWorldBounds(visual).size.y;
                }

                StabilizeRenderers(visual);
            }
            else
            {
                Debug.LogWarning($"[Layout] Модель не найдена: Resources/{modelResource} — куб-заглушка");
            }
        }

        if (visual == null)
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            Vector3 fs = fallbackScale * scaleMul;
            visual.transform.localPosition = new Vector3(0f, fs.y * 0.5f + poleHeight, 0f);
            visual.transform.localScale = fs;
            Object.Destroy(visual.GetComponent<Collider>());
            TintPrimitive(visual, color);
            StabilizeRenderers(visual);
            visualHeight = fs.y + poleHeight;
        }

        var station = go.AddComponent<StationInteractable>();
        station.Configure(kind, label, prompt, 4.5f);

        var triggerGo = new GameObject("UseTrigger");
        triggerGo.transform.SetParent(go.transform, false);
        triggerGo.transform.localPosition = new Vector3(0f, visualHeight * 0.45f, 0f);
        var sphere = triggerGo.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = Mathf.Clamp(visualHeight * 0.55f, 1.2f, 2.8f);

        var block = go.AddComponent<BoxCollider>();
        block.center = new Vector3(0f, visualHeight * 0.5f, 0f);
        float footprint = Mathf.Clamp(Mathf.Min(maxFootprint * scaleMul * 0.55f, visualHeight * 0.85f), 0.8f, 2.4f);
        block.size = new Vector3(footprint, visualHeight, footprint);

        AttachLabelAndBeacon(parent, go.transform, label, color, visualHeight + 0.45f);

        if (kind == StationKind.JobBoard)
            JobBoardScreen.Attach(go.transform, visual);

        return go;
    }

    /// <summary>Убирает камеры/свет/слушатели из FBX — иначе мерцание и «вид со станка».</summary>
    static void StripEmbeddedSceneJunk(GameObject visual)
    {
        foreach (var cam in visual.GetComponentsInChildren<Camera>(true))
        {
            if (cam == null) continue;
            if (cam.CompareTag("MainCamera"))
                cam.tag = "Untagged";
            Object.Destroy(cam);
        }
        foreach (var listener in visual.GetComponentsInChildren<AudioListener>(true))
        {
            if (listener != null)
                Object.Destroy(listener);
        }
        foreach (var light in visual.GetComponentsInChildren<Light>(true))
        {
            if (light != null)
                Object.Destroy(light);
        }
    }

    /// <summary>Меньше shadow-acne / мерцания на белых мешах.</summary>
    static void StabilizeRenderers(GameObject visual)
    {
        foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            r.allowOcclusionWhenDynamic = false;

            // Слишком «мокрый» дефолтный материал бликует и мерцает
            foreach (var mat in r.materials)
            {
                if (mat == null) continue;
                if (mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", Mathf.Min(mat.GetFloat("_Smoothness"), 0.25f));
                if (mat.HasProperty("_Glossiness"))
                    mat.SetFloat("_Glossiness", 0.25f);
                if (mat.HasProperty("_Metallic"))
                    mat.SetFloat("_Metallic", Mathf.Min(mat.GetFloat("_Metallic"), 0.15f));
            }
        }
    }

    static float FitAndSeatOnFloor(GameObject visual, Transform stationRoot, float targetHeight, float maxFootprint)
    {
        Bounds b = GetWorldBounds(visual);
        float srcH = Mathf.Max(b.size.y, 0.001f);
        float srcFoot = Mathf.Max(b.size.x, b.size.z);

        float scale = targetHeight / srcH;
        float footAfter = srcFoot * scale;
        if (footAfter > maxFootprint)
            scale *= maxFootprint / footAfter;

        visual.transform.localScale = Vector3.one * scale;

        b = GetWorldBounds(visual);
        float dy = stationRoot.position.y - b.min.y + 0.01f; // чуть над полом — меньше z-fight
        visual.transform.position += Vector3.up * dy;

        b = GetWorldBounds(visual);
        Vector3 center = b.center;
        Vector3 root = stationRoot.position;
        visual.transform.position += new Vector3(root.x - center.x, 0f, root.z - center.z);

        return Mathf.Max(GetWorldBounds(visual).size.y, 0.5f);
    }

    static Bounds GetWorldBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return new Bounds(go.transform.position + Vector3.up * 0.5f, Vector3.one);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                b.Encapsulate(renderers[i].bounds);
        }
        return b;
    }

    static void AttachLabelAndBeacon(Transform labelsParent, Transform follow, string label, Color color, float height)
    {
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(labelsParent, false);
        labelGo.transform.position = follow.position + Vector3.up * height;
        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = label + "\n[E]";
        tm.characterSize = 0.04f;
        tm.fontSize = 36;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        tm.fontStyle = FontStyle.Bold;
        var billboard = labelGo.AddComponent<BillboardLabel>();
        billboard.Follow = follow;
        billboard.HeightOffset = height;

        var beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        beacon.name = "Beacon";
        beacon.transform.SetParent(labelsParent, false);
        beacon.transform.position = follow.position + Vector3.up * (height + 0.35f);
        beacon.transform.localScale = Vector3.one * 0.22f;
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
        var bf = beacon.AddComponent<StationBeaconFollow>();
        bf.Target = follow;
        bf.HeightOffset = height + 0.35f;
    }

    static void TintPrimitive(GameObject go, Color color)
    {
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        mat.color = color;
        rend.material = mat;
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
        go.transform.localScale = new Vector3(scale.x, 0.02f, scale.z);
        Object.Destroy(go.GetComponent<Collider>());
        TintPrimitive(go, color);
        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            // Разметка не должна кидать/ловить тени — иначе мерцает на полу
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
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
