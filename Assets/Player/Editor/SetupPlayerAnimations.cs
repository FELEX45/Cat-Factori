using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetupPlayerAnimations
{
    static SetupPlayerAnimations()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            move playerMove = Object.FindFirstObjectByType<move>();
            if (playerMove == null)
                return;
            if (playerMove.walkClip != null && playerMove.runClip != null && playerMove.danceClip != null)
                return;

            MoveEditor.AssignClips(playerMove, showDialog: false);
        };
    }

    [MenuItem("Tools/Setup Player Animations")]
    public static void Setup()
    {
        move playerMove = Object.FindFirstObjectByType<move>();
        if (playerMove == null)
        {
            EditorUtility.DisplayDialog("Анимации", "В сцене нет объекта со скриптом move (PlayerModel).", "OK");
            return;
        }

        MoveEditor.AssignClips(playerMove, showDialog: true);
    }
}
