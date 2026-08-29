using UnityEngine;

public enum ItemId
{
    None = 0,
    IronOre,
    CopperOre,
    Coal,
    Stone,
}

public static class ItemConfig
{
    public static readonly ItemId[] Ores =
    {
        ItemId.IronOre,
        ItemId.CopperOre,
        ItemId.Coal,
        ItemId.Stone,
    };

    public static string DisplayName(ItemId id) => id switch
    {
        ItemId.IronOre   => "Iron Ore",
        ItemId.CopperOre => "Copper Ore",
        ItemId.Coal      => "Coal",
        ItemId.Stone     => "Stone",
        _                => "None",
    };

    public static Color Color(ItemId id) => id switch
    {
        ItemId.IronOre   => new Color(0.55f, 0.62f, 0.72f),
        ItemId.CopperOre => new Color(0.78f, 0.42f, 0.22f),
        ItemId.Coal      => new Color(0.16f, 0.16f, 0.18f),
        ItemId.Stone     => new Color(0.62f, 0.58f, 0.48f),
        _                => PackageConfig.DefaultColor,
    };

    public static ItemId RandomOre() =>
        Ores[Random.Range(0, Ores.Length)];
}