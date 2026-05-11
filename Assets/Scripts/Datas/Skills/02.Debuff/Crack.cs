using UnityEngine;

[CreateAssetMenu(fileName = "Crack", menuName = "Skill/Debuff/Crack", order = 204)]
public class Crack : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int factor;
    [SerializeField][Min(0f)] private float duration;

#if UNITY_EDITOR
    public override ValueType[] GetValues()
        => new[] { ValueType.Factor, ValueType.Duration };
#endif

    public override System.Predicate<Monster> GetFilter()
        => _monster => !_monster.Debuff.HasBonusDamage;

    public override void SetValues(Tower _tower)
    {
        factor = _tower.GetValueInt(this, ValueType.Factor);
        duration = _tower.GetValue(this, ValueType.Duration);
    }

    public override void OnAttack(Tower _tower, Monster _target, ref bool _instead)
    {
        _target.Debuff.ActiveBonus();
    }

    public override void OnHit(Tower _tower, Bullet _bullet, Monster _target, ref bool _instead)
    {
        if (_target == null || _target.IsInvalid()) return;

        ViewEffect effect = EntityManager.Instance?.MakeEffect(_tower, _target, duration);
        _target.Debuff.ApplyBonus(factor, duration, effect);
    }
}
