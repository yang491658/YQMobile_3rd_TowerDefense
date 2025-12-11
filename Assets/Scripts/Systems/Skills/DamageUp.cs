using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageUp", menuName = "TowerSkill/Buff/DamageUp", order = 301)]
public class DamageUp : TowerSkill
{
    [Header("Skill")]
    [SerializeField][Min(0)] private int percent;
    [SerializeField][Min(0)] private int count;
    [SerializeField][Min(0f)] private float duration;
    [SerializeField][Min(0f)] private float cooldown;

    private float timer;

    public override void SetValues(Tower _tower)
    {
        percent = _tower.GetValueInt(ValueType.Percent);
        count = _tower.GetValueInt(ValueType.Count);
        duration = _tower.GetValue(ValueType.Duration);
        cooldown = _tower.GetValue(ValueType.Cooldown);
    }

    public override void OnGenerate(Tower _tower)
    {
        timer = 0f;
    }

    public override void OnUpdate(Tower _tower, float _deltaTime)
    {
        timer -= _deltaTime;
        if (timer > 0f) return;

        ApplyBuff(_tower);
    }

    private void ApplyBuff(Tower _tower)
    {
        List<Tower> targets = EntityManager.Instance?.GetAttackTowers(count);

        for (int i = 0; i < targets.Count; i++)
        {
            Tower target = targets[i];

            Effect e = EntityManager.Instance?.MakeEffect(_tower, target, _scale: 0.5f, _duration: duration);
            target.ApplyDamageBuff(percent, duration, e);
        }

        timer = duration + cooldown;
    }
}
