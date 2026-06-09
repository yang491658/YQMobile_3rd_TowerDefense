using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Bounce", menuName = "Skill/Summon/Bounce", order = 402)]
public class Bounce : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int count;

    [Header("Const")]
    private const float speed = 10f;

    [Header("Others")]
    private Coroutine routine;
    private const float scale = 0.5f;
    private const float interval = 0.15f;

#if UNITY_EDITOR
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
        if (routine != null) return;

        routine = EntityManager.Instance?.StartCoroutine(BounceCoroutine(_tower, _target, _target.Index));
    }

    private IEnumerator BounceCoroutine(Tower _tower, Monster _target, int _index)
    {
        for (int i = 0; i < count; i++)
        {
            if (_tower == null) break;
            if (_target == null || _target.IsInvalid(_index)) break;

            Vector3 pos = _tower.transform.position;

            EntityManager.Instance?.MakeSummon(this, _tower, pos, scale, speed)
                ?.SetBounce(_target);

            yield return new WaitForSeconds(interval);
        }

        routine = null;
    }
}
