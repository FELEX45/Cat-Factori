using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(move))]
public class MoveEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Загрузить все анимации движения", GUILayout.Height(36)))
        {
            AssignClips((move)target, showDialog: true);
        }
    }

    public static void AssignClips(move target, bool showDialog = false)
    {
        AnimationClip idle = Load("Idle", "Assets/Player/Idle.fbx");
        AnimationClip walk = Load("Walking", "Assets/Player/Walking.fbx");
        AnimationClip run = Load("Running", "Assets/Player/Running.fbx");
        AnimationClip walkBack = Load("Walking Backwards", "Assets/Player/Walking Backwards.fbx");
        AnimationClip strafeLeftWalk = Load("Left Strafe Walking", "Assets/Player/Left Strafe Walking.fbx");
        AnimationClip strafeRightWalk = Load("Right Strafe Walking", "Assets/Player/Right Strafe Walking.fbx");
        AnimationClip strafeLeftRun = LoadExact("Left Strafe", "Assets/Player/Left Strafe.fbx", exclude: "Walking");
        AnimationClip strafeRightRun = LoadExact("Right Strafe", "Assets/Player/Right Strafe.fbx", exclude: "Walking");
        AnimationClip jump = Load("Jumping", "Assets/Player/Jumping.fbx");
        AnimationClip dance = Load("Hip Hop Dancing", "Assets/Player/Hip Hop Dancing.fbx");

        if (idle == null || walk == null || run == null || walkBack == null ||
            strafeLeftWalk == null || strafeRightWalk == null ||
            strafeLeftRun == null || strafeRightRun == null || jump == null || dance == null)
        {
            string msg =
                $"Не все клипы найдены:\n" +
                $"Idle={idle}\nWalk={walk}\nRun={run}\nWalkBack={walkBack}\n" +
                $"StrafeLWalk={strafeLeftWalk}\nStrafeRWalk={strafeRightWalk}\n" +
                $"StrafeLRun={strafeLeftRun}\nStrafeRRun={strafeRightRun}\n" +
                $"Jump={jump}\nDance={dance}";
            if (showDialog)
                EditorUtility.DisplayDialog("Анимации", msg, "OK");
            else
                Debug.LogWarning(msg);
            return;
        }

        Undo.RecordObject(target, "Assign animation clips");
        target.idleClip = idle;
        target.walkClip = walk;
        target.runClip = run;
        target.walkBackClip = walkBack;
        target.strafeLeftWalkClip = strafeLeftWalk;
        target.strafeRightWalkClip = strafeRightWalk;
        target.strafeLeftRunClip = strafeLeftRun;
        target.strafeRightRunClip = strafeRightRun;
        target.jumpClip = jump;
        target.danceClip = dance;

        ForceLoop(idle);
        ForceLoop(walk);
        ForceLoop(run);
        ForceLoop(walkBack);
        ForceLoop(strafeLeftWalk);
        ForceLoop(strafeRightWalk);
        ForceLoop(strafeLeftRun);
        ForceLoop(strafeRightRun);
        // Jump без loop
        ForceLoop(dance);

        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();

        Animator anim = target.GetComponent<Animator>();
        if (anim == null)
            anim = Undo.AddComponent<Animator>(target.gameObject);
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        anim.runtimeAnimatorController = null;

        if (showDialog)
            EditorUtility.DisplayDialog("Анимации", "Все анимации движения назначены.", "OK");
        Debug.Log("Анимации назначены: Idle/Walk/Run/Back/Strafes/Jump/Dance");
    }

    static AnimationClip Load(string namePart, string fbxPath)
    {
        AnimationClip clip = FindClip(namePart);
        if (clip == null)
            clip = FindClipInFbx(fbxPath);
        return clip;
    }

    static AnimationClip LoadExact(string namePart, string fbxPath, string exclude)
    {
        // Сначала пробуем точный FBX
        AnimationClip fromFbx = FindClipInFbx(fbxPath);
        if (fromFbx != null)
            return fromFbx;

        string[] guids = AssetDatabase.FindAssets($"t:AnimationClip {namePart}", new[] { "Assets/Player" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.IndexOf(exclude, System.StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip &&
                    !clip.name.StartsWith("__preview__") &&
                    clip.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                    clip.name.IndexOf(exclude, System.StringComparison.OrdinalIgnoreCase) < 0)
                    return clip;
            }
        }
        return null;
    }

    static void ForceLoop(AnimationClip clip)
    {
        if (clip == null)
            return;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.loopBlend = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    static AnimationClip FindClip(string namePart)
    {
        string[] guids = AssetDatabase.FindAssets($"t:AnimationClip {namePart}", new[] { "Assets/Player" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip &&
                    !clip.name.StartsWith("__preview__") &&
                    clip.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return clip;
            }

            AnimationClip direct = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (direct != null && !direct.name.StartsWith("__preview__"))
                return direct;
        }
        return null;
    }

    static AnimationClip FindClipInFbx(string fbxPath)
    {
        AnimationClip direct = AssetDatabase.LoadAssetAtPath<AnimationClip>(fbxPath);
        if (direct != null && !direct.name.StartsWith("__preview__"))
            return direct;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        return null;
    }
}
