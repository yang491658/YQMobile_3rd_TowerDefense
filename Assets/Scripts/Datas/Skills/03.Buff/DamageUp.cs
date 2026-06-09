using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageUp", menuName = "Skill/Buff/DamageUp", order = 301)]
public class DamageUp : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int factor;

    [Header("Const")]
    private const int range = 1;

    [Header("Others")]
    private readonly List<Tower> targets = new();
    private readonly HashSet<Tower> currents = new();

#if UNITY_EDITOR
    public override ValueType[] GetValues()
        => new[] { ValueType.Factor };
#endif

    public override void SetValues(Tower _tower)
    {
        factor = _tower.GetValueInt(this, ValueType.Factor);
    }

    public override void OnGenerate(Tower _tower)
    {
        targets.Clear();
        currents.Clear();
    }

    public override void OnUpdate(Tower _tower, Monster _target, float _deltaTime)
    {
        List<Tower> current = EntityManager.Instance?.GetTowersInRange(_tower.transform.position, range, _square: true);

        currents.Clear();
        for (int i = 0; i < current.Count; i++)
        {
            Tower target = current[i];
            if (target.Damage <= 0) continue;

            currents.Add(target);
        }

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            Tower target = targets[i];
            if (currents.Contains(target)) continue;

            TowerBuff buff = target.Buff;
            buff.RemoveStat(_tower, TowerBuff.SubType.Damage);

            targets.RemoveAt(i);
        }

        foreach (Tower target in currents)
        {
            TowerBuff buff = target.Buff;

            buff.ApplyStat(_tower, TowerBuff.SubType.Damage, factor, 0f, TowerBuff.ApplyType.Refresh);

            if (!targets.Contains(target))
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

    public override void OnDespawn(Tower _tower)
    {
        ClearBuff(_tower);
    }

    private void ClearBuff(Tower _tower)
    {
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            Tower target = targets[i];
            TowerBuff buff = target.Buff;

            buff.RemoveStat(_tower, TowerBuff.SubType.Damage);
        }

        targets.Clear();
        currents.Clear();
    }
}
