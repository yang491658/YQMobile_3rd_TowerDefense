using UnityEngine;

[CreateAssetMenu(fileName = "Slow", menuName = "Skill/Debuff/Slow", order = 202)]
public class Slow : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int factor;
    [SerializeField][Min(0f)] private float duration;

#if UNITY_EDITOR
    public override void SetID() => ID = 202;
    public override ValueType[] GetValues()
        => new[] { ValueType.Factor, ValueType.Duration };
#endif

    public override System.Predicate<Monster> GetFilter()
        => _monster => !_monster.GetDebuff().HasSpeedControl();

    public override void SetValues(Tower _tower)
    {
        factor = _tower.GetValueInt(this, ValueType.Factor);
        duration = _tower.GetValue(this, ValueType.Duration);
    }

    public override void OnAttack(Tower _tower, Monster _target, ref bool _instead)
    {
        _target.GetDebuff().ActiveSpeed();
    }

    public override void OnHit(Tower _tower, Monster _target, ref bool _instead)
    {
        if (_target == null || _target.IsInvalid()) return;

        int value = Mathf.Min(factor, 90);
        ViewEffect effect = EntityManager.Instance?.MakeEffect(_tower, _target, duration);
        _target.GetDebuff().ApplySpeed(value, duration, effect);
    }
}
