using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageUp", menuName = "Skill/Buff/DamageUp", order = 301)]
public class DamageUp : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int range;
    [SerializeField][Min(0)] private int factor;

    private readonly List<Tower> targets = new();
    private readonly HashSet<Tower> currents = new();

#if UNITY_EDITOR
    public override void SetID() => ID = 301;
    public override ValueType[] GetValues()
        => new[] { ValueType.Range, ValueType.Factor };
#endif

    public override void SetValues(Tower _tower)
    {
        range = _tower.GetValueInt(this, ValueType.Range);
        factor = _tower.GetValueInt(this, ValueType.Factor);
    }

    public override void OnGenerate(Tower _tower)
    {
        targets.Clear();
    }

    public override void OnUpdate(Tower _tower, Monster _target, float _deltaTime)
    {
        List<Tower> current = EntityManager.Instance?.GetTowersInRange(_tower.transform.position, range);

        currents.Clear();
        for (int i = 0; i < current.Count; i++) currents.Add(current[i]);

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            Tower target = targets[i];
            if (currents.Contains(target)) continue;

            TowerBuff buff = target.GetBuff();
            buff.RemoveStat(_tower, this, TowerBuff.SubType.Damage);
            buff.RemoveStat(_tower, this, TowerBuff.SubType.Critical);
            targets.RemoveAt(i);
        }

        for (int i = 0; i < current.Count; i++)
        {
            Tower target = current[i];
            TowerBuff buff = target.GetBuff();

            buff.ApplyStat(_tower, this, TowerBuff.SubType.Damage, factor, 0f, TowerBuff.ApplyType.Refresh);
            buff.ApplyStat(_tower, this, TowerBuff.SubType.Critical, factor, 0f, TowerBuff.ApplyType.Refresh);

            if (targets.Contains(target)) continue;

            EntityManager.Instance?.MakeEffect(_tower, target);
            targets.Add(target);
        }
    }

    public override void OnMerge(Tower _tower, Tower _target)
    {
        ClearBuff(_tower);
    }

    public override void OnSell(Tower _tower)
    {
        ClearBuff(_tower);
    }

    private void ClearBuff(Tower _tower)
    {
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            Tower target = targets[i];
            TowerBuff buff = target.GetBuff();

            buff.RemoveStat(_tower, this, TowerBuff.SubType.Damage);
            buff.RemoveStat(_tower, this, TowerBuff.SubType.Critical);
        }

        targets.Clear();
    }
}