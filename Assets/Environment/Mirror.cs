using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(MeshRenderer))]
public class Mirror : MonoBehaviour
{
    [SerializeField] int textureSize = 1024;
    [SerializeField] float clipPlaneOffset = 0.05f;
    [Tooltip("Если пусто — загрузится Resources/MirrorUnlit")]
    [SerializeField] Shader mirrorShader;

    Camera _mirrorCam;
    RenderTexture _rt;
    MeshRenderer _renderer;
    Material _mat;

    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();

        _rt = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.ARGB32)
        {
            name = "MirrorRT",
            antiAliasing = 1
        };
        _rt.Create();

        var camGo = new GameObject("MirrorCamera");
        _mirrorCam = camGo.AddComponent<Camera>();
        _mirrorCam.enabled = false;
        _mirrorCam.allowHDR = false;
        _mirrorCam.allowMSAA = false;
        _mirrorCam.targetTexture = _rt;
        _mirrorCam.nearClipPlane = 0.05f;
        _mirrorCam.farClipPlane = 250f;
        _mirrorCam.clearFlags = CameraClearFlags.Skybox;

        var mirrorListener = camGo.GetComponent<AudioListener>();
        if (mirrorListener != null)
            Destroy(mirrorListener);

        var urpData = camGo.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderType = CameraRenderType.Base;
        urpData.renderShadows = true;

        Shader shader = ResolveShader();
        if (shader == null)
        {
            Debug.LogError("[Mirror] Шейдер не найден. Проверь Assets/Resources/MirrorUnlit.shader");
            enabled = false;
            return;
        }

        _mat = new Material(shader) { name = "MirrorMaterial" };
        if (_mat.HasProperty(BaseMapId))
            _mat.SetTexture(BaseMapId, _rt);
        else
            _mat.mainTexture = _rt;

        if (_mat.HasProperty("_Cull"))
            _mat.SetFloat("_Cull", 0f);

        _renderer.material = _mat;
    }

    Shader ResolveShader()
    {
        if (mirrorShader != null)
            return mirrorShader;

        // Resources — попадает в билд (в отличие от Shader.Find для package-шейдеров)
        Shader shader = Resources.Load<Shader>("MirrorUnlit");
        if (shader != null)
            return shader;

        shader = Shader.Find("CatFactori/MirrorUnlit");
        if (shader != null)
            return shader;

        shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
            return shader;

        return Shader.Find("Unlit/Texture");
    }

    void OnDestroy()
    {
        if (_mirrorCam != null)
            Destroy(_mirrorCam.gameObject);

        if (_rt != null)
        {
            _rt.Release();
            Destroy(_rt);
        }

        if (_mat != null)
            Destroy(_mat);
    }

    void LateUpdate()
    {
        Camera src = Camera.main;
        if (src == null || _mirrorCam == null || !_renderer.isVisible)
            return;

        Vector3 pos = transform.position;
        Vector3 normal = transform.forward;
        if (Vector3.Dot(normal, src.transform.position - pos) < 0f)
            normal = -normal;

        UpdateReflectionCamera(src, pos, normal);

        _renderer.forceRenderingOff = true;
        bool prevCull = GL.invertCulling;
        GL.invertCulling = true;
        _mirrorCam.Render();
        GL.invertCulling = prevCull;
        _renderer.forceRenderingOff = false;
    }

    void UpdateReflectionCamera(Camera src, Vector3 pos, Vector3 normal)
    {
        float d = -Vector3.Dot(normal, pos) - clipPlaneOffset;
        Vector4 plane = new Vector4(normal.x, normal.y, normal.z, d);

        Matrix4x4 reflection = Matrix4x4.identity;
        CalculateReflectionMatrix(ref reflection, plane);

        _mirrorCam.worldToCameraMatrix = src.worldToCameraMatrix * reflection;

        Vector4 clipPlane = CameraSpacePlane(_mirrorCam, pos, normal, 1f);
        _mirrorCam.projectionMatrix = src.CalculateObliqueMatrix(clipPlane);
        _mirrorCam.fieldOfView = src.fieldOfView;
        _mirrorCam.aspect = src.aspect;
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer < 0)
            playerLayer = 6;
        _mirrorCam.cullingMask = src.cullingMask | (1 << playerLayer);

        Vector3 reflectedPos = reflection.MultiplyPoint(src.transform.position);
        Vector3 reflectedFwd = reflection.MultiplyVector(src.transform.forward);
        Vector3 reflectedUp = reflection.MultiplyVector(src.transform.up);
        _mirrorCam.transform.SetPositionAndRotation(
            reflectedPos,
            Quaternion.LookRotation(reflectedFwd, reflectedUp));
    }

    Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * clipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    static void CalculateReflectionMatrix(ref Matrix4x4 matrix, Vector4 plane)
    {
        matrix.m00 = 1f - 2f * plane[0] * plane[0];
        matrix.m01 = -2f * plane[0] * plane[1];
        matrix.m02 = -2f * plane[0] * plane[2];
        matrix.m03 = -2f * plane[3] * plane[0];

        matrix.m10 = -2f * plane[1] * plane[0];
        matrix.m11 = 1f - 2f * plane[1] * plane[1];
        matrix.m12 = -2f * plane[1] * plane[2];
        matrix.m13 = -2f * plane[3] * plane[1];

        matrix.m20 = -2f * plane[2] * plane[0];
        matrix.m21 = -2f * plane[2] * plane[1];
        matrix.m22 = 1f - 2f * plane[2] * plane[2];
        matrix.m23 = -2f * plane[3] * plane[2];

        matrix.m30 = 0f;
        matrix.m31 = 0f;
        matrix.m32 = 0f;
        matrix.m33 = 1f;
    }
}
