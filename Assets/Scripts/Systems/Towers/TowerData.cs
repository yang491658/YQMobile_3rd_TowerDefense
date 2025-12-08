using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#region Enum
public enum TowerGrade
{
    Normal,
    Rare,
    Hero,
    Legend,
}

public enum TowerRole
{
    Dealer,
    Debuff,
    Buff,
    Summon,
    Economy,
    Control,
}

public enum AttackTarget
{
    None,
    Random,
    First,
    Last,
    Near,
    Far,
    Weak,
    Strong,
}
#endregion

[CreateAssetMenu(fileName = "Tower", menuName = "TowerData", order = 0)]
public class TowerData : ScriptableObject
{
    [Header("Data")]
    public int ID;
    public string Name;
    public Sprite Symbol;
    public Color Color = Color.black;

    [Header("Type")]
    public TowerGrade Grade;
    public TowerRole Role;

    [Header("Battle")]
    public int AttackDamage = 1;
    public float AttackSpeed = 3f;
    public AttackTarget AttackTarget = AttackTarget.First;

    [Header("Skill")]
    public List<TowerSkill> Skills = new List<TowerSkill>();
    public List<Vector3> Values = new List<Vector3>();

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
            if (d != null && d != this && d.Symbol != null)
            {
                string path = AssetDatabase.GetAssetPath(d.Symbol);
                string dir = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(dir)) continue;

                dir = dir.Replace("\\", "/");
                if (!dir.EndsWith("/Images/Towers")) continue;

                used.Add(d.Symbol.name);
            }
        }

        Sprite pick = null;
        if (Symbol == null || used.Contains(Symbol.name))
        {
            for (int i = 0; i < baseSprites.Count; i++)
            {
                Sprite s = baseSprites[i];
                if (used.Contains(s.name)) continue;

                pick = s;
                break;
            }

            if (pick == null)
            {
                for (int i = 0; i < baseSprites.Count; i++)
                {
                    Sprite s = baseSprites[i];
                    if (s.name == "Star")
                    {
                        pick = s;
                        break;
                    }
                }
            }
            Symbol = pick;
        }

        if (Symbol != null && Symbol.name != "Star")
        {
            var m = Regex.Match(Symbol.name, @"^(?<num>\d+)\.");
            ID = m.Success ? int.Parse(m.Groups["num"].Value) : ID;

            string rawName = Symbol.name;
            Name = Regex.Replace(rawName, @"^\d+\.", "");
        }
        else
        {
            ID = 0;
            Name = null;
        }

        for (int i = 0; i < Values.Count; i++)
        {
            Values[i] = ValidateValue(Values[i]);
        }

        EditorUtility.SetDirty(this);
    }

    private Vector3 ValidateValue(Vector3 _value)
    {
        _value.x = Mathf.Max(_value.x, 0f);

        if (_value.y == 0f)
        {
            _value.y = 0f;
            _value.z = 0f;
        }

        if (_value.z > 0f)
        {
            _value.y = 1f;
            _value.z = 1f;
        }
        else _value.z = 0f;

        return _value;
    }
#endif

    public TowerData Clone()
    {
        TowerData clone = CreateInstance<TowerData>();

        clone.ID = this.ID;
        clone.Name = this.Name;
        clone.Symbol = this.Symbol;
        clone.Color = this.Color;

        clone.Grade = this.Grade;
        clone.Role = this.Role;

        clone.AttackDamage = this.AttackDamage;
        clone.AttackSpeed = this.AttackSpeed;
        clone.AttackTarget = this.AttackTarget;

        clone.Skills = new List<TowerSkill>(Skills);
        clone.Values = new List<Vector3>(Values);

        return clone;
    }
}
