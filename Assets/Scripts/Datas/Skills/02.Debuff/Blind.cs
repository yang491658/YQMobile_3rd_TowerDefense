using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Blind", menuName = "Skill/Debuff/Blind", order = 205)]
public class Blind : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int chance;
    [SerializeField][Min(0f)] private float duration;

    private readonly HashSet<long> targets = new();

    private static long GetTargetKey(Monster _target)
        => ((long)(uint)_target.GetInstanceID() << 32) | (uint)_target.Index;

#if UNITY_EDITOR
    public override void SetID() => ID = 205;
    public override ValueType[] GetValues()
        => new[] { ValueType.Chance, ValueType.Duration };
#endif

    public override System.Predicate<Monster> GetFilter()
        => _monster => !_monster.GetDebuff().HasDirectionControl();

    public override void SetValues(Tower _tower)
    {
        chance = _tower.GetValueInt(this, ValueType.Chance);
        duration = _tower.GetValue(this, ValueType.Duration);
    }

    public override void OnGenerate(Tower _tower)
    {
        targets.Clear();
    }

    public override void OnAttack(Tower _tower, Monster _target, ref bool _instead)
    {
        float c = Mathf.Min(chance, 100f);
        if (Random.value < c / 100f)
        {
            if (targets.Add(GetTargetKey(_target)))
                _target.GetDebuff().ActiveDirection();
        }
    }

    public override void OnHit(Tower _tower, Monster _target, ref bool _instead)
    {
        if (_target == null || _target.IsInvalid()) return;

        if (targets.Remove(GetTargetKey(_target)))
        {
            ViewEffect effect = EntityManager.Instance?.MakeEffect(_tower, _target, duration);
            _target.GetDebuff().ApplyDirection(0, duration, effect);
        }
    }
}
