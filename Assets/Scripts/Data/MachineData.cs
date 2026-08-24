using UnityEngine;

[CreateAssetMenu(fileName = "NewMachine", menuName = "Automation/Machine")]
public class MachineData : ScriptableObject
{
    public string machineName;
    public GameObject prefab;          // the visual prefab
    public MachineType type;
    public float baseSpeed = 1f;       // multiplier
    public int inputSlots = 2;
    public int outputSlots = 1;
    public int powerCost = 10;         // optional
}