using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Поднимает геймплей завода в MainScene (офлайн и онлайн).</summary>
public class FactoryGameplayBootstrap : MonoBehaviour
{
    bool _spawnedOffline;

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

        // Play прямо из MainScene / после онлайн без Reload — всегда чистый офлайн, если сети нет
        bool networkLive = Unity.Netcode.NetworkManager.Singleton != null
                           && Unity.Netcode.NetworkManager.Singleton.IsListening;
        if (!networkLive)
            GameSessionMode.SetOffline();

        var go = new GameObject("FactoryGameplay");
        go.AddComponent<FactoryGameplayBootstrap>();
        go.AddComponent<GameSessionState>();
        go.AddComponent<GameplayHud>();
        go.AddComponent<BossTaskSystem>();
        go.AddComponent<ShiftEventSystem>();
        go.AddComponent<PaintUpgradeState>();

        var layout = go.AddComponent<FactoryLayoutBuilder>();
        layout.Build();

        var session = go.GetComponent<GameSessionState>();
        var product = SeasonProgress.NextProduct();
        int quota = SeasonProgress.GigUnlocked ? 2 : 1;
        session.BeginShift(product, 500f, quota, 900f);

        EnsureLocalInteractor();
    }

    void Update()
    {
        EnsureLocalInteractor();
        EnsureOfflinePlayerReady();
    }

    void EnsureOfflinePlayerReady()
    {
        // Онлайн-спавн делает NetworkPlayer
        if (GameSessionMode.IsOnline && NetworkPlayer.LocalPlayer != null)
            return;

        var localMove = FindLocalMove();
        if (localMove == null)
            return;

        if (!localMove.enabled)
            localMove.enabled = true;

        var cc = localMove.GetComponent<CharacterController>();
        if (cc != null && !cc.enabled)
            cc.enabled = true;

        if (!_spawnedOffline)
        {
            _spawnedOffline = true;
            // На случай: модели могли создать камеры до привязки
            DisableAllPropCameras();
            PlaceOfflinePlayer(localMove, cc);
            localMove.BindSceneCamera();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log($"[Bootstrap] Офлайн-игрок на спавне {localMove.transform.position}, cam parent={(Camera.main != null ? Camera.main.transform.parent : null)}");
        }
    }

    static void DisableAllPropCameras()
    {
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (c == null) continue;
            Transform t = c.transform;
            bool underProps = false;
            while (t != null)
            {
                if (t.name == "FactoryZones" || t.name == "Visual" || t.name == "FactoryGameplay")
                {
                    underProps = true;
                    break;
                }
                t = t.parent;
            }
            if (!underProps) continue;
            c.enabled = false;
            if (c.CompareTag("MainCamera"))
                c.tag = "Untagged";
            Object.Destroy(c);
        }
    }

    static void PlaceOfflinePlayer(move localMove, CharacterController cc)
    {
        Vector3 pos = NetworkPlayer.GetSpawnPosition(0);
        // Чуть в коридоре между КБ и цехом, лицом к цеху
        var hall = Object.FindAnyObjectByType<FactoryHall>();
        if (hall != null)
        {
            Vector3 o = hall.transform.position;
            pos = new Vector3(o.x, o.y + 0.35f, o.z);
        }

        if (cc != null)
            cc.enabled = false;
        localMove.transform.SetPositionAndRotation(pos, Quaternion.identity);
        if (cc != null)
            cc.enabled = true;

        localMove.BindSceneCamera();
    }

    static void EnsureLocalInteractor()
    {
        var localMove = FindLocalMove();
        if (localMove == null)
            return;

        if (localMove.GetComponent<PlayerInteractor>() == null)
            localMove.gameObject.AddComponent<PlayerInteractor>();
    }

    static move FindLocalMove()
    {
        if (NetworkPlayer.LocalPlayer != null)
        {
            var m = NetworkPlayer.LocalPlayer.GetComponent<move>();
            if (m != null)
                return m;
        }

        foreach (var m in Object.FindObjectsByType<move>(FindObjectsInactive.Exclude))
        {
            if (m != null && m.isActiveAndEnabled)
                return m;
        }

        // Даже если move выключен — включим офлайн
        foreach (var m in Object.FindObjectsByType<move>(FindObjectsInactive.Exclude))
        {
            if (m != null)
                return m;
        }

        return null;
    }
}
