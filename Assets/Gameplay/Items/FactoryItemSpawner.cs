using UnityEngine;

public static class FactoryItemSpawner
{
    public static FactoryItem SpawnBlank(PartJob part)
    {
        return Spawn(part, FactoryItemKind.Blank, new Color(0.55f, 0.55f, 0.6f), GetWarehouseDrop());
    }

    public static FactoryItem SpawnFinishedPart(PartJob part)
    {
        return Spawn(part, FactoryItemKind.FinishedPart, new Color(0.35f, 0.7f, 0.4f), GetMachineOutDrop());
    }

    public static FactoryItem SpawnScrap(PartJob part)
    {
        return Spawn(part, FactoryItemKind.Scrap, new Color(0.7f, 0.25f, 0.2f), GetMachineOutDrop());
    }

    public static FactoryItem SpawnBlueprint(PartJob part)
    {
        return Spawn(part, FactoryItemKind.Blueprint, new Color(0.95f, 0.9f, 0.55f), GetDraftDrop());
    }

    static FactoryItem Spawn(PartJob part, FactoryItemKind kind, Color color, Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"{kind}_{part.partName}";
        go.transform.position = pos;
        go.transform.localScale = kind == FactoryItemKind.Blueprint
            ? new Vector3(0.35f, 0.02f, 0.25f)
            : new Vector3(0.35f, 0.2f, 0.25f);

        var rend = go.GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = color;

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 1f;

        var item = go.AddComponent<FactoryItem>();
        item.Kind = kind;
        item.PartId = part.partId;
        item.PartName = part.partName;
        item.Metal = part.orderedMetal;
        item.Dimensions = kind == FactoryItemKind.FinishedPart || kind == FactoryItemKind.Scrap
            ? part.actual
            : part.ideal;
        item.InTolerance = part.inTolerance;
        item.SetPrompt($"Взять: {part.partName}");
        item.SetUseRadius(3.5f);

        return item;
    }

    static Vector3 FloorY(Vector3 xz)
    {
        float y = 10.45f;
        var hall = Object.FindAnyObjectByType<FactoryHall>();
        if (hall != null)
            y = hall.transform.position.y + 0.25f;
        return new Vector3(xz.x, y, xz.z);
    }

    static Vector3 GetWarehouseDrop() => FloorY(new Vector3(-8f, 0f, -6f));
    static Vector3 GetMachineOutDrop() => FloorY(new Vector3(7f, 0f, -4f));
    static Vector3 GetDraftDrop() => FloorY(new Vector3(-7f, 0f, 3f));
}
