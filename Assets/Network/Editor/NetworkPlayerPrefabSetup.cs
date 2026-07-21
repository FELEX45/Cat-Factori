using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Создаёт Resources/NetworkPlayer.prefab для спавна в онлайн-сессии.
/// </summary>
public static class NetworkPlayerPrefabSetup
{
    const string ResourcesDir = "Assets/Network/Resources";
    const string PrefabPath = "Assets/Network/Resources/NetworkPlayer.prefab";
    const string PlayerFbxPath = "Assets/Player/PlayerModel.fbx";

    [InitializeOnLoadMethod]
    static void AutoCreate()
    {
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                CreatePrefab();
        };
    }

    [MenuItem("Tools/Cat Factori/Setup Network Player Prefab")]
    public static void CreatePrefabMenu()
    {
        CreatePrefab();
    }

    public static void CreatePrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Network"))
            AssetDatabase.CreateFolder("Assets", "Network");
        if (!AssetDatabase.IsValidFolder(ResourcesDir))
            AssetDatabase.CreateFolder("Assets/Network", "Resources");

        GameObject root = null;
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerFbxPath);
        if (fbx != null)
        {
            root = Object.Instantiate(fbx);
            root.name = "NetworkPlayer";
        }
        else
        {
            root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "NetworkPlayer";
            Object.DestroyImmediate(root.GetComponent<Collider>());
        }

        // Убрать лишние камеры/аудио из FBX, если есть
        foreach (var cam in root.GetComponentsInChildren<Camera>(true))
            Object.DestroyImmediate(cam.gameObject);
        foreach (var al in root.GetComponentsInChildren<AudioListener>(true))
            Object.DestroyImmediate(al);

        var cc = root.GetComponent<CharacterController>();
        if (cc == null)
            cc = root.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        if (root.GetComponent<Animator>() == null)
            root.AddComponent<Animator>();

        var movement = root.GetComponent<move>();
        if (movement == null)
            movement = root.AddComponent<move>();
        movement.enabled = false;
        TryAssignClips(movement);

        if (root.GetComponent<NetworkObject>() == null)
            root.AddComponent<NetworkObject>();

        var nt = root.GetComponent<NetworkTransform>();
        if (nt == null)
            nt = root.AddComponent<NetworkTransform>();
        var ntSo = new SerializedObject(nt);
        var interpolate = ntSo.FindProperty("Interpolate") ?? ntSo.FindProperty("m_Interpolate");
        if (interpolate != null)
        {
            interpolate.boolValue = true;
            ntSo.ApplyModifiedPropertiesWithoutUndo();
        }

        var np = root.GetComponent<NetworkPlayer>();
        if (np == null)
            np = root.AddComponent<NetworkPlayer>();

        // Привязка move через SerializedObject
        var so = new SerializedObject(np);
        var prop = so.FindProperty("movement");
        if (prop != null)
        {
            prop.objectReferenceValue = movement;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Network] Prefab сохранён: {PrefabPath}");
    }

    static void TryAssignClips(move movement)
    {
        var so = new SerializedObject(movement);
        AssignClip(so, "idleClip", "Assets/Player/Idle.fbx");
        AssignClip(so, "walkClip", "Assets/Player/Walking.fbx");
        AssignClip(so, "runClip", "Assets/Player/Running.fbx");
        AssignClip(so, "walkBackClip", "Assets/Player/Walking Backwards.fbx");
        AssignClip(so, "strafeLeftWalkClip", "Assets/Player/Left Strafe Walking.fbx");
        AssignClip(so, "strafeRightWalkClip", "Assets/Player/Right Strafe Walking.fbx");
        AssignClip(so, "strafeLeftRunClip", "Assets/Player/Left Strafe.fbx");
        AssignClip(so, "strafeRightRunClip", "Assets/Player/Right Strafe.fbx");
        AssignClip(so, "jumpClip", "Assets/Player/Jumping.fbx");
        AssignClip(so, "danceClip", "Assets/Player/Hip Hop Dancing.fbx");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AssignClip(SerializedObject so, string property, string assetPath)
    {
        var prop = so.FindProperty(property);
        if (prop == null)
            return;

        var clip = FindFirstClip(assetPath);
        if (clip != null)
            prop.objectReferenceValue = clip;
    }

    static AnimationClip FindFirstClip(string fbxPath)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        if (assets == null)
            return null;
        foreach (var a in assets)
        {
            if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        return null;
    }
}
