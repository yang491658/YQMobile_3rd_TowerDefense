using UnityEngine;

[CreateAssetMenu(fileName = "Orbit", menuName = "Skill/Summon/Orbit", order = 401)]
public class Orbit : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int count;

    private const float scale = 0.35f;
    private const float rate = 0.15f;

#if UNITY_EDITOR
    public override void SetID() => ID = 401;
    public override ValueType[] GetValues()
        => new[] { ValueType.Count };
#endif

    public override void SetValues(Tower _tower)
    {
        count = _tower.GetValueInt(this, ValueType.Count);
    }

    public override void OnUpdate(Tower _tower, Monster _target, float _deltaTime)
    {
        if (_tower.GetSummonCount(this) == count) return;

        _tower.ClearSummon(this);

        Vector3 center = _tower.transform.position;
        float radius = _tower.transform.localScale.x;

        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = step * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

            EntityManager.Instance?.MakeSummon(this, _tower, pos, scale, rate)
                ?.SetOrbit(radius, angle);
        }
    }
}