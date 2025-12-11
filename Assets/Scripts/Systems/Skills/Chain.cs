using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Chain", menuName = "TowerSkill/Dealer/Chain", order = 102)]
public class Chain : TowerSkill
{
    private float interval = 0.1f;

    [Header("Skill Value")]
    [SerializeField][Min(0)] private int damage;
    [SerializeField][Min(0)] private int count;

    public override void SetValues(Tower _tower)
    {
        damage = _tower.GetValueInt(ValueType.Damage);
        count = _tower.GetValueInt(ValueType.Count);
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        int start = EntityManager.Instance.GetMonsterNumber(_target);

        EntityManager.Instance?.MakeEffect(_tower, _target.transform);
        _tower.StartCoroutine(ChainRoutine(_tower, start, count - 1));
    }

    private IEnumerator ChainRoutine(Tower _tower, int _start, int _count)
    {
        int hits = 0;
        int index = _start + 1;

        yield return new WaitForSeconds(interval);

        while (hits++ < _count)
        {
            if (index >= EntityManager.Instance.GetMonsterCount())
                yield break;

            Monster m = EntityManager.Instance?.GetMonsterByIndex(index++);

            EntityManager.Instance?.MakeEffect(_tower, m.transform, _duration: 0.3f);
            m.TakeDamage(damage, _direct: true);

            yield return new WaitForSeconds(interval);
        }
    }
}
