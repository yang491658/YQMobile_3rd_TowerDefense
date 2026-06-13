using UnityEngine;

[CreateAssetMenu(fileName = "Slow", menuName = "Skill/Debuff/Slow", order = 202)]
public class Slow : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int factor;
    [SerializeField][Min(0f)] private float duration;

#if UNITY_EDITOR
    public override ValueType[] GetValues()
        => new[] { ValueType.Factor, ValueType.Duration };
#endif

    public override System.Predicate<Monster> GetFilter()
        => _monster => !_monster.Debuff.HasMoveControl;

    public override void SetValues(Tower _tower)
    {
        factor = _tower.GetValueInt(this, ValueType.Factor);
        duration = _tower.GetValue(this, ValueType.Duration);
    }

    public override void OnAttack(Tower _tower, Monster _target, ref bool _instead)
    {
        _target.Debuff.ActiveMove();
    }

    public override void OnHit(Tower _tower, Bullet _bullet, Monster _target, ref bool _instead)
    {
        if (_target == null || _target.IsInvalid()) return;

        int value = Mathf.Clamp(factor, 1, 99);
        ViewEffect effect = EntityManager.Instance?.MakeEffect(_tower, _target, duration);
        _target.Debuff.ApplyMove(value, duration, effect);
    }
}
