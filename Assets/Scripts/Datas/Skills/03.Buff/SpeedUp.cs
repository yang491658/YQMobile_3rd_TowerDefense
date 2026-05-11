using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpeedUp", menuName = "Skill/Buff/SpeedUp", order = 302)]
public class SpeedUp : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int factor;

    private readonly List<Tower> targets = new();
    private readonly HashSet<Tower> targetSet = new();
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
        targetSet.Clear();
        currents.Clear();
    }

    public override void OnUpdate(Tower _tower, Monster _target, float _deltaTime)
    {
        List<Tower> current = EntityManager.Instance?.GetTowersInRange(_tower.transform.position, _square: true);

        currents.Clear();
        for (int i = 0; i < current.Count; i++)
        {
            Tower target = current[i];
            if (target.Role == TowerRole.Buff) continue;

            currents.Add(target);
        }

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            Tower target = targets[i];
            if (currents.Contains(target)) continue;

            TowerBuff buff = target.Buff;
            buff.RemoveStat(_tower, TowerBuff.SubType.Speed);
            buff.RemoveStat(_tower, TowerBuff.SubType.Chance);

            targetSet.Remove(target);
            targets.RemoveAt(i);
        }

        foreach (Tower target in currents)
        {
            TowerBuff buff = target.Buff;

            buff.ApplyStat(_tower, TowerBuff.SubType.Speed, factor, 0f, TowerBuff.ApplyType.Refresh);
            buff.ApplyStat(_tower, TowerBuff.SubType.Chance, factor, 0f, TowerBuff.ApplyType.Refresh);

            if (targetSet.Add(target))
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
            TowerBuff buff = target.Buff;

            buff.RemoveStat(_tower, TowerBuff.SubType.Speed);
            buff.RemoveStat(_tower, TowerBuff.SubType.Chance);
        }

        targets.Clear();
        targetSet.Clear();
        currents.Clear();
    }
}
