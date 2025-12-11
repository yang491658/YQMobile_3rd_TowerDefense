using System.Collections.Generic;
using UnityEngine;

public class TowerBuff : MonoBehaviour
{
    [Header("Damage Buff")]
    [SerializeField][Min(0)] private int damagePercent;
    [SerializeField][Min(0f)] private float damageDuration;
    [SerializeField] private Effect buffEffect;

    private readonly List<DamageBuff> buffs = new List<DamageBuff>();

    private sealed class DamageBuff
    {
        public int percent;
        public float timer;

        public DamageBuff(int _percent, float _duration)
        {
            percent = _percent;
            timer = _duration;
        }
    }

    private void Update()
    {
        if (buffs.Count == 0) return;

        float dt = Time.deltaTime;

        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            DamageBuff buff = buffs[i];
            buff.timer -= dt;

            if (buff.timer <= 0f)
                buffs.RemoveAt(i);
        }

        UpdateSummary();
    }

    public void ApplyDamageBuff(int _percent, float _duration, Effect _effect)
    {
        if (_percent <= 0 || _duration <= 0f) return;

        buffs.Add(new DamageBuff(_percent, _duration));
        UpdateSummary();

        if (buffEffect == null)
            buffEffect = _effect;
        else
        {
            Destroy(buffEffect.gameObject);
            buffEffect = _effect;
        }
    }

    public int GetBuffDamage(int _damage)
    {
        if (buffs.Count == 0 || damagePercent <= 0) return _damage;

        float ratio = 1f + damagePercent / 100f;
        return Mathf.RoundToInt(_damage * ratio);
    }

    private void UpdateSummary()
    {
        int sumPercent = 0;
        float maxTime = 0f;

        for (int i = 0; i < buffs.Count; i++)
        {
            DamageBuff buff = buffs[i];
            sumPercent += buff.percent;
            if (buff.timer > maxTime)
                maxTime = buff.timer;
        }

        damagePercent = sumPercent;
        damageDuration = maxTime;
    }
}
