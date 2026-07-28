using UnityEngine;

/// <summary>Прокачка Paint между сменами (лёгкая мета).</summary>
public class PaintUpgradeState : MonoBehaviour
{
    public static PaintUpgradeState Instance { get; private set; }

    public bool StraightLines { get; private set; }
    public bool GridSnap { get; private set; }
    public bool CopyMeasure { get; private set; }
    public int Points { get; private set; }

    void Awake()
    {
        Instance = this;
        StraightLines = PlayerPrefs.GetInt("CatFactori.PaintStraight", 0) == 1;
        GridSnap = PlayerPrefs.GetInt("CatFactori.PaintGrid", 0) == 1;
        CopyMeasure = PlayerPrefs.GetInt("CatFactori.PaintCopy", 0) == 1;
        Points = PlayerPrefs.GetInt("CatFactori.PaintPts", 0);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddPoints(int amount)
    {
        Points += amount;
        PlayerPrefs.SetInt("CatFactori.PaintPts", Points);
        PlayerPrefs.Save();
    }

    public bool TryUnlockStraight(int cost = 50)
    {
        if (StraightLines) return true;
        if (Points < cost) return false;
        Points -= cost;
        StraightLines = true;
        PlayerPrefs.SetInt("CatFactori.PaintStraight", 1);
        PlayerPrefs.SetInt("CatFactori.PaintPts", Points);
        PlayerPrefs.Save();
        return true;
    }

    public void UnlockAllForDebug()
    {
        StraightLines = GridSnap = CopyMeasure = true;
    }
}
