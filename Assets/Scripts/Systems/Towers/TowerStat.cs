using UnityEngine;

[CreateAssetMenu(fileName = "TowerStat", menuName = "Towers/Tables/TowerStat", order = 104)]
public class TowerStat : ScriptableObject
{
    [System.Serializable]
    public struct Stat4
    {
        [Min(0)] public int attackDamage;
        [Min(0)] public int attackSpeed;
        [Min(0)] public int criticalChance;
        [Min(0)] public int criticalDamage;

        public Stat4(int _damage, int _speed, int _chance, int _critical)
        {
            attackDamage = Mathf.Max(_damage, 0);
            attackSpeed = Mathf.Max(_speed, 0);
            criticalChance = attackDamage > 0 ? Mathf.Max(_chance, 0) : 0;
            criticalDamage = criticalChance > 0 ? Mathf.Max(_critical, 100) : 100;
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
    [SerializeField] private Stat4 dealer = new Stat4(10, 20, 5, 150);
    [SerializeField] private Stat4 debuff = new Stat4(5, 15, 5, 150);
    [SerializeField] private Stat4 buff = new Stat4(0, 0, 0, 100);
    [SerializeField] private Stat4 summon = new Stat4(10, 5, 5, 150);

    public Stat4 GetStat(TowerRole _role, TowerGrade _grade)
    {
        int gradeValue = GetGradeStat(_grade);
        Stat4 stat = GetRoleStat(_role);

        stat.attackDamage *= gradeValue;
        stat.attackSpeed *= gradeValue;

        return stat;
    }

    public int GetGradeStat(TowerGrade _grade) => _grade switch
    {
        TowerGrade.Normal or TowerGrade.Temp => normal,
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
        _ => default,
    };
}
