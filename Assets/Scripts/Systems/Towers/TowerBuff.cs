using System.Collections.Generic;
using UnityEngine;

public class TowerBuff : MonoBehaviour
{
    [Header("Buff")]
    [SerializeField][Min(0)] private int damageBonus;
    [SerializeField][Min(0)] private int speedBonus;
    [SerializeField][Min(0)] private int chanceBonus;
    [SerializeField][Min(0)] private int criticalBonus;

    private readonly List<Buff> buffs = new List<Buff>();
    private readonly GameObject[] buffEffects = new GameObject[4];
    [SerializeField][Min(0f)] private float effectOffset = 0.35f;

    private sealed class Buff
    {
        public int damageBonus;
        public int speedBonus;
        public int chanceBonus;
        public int criticalBonus;
        public float timer;
        public int order;

        public Buff(
            int _damageBonus = 0,
            int _speedBonus = 0,
            int _chanceBonus = 0,
            int _criticalBonus = 0,
            float _duration = 0f)
        {
            damageBonus = _damageBonus;
            speedBonus = _speedBonus;
            chanceBonus = _chanceBonus;
            criticalBonus = _criticalBonus;
            timer = _duration;
            order = 0;
        }

        public static Buff Damage(int _bonus, float _duration)
            => new Buff(_bonus, 0, 0, 0, _duration);

        public static Buff Speed(int _bonus, float _duration)
            => new Buff(0, _bonus, 0, 0, _duration);

        public static Buff Chance(int _bonus, float _duration)
            => new Buff(0, 0, _bonus, 0, _duration);

        public static Buff Critical(int _bonus, float _duration)
            => new Buff(0, 0, 0, _bonus, _duration);
    }

    private void Update()
    {
        if (buffs.Count == 0) return;

        float dt = Time.deltaTime;

        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            Buff buff = buffs[i];
            buff.timer -= dt;

            if (buff.timer <= 0f)
            {
                int order = buff.order;
                buffs.RemoveAt(i);

                if (order > 0)
                {
                    bool alive = false;
                    for (int j = 0; j < buffs.Count; j++)
                        if (buffs[j].order == order) { alive = true; break; }

                    if (!alive) RemoveEffect(order);
                }
            }
        }

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        int sumDamage = 0;
        int sumSpeed = 0;
        int sumCritChance = 0;
        int sumCritDamage = 0;

        for (int i = 0; i < buffs.Count; i++)
        {
            Buff buff = buffs[i];
            sumDamage += buff.damageBonus;
            sumSpeed += buff.speedBonus;
            sumCritChance += buff.chanceBonus;
            sumCritDamage += buff.criticalBonus;
        }

        damageBonus = sumDamage;
        speedBonus = sumSpeed;
        chanceBonus = sumCritChance;
        criticalBonus = sumCritDamage;
    }

    #region 적용
    private void Apply(Buff _buff, Effect _effect, int _order)
    {
        if (_buff.timer <= 0f) return;

        bool hasValue =
            _buff.damageBonus != 0 ||
            _buff.speedBonus != 0 ||
            _buff.chanceBonus != 0 ||
            _buff.criticalBonus != 0;

        if (!hasValue) return;

        _buff.order = _order;

        buffs.Add(_buff);
        UpdateSummary();

        ApplyEffect(_effect, _order);
    }

    public void ApplyDamageBuff(int _bonus, float _duration, Effect _effect = null, int _order = 1)
        => Apply(Buff.Damage(_bonus, _duration), _effect, _order);

    public void ApplySpeedBuff(int _bonus, float _duration, Effect _effect = null, int _order = 1)
        => Apply(Buff.Speed(_bonus, _duration), _effect, _order);

    public void ApplyChanceBuff(int _bonus, float _duration, Effect _effect = null, int _order = 1)
        => Apply(Buff.Chance(_bonus, _duration), _effect, _order);

    public void ApplyCriticalBuff(int _bonus, float _duration, Effect _effect = null, int _order = 1)
        => Apply(Buff.Critical(_bonus, _duration), _effect, _order);
    #endregion

    #region 이펙트
    private void ApplyEffect(Effect _effect, int _order)
    {
        if (_effect == null) return;

        if (_order <= 0)
        {
            Destroy(_effect.gameObject);
            return;
        }

        int idx = _order - 1;
        if (idx < 0 || idx >= buffEffects.Length)
        {
            Destroy(_effect.gameObject);
            return;
        }

        GameObject old = buffEffects[idx];
        if (old != null)
            Destroy(old);

        buffEffects[idx] = _effect.gameObject;

        UpdateEffect();
    }

    private void UpdateEffect()
    {
        for (int i = 0; i < buffEffects.Length; i++)
        {
            GameObject go = buffEffects[i];
            if (go == null) continue;

            Vector3 pos = Vector3.zero;

            switch (i)
            {
                case 0: pos = new Vector3(effectOffset, effectOffset, 0f); break;
                case 1: pos = new Vector3(effectOffset, -effectOffset, 0f); break;
                case 2: pos = new Vector3(-effectOffset, -effectOffset, 0f); break;
                case 3: pos = new Vector3(-effectOffset, effectOffset, 0f); break;
            }

            Transform t = go.transform;
            t.localPosition = pos;
            t.localScale = Vector3.one * effectOffset;
        }
    }

    private void RemoveEffect(int _order)
    {
        if (_order <= 0) return;

        int idx = _order - 1;
        if (idx < 0 || idx >= buffEffects.Length) return;

        GameObject go = buffEffects[idx];
        if (go != null)
            Destroy(go);

        buffEffects[idx] = null;
    }
    #endregion

    #region 계산
    private int CalcStat(int _base, int _bonus, bool _isPercent)
    {
        if (buffs.Count == 0 || _bonus <= 0) return _base;

        if (_isPercent)
        {
            float ratio = 1f + _bonus / 100f;
            return Mathf.RoundToInt(_base * ratio);
        }

        int value = _base + _bonus;
        return Mathf.Max(value, 0);
    }

    public int CalcDamage(int _damage) => CalcStat(_damage, damageBonus, true);
    public int CalcSpeed(int _speed) => CalcStat(_speed, speedBonus, true);
    public int CalcChance(int _chance) => CalcStat(_chance, chanceBonus, false);
    public int CalcCritical(int _critical) => CalcStat(_critical, criticalBonus, false);
    #endregion
}
