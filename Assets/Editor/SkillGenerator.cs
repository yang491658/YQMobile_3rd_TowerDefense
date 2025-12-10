#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SkillGenerator
{
    private const string assetFolder = "Assets/Datas/Skills";

    static SkillGenerator()
    {
        EditorApplication.delayCall += GenerateAssets;
    }

    private static void GenerateAssets()
    {
        EnsureFolder(assetFolder);

        var types = TypeCache.GetTypesDerivedFrom<Skill>();
        for (int i = 0; i < types.Count; i++)
        {
            Type t = types[i];
            if (t.IsAbstract) continue;

            string path = assetFolder + "/" + t.Name + ".asset";

            Skill asset = AssetDatabase.LoadAssetAtPath<Skill>(path);
            if (asset != null) continue;

            Skill instance = ScriptableObject.CreateInstance(t) as Skill;
            if (instance == null) continue;

            AssetDatabase.CreateAsset(instance, path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolder(string _folder)
    {
        if (AssetDatabase.IsValidFolder(_folder)) return;

        string parent = "Assets";
        string subPath = _folder.Substring(parent.Length + 1);
        string[] parts = subPath.Split('/');

        for (int i = 0; i < parts.Length; i++)
        {
            string current = parent + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(current))
                AssetDatabase.CreateFolder(parent, parts[i]);

            parent = current;
        }
    }
}
#endif
