using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "TowerData", menuName = "Tower/Data", order = 101)]
public class TowerData : ScriptableObject
{
    [Header("Base")]
    public Sprite Image;
    public int ID;
    public string Name;
    public Color Color = Color.black;

    [Header("Type")]
    public TowerGrade Grade = TowerGrade.Temp;
    public TowerRole Role = TowerRole.None;

    [Header("Stat")]
    public AttackTarget Target = AttackTarget.First;

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
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Towers");
        List<Sprite> baseSprites = new();
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            string path = AssetDatabase.GetAssetPath(sprite);
            string dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir)) continue;

            dir = dir.Replace("\\", "/");
            if (!dir.EndsWith("/Images/Towers")) continue;

            baseSprites.Add(sprite);
        }

        HashSet<string> used = new();
        foreach (string guid in AssetDatabase.FindAssets("t:TowerData"))
        {
            TowerData data = AssetDatabase.LoadAssetAtPath<TowerData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data != null && data != this && data.Image != null)
            {
                string path = AssetDatabase.GetAssetPath(data.Image);
                string dir = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(dir)) continue;

                dir = dir.Replace("\\", "/");
                if (!dir.EndsWith("/Images/Towers")) continue;

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
            int number = 0;
            if (split.Length > 0)
                int.TryParse(split[0], out number);

            ID = (int)Grade * 1000 + (int)Role * 100 + number % 100;
            Name = split.Length > 1 ? split[1] : Image.name;
        }
        else
        {
            Role = TowerRole.None;
            ID = 9000 + (int)Grade;
            Name = "Temp";
        }
    }

    private void AutoValue()
    {
    }
#endif
}
