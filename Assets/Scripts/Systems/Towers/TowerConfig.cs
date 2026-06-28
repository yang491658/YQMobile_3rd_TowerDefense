using UnityEngine;

[System.Serializable]
public struct Stat4
{
    [Min(0)] public int attackDamage;
    [Min(0)] public int attackSpeed;
    [Min(0)] public int criticalChance;
    [Min(0)] public int criticalDamage;

    public Stat4(int _damage, int _speed)
    {
        attackDamage = Mathf.Max(_damage, 0);
        attackSpeed = Mathf.Max(_speed, 0);
        criticalChance = 0;
        criticalDamage = 150;
    }
}

[System.Serializable]
public struct DamageData
{
    public DamageType type;
    public Color color;
    [Min(0)] public int font;

    public DamageData(DamageType _type, Color _color, int _font = 50)
    {
        type = _type;
        color = _color;
        font = Mathf.Max(_font, 0);
    }
}

public enum DamageType { Normal, Critical, DoT }

[CreateAssetMenu(fileName = "TowerConfig", menuName = "Table/Tower/Config", order = 51)]
public class TowerConfig : ScriptableObject
{
    [Header("Tower Color")]
    [SerializeField][InspectorName("일반")] private Color normalColor = Color.black;
    [SerializeField][InspectorName("희귀")] private Color rareColor = Color.magenta;
    [SerializeField][InspectorName("서사")] private Color epicColor = Color.blue;
    [SerializeField][InspectorName("유일")] private Color uniqueColor = Color.green;
    [SerializeField][InspectorName("전설")] private Color legendColor = Color.yellow;
    [SerializeField][InspectorName("신화")] private Color mythicColor = Color.red;

    [Header("Tower Stat")]
    [SerializeField][Min(0)] private int normal = 1;
    [SerializeField][Min(0)] private int rare = 3;
    [SerializeField][Min(0)] private int epic = 7;
    [SerializeField][Min(0)] private int unique = 15;
    [SerializeField][Min(0)] private int legend = 35;
    [SerializeField][Min(0)] private int mythic = 100;
    [Space]
    [SerializeField] private Stat4 dealer = new(10, 20);
    [SerializeField] private Stat4 debuff = new(5, 15);
    [SerializeField] private Stat4 buff = new(0, 0);
    [SerializeField] private Stat4 summon = new(10, 5);

    [Header("Tower Damage")]
    [SerializeField] private DamageData normalDamage = new(DamageType.Normal, Color.black, 50);
    [SerializeField] private DamageData criticalDamage = new(DamageType.Critical, Color.red, 65);
    [SerializeField] private DamageData dotDamage = new(DamageType.DoT, Color.green, 35);

    #region 타워 색상
    public Color GetColor(TowerGrade _grade)
        => _grade switch
        {
            TowerGrade.Normal => normalColor,
            TowerGrade.Rare => rareColor,
            TowerGrade.Epic => epicColor,
            TowerGrade.Unique => uniqueColor,
            TowerGrade.Legend => legendColor,
            TowerGrade.Mythic => mythicColor,
            _ => Color.black,
        };
    #endregion

    #region 타워 스탯
    public Stat4 GetStat(TowerRole _role, TowerGrade _grade, int _rank)
    {
        Stat4 stat = GetRoleStat(_role);
        int gradeValue = GetGradeStat(_grade);

        switch (_role)
        {
            case TowerRole.Dealer:
                gradeValue = Mathf.FloorToInt(gradeValue / 2f + 0.5f);
                stat.attackDamage *= _rank * gradeValue;
                stat.attackSpeed *= _rank * gradeValue;
                break;

            case TowerRole.Debuff:
                stat.attackDamage *= _rank;
                stat.attackSpeed *= _rank * gradeValue;
                break;

            case TowerRole.Summon:
                stat.attackDamage *= _rank * gradeValue;
                stat.attackSpeed *= _rank;
                break;

            default:
                stat.attackDamage *= _rank;
                stat.attackSpeed *= _rank;
                break;
        }

        if (stat.attackDamage > 0)
            stat.criticalChance = 1 + (_rank - 1) * 4;
        else
            stat.criticalDamage = 100;

        return stat;
    }

    public int GetGradeStat(TowerGrade _grade)
        => _grade switch
        {
            TowerGrade.Normal => normal,
            TowerGrade.Rare => rare,
            TowerGrade.Epic => epic,
            TowerGrade.Unique => unique,
            TowerGrade.Legend => legend,
            TowerGrade.Mythic => mythic,
            _ => 1,
        };

    private Stat4 GetRoleStat(TowerRole _role)
        => _role switch
        {
            TowerRole.Dealer => dealer,
            TowerRole.Debuff => debuff,
            TowerRole.Buff => buff,
            TowerRole.Summon => summon,
            _ => dealer,
        };
    #endregion

    #region 타워 데미지
    public DamageData GetDamage(DamageType _type)
        => _type switch
        {
            DamageType.Critical => criticalDamage,
            DamageType.DoT => dotDamage,
            _ => normalDamage,
        };
    #endregion
}
