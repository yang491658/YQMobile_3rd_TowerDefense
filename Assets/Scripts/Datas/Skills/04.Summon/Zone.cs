using UnityEngine;

[CreateAssetMenu(fileName = "Zone", menuName = "Skill/Summon/Zone", order = 403)]
public class Zone : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0f)] private float scale;
    [SerializeField][Min(0f)] private float duration;
    [SerializeField][Min(0f)] private float cooldown;

#if UNITY_EDITOR
    public override void SetID() => ID = 403;
    public override ValueType[] GetValues()
        => new[] { ValueType.Scale, ValueType.Duration, ValueType.Cooldown };
#endif

    public override void SetValues(Tower _tower)
    {
        scale = _tower.GetValue(this, ValueType.Scale);
        duration = _tower.GetValue(this, ValueType.Duration);
        cooldown = _tower.GetValue(this, ValueType.Cooldown);
    }

    public override void OnUpdate(Tower _tower, Monster _target, float _deltaTime)
    {
        if (_target == null || _target.IsInvalid()) return;
        if (IsCooldown()) return;

        EntityManager.Instance?.MakeSummon(this, _tower, _target.transform.position, scale)
            ?.SetZone(duration, _tower.GetDamage());

        StartCooldown(_tower, duration + cooldown);
    }
}
