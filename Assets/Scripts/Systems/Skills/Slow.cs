using UnityEngine;

[CreateAssetMenu(fileName = "Slow", menuName = "TowerSkill/Slow", order = 4)]
public class Slow : Skill
{
    [Header("Skill")]
    [SerializeField] private int percent;
    [SerializeField] private float duration;

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
