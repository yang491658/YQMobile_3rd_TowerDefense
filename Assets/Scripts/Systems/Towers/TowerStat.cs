using UnityEngine;

[CreateAssetMenu(fileName = "TowerStat", menuName = "Table/Tower/Stat", order = 53)]
public class TowerStat : ScriptableObject
{
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

    [Header("Grade")]
    [SerializeField][Min(0)] private int normal = 1;
    [SerializeField][Min(0)] private int rare = 3;
    [SerializeField][Min(0)] private int epic = 7;
    [SerializeField][Min(0)] private int unique = 15;
    [SerializeField][Min(0)] private int legend = 35;
    [SerializeField][Min(0)] private int mythic = 100;

    [Header("Role")]
    [SerializeField] private Stat4 dealer = new(10, 20);
    [SerializeField] private Stat4 debuff = new(5, 15);
    [SerializeField] private Stat4 buff = new(0, 0);
    [SerializeField] private Stat4 summon = new(10, 5);

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

    public int GetGradeStat(TowerGrade _grade) => _grade switch
    {
        TowerGrade.Normal => normal,
        TowerGrade.Rare => rare,
        TowerGrade.Epic => epic,
        TowerGrade.Unique => unique,
        TowerGrade.Legend => legend,
        TowerGrade.Mythic => mythic,
        _ => 1,
    };

    private Stat4 GetRoleStat(TowerRole _role) => _role switch
    {
        TowerRole.Dealer => dealer,
        TowerRole.Debuff => debuff,
        TowerRole.Buff => buff,
        TowerRole.Summon => summon,
        _ => dealer,
    };
}
