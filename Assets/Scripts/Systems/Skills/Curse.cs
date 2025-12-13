using UnityEngine;

[CreateAssetMenu(fileName = "Curse", menuName = "TowerSkill/Debuff/Curse", order = 203)]
public class Curse : TowerSkill
{
    [Header("Skill Value")]
    [SerializeField][Min(0)] private int factor;
    [SerializeField][Min(0f)] private float duration;

    public override void SetValues(Tower _tower)
    {
        factor = _tower.GetValueInt(ValueType.Factor);
        duration = _tower.GetValue(ValueType.Duration);
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        Effect e = EntityManager.Instance?.MakeEffect(_tower, _target, _duration: duration);
        _target.ApplyCurse(factor, duration, e);
    }
}
