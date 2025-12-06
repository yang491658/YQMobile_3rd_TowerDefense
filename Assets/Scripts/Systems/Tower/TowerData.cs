using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Tower", menuName = "TowerData", order = 1)]
public class TowerData : ScriptableObject
{
    [Header("Data")]
    public int ID;
    public string Name;
    public Sprite BaseImage;
    public Sprite SymbolImage;
    public Color Color = Color.red;
    
    [Header("Type")]
    public TowerGrade Grade;
    public TowerRole Role;

    [Header("Battle")]
    public int AttackDamage = 1;
    public float AttackSpeed = 3f;
    public AttackTarget AttackTarget = AttackTarget.First;

    [Header("Effect")]
    public TriggerTime Trigger = TriggerTime.None;
    public HitType HitType = HitType.None;
    public DebuffType DebuffType = DebuffType.None;
    public BuffType BuffType = BuffType.None;

    [Header("Value")]
    public float Value1;
    public float Value2;
    public float Value3;

#if UNITY_EDITOR
    private void OnValidate()
    {
        var sprites = Resources.LoadAll<Sprite>("Images/Towers");
        var baseSprites = new List<Sprite>();

        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite s = sprites[i];
            string path = AssetDatabase.GetAssetPath(s);
            string dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir)) continue;

            dir = dir.Replace("\\", "/");
            if (!dir.EndsWith("/Images/Towers")) continue;

            baseSprites.Add(s);
        }

        var used = new HashSet<string>();

        foreach (var g in AssetDatabase.FindAssets("t:TowerData"))
        {
            var d = AssetDatabase.LoadAssetAtPath<TowerData>(AssetDatabase.GUIDToAssetPath(g));
            if (d != null && d != this && d.BaseImage != null)
            {
                string path = AssetDatabase.GetAssetPath(d.BaseImage);
                string dir = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(dir)) continue;

                dir = dir.Replace("\\", "/");
                if (!dir.EndsWith("/Images/Towers")) continue;

                used.Add(d.BaseImage.name);
            }
        }

        Sprite pick = null;
        if (BaseImage == null || used.Contains(BaseImage.name))
        {
            for (int i = 0; i < baseSprites.Count; i++)
            {
                Sprite s = baseSprites[i];
                if (used.Contains(s.name)) continue;

                pick = s;
                break;
            }
            BaseImage = pick;
        }

        if (BaseImage != null)
        {
            var m = Regex.Match(BaseImage.name, @"^(?<num>\d+)\.");
            ID = m.Success ? int.Parse(m.Groups["num"].Value) : ID;

            string rawName = BaseImage.name;
            Name = Regex.Replace(rawName, @"^\d+\.", "");
        }
        else
        {
            ID = 0;
            Name = null;
        }

        var symbolSprites = Resources.LoadAll<Sprite>("Images/Towers/Symbol");
        if (ID > 0)
        {
            string prefix = ID.ToString("D2") + ".";
            for (int i = 0; i < symbolSprites.Length; i++)
            {
                Sprite s = symbolSprites[i];
                if (s.name.StartsWith(prefix))
                {
                    SymbolImage = s;
                    break;
                }
            }
        }

        EditorUtility.SetDirty(this);
    }
#endif

    public TowerData Clone()
    {
        TowerData clone = CreateInstance<TowerData>();

        clone.ID = this.ID;
        clone.Name = this.Name;
        clone.BaseImage = this.BaseImage;
        clone.SymbolImage = this.SymbolImage;
        clone.Color = this.Color;

        clone.AttackDamage = this.AttackDamage;
        clone.AttackSpeed = this.AttackSpeed;

        clone.Grade = this.Grade;
        clone.Role = this.Role;

        clone.AttackTarget = this.AttackTarget;
        clone.Trigger = this.Trigger;

        clone.HitType = this.HitType;
        clone.DebuffType = this.DebuffType;
        clone.BuffType = this.BuffType;

        clone.Value1 = this.Value1;
        clone.Value2 = this.Value2;
        clone.Value3 = this.Value3;

        return clone;
    }
}
