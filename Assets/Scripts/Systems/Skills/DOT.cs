using UnityEngine;

[CreateAssetMenu(fileName = "DOT", menuName = "TowerSkill/DOT", order = 3)]
public class DOT : Skill
{
    [Header("Skill")]
    [SerializeField] private float damage;
    [SerializeField] private float duration;

    public override void SetValues(Tower _tower)
    {
        damage = _tower.GetValue(ValueType.Damage);
        duration = _tower.GetValue(ValueType.Duration);
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        Effect e = EntityManager.Instance.MakeEffect(_tower, _target.transform, 1f, duration);
        _target.ApplyDot(damage, duration, e);
    }
}
