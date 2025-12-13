using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatUp", menuName = "TowerSkill/Buff/StatUp", order = 301)]
public class StatUp : TowerSkill
{
    private enum BuffType
    {
        [InspectorName("공업")] Damage,
        [InspectorName("속업")] Speed,
    }

    [SerializeField] private BuffType buffType = BuffType.Damage;

    [Header("Skill")]
    [SerializeField][Min(0)] private int bonus;
    [SerializeField][Min(0f)] private float duration;
    [SerializeField][Min(0f)] private float cooldown;

    private float timer;
    private Coroutine cooldownRoutine;

    public override void SetValues(Tower _tower)
    {
        bonus = _tower.GetValueInt(ValueType.Bonus);
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
        List<Tower> targets = EntityManager.Instance?.GetAttackTowers();
        if (targets.Count == 0) return;

        for (int i = 0; i < targets.Count; i++)
        {
            Tower target = targets[i];
            Effect e = EntityManager.Instance?.MakeEffect(_tower, target, _duration: duration);

            switch (buffType)
            {
                case BuffType.Damage:
                    target.ApplyDamageBuff(bonus, duration, e, 1);
                    target.ApplyCriticalBuff(bonus, duration);
                    break;
                case BuffType.Speed:
                    target.ApplySpeedBuff(bonus, duration, e, 2);
                    target.ApplyChanceBuff(bonus, duration);
                    break;
            }
        }

        timer = duration + cooldown;

        if (cooldownRoutine != null)
            _tower.StopCoroutine(cooldownRoutine);

        cooldownRoutine = _tower.StartCoroutine(CooldownCoroutine(_tower, duration, cooldown));
    }

    private IEnumerator CooldownCoroutine(Tower _tower, float _duration, float _cooldown)
    {
        float wait = 0f;
        while (wait < _duration)
        {
            wait += Time.deltaTime;
            yield return null;
        }

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
