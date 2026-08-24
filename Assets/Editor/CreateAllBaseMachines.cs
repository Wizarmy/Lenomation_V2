#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class MachineDataCreator
{
    [MenuItem("Automation/Create All Base Machines")]
    public static void CreateAllBaseMachines()
    {
        // Make sure the Machines folder exists
        string folderPath = "Assets/ScriptableObjects/Machines";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");

            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Machines");
        }

        // Create the machines
        CreateMachine("Smelter",    "Smelter",    MachineType.Smelter,    1.0f, 2, 1, 15);
        CreateMachine("Refiner",    "Refiner",    MachineType.Refiner,    1.0f, 2, 1, 20);
        CreateMachine("Assembler",  "Assembler",  MachineType.Assembler,  1.0f, 3, 1, 25);
        CreateMachine("Miner",      "Miner",       MachineType.Miner,       1.0f, 0, 1, 10);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("All base MachineData assets created successfully!");
    }

    private static void CreateMachine(
        string fileName,
        string displayName,
        MachineType type,
        float baseSpeed,
        int inputSlots,
        int outputSlots,
        int powerCost)
    {
        string path = $"Assets/ScriptableObjects/Machines/{fileName}.asset";

        // Skip if it already exists
        if (AssetDatabase.LoadAssetAtPath<MachineData>(path) != null)
        {
            Debug.Log($"Skipped {fileName} (already exists)");
            return;
        }

        MachineData machine = ScriptableObject.CreateInstance<MachineData>();
        machine.machineName = displayName;
        machine.type = type;
        machine.baseSpeed = baseSpeed;
        machine.inputSlots = inputSlots;
        machine.outputSlots = outputSlots;
        machine.powerCost = powerCost;
        // prefab is left empty for now – you will assign it later

        AssetDatabase.CreateAsset(machine, path);
        Debug.Log($"Created machine: {fileName}");
    }
}
#endif