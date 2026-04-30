using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Splash", menuName = "Skill/Dealing/Splash", order = 101)]
public class Splash : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int damage;
    [SerializeField][Min(0)] private int range;

#if UNITY_EDITOR
    public override void SetID() => ID = 101;
    public override ValueType[] GetValues()
        => new[] { ValueType.Damage, ValueType.Range };
#endif

    public override void SetValues(Tower _tower)
    {
        damage = _tower.GetValueInt(this, ValueType.Damage);
        range = _tower.GetValueInt(this, ValueType.Range);
    }

    public override void OnHit(Tower _tower, Monster _target, ref bool _instead)
    {
        Vector3 pos = _target.transform.position;
        List<Monster> monsters = EntityManager.Instance?.GetMonstersInRange(pos, range);

        EntityManager.Instance?.MakeEffect(_tower, pos, range * 2f);

        for (int i = 0; i < monsters.Count; i++)
        {
            Monster monster = monsters[i];
            if (monster == _target) continue;

            monster.TakeDamage(damage, _direct: true);
        }
    }

    public override void OnImpact(Tower _tower, Vector3 _pos)
    {
        EntityManager.Instance?.MakeEffect(_tower, _pos, range * 2f);

        List<Monster> monsters = EntityManager.Instance?.GetMonstersInRange(_pos, range);
        for (int i = 0; i < monsters.Count; i++)
            monsters[i].TakeDamage(damage, _direct: true);
    }
}