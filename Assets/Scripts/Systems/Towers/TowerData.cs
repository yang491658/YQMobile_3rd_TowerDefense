using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

[System.Serializable]
public struct SkillConfig
{
    public TowerGrade grade;
    public TowerSkill skill;
    public List<SkillValue> values;
}

[CreateAssetMenu(fileName = "TowerData", menuName = "Data/Tower", order = 1)]
public class TowerData : ScriptableObject
{
    [Header("Data")]
    public Sprite Icon;
    public int ID;
    public string Name;
    public Sprite Symbol;
    public Color Color = Color.black;

    [Header("Type")]
    public TowerRole Role = TowerRole.Dealer;
    public AttackTarget Target = AttackTarget.First;
    public List<SkillConfig> Skills = new();

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoIcon();
        AutoName();
        AutoSymbol();
        AutoSkill();
        AutoValue();

        EditorUtility.SetDirty(this);
    }

    private void AutoIcon()
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
            if (data != null && data != this && data.Icon != null)
            {
                string path = AssetDatabase.GetAssetPath(data.Icon);
                string dir = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(dir)) continue;

                dir = dir.Replace("\\", "/");
                if (!dir.EndsWith("/Images/Towers")) continue;

                used.Add(data.Icon.name);
            }
        }

        Sprite pick = null;
        if (Icon == null || used.Contains(Icon.name))
        {
            for (int i = 0; i < baseSprites.Count; i++)
            {
                Sprite sprite = baseSprites[i];
                if (used.Contains(sprite.name)) continue;

                pick = sprite;
                break;
            }
            Icon = pick;
        }
    }

    private void AutoName()
    {
        if (Icon != null)
        {
            string[] split = Icon.name.Split('.', 2);
            int number = 0;
            if (split.Length > 0)
                int.TryParse(split[0], out number);

            ID = (int)Role * 100 + number % 100;
            Name = split.Length > 1 ? split[1] : Icon.name;
        }
        else
        {
            Role = TowerRole.Dealer;
            ID = 999;
            Name = TowerGrade.Temp.ToString();
        }
    }

    private void AutoSymbol()
    {
        Symbol = null;

        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Symbols");
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite.name != Role.ToString()) continue;

            Symbol = sprite;
            return;
        }
    }

    private void AutoSkill()
    {
        if (Skills == null)
            Skills = new();

        if (ID == 999)
        { Skills = new(); return; }

        TowerGrade start = TowerGrade.Normal;
        TowerGrade end = TowerGrade.Legend;

        if (Role == TowerRole.Debuff || Role == TowerRole.Buff)
        {
            start = TowerGrade.Rare;
            end = TowerGrade.Unique;
        }
        else if (Role == TowerRole.Summon)
        {
            start = TowerGrade.Epic;
            end = TowerGrade.Unique;
        }

        HashSet<TowerGrade> grades = new();
        for (int i = 0; i < Skills.Count; i++)
        {
            TowerGrade grade = Skills[i].grade;
            if (grade < start || grade > end || !grades.Add(grade))
                Skills.RemoveAt(i--);
        }

        for (int i = (int)start; i <= (int)end; i++)
        {
            TowerGrade grade = (TowerGrade)i;
            bool exists = false;

            for (int j = 0; j < Skills.Count; j++)
            {
                if (Skills[j].grade != grade) continue;

                exists = true;
                break;
            }

            if (exists) continue;

            SkillConfig config = new()
            {
                grade = grade,
                values = new()
            };
            Skills.Add(config);
        }

        Skills.Sort((_a, _b) => _a.grade.CompareTo(_b.grade));
    }

    private void AutoValue()
    {
        if (Skills == null)
            Skills = new();

        for (int i = Skills.Count - 1; i >= 0; i--)
        {
            SkillConfig config = Skills[i];

            if (config.values == null)
                config.values = new();

            if (config.skill == null)
            {
                config.values.Clear();
                Skills[i] = config;
                continue;
            }

            ValidateSkill(ref config);
            Skills[i] = config;
        }
    }

    private void ValidateSkill(ref SkillConfig _config)
    {
        if (_config.skill == null) return;
        if (_config.values == null) _config.values = new();

        HashSet<ValueType> required = new();

        ValueType[] types = _config.skill.GetValues();
        if (types != null && types.Length > 0)
        {
            for (int i = 0; i < types.Length; i++)
            {
                ValueType type = types[i];
                if (!required.Add(type)) continue;

                bool exists = false;
                for (int j = 0; j < _config.values.Count; j++)
                    if (_config.values[j].valueType == type)
                    { exists = true; break; }

                if (!exists)
                    _config.values.Add(new SkillValue(type, 0f, RankType.None));
            }
        }

        for (int i = _config.values.Count - 1; i >= 0; i--)
        {
            SkillValue value = _config.values[i];

            if (!required.Contains(value.valueType))
            { _config.values.RemoveAt(i); continue; }

            ValidateValue(ref value);
            _config.values[i] = value;
        }
    }

    private void ValidateValue(ref SkillValue _value)
    {
        _value.baseValue = Mathf.Max(_value.baseValue, 0f);

        if (_value.rankType == RankType.None)
            _value.rankBonus = 0f;
        else if (_value.rankType == RankType.Multiply
            || _value.rankType == RankType.Divide)
            _value.rankBonus = 1f;
    }
#endif

    #region GET
    public bool HasGrade(TowerGrade _grade)
    {
        if (Skills.Count <= 0)
            return _grade == TowerGrade.Temp;

        for (int i = 0; i < Skills.Count; i++)
        {
            SkillConfig config = Skills[i];
            if (config.grade == _grade)
                return true;
        }

        return false;
    }
    #endregion

    #region 프로퍼티
    public TowerGrade RandomGrade => Skills.Count > 0 ? Skills[Random.Range(0, Skills.Count)].grade : TowerGrade.Temp;
    public TowerGrade MinGrade => Skills.Count > 0 ? Skills[0].grade : TowerGrade.Temp;
    public TowerGrade MaxGrade => Skills.Count > 0 ? Skills[Skills.Count - 1].grade : TowerGrade.Temp;
    #endregion
}
