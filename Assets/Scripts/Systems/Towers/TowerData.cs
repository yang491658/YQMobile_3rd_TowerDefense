using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
    public AttackTarget AttackTarget = AttackTarget.First;
    public TowerValue AttackDamage = new TowerValue(1f, RankType.Multiply);
    public TowerValue AttackSpeed = new TowerValue(3f, RankType.Divide);
    [Space]
    public TowerValue CriticalChance = new TowerValue(10f, RankType.Multiply);
    public TowerValue CriticalDamage = new TowerValue(150f, RankType.None);

    [Header("Skill")]
    public List<TowerSkill> Skills = new List<TowerSkill>();
    public List<SkillValue> Values = new List<SkillValue>();

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

        AttackDamage = ValidateValue(AttackDamage);
        AttackSpeed = ValidateValue(AttackSpeed);
        CriticalChance = ValidateValue(CriticalChance);
        CriticalDamage = ValidateValue(CriticalDamage);

        for (int i = 0; i < Values.Count; i++)
            Values[i] = ValidateValue(Values[i]);

        EditorUtility.SetDirty(this);
    }

    private T ValidateValue<T>(T _value, ref float _baseValue, RankType _rankType, ref float _rankBonus)
    {
        _baseValue = Mathf.Max(_baseValue, 0f);

        if (_rankType == RankType.None)
            _rankBonus = 0f;
        else if (_rankType == RankType.Multiply
            || _rankType == RankType.Divide)
            _rankBonus = 1f;

        return _value;
    }

    private TowerValue ValidateValue(TowerValue _value)
        => ValidateValue(_value, ref _value.baseValue, _value.rankType, ref _value.rankBonus);

    private SkillValue ValidateValue(SkillValue _value)
        => ValidateValue(_value, ref _value.baseValue, _value.rankType, ref _value.rankBonus);

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
        clone.CriticalChance = this.CriticalChance;
        clone.CriticalDamage = this.CriticalDamage;

        clone.Skills = new List<TowerSkill>(Skills);
        clone.Values = new List<SkillValue>(Values);

        return clone;
    }
}
