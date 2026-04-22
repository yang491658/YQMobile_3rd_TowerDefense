using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Bounce", menuName = "Skill/Summon/Bounce", order = 402)]
public class Bounce : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int count;

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

    public override void OnUpdate(Tower _tower, Monster _target, float _deltaTime)
    {
        if (_tower.GetSummonCount(this) > 0) return;
        if (_target == null || _target.IsInvalid()) return;

        EntityManager.Instance?.StartCoroutine(SummonCoroutine(_tower, _target));
    }

    private IEnumerator SummonCoroutine(Tower _tower, Monster _target)
    {
        Vector3 pos = _tower.transform.position;

        for (int i = 0; i < count; i++)
        {
            if (_target == null || _target.IsInvalid()) yield break;

            EntityManager.Instance?.MakeSummon(this, _tower, pos, scale, rate)
                ?.SetBounce(_target, rotate);

            yield return new WaitForSeconds(interval);
        }
    }
}