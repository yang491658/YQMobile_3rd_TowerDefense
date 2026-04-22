using UnityEngine;

[CreateAssetMenu(fileName = "Crack", menuName = "Skill/Debuff/Crack", order = 204)]
public class Crack : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int factor;
    [SerializeField][Min(0f)] private float duration;

#if UNITY_EDITOR
    public override void SetID() => ID = 204;
    public override ValueType[] GetValues()
        => new[] { ValueType.Factor, ValueType.Duration };
#endif

    public override System.Predicate<Monster> GetFilter()
        => _monster => !_monster.GetDebuff().HasDamageAmp();

    public override void SetValues(Tower _tower)
    {
        factor = _tower.GetValueInt(this, ValueType.Factor);
        duration = _tower.GetValue(this, ValueType.Duration);
    }

    public override void OnAttack(Tower _tower, Monster _target, ref bool _instead)
    {
        _target.GetDebuff().ActiveDamage();
    }

    public override void OnHit(Tower _tower, Monster _target, ref bool _instead)
    {
        if (_target == null || _target.IsInvalid()) return;

        ViewEffect effect = EntityManager.Instance?.MakeEffect(_tower, _target, duration);
        _target.GetDebuff().ApplyDamage(factor, duration, effect);
    }
}
