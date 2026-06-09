using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Zone", menuName = "Skill/Summon/Zone", order = 403)]
public class Zone : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0f)] private float duration;

    [Header("Const")]
    private const float scale = 1f;
    private const float cooldown = 1f;

    [Header("Others")]
    private Coroutine routine;

#if UNITY_EDITOR
    public override ValueType[] GetValues()
        => new[] { ValueType.Duration };
#endif

    public override void SetValues(Tower _tower)
    {
        duration = _tower.GetValue(this, ValueType.Duration);
    }

    public override void OnUpdate(Tower _tower, Monster _target, float _deltaTime)
    {
        if (_tower.GetSummonCount(this) > 0) return;
        if (_target == null || _target.IsInvalid()) return;
        if (IsCooldown) return;
        if (routine != null) return;

        EntityManager.Instance?.MakeSummon(this, _tower, _target.transform.position, scale, _tower.Damage)
            ?.SetZone(duration);

        routine = EntityManager.Instance?.StartCoroutine(ZoneCoroutine(_tower));
    }

    private IEnumerator ZoneCoroutine(Tower _tower)
    {
        yield return new WaitForSeconds(duration);

        if (_tower != null)
            StartCooldown(_tower, cooldown);

        routine = null;
    }
}
