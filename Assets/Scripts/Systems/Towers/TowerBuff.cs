using System.Collections.Generic;
using UnityEngine;

public class TowerBuff : MonoBehaviour
{
    [Header("Damage Buff")]
    [SerializeField][Min(0)] private int damagePercent;
    [SerializeField][Min(0)] private int speedPercent;
    [SerializeField][Min(0)] private int chanceBonus;
    [SerializeField][Min(0)] private int criticalBonus;
    [SerializeField] private Effect buffEffect;

    private readonly List<Buff> buffs = new List<Buff>();

    private sealed class Buff
    {
        public int damagePercent;
        public int speedPercent;
        public int chanceBonus;
        public int criticalBonus;
        public float timer;

        public Buff(
            int _damagePercent = 0,
            int _speedPercent = 0,
            int _chanceBonus = 0,
            int _criticalBonus = 0,
            float _duration = 0f)
        {
            damagePercent = _damagePercent;
            speedPercent = _speedPercent;
            chanceBonus = _chanceBonus;
            criticalBonus = _criticalBonus;
            timer = _duration;
        }

        public static Buff Damage(int _percent, float _duration)
            => new Buff(_percent, 0, 0, 0, _duration);

        public static Buff Speed(int _percent, float _duration)
            => new Buff(0, _percent, 0, 0, _duration);

        public static Buff CriticalChance(int _bonus, float _duration)
            => new Buff(0, 0, _bonus, 0, _duration);

        public static Buff CriticalDamage(int _bonus, float _duration)
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
                buffs.RemoveAt(i);
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
            sumDamage += buff.damagePercent;
            sumSpeed += buff.speedPercent;
            sumCritChance += buff.chanceBonus;
            sumCritDamage += buff.criticalBonus;
        }

        damagePercent = sumDamage;
        speedPercent = sumSpeed;
        chanceBonus = sumCritChance;
        criticalBonus = sumCritDamage;
    }

    #region 적용
    private void Apply(Buff _buff, Effect _effect)
    {
        if (_buff.timer <= 0f) return;

        bool hasValue =
            _buff.damagePercent != 0 ||
            _buff.speedPercent != 0 ||
            _buff.chanceBonus != 0 ||
            _buff.criticalBonus != 0;

        if (!hasValue) return;

        buffs.Add(_buff);
        UpdateSummary();

        if (_effect == null) return;

        if (buffEffect == null)
            buffEffect = _effect;
        else
        {
            Destroy(buffEffect.gameObject);
            buffEffect = _effect;
        }
    }

    public void ApplyDamageBuff(int _percent, float _duration, Effect _effect)
        => Apply(Buff.Damage(_percent, _duration), _effect);

    public void ApplySpeedBuff(int _percent, float _duration, Effect _effect)
        => Apply(Buff.Speed(_percent, _duration), _effect);

    public void ApplyChanceBuff(int _bonus, float _duration, Effect _effect)
        => Apply(Buff.CriticalChance(_bonus, _duration), _effect);

    public void ApplyCriticalBuff(int _bonus, float _duration, Effect _effect)
        => Apply(Buff.CriticalDamage(_bonus, _duration), _effect);
    #endregion

    #region 계산
    private int CalcStat(int _base, int _buff, bool _isPercent)
    {
        if (buffs.Count == 0 || _buff <= 0) return _base;

        if (_isPercent)
        {
            float ratio = 1f + _buff / 100f;
            return Mathf.RoundToInt(_base * ratio);
        }

        int value = _base + _buff;
        return Mathf.Max(value, 0);
    }

    public int CalcDamage(int _damage) => CalcStat(_damage, damagePercent, true);
    public int CalcSpeed(int _speed) => CalcStat(_speed, speedPercent, true);
    public int CalcChance(int _chance) => CalcStat(_chance, chanceBonus, false);
    public int CalcCritical(int _critical) => CalcStat(_critical, criticalBonus, false);
    #endregion
}
