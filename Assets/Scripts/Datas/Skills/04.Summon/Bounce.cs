using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Bounce", menuName = "Skill/Summon/Bounce", order = 402)]
public class Bounce : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int count;

    private Monster target;
    private int targetIndex;

    private const float scale = 0.5f;
    private const float rate = 0.1f;
    private const float rotate = 1800f;
    private const float interval = 0.15f;

#if UNITY_EDITOR
    public override void SetID() => ID = 402;
    public override ValueType[] GetValues()
        => new[] { ValueType.Count };
#endif

    public override void SetValues(Tower _tower)
    {
        count = _tower.GetValueInt(this, ValueType.Count);
    }

    public override void OnUpdate(Tower _tower, float _deltaTime)
    {
        if (_tower.GetSummonCount(this) > 0) return;

        if (target == null || target.IsInvalid(targetIndex))
        {
            target = EntityManager.Instance?.GetMonsterNearest(_tower.transform.position);
            targetIndex = target != null ? target.Index : 0;
        }
        if (target == null || target.IsInvalid(targetIndex)) return;

        _tower.StartCoroutine(SummonCoroutine(_tower));
    }

    private IEnumerator SummonCoroutine(Tower _tower)
    {
        Vector3 pos = _tower.transform.position;

        for (int i = 0; i < count; i++)
        {
            if (target == null || target.IsInvalid(targetIndex))
            {
                target = EntityManager.Instance?.GetMonsterNearest(pos);
                targetIndex = target != null ? target.Index : 0;
            }
            if (target == null || target.IsInvalid(targetIndex)) yield break;

            EntityManager.Instance?.MakeSummon(this, _tower, pos, scale, rate)
                ?.SetBounce(target, rotate);

            yield return new WaitForSeconds(interval);
        }
    }
}