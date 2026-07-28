using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Общие настройки игрока (Main Menu и ESC) — PlayerPrefs.
/// </summary>
public static class PlayerProfile
{
    const string NicknameKey = "CatFactori.Nickname";
    const string VolumeKey = "CatFactori.Volume";
    const string MouseSensKey = "CatFactori.MouseSens";
    const string DisplayModeKey = "CatFactori.DisplayMode";
    const string ResolutionIndexKey = "CatFactori.ResIndex";
    const string QualityKey = "CatFactori.Quality";
    const string VSyncKey = "CatFactori.VSync";
    const string FrameCapKey = "CatFactori.FrameCap";
    const string DefaultNickname = "Игрок";
    public const int MaxNicknameLength = 24;

    /// <summary>0 Exclusive Fullscreen, 1 Windowed, 2 Borderless (FullScreenWindow).</summary>
    public enum DisplayModeKind
    {
        ExclusiveFullscreen = 0,
        Windowed = 1,
        Borderless = 2
    }

    public static string Nickname
    {
        get
        {
            string value = PlayerPrefs.GetString(NicknameKey, "");
            if (string.IsNullOrWhiteSpace(value))
                return DefaultNickname;
            return value.Trim();
        }
        set
        {
            PlayerPrefs.SetString(NicknameKey, SanitizeNickname(value));
            PlayerPrefs.Save();
        }
    }

    public static float Volume
    {
        get => PlayerPrefs.GetFloat(VolumeKey, 0.8f);
        set
        {
            PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            AudioListener.volume = Mathf.Clamp01(value);
        }
    }

    public static float MouseSensitivity
    {
        get => PlayerPrefs.GetFloat(MouseSensKey, 0.12f);
        set
        {
            PlayerPrefs.SetFloat(MouseSensKey, Mathf.Clamp(value, 0.02f, 0.5f));
            PlayerPrefs.Save();
        }
    }

    /// <summary>Нормализованное 0..1 значение чувствительности для UI-слайдера.</summary>
    public static float MouseSensitivityNormalized
    {
        get => Mathf.InverseLerp(0.02f, 0.5f, MouseSensitivity);
        set => MouseSensitivity = Mathf.Lerp(0.02f, 0.5f, Mathf.Clamp01(value));
    }

    public static DisplayModeKind DisplayMode
    {
        get => (DisplayModeKind)PlayerPrefs.GetInt(DisplayModeKey, (int)DisplayModeKind.Borderless);
        set
        {
            PlayerPrefs.SetInt(DisplayModeKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static int ResolutionIndex
    {
        get => PlayerPrefs.GetInt(ResolutionIndexKey, -1);
        set
        {
            PlayerPrefs.SetInt(ResolutionIndexKey, value);
            PlayerPrefs.Save();
        }
    }

    public static int QualityLevel
    {
        get
        {
            int q = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
            return Mathf.Clamp(q, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        }
        set
        {
            PlayerPrefs.SetInt(QualityKey, Mathf.Clamp(value, 0, Mathf.Max(0, QualitySettings.names.Length - 1)));
            PlayerPrefs.Save();
        }
    }

    public static bool VSync
    {
        get => PlayerPrefs.GetInt(VSyncKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(VSyncKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>0 = без лимита, иначе целевой FPS (30/60/120).</summary>
    public static int FrameCap
    {
        get => PlayerPrefs.GetInt(FrameCapKey, 0);
        set
        {
            PlayerPrefs.SetInt(FrameCapKey, Mathf.Max(0, value));
            PlayerPrefs.Save();
        }
    }

    public static void ApplyAudio()
    {
        AudioListener.volume = Volume;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyOnStartup()
    {
        ApplyGraphics();
        ApplyAudio();
    }

    public static void ApplyAll()
    {
        ApplyAudio();
        ApplyGraphics();
    }

    public static void ApplyGraphics()
    {
        int qMax = Mathf.Max(0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(Mathf.Clamp(QualityLevel, 0, qMax), true);

        QualitySettings.vSyncCount = VSync ? 1 : 0;
        Application.targetFrameRate = VSync ? -1 : (FrameCap > 0 ? FrameCap : -1);

        var modes = GetUniqueResolutions();
        int idx = ResolutionIndex;
        if (idx < 0 || idx >= modes.Count)
            idx = FindClosestResolutionIndex(modes, Screen.width, Screen.height);

        Resolution chosen = modes.Count > 0
            ? modes[Mathf.Clamp(idx, 0, modes.Count - 1)]
            : new Resolution { width = Screen.width, height = Screen.height, refreshRateRatio = Screen.currentResolution.refreshRateRatio };

        FullScreenMode mode = DisplayMode switch
        {
            DisplayModeKind.ExclusiveFullscreen => FullScreenMode.ExclusiveFullScreen,
            DisplayModeKind.Windowed => FullScreenMode.Windowed,
            _ => FullScreenMode.FullScreenWindow
        };

        Screen.SetResolution(chosen.width, chosen.height, mode);
        ResolutionIndex = modes.Count > 0 ? Mathf.Clamp(idx, 0, modes.Count - 1) : ResolutionIndex;
    }

    public static List<Resolution> GetUniqueResolutions()
    {
        var list = new List<Resolution>();
        var seen = new HashSet<string>();
        var raw = Screen.resolutions;
        // От большего к меньшему
        foreach (var r in raw.OrderByDescending(r => r.width * r.height).ThenByDescending(r => RefreshHz(r)))
        {
            string key = $"{r.width}x{r.height}";
            if (seen.Add(key))
                list.Add(r);
        }

        if (list.Count == 0)
        {
            list.Add(new Resolution
            {
                width = Mathf.Max(1280, Screen.width),
                height = Mathf.Max(720, Screen.height)
            });
        }

        return list;
    }

    public static int FindClosestResolutionIndex(List<Resolution> modes, int w, int h)
    {
        if (modes == null || modes.Count == 0)
            return 0;
        int best = 0;
        int bestScore = int.MaxValue;
        for (int i = 0; i < modes.Count; i++)
        {
            int score = Mathf.Abs(modes[i].width - w) + Mathf.Abs(modes[i].height - h);
            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        return best;
    }

    public static string DisplayModeLabel(DisplayModeKind mode) => mode switch
    {
        DisplayModeKind.ExclusiveFullscreen => "Полный экран",
        DisplayModeKind.Windowed => "В окне",
        DisplayModeKind.Borderless => "В окне на весь экран",
        _ => mode.ToString()
    };

    public static string FrameCapLabel(int cap) => cap <= 0 ? "Без лимита" : $"{cap} FPS";

    public static string ResolutionLabel(Resolution r)
    {
        int hz = Mathf.RoundToInt(RefreshHz(r));
        return hz > 0 ? $"{r.width}×{r.height} @{hz}Гц" : $"{r.width}×{r.height}";
    }

    static float RefreshHz(Resolution r)
    {
        try
        {
            return (float)r.refreshRateRatio.value;
        }
        catch
        {
#pragma warning disable CS0618
            return r.refreshRate;
#pragma warning restore CS0618
        }
    }

    public static string SanitizeNickname(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DefaultNickname;
        value = value.Trim();
        if (value.Length > MaxNicknameLength)
            value = value.Substring(0, MaxNicknameLength);
        return value;
    }
}
