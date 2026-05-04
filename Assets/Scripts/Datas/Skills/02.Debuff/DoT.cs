using UnityEngine;

[CreateAssetMenu(fileName = "DoT", menuName = "Skill/Debuff/DoT", order = 203)]
public class DoT : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int damage;
    [SerializeField][Min(0f)] private float duration;

#if UNITY_EDITOR
    public override void SetID() => ID = 203;
    public override ValueType[] GetValues()
        => new[] { ValueType.Damage, ValueType.Duration };
#endif

    public override System.Predicate<Monster> GetFilter()
        => _monster => !_monster.GetDebuff().HasTickDamage();

    public override void SetValues(Tower _tower)
    {
        damage = _tower.GetValueInt(this, ValueType.Damage);
        duration = _tower.GetValue(this, ValueType.Duration);
    }

    public override void OnAttack(Tower _tower, Monster _target, ref bool _instead)
    {
        _target.GetDebuff().ActiveTick();
    }

    public override void OnHit(Tower _tower, Bullet _bullet, Monster _target, ref bool _instead)
    {
        if (_target == null || _target.IsInvalid()) return;

        ViewEffect effect = EntityManager.Instance?.MakeEffect(_tower, _target, duration);
        _target.GetDebuff().ApplyTick(damage, duration, effect);
    }
}
