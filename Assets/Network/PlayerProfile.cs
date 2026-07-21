using UnityEngine;

/// <summary>
/// Общие настройки игрока (Main Menu и ESC) — PlayerPrefs.
/// </summary>
public static class PlayerProfile
{
    const string NicknameKey = "CatFactori.Nickname";
    const string VolumeKey = "CatFactori.Volume";
    const string MouseSensKey = "CatFactori.MouseSens";
    const string DefaultNickname = "Игрок";
    public const int MaxNicknameLength = 24;

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

    public static void ApplyAudio()
    {
        AudioListener.volume = Volume;
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
