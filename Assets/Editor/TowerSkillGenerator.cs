#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class TowerSkillGenerator
{
    private const string assetFolder = "Assets/Datas/Skills";
    private static readonly Regex idPrefixRegex = new Regex(@"^(?<id>\d+)\.(?<name>.+)$", RegexOptions.Compiled);

    static TowerSkillGenerator() => EditorApplication.delayCall += GenerateAssets;

    private static void GenerateAssets()
    {
        EnsureFolder(assetFolder);

        var types = TypeCache.GetTypesDerivedFrom<TowerSkill>();
        for (int i = 0; i < types.Count; i++)
        {
            Type type = types[i];
            if (type == null || type.IsAbstract) continue;

            CreateAssetMenuAttribute menuAttr = GetCreateAssetMenu(type);
            int defaultID = menuAttr != null ? menuAttr.order : 0;
            string defaultBaseName = (menuAttr != null && !string.IsNullOrEmpty(menuAttr.fileName)) ? menuAttr.fileName : type.Name;

            string relativeFolder = GetRelativeFolder(menuAttr);
            string targetFolder = CombinePath(assetFolder, relativeFolder);
            EnsureFolder(targetFolder);

            List<string> paths = FindAssetsOfExactType(type);

            for (int p = 0; p < paths.Count; p++)
            {
                string path = NormalizeSlashes(paths[p]);
                TowerSkill skill = AssetDatabase.LoadAssetAtPath(path, type) as TowerSkill;
                if (skill == null) continue;

                string fileNoExt = Path.GetFileNameWithoutExtension(path);

                int id = skill.GetID();
                if (id <= 0 && TryParseIDFromFileName(fileNoExt, out int parsedID))
                {
                    skill.SetID(parsedID);
                    EditorUtility.SetDirty(skill);
                    id = parsedID;
                }

                string baseName = GetBaseName(skill, fileNoExt, defaultBaseName);

                string desiredPath = path;
                bool canRenameByID = id > 0;

                if (canRenameByID)
                {
                    string desiredFile = $"{id}.{baseName}.asset";
                    string candidatePath = $"{targetFolder}/{desiredFile}";

                    UnityEngine.Object occupant = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(candidatePath);
                    if (occupant == null || occupant == skill)
                        desiredPath = candidatePath;
                    else
                        desiredPath = $"{targetFolder}/{Path.GetFileName(path)}";
                }
                else
                {
                    desiredPath = $"{targetFolder}/{Path.GetFileName(path)}";
                }

                string currentDir = NormalizeSlashes(Path.GetDirectoryName(path));
                string desiredDir = NormalizeSlashes(Path.GetDirectoryName(desiredPath));

                bool needMoveFolder = !PathsEqual(currentDir, desiredDir);
                bool needRenameFile = !PathsEqual(path, desiredPath);

                if (needMoveFolder || needRenameFile)
                {
                    string finalPath = MoveAssetToPath(path, desiredPath);
                    paths[p] = finalPath;
                }
            }

            if (defaultID > 0)
            {
                bool hasDefault = HasAssetWithID(type, defaultID);
                if (!hasDefault)
                {
                    string createPath = $"{targetFolder}/{defaultID}.{defaultBaseName}.asset";
                    CreateAssetAtPath(type, createPath, defaultID, defaultBaseName);
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static bool HasAssetWithID(Type _type, int _id)
    {
        List<string> paths = FindAssetsOfExactType(_type);
        for (int i = 0; i < paths.Count; i++)
        {
            TowerSkill skill = AssetDatabase.LoadAssetAtPath(paths[i], _type) as TowerSkill;
            if (skill != null && skill.GetID() == _id) return true;
        }
        return false;
    }

    private static CreateAssetMenuAttribute GetCreateAssetMenu(Type _type)
    {
        object[] attrs = _type.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false);
        if (attrs == null || attrs.Length == 0) return null;
        return attrs[0] as CreateAssetMenuAttribute;
    }

    private static string GetRelativeFolder(CreateAssetMenuAttribute _attr)
    {
        if (_attr == null || string.IsNullOrEmpty(_attr.menuName)) return string.Empty;

        string menu = _attr.menuName.Replace('\\', '/').Trim('/');
        if (menu.StartsWith("TowerSkill/", StringComparison.Ordinal))
            menu = menu.Substring("TowerSkill/".Length);

        if (string.IsNullOrEmpty(menu)) return string.Empty;

        int lastSlash = menu.LastIndexOf('/');
        if (lastSlash < 0) return string.Empty;

        string categoryPath = menu.Substring(0, lastSlash).Trim('/');
        if (string.IsNullOrEmpty(categoryPath)) return string.Empty;

        string[] parts = categoryPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
            parts[i] = PrefixCategory(parts[i]);

        return string.Join("/", parts);
    }

    private static string PrefixCategory(string _name)
    {
        if (string.IsNullOrEmpty(_name)) return _name;

        if (string.Equals(_name, "Dealing", StringComparison.Ordinal)) return "01.Dealing";
        if (string.Equals(_name, "Debuff", StringComparison.Ordinal)) return "02.Debuff";
        if (string.Equals(_name, "Buff", StringComparison.Ordinal)) return "03.Buff";

        return _name;
    }

    private static List<string> FindAssetsOfExactType(Type _type)
    {
        List<string> results = new List<string>();

        string[] guids = AssetDatabase.FindAssets($"t:{_type.Name}", new[] { "Assets" });
        if (guids == null || guids.Length == 0) return results;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(path)) continue;

            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, _type);
            if (obj == null || obj.GetType() != _type) continue;

            results.Add(NormalizeSlashes(path));
        }

        return results;
    }

    private static bool TryParseIDFromFileName(string _fileNoExt, out int _id)
    {
        _id = 0;
        if (string.IsNullOrEmpty(_fileNoExt)) return false;

        Match m = idPrefixRegex.Match(_fileNoExt);
        if (!m.Success) return false;

        if (!int.TryParse(m.Groups["id"].Value, out int v)) return false;

        _id = v;
        return true;
    }

    private static string StripIDPrefix(string _name)
    {
        if (string.IsNullOrEmpty(_name)) return string.Empty;

        Match m = idPrefixRegex.Match(_name);
        return m.Success ? m.Groups["name"].Value : _name;
    }

    private static string GetBaseName(TowerSkill _skill, string _fileNoExt, string _fallback)
    {
        string fromFile = StripIDPrefix(_fileNoExt);
        if (!string.IsNullOrEmpty(fromFile)) return fromFile;

        string fromObj = _skill != null ? StripIDPrefix(_skill.name) : string.Empty;
        if (!string.IsNullOrEmpty(fromObj)) return fromObj;

        return _fallback;
    }

    private static void CreateAssetAtPath(Type _type, string _path, int _id, string _baseName)
    {
        string path = NormalizeSlashes(_path);
        UnityEngine.Object existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if (existing != null)
            path = AssetDatabase.GenerateUniqueAssetPath(path);

        TowerSkill instance = ScriptableObject.CreateInstance(_type) as TowerSkill;
        if (instance == null) return;

        instance.name = _baseName;
        instance.SetID(_id);
        EditorUtility.SetDirty(instance);

        AssetDatabase.CreateAsset(instance, path);
    }

    private static string MoveAssetToPath(string _srcPath, string _dstPath)
    {
        string srcPath = NormalizeSlashes(_srcPath);
        string dstPath = NormalizeSlashes(_dstPath);

        if (PathsEqual(srcPath, dstPath)) return srcPath;

        UnityEngine.Object dstObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dstPath);
        string finalDst = dstObj == null ? dstPath : AssetDatabase.GenerateUniqueAssetPath(dstPath);

        string err = AssetDatabase.MoveAsset(srcPath, finalDst);
        if (!string.IsNullOrEmpty(err))
        {
            finalDst = AssetDatabase.GenerateUniqueAssetPath(dstPath);
            AssetDatabase.MoveAsset(srcPath, finalDst);
        }

        return NormalizeSlashes(finalDst);
    }

    private static void EnsureFolder(string _folder)
    {
        string folder = NormalizeSlashes(_folder).TrimEnd('/');
        if (AssetDatabase.IsValidFolder(folder)) return;

        const string root = "Assets";
        if (!folder.StartsWith(root + "/", StringComparison.Ordinal))
            folder = root + "/" + folder.TrimStart('/');

        string subPath = folder.Substring(root.Length + 1);
        string[] parts = subPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        string parent = root;
        for (int i = 0; i < parts.Length; i++)
        {
            string current = parent + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(current))
                AssetDatabase.CreateFolder(parent, parts[i]);
            parent = current;
        }
    }

    private static string CombinePath(string _a, string _b)
        => string.IsNullOrEmpty(_b) ? NormalizeSlashes(_a) : NormalizeSlashes($"{_a.TrimEnd('/')}/{_b.TrimStart('/')}");

    private static string NormalizeSlashes(string _path)
        => string.IsNullOrEmpty(_path) ? string.Empty : _path.Replace('\\', '/');

    private static bool PathsEqual(string _a, string _b)
        => string.Equals(NormalizeSlashes(_a).TrimEnd('/'), NormalizeSlashes(_b).TrimEnd('/'), StringComparison.Ordinal);
}
#endif
