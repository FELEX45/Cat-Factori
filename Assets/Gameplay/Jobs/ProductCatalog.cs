using UnityEngine;

/// <summary>Встроенный каталог первого изделия «Корпус реле» (без обязательных .asset в редакторе).</summary>
public static class ProductCatalog
{
    static ProductDefinition _relay;
    static PartDefinition _plate;
    static PartDefinition _shaft;
    static PartDefinition _cover;

    public static ProductDefinition RelayHousing
    {
        get
        {
            Ensure();
            return _relay;
        }
    }

    static void Ensure()
    {
        if (_relay != null) return;

        _plate = ScriptableObject.CreateInstance<PartDefinition>();
        _plate.name = "Part_Plate";
        _plate.displayName = "Основание";
        _plate.ideal = new PartDimensions { lengthMm = 120f, widthMm = 80f, heightMm = 8f, holeDiameterMm = 6f };
        _plate.toleranceMm = 1.5f;
        _plate.requiredMetal = MetalGrade.SteelSt3;
        _plate.requiredMachine = MachineType.Cutting;
        _plate.blankCost = 30f;

        _shaft = ScriptableObject.CreateInstance<PartDefinition>();
        _shaft.name = "Part_Shaft";
        _shaft.displayName = "Вал";
        _shaft.ideal = new PartDimensions { lengthMm = 60f, widthMm = 12f, heightMm = 12f, holeDiameterMm = 0f };
        _shaft.toleranceMm = 1.0f;
        _shaft.requiredMetal = MetalGrade.Steel45;
        _shaft.requiredMachine = MachineType.Lathe;
        _shaft.blankCost = 40f;

        _cover = ScriptableObject.CreateInstance<PartDefinition>();
        _cover.name = "Part_Cover";
        _cover.displayName = "Крышка";
        _cover.ideal = new PartDimensions { lengthMm = 120f, widthMm = 80f, heightMm = 4f, holeDiameterMm = 6f };
        _cover.toleranceMm = 1.5f;
        _cover.requiredMetal = MetalGrade.Aluminum;
        _cover.requiredMachine = MachineType.Press;
        _cover.blankCost = 25f;

        _relay = ScriptableObject.CreateInstance<ProductDefinition>();
        _relay.name = "Product_RelayHousing";
        _relay.displayName = "Корпус реле";
        _relay.difficulty = 1;
        _relay.parts = new[] { _plate, _shaft, _cover };
    }

    public static string MetalName(MetalGrade g) => g switch
    {
        MetalGrade.SteelSt3 => "Ст3",
        MetalGrade.Steel45 => "Сталь 45",
        MetalGrade.Aluminum => "Алюминий",
        MetalGrade.Brass => "Латунь",
        _ => g.ToString()
    };
}
