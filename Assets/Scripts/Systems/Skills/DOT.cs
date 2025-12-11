using UnityEngine;

[CreateAssetMenu(fileName = "DOT", menuName = "TowerSkill/Debuff/DOT", order = 201)]
public class DOT : TowerSkill
{
    [Header("Skill Value")]
    [SerializeField][Min(0)] private int damage;
    [SerializeField][Min(0)] private float duration;

    public override void SetValues(Tower _tower)
    {
        damage = _tower.GetValueInt(ValueType.Damage);
        duration = _tower.GetValue(ValueType.Duration);
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        Effect e = EntityManager.Instance?.MakeEffect(_tower, _target, _duration: duration);
        _target.ApplyDot(damage, duration, e);
    }
}
