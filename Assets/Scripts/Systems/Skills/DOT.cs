using UnityEngine;

[CreateAssetMenu(fileName = "DOT", menuName = "TowerSkill/DOT", order = 31)]
public class DOT : Skill
{
    [Header("Skill")]
    [SerializeField] private int damage;
    [SerializeField] private float duration;

    public override void SetValues(Tower _tower)
    {
        damage = _tower.GetValueInt(ValueType.Damage);
        duration = _tower.GetValue(ValueType.Duration);
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        Effect e = EntityManager.Instance.MakeEffect(_tower, _target.transform, _duration: duration);
        _target.ApplyDot(damage, duration, e);
    }
}
