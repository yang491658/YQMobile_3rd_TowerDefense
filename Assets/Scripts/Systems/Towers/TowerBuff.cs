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
    private readonly List<GameObject> buffEffects = new List<GameObject>();
    [SerializeField][Min(0f)] private float effectOffset = 0.35f;

    private sealed class Buff
    {
        public int damageBonus;
        public int speedBonus;
        public int chanceBonus;
        public int criticalBonus;
        public float timer;

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
    private void Apply(Buff _buff, Effect _effect, int _index)
    {
        if (_buff.timer <= 0f) return;

        bool hasValue =
            _buff.damageBonus != 0 ||
            _buff.speedBonus != 0 ||
            _buff.chanceBonus != 0 ||
            _buff.criticalBonus != 0;

        if (!hasValue) return;

        buffs.Add(_buff);
        UpdateSummary();

        ApplyEffect(_effect, _index);
    }

    private void ApplyEffect(Effect _effect, int _index)
    {
        if (_effect == null) return;

        if (_index < 1 || _index > 4)
        {
            Destroy(_effect.gameObject);
            return;
        }

        int index = _index - 1;

        while (buffEffects.Count <= index)
            buffEffects.Add(null);

        GameObject e = buffEffects[index];
        Destroy(e);
        buffEffects[index] = _effect.gameObject;

        Vector3 pos = Vector3.zero;

        if (index == 0)
            pos = new Vector3(-effectOffset, effectOffset, 0f);
        else if (index == 1)
            pos = new Vector3(effectOffset, effectOffset, 0f);
        else if (index == 2)
            pos = new Vector3(effectOffset, -effectOffset, 0f);
        else if (index == 3)
            pos = new Vector3(-effectOffset, -effectOffset, 0f);

        Transform t = _effect.transform;
        t.localPosition = pos;
        t.localScale = Vector3.one * effectOffset;
    }

    public void ApplyDamageBuff(int _bonus, float _duration, Effect _effect = null, int _index = 1)
        => Apply(Buff.Damage(_bonus, _duration), _effect, _index);

    public void ApplySpeedBuff(int _bonus, float _duration, Effect _effect = null, int _index = 1)
        => Apply(Buff.Speed(_bonus, _duration), _effect, _index);

    public void ApplyChanceBuff(int _bonus, float _duration, Effect _effect = null, int _index = 1)
        => Apply(Buff.Chance(_bonus, _duration), _effect, _index);

    public void ApplyCriticalBuff(int _bonus, float _duration, Effect _effect = null, int _index = 1)
        => Apply(Buff.Critical(_bonus, _duration), _effect, _index);
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
