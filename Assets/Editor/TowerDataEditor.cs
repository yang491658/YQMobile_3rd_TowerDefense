#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(TowerData))]
public class TowerDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        TowerData data = (TowerData)target;

        string idStr = data.ID.ToString("D2");
        string newName = $"Tower{idStr}_{data.Name}";

        string path = AssetDatabase.GetAssetPath(data);
        string currentName = System.IO.Path.GetFileNameWithoutExtension(path);

        if (currentName != newName)
        {
            AssetDatabase.RenameAsset(path, newName);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
