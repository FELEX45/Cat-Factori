using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Цельный заводской зал: один visual-mesh + надёжные BoxCollider (CharacterController не проваливается).
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class FactoryHall : MonoBehaviour
{
    const float ColliderThickness = 0.5f;

    [Header("Size (meters)")]
    [SerializeField] float width = 100f;
    [SerializeField] float height = 50f;
    [SerializeField] float depth = 100f;

    [Header("UV: meters per texture repeat")]
    [SerializeField] float floorTileMeters = 4f;
    [SerializeField] float wallTileMeters = 4f;
    [SerializeField] float ceilingTileMeters = 8f;

    [Header("Materials (optional — loaded from Resources/Factory if empty)")]
    [SerializeField] Material floorMaterial;
    [SerializeField] Material wallMaterial;
    [SerializeField] Material ceilingMaterial;

    [Header("Lighting")]
    [SerializeField] bool ensureDirectionalLight = true;
    [SerializeField] Color lightColor = new Color(1f, 0.96f, 0.88f, 1f);
    [SerializeField] float lightIntensity = 1.35f;
    [SerializeField] Vector3 lightEuler = new Vector3(50f, -30f, 0f);

    Mesh _mesh;
    Transform _collidersRoot;

    void Awake()
    {
        Build();
        if (ensureDirectionalLight)
            EnsureLight();
    }

    [ContextMenu("Rebuild Hall")]
    public void Build()
    {
        EnsureMaterials();
        if (_mesh != null)
        {
            if (Application.isPlaying)
                Destroy(_mesh);
            else
                DestroyImmediate(_mesh);
        }

        _mesh = BuildInvertedBoxMesh(width, height, depth, floorTileMeters, wallTileMeters, ceilingTileMeters);
        _mesh.name = "FactoryHallMesh";

        var filter = GetComponent<MeshFilter>();
        filter.sharedMesh = _mesh;

        var renderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterials = new[] { floorMaterial, wallMaterial, ceilingMaterial };
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;

        // Убираем старый MeshCollider — на тонкой оболочке CharacterController проваливается
        var oldMeshCol = GetComponent<MeshCollider>();
        if (oldMeshCol != null)
            Destroy(oldMeshCol);

        RebuildBoxColliders();
    }

    void RebuildBoxColliders()
    {
        if (_collidersRoot != null)
        {
            if (Application.isPlaying)
                Destroy(_collidersRoot.gameObject);
            else
                DestroyImmediate(_collidersRoot.gameObject);
        }

        var rootGo = new GameObject("Colliders");
        _collidersRoot = rootGo.transform;
        _collidersRoot.SetParent(transform, false);
        _collidersRoot.localPosition = Vector3.zero;
        _collidersRoot.localRotation = Quaternion.identity;
        _collidersRoot.localScale = Vector3.one;

        float hx = width * 0.5f;
        float hz = depth * 0.5f;
        float t = ColliderThickness;

        // Пол: верхняя грань на y=0 (совпадает с visual floor)
        AddBox("FloorCollider", new Vector3(0f, -t * 0.5f, 0f), new Vector3(width, t, depth));

        // Потолок: нижняя грань на y=height
        AddBox("CeilingCollider", new Vector3(0f, height + t * 0.5f, 0f), new Vector3(width, t, depth));

        // Стены: внутренняя грань на ±hx / ±hz
        AddBox("WallNegZ", new Vector3(0f, height * 0.5f, -hz - t * 0.5f), new Vector3(width + t * 2f, height, t));
        AddBox("WallPosZ", new Vector3(0f, height * 0.5f, hz + t * 0.5f), new Vector3(width + t * 2f, height, t));
        AddBox("WallNegX", new Vector3(-hx - t * 0.5f, height * 0.5f, 0f), new Vector3(t, height, depth));
        AddBox("WallPosX", new Vector3(hx + t * 0.5f, height * 0.5f, 0f), new Vector3(t, height, depth));
    }

    void AddBox(string name, Vector3 localPos, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_collidersRoot, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        var box = go.AddComponent<BoxCollider>();
        box.center = Vector3.zero;
        box.size = size;
    }

    void EnsureMaterials()
    {
        if (floorMaterial == null)
            floorMaterial = CreateRuntimeMaterial("Factory/ConcreteFloor", 0.05f, 0.28f, doubleSided: true);
        else
        {
            ApplyTexture(floorMaterial, "Factory/ConcreteFloor");
            SetDoubleSided(floorMaterial);
        }

        if (wallMaterial == null)
            wallMaterial = CreateRuntimeMaterial("Factory/MetalWall", 0.55f, 0.42f, doubleSided: false);
        else
            ApplyTexture(wallMaterial, "Factory/MetalWall");

        if (ceilingMaterial == null)
            ceilingMaterial = CreateRuntimeMaterial("Factory/IndustrialCeiling", 0.2f, 0.35f, doubleSided: true);
        else
        {
            ApplyTexture(ceilingMaterial, "Factory/IndustrialCeiling");
            SetDoubleSided(ceilingMaterial);
        }
    }

    static void SetDoubleSided(Material mat)
    {
        if (mat == null)
            return;
        if (mat.HasProperty("_Cull"))
            mat.SetFloat("_Cull", 0f); // Off
        mat.doubleSidedGI = true;
    }

    static void ApplyTexture(Material mat, string resourceTexture)
    {
        if (mat == null)
            return;
        var tex = Resources.Load<Texture2D>(resourceTexture);
        if (tex == null)
            return;
        if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex"))
            mat.SetTexture("_MainTex", tex);
    }

    static Material CreateRuntimeMaterial(string resourceTexture, float metallic, float smoothness, bool doubleSided = false)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("URP/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        var mat = new Material(shader) { name = resourceTexture.Replace('/', '_') + "_Runtime" };
        var tex = Resources.Load<Texture2D>(resourceTexture);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", tex);
        }

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness"))
            mat.SetFloat("_Glossiness", smoothness);

        if (doubleSided)
            SetDoubleSided(mat);

        return mat;
    }

    void EnsureLight()
    {
        if (FindAnyObjectByType<Light>() != null)
            return;

        var go = new GameObject("Directional Light");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, height * 0.85f, 0f);
        go.transform.localRotation = Quaternion.Euler(lightEuler);

        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = lightColor;
        light.intensity = lightIntensity;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.65f;
        light.shadowBias = 0.05f;
        light.shadowNormalBias = 0.4f;

        if (go.GetComponent<UniversalAdditionalLightData>() == null)
            go.AddComponent<UniversalAdditionalLightData>();
    }

    /// <summary>
    /// Инвертированный бокс: normals внутрь. Submesh 0 = пол, 1 = стены, 2 = потолок.
    /// Локально: пол на y=0, потолок на y=height, центр по XZ в нуле.
    /// </summary>
    public static Mesh BuildInvertedBoxMesh(
        float width, float height, float depth,
        float floorTile, float wallTile, float ceilingTile)
    {
        float hx = width * 0.5f;
        float hz = depth * 0.5f;
        float y0 = 0f;
        float y1 = height;

        var c000 = new Vector3(-hx, y0, -hz);
        var c100 = new Vector3(hx, y0, -hz);
        var c010 = new Vector3(-hx, y1, -hz);
        var c110 = new Vector3(hx, y1, -hz);
        var c001 = new Vector3(-hx, y0, hz);
        var c101 = new Vector3(hx, y0, hz);
        var c011 = new Vector3(-hx, y1, hz);
        var c111 = new Vector3(hx, y1, hz);

        var verts = new System.Collections.Generic.List<Vector3>(24);
        var norms = new System.Collections.Generic.List<Vector3>(24);
        var uvs = new System.Collections.Generic.List<Vector2>(24);
        var floorTris = new System.Collections.Generic.List<int>(6);
        var wallTris = new System.Collections.Generic.List<int>(24);
        var ceilTris = new System.Collections.Generic.List<int>(6);

        void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal,
            Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD,
            System.Collections.Generic.List<int> tris)
        {
            int i = verts.Count;
            verts.Add(a); norms.Add(normal); uvs.Add(uvA);
            verts.Add(b); norms.Add(normal); uvs.Add(uvB);
            verts.Add(c); norms.Add(normal); uvs.Add(uvC);
            verts.Add(d); norms.Add(normal); uvs.Add(uvD);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        float fu = width / Mathf.Max(0.01f, floorTile);
        float fv = depth / Mathf.Max(0.01f, floorTile);
        // Пол: winding как у стен (видимый изнутри). Нормаль вверх для освещения.
        AddQuad(c000, c001, c101, c100, Vector3.up,
            new Vector2(0, 0), new Vector2(0, fv), new Vector2(fu, fv), new Vector2(fu, 0),
            floorTris);

        float cu = width / Mathf.Max(0.01f, ceilingTile);
        float cv = depth / Mathf.Max(0.01f, ceilingTile);
        // Потолок: видимый снизу изнутри
        AddQuad(c010, c110, c111, c011, Vector3.down,
            new Vector2(0, 0), new Vector2(cu, 0), new Vector2(cu, cv), new Vector2(0, cv),
            ceilTris);

        float wu = width / Mathf.Max(0.01f, wallTile);
        float wh = height / Mathf.Max(0.01f, wallTile);
        float du = depth / Mathf.Max(0.01f, wallTile);

        AddQuad(c000, c100, c110, c010, Vector3.forward,
            new Vector2(0, 0), new Vector2(wu, 0), new Vector2(wu, wh), new Vector2(0, wh),
            wallTris);
        AddQuad(c101, c001, c011, c111, Vector3.back,
            new Vector2(0, 0), new Vector2(wu, 0), new Vector2(wu, wh), new Vector2(0, wh),
            wallTris);
        AddQuad(c001, c000, c010, c011, Vector3.right,
            new Vector2(0, 0), new Vector2(du, 0), new Vector2(du, wh), new Vector2(0, wh),
            wallTris);
        AddQuad(c100, c101, c111, c110, Vector3.left,
            new Vector2(0, 0), new Vector2(du, 0), new Vector2(du, wh), new Vector2(0, wh),
            wallTris);

        var mesh = new Mesh { name = "FactoryHall" };
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 3;
        mesh.SetTriangles(floorTris, 0);
        mesh.SetTriangles(wallTris, 1);
        mesh.SetTriangles(ceilTris, 2);
        mesh.RecalculateBounds();
        return mesh;
    }

    void OnDestroy()
    {
        if (_mesh != null)
            Destroy(_mesh);
    }
}
