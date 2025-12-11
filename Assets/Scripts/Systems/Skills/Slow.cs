using UnityEngine;

[CreateAssetMenu(fileName = "Slow", menuName = "TowerSkill/Debuff/Slow", order = 202)]
public class Slow : TowerSkill
{
    [Header("Skill Value")]
    [SerializeField][Min(0)] private int percent;
    [SerializeField][Min(0f)] private float duration;

    public override void SetValues(Tower _tower)
    {
        percent = _tower.GetValueInt(ValueType.Percent);
        duration = _tower.GetValue(ValueType.Duration);
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        Effect e = EntityManager.Instance.MakeEffect(_tower, _target.transform, _duration: duration);
        _target.ApplySlow(percent, duration, e);
    }
}
