using UnityEngine;

[CreateAssetMenu(fileName = "Rapid", menuName = "TowerSkill/Rapid", order = 2)]
public class Rapid : Skill
{
    private int hitCount = 0;

    [Header("Skill")]
    [SerializeField] private int count;

    public override void SetValues(Tower _tower)
    {
        count = Mathf.Max(Mathf.RoundToInt(_tower.GetValue(ValueType.Count)), 1);
    }

    public override void OnTakeDamage(Tower _tower, Monster _target, ref float _damage, ref bool _critical)
    {
        hitCount++;
        if (hitCount < count) return;

        hitCount = 0;

        TowerData data = _tower.GetData();
        float baseDamage = data.AttackDamage * _tower.GetRank();
        float damage = baseDamage * data.CriticalDamage / 100f;

        _damage = damage;
        _critical = true;
    }
}
