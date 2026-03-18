#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(TowerData))]
public class TowerDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        TowerData data = (TowerData)target;

        string id = data.ID.ToString("D3");
        string newName = $"Tower{id}_{data.Name}";

        string path = AssetDatabase.GetAssetPath(data);
        string currentName = System.IO.Path.GetFileNameWithoutExtension(path);

        if (currentName != newName)
        {
            string error = AssetDatabase.RenameAsset(path, newName);
            if (error == string.Empty)
                AssetDatabase.SaveAssets();
        }
    }
}
#endif
