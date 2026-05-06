using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Bounce", menuName = "Skill/Summon/Bounce", order = 402)]
public class Bounce : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int count;
    [SerializeField][Min(0f)] private float speed;

    private const float scale = 0.5f;
    private const float rotate = 180f;
    private const float interval = 0.15f;

#if UNITY_EDITOR
    public override void SetID() => ID = 402;
    public override ValueType[] GetValues()
        => new[] { ValueType.Count, ValueType.Speed };
#endif

    public override void SetValues(Tower _tower)
    {
        count = _tower.GetValueInt(this, ValueType.Count);
        speed = _tower.GetValue(this, ValueType.Speed);
    }

    public override void OnUpdate(Tower _tower, Monster _target, float _deltaTime)
    {
        if (_tower.GetSummonCount(this) > 0) return;
        if (_target == null || _target.IsInvalid()) return;

        EntityManager.Instance?.StartCoroutine(BounceCoroutine(_tower, _target, _target.Index));
    }

    private IEnumerator BounceCoroutine(Tower _tower, Monster _target, int _index)
    {
        Vector3 pos = _tower.transform.position;

        for (int i = 0; i < count; i++)
        {
            if (_tower == null) yield break;
            if (_target == null || _target.IsInvalid(_index)) yield break;

            EntityManager.Instance?.MakeSummon(this, _tower, pos, scale, speed)
                ?.SetBounce(_target, speed * rotate);

            yield return new WaitForSeconds(interval);
        }
    }
}
