#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(BossData))]
public class BossDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        BossData data = (BossData)target;

        string id = data.ID.ToString("D2");
        string newName = $"Boss{id}_{data.Name}";

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
