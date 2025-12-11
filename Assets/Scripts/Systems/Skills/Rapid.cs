using UnityEngine;

[CreateAssetMenu(fileName = "Rapid", menuName = "TowerSkill/Dealer/Rapid", order = 103)]
public class Rapid : TowerSkill
{
    private int hitCount = 0;

    [Header("Skill Value")]
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

        if (_critical) return;

        TowerData data = _tower.GetData();
        int critDamage = data.CriticalDamage;

        _damage = _damage * critDamage / 100;
        _critical = true;
    }
}
