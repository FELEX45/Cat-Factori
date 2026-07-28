using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Поднимает геймплей завода в MainScene (офлайн и онлайн).</summary>
public class FactoryGameplayBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != LobbySessionManager.GameSceneName)
            return;
        Ensure();
    }

    public static void Ensure()
    {
        if (Object.FindAnyObjectByType<FactoryGameplayBootstrap>() != null)
            return;

        var go = new GameObject("FactoryGameplay");
        go.AddComponent<FactoryGameplayBootstrap>();
        go.AddComponent<GameSessionState>();
        go.AddComponent<GameplayHud>();
        go.AddComponent<BossTaskSystem>();
        go.AddComponent<ShiftEventSystem>();
        go.AddComponent<PaintUpgradeState>();

        var layout = go.AddComponent<FactoryLayoutBuilder>();
        layout.Build();

        // Старт смены сразу — вертикальный срез / сезон
        var session = go.GetComponent<GameSessionState>();
        var product = SeasonProgress.NextProduct();
        int quota = SeasonProgress.GigUnlocked ? 2 : 1;
        session.BeginShift(product, 500f, quota, 900f);

        // Интерактор на локальном игроке
        EnsureLocalInteractor();
    }

    void Update()
    {
        EnsureLocalInteractor();
    }

    static void EnsureLocalInteractor()
    {
        move localMove = null;
        if (NetworkPlayer.LocalPlayer != null)
            localMove = NetworkPlayer.LocalPlayer.GetComponent<move>();
        if (localMove == null || !localMove.enabled)
        {
            foreach (var m in Object.FindObjectsByType<move>(FindObjectsInactive.Exclude))
            {
                if (m.enabled && m.isActiveAndEnabled)
                {
                    localMove = m;
                    break;
                }
            }
        }

        if (localMove == null)
            return;

        if (localMove.GetComponent<PlayerInteractor>() == null)
            localMove.gameObject.AddComponent<PlayerInteractor>();
    }
}
