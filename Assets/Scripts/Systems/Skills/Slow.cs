using UnityEngine;

[CreateAssetMenu(fileName = "Slow", menuName = "TowerSkill/Slow", order = 3)]
public class Slow : Skill
{
    private float percent;
    private float duration;

    public override void SetValues(Tower _tower)
    {
        percent = _tower.GetValue(ValueType.Percent);
        duration = _tower.GetValue(ValueType.Duration);
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        Effect e = EntityManager.Instance.MakeEffect(_tower, _target.transform, 1f, duration);
        _target.ApplySlow(percent, duration, e);
    }
}
