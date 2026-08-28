#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class InserterPrefabCreator
{
    [MenuItem("Automation/Create Inserter Prefab")]
    public static void CreateInserterPrefab()
    {
        PathingUtility.EnsureAllFolders();
        PathingUtility.DeleteFolderAndRecreate(PathingConfig.InserterFolder);

        var baseMat  = VisualsUtility.GetOrCreateMaterial("InserterBase",  InserterConfig.BaseColor);
        var towerMat = VisualsUtility.GetOrCreateMaterial("InserterTower", InserterConfig.TowerColor);
        var boomMat  = VisualsUtility.GetOrCreateMaterial("InserterBoom",  InserterConfig.BoomColor);
        var grabMat  = VisualsUtility.GetOrCreateMaterial("InserterGrab",  InserterConfig.GrabColor);

        Mesh baseMesh   = WriteBox("InserterBaseMesh",  InserterConfig.BaseSize);
        Mesh towerMesh  = WriteBox("InserterTowerMesh", InserterConfig.TowerSize);
        Mesh boom0Mesh  = WriteBox("InserterBoom0Mesh", InserterConfig.Boom0Size);
        Mesh boom1Mesh  = WriteBox("InserterBoom1Mesh", InserterConfig.Boom1Size);
        Mesh boom2Mesh  = WriteBox("InserterBoom2Mesh", InserterConfig.Boom2Size);
        Mesh magnetMesh = WriteCylinder("InserterMagnetMesh",
            InserterConfig.MagnetRadius, InserterConfig.MagnetHeight, 16);

        var root = new GameObject("Inserter");

        float baseH  = InserterConfig.BaseSize.y;
        float towerH = InserterConfig.TowerSize.y;

        AddPart(root.transform, "Base", baseMesh, baseMat,
            new Vector3(0f, baseH * 0.5f, 0f));

        Transform tower = AddPart(root.transform, "Tower", towerMesh, towerMat,
            new Vector3(0f, baseH + towerH * 0.5f, 0f));

        var slew = new GameObject("Slew").transform;
        slew.SetParent(root.transform, false);
        slew.localPosition = new Vector3(0f, InserterConfig.BoomHeight, 0f);

        Transform boom0 = AddPart(slew, "Boom0", boom0Mesh, boomMat,
            new Vector3(0f, 0f, InserterConfig.Boom0Size.z * 0.5f));

        Transform boom1 = AddPart(boom0, "Boom1", boom1Mesh, boomMat, Vector3.zero);
        Transform boom2 = AddPart(boom1, "Boom2", boom2Mesh, boomMat, Vector3.zero);

        var grab = new GameObject("Grab").transform;
        grab.SetParent(boom2, false);
        grab.localPosition = new Vector3(0f, 0f, InserterConfig.Boom2Size.z * 0.5f);

        Transform magnet = AddPart(grab, "Magnet", magnetMesh, grabMat, Vector3.zero);
        magnet.localRotation = Quaternion.Euler(90f, 0f, 0f);
        magnet.localPosition = new Vector3(0f, 0f, InserterConfig.MagnetHeight * 0.5f);

        var col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(
            CoreConfig.TileSize * 0.8f,
            baseH + towerH,
            CoreConfig.TileSize * 0.8f);
        col.center = new Vector3(0f, (baseH + towerH) * 0.5f, 0f);

        var ins = root.AddComponent<Inserter>();
        ins.tower  = tower;
        ins.slew   = slew;
        ins.boom0  = boom0;
        ins.boom1  = boom1;
        ins.boom2  = boom2;
        ins.grab   = grab;
        ins.magnet = magnet;
        ins.yaw    = InserterConfig.DefaultYaw;
        ins.extend = 0f;
        ins.ApplyPose();

        PrefabUtility.SaveAsPrefabAsset(root, InserterConfig.PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Inserter prefab created: " + InserterConfig.PrefabPath);
    }

    static Mesh WriteBox(string name, Vector3 size)
    {
        string path = PathingConfig.InserterFolder + name + ".asset";
        var mesh = VisualsUtility.CreateSolidBoxMesh(size, Vector3.zero, name);
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    static Mesh WriteCylinder(string name, float radius, float height, int segs)
    {
        string path = PathingConfig.InserterFolder + name + ".asset";
        var mesh = VisualsUtility.CreateCylinderMesh(radius, height, segs, name);
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    static Transform AddPart(Transform parent, string name, Mesh mesh, Material mat, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }
}
#endif