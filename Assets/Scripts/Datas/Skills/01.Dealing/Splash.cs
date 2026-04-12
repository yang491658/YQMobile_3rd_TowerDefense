using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Splash", menuName = "Skill/Dealing/Splash", order = 101)]
public class Splash : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int damage;
    [SerializeField][Min(0)] private int range;

    private Vector3 targetPos;

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

    public override void OnAttack(Tower _tower, Monster _target, ref bool _instead)
    {
        targetPos = _target.transform.position;
    }

    public override void OnHit(Tower _tower, Monster _target, ref bool _instead)
    {
        if (_target != null)
            targetPos = _target.transform.position;

        EntityManager.Instance?.MakeEffect(_tower, targetPos, range * 2f);

        List<Monster> monsters = EntityManager.Instance?.GetMonstersInRange(targetPos, range);
        for (int i = 0; i < monsters.Count; i++)
        {
            Monster monster = monsters[i];
            if (monster == _target) continue;

            monster.TakeDamage(damage, _direct: true);
        }
    }
}