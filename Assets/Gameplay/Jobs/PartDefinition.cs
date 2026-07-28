using UnityEngine;

public enum MetalGrade
{
    SteelSt3 = 0,
    Steel45 = 1,
    Aluminum = 2,
    Brass = 3
}

public enum PartPipelineStatus : byte
{
    None = 0,
    Measuring = 1,
    Drawing = 2,
    Ordered = 3,
    AtMachine = 4,
    Ready = 5,
    Assembled = 6,
    Scrap = 7
}

public enum MachineType
{
    Cutting = 0,
    Lathe = 1,
    Mill = 2,
    Press = 3
}

[System.Serializable]
public struct PartDimensions
{
    public float lengthMm;
    public float widthMm;
    public float heightMm;
    public float holeDiameterMm;
}

[CreateAssetMenu(menuName = "Cat Factori/Part Definition", fileName = "Part_")]
public class PartDefinition : ScriptableObject
{
    public string displayName = "Деталь";
    public PartDimensions ideal;
    public float toleranceMm = 1.5f;
    public MetalGrade requiredMetal = MetalGrade.SteelSt3;
    public MachineType requiredMachine = MachineType.Cutting;
    public float blankCost = 25f;
}

[CreateAssetMenu(menuName = "Cat Factori/Product Definition", fileName = "Product_")]
public class ProductDefinition : ScriptableObject
{
    public string displayName = "Изделие";
    public PartDefinition[] parts;
    public int difficulty = 1;
}
