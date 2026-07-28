using UnityEngine;

/// <summary>Сезонный прогресс: 3 смены → гига-заказ.</summary>
public static class SeasonProgress
{
    const string KeyShifts = "CatFactori.SeasonShifts";
    const string KeyGigUnlocked = "CatFactori.GigUnlocked";

    public static int CompletedShifts
    {
        get => PlayerPrefs.GetInt(KeyShifts, 0);
        set
        {
            PlayerPrefs.SetInt(KeyShifts, value);
            PlayerPrefs.Save();
        }
    }

    public static bool GigUnlocked => PlayerPrefs.GetInt(KeyGigUnlocked, 0) == 1 || CompletedShifts >= 3;

    public static void OnShiftCompleted()
    {
        CompletedShifts++;
        PaintUpgradeState.Instance?.AddPoints(25);
        if (CompletedShifts >= 3)
        {
            PlayerPrefs.SetInt(KeyGigUnlocked, 1);
            PlayerPrefs.Save();
            GameplayHud.Instance?.ShowToast("Разблокирован гига-заказ! (следующая смена)");
        }
    }

    public static ProductDefinition NextProduct()
    {
        if (GigUnlocked && CompletedShifts >= 3)
            return CreateGigProduct();
        return ProductCatalog.RelayHousing;
    }

    static ProductDefinition CreateGigProduct()
    {
        var p = ScriptableObject.CreateInstance<ProductDefinition>();
        p.name = "Product_GigFrame";
        p.displayName = "Гига-рама (финал арки)";
        p.difficulty = 3;
        p.parts = ProductCatalog.RelayHousing.parts; // пока те же детали, вышеквотная смена
        return p;
    }
}
