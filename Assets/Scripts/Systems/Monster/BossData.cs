using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "BossData", menuName = "Data/Boss", order = 2)]
public class BossData : ScriptableObject
{
    [Header("Base")]
    public Sprite Image;
    public int ID;
    public string Name;

    [Header("Stat")]
    [Min(0f)] public float MoveSpeed = 1f;
    [Min(0)] public int MaxHealth;

    [Header("Reward")]
    [Min(0)] public int Exp;
    [Min(0)] public int Gold;

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoImage();
        AutoName();
        AutoValue();

        EditorUtility.SetDirty(this);
    }

    private void AutoImage()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Monsters");
        List<Sprite> baseSprites = new();
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            string path = AssetDatabase.GetAssetPath(sprite);
            string dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir)) continue;

            dir = dir.Replace("\\", "/");
            if (!dir.EndsWith("/Images/Monsters")) continue;

            baseSprites.Add(sprite);
        }

        HashSet<string> used = new();
        foreach (string guid in AssetDatabase.FindAssets("t:BossData"))
        {
            BossData data = AssetDatabase.LoadAssetAtPath<BossData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data != null && data != this && data.Image != null)
            {
                string path = AssetDatabase.GetAssetPath(data.Image);
                string dir = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(dir)) continue;

                dir = dir.Replace("\\", "/");
                if (!dir.EndsWith("/Images/Monsters")) continue;

                used.Add(data.Image.name);
            }
        }

        Sprite pick = null;
        if (Image == null || used.Contains(Image.name))
        {
            for (int i = 0; i < baseSprites.Count; i++)
            {
                Sprite sprite = baseSprites[i];
                if (used.Contains(sprite.name)) continue;

                pick = sprite;
                break;
            }
            Image = pick;
        }
    }

    private void AutoName()
    {
        if (Image != null)
        {
            string[] split = Image.name.Split('.', 2);
            if (split.Length > 0)
                int.TryParse(split[0], out ID);

            Name = split.Length > 1 ? split[1] : Image.name;
        }
        else
        {
            HashSet<int> used = new();
            foreach (string guid in AssetDatabase.FindAssets("t:BossData"))
            {
                BossData data = AssetDatabase.LoadAssetAtPath<BossData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data != null && data != this)
                    used.Add(data.ID);
            }

            ID = 1; while (used.Contains(ID)) ID++;
            Name = "Temp";
        }
    }

    private void AutoValue()
    {
        if (Name == "Temp")
        {
            MaxHealth = ID * 1000;
            Exp = ID * 100;
            Gold = ID * 100;
        }
    }
#endif
}
