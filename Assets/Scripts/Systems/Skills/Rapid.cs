using UnityEngine;

[CreateAssetMenu(fileName = "Rapid", menuName = "TowerSkill/Rapid", order = 21)]
public class Rapid : TowerSkill
{
    private int hitCount = 0;

    [Header("Skill")]
    [SerializeField][Min(0)] private int count;

    public override void SetValues(Tower _tower)
    {
        count = _tower.GetValueInt(ValueType.Count);
    }

    public override void OnTakeDamage(Tower _tower, Monster _target, ref int _damage, ref bool _critical)
    {
        hitCount++;
        if (hitCount < count) return;

        hitCount = 0;

        TowerData data = _tower.GetData();
        int baseDamage = data.AttackDamage * _tower.GetRank();
        int damage = baseDamage * data.CriticalDamage / 100;

        _damage = damage;
        _critical = true;
    }
}
