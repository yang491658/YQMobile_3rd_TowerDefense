using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatUp", menuName = "TowerSkill/Buff/StatUp", order = 301)]
public class StatUp : TowerSkill
{
    private enum BuffType
    {
        [InspectorName("공격력")] Damage,
        [InspectorName("공격속도")] Speed,
        [InspectorName("치명타 확률")] Chance,
        [InspectorName("치명타 피해")] Critical,
    }

    [SerializeField] private BuffType buffType = BuffType.Damage;

    [Header("Skill")]
    [SerializeField][Min(0)] private int bonus;
    [SerializeField][Min(0)] private int count;
    [SerializeField][Min(0f)] private float duration;
    [SerializeField][Min(0f)] private float cooldown;

    private float timer;
    private Coroutine cooldownRoutine;

    public override void SetValues(Tower _tower)
    {
        bonus = _tower.GetValueInt(ValueType.Percent);
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

        if (targets.Count == 0) return;

        for (int i = 0; i < targets.Count; i++)
        {
            Tower target = targets[i];
            Effect e = EntityManager.Instance?.MakeEffect(_tower, target, _scale: 0.5f, _duration: duration);

            switch (buffType)
            {
                case BuffType.Damage:
                    target.ApplyDamageBuff(bonus, duration, e);
                    break;
                case BuffType.Speed:
                    target.ApplySpeedBuff(bonus, duration, e);
                    break;
                case BuffType.Chance:
                    target.ApplyChanceBuff(bonus, duration, e);
                    break;
                case BuffType.Critical:
                    target.ApplyCriticalBuff(bonus, duration, e);
                    break;
            }
        }

        timer = cooldown;

        if (cooldownRoutine != null)
            _tower.StopCoroutine(cooldownRoutine);

        cooldownRoutine = _tower.StartCoroutine(CooldownCoroutine(_tower, cooldown));
    }

    private IEnumerator CooldownCoroutine(Tower _tower, float _cooldown)
    {
        SpriteRenderer sr = _tower.GetSR();
        sr.color = Color.gray;

        float time = 0f;
        while (time < _cooldown)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / _cooldown);

            sr.color = Color.Lerp(Color.gray, Color.white, t);
            yield return null;
        }

        sr.color = Color.white;
        cooldownRoutine = null;
    }
}
