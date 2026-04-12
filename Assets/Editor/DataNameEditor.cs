#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DataNameEditor
{
    public static void Rename(Object _target, string _newName)
    {
        string path = AssetDatabase.GetAssetPath(_target);
        string currentName = Path.GetFileNameWithoutExtension(path);

        if (currentName == _newName) return;

        string error = AssetDatabase.RenameAsset(path, _newName);
        if (error == string.Empty)
            AssetDatabase.SaveAssets();
    }
}

[CustomEditor(typeof(TowerData))]
public class TowerDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        TowerData data = (TowerData)target;
        string id = data.ID.ToString("D3");
        string newName = $"Tower{id}_{data.Name}";

        DataNameEditor.Rename(data, newName);
    }
}

[CustomEditor(typeof(BossData))]
public class BossDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        BossData data = (BossData)target;
        string id = data.ID.ToString("D2");
        string newName = $"Boss{id}_{data.Name}";

        DataNameEditor.Rename(data, newName);
    }
}

[CustomEditor(typeof(TowerSkill), true)]
public class SkillDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        TowerSkill data = (TowerSkill)target;
        string newName = $"{data.ID}.{data.GetType().Name}";

        DataNameEditor.Rename(data, newName);
    }
}
#endif