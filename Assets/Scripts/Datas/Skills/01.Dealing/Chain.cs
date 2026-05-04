using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Chain", menuName = "Skill/Dealing/Chain", order = 103)]
public class Chain : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int damage;
    [SerializeField][Min(0)] private int count;

    private const float interval = 0.1f;

#if UNITY_EDITOR
    public override void SetID() => ID = 103;
    public override ValueType[] GetValues()
        => new[] { ValueType.Damage, ValueType.Count };
#endif

    public override void SetValues(Tower _tower)
    {
        damage = _tower.GetValueInt(this, ValueType.Damage);
        count = _tower.GetValueInt(this, ValueType.Count);
    }

    public override void OnHit(Tower _tower, Bullet _bullet, Monster _target, ref bool _instead)
    {
        if (_target == null || _target.IsInvalid()) return;

        EntityManager.Instance?.MakeEffect(_tower, _target);
        EntityManager.Instance?.StartCoroutine(ChainCoroutine(_tower, _target.Index));
    }

    public override void OnImpact(Tower _tower, Bullet _bullet, Vector3 _pos)
    {
        EntityManager.Instance?.MakeEffect(_tower, _pos, 0.85f);

        Monster target = EntityManager.Instance?.GetMonsterNearest(_pos);
        if (target == null || target.IsInvalid()) return;

        EntityManager.Instance?.StartCoroutine(ChainCoroutine(_tower, target.Index - 1));
    }

    private IEnumerator ChainCoroutine(Tower _tower, int _start)
    {
        int index = _start;
        int hit = 0;

        yield return new WaitForSeconds(interval);

        while (hit < count)
        {
            Monster target = GetNext(index);
            if (target == null) yield break;

            index = target.Index;

            EntityManager.Instance?.MakeEffect(_tower, target);
            target.TakeDamage(damage);

            if (++hit < count)
                yield return new WaitForSeconds(interval);
        }
    }

    private Monster GetNext(int _start)
    {
        List<Monster> monsters = EntityManager.Instance?.GetMonsters();

        Monster best = null;
        int bestIndex = int.MaxValue;

        for (int i = 0; i < monsters.Count; i++)
        {
            Monster monster = monsters[i];
            if (monster == null || monster.IsInvalid()) continue;

            int index = monster.Index;
            if (index <= _start || index >= bestIndex) continue;

            best = monster;
            bestIndex = index;
        }

        return best;
    }
}
