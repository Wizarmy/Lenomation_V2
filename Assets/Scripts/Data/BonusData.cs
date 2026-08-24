[System.Serializable]
public class BonusData
{
    public BonusType type;
    public float value;   // e.g. 0.2 = +20%
}

public enum BonusType
{
    SmeltSpeed,
    RefineYield,
    MachineSpeed,
    PowerEfficiency
}