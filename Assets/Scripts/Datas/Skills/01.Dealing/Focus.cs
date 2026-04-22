using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Focus", menuName = "Skill/Dealing/Focus", order = 104)]
public class Focus : TowerSkill
{
    [SerializeField][Min(0)] private int range;

#if UNITY_EDITOR
    public override void SetID() => ID = 104;
    public override ValueType[] GetValues()
        => new[] { ValueType.Range };
#endif

    public override void SetValues(Tower _tower)
    {
        range = _tower.GetValueInt(this, ValueType.Range);
    }

    public override void OnHit(Tower _tower, Monster _target, ref bool _instead)
    {
        _instead = true;

        Vector3 pos = _target.transform.position;
        List<Monster> monsters = EntityManager.Instance?.GetMonstersInRange(pos, range);

        for (int i = 0; i < monsters.Count; i++)
            EntityManager.Instance?.MakeEffect(_tower, monsters[i]);

        int count = Mathf.Max(monsters.Count, 1);
        _tower.HitDamage(_target, _tower.GetDamage() * count);
    }
}
