using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Monster))]
public class MonsterDebuff : MonoBehaviour
{
    [System.Serializable]
    private sealed class Debuff
    {
        [Min(0)] public int value;
        [Min(0f)] public float duration;
        [Min(0f)] public float timer;
        [Min(0f)] public float immune;

        [HideInInspector] public int reserve;
        [HideInInspector] public bool boss;
        [HideInInspector] public ViewEffect effect;

        public bool IsActive => reserve > 0 || timer > 0f;

        public void SetBoss(bool _boss) => boss = _boss;

        public void Active()
        {
            if (immune > 0f) return;
            reserve++;
        }

        public bool CanApply(int _value, float _duration)
        {
            if (boss)
            {
                if (immune > 0f) return false;
                if (timer > 0f) return _value > value;
                return true;
            }

            if (timer <= 0f) return true;
            if (_value <= 0) return _duration > timer;
            return _value * _duration > value * timer;
        }

        public bool CanApply(float _duration) => CanApply(value, _duration);

        public bool Apply(int _value, float _duration, ViewEffect _effect, bool _force = false)
        {
            reserve = Mathf.Max(reserve - 1, 0);

            float d = Mathf.Max(_duration, 1f);
            if (!_force && !CanApply(_value, d))
            {
                if (_effect != null) _effect.Despawn();
                return false;
            }

            value = _value;
            if (!boss || timer <= 0f)
            {
                duration = d;
                timer = d;
            }
            if (effect != null) effect.Despawn();
            effect = _effect;

            return true;
        }

        public bool Update(float _deltaTime)
        {
            if (immune > 0f)
                immune = Mathf.Max(immune - _deltaTime, 0f);

            if (timer <= 0f || duration <= 0f) return false;

            timer -= _deltaTime;
            if (timer <= 0f)
            {
                float time = duration;
                Reset();
                if (boss) immune = time;
                return false;
            }
            return true;
        }

        public void Reset()
        {
            reserve = 0;
            value = 0;
            duration = 0f;
            timer = 0f;
            if (effect != null) effect.Despawn();
            effect = null;
            immune = 0f;
        }
    }

    [Header("Origin")]
    [SerializeField] private Monster monster;
    private float baseSpeed;

    [Header("Debuff")]
    [SerializeField] private Debuff tickDamage = new();
    private float tickTimer;
    [SerializeField] private Debuff damageAmp = new();
    [SerializeField] private Debuff moveControl = new();

    [Header("Effect")]
    [SerializeField] private List<ViewEffect> effects = new();
    private int index;
    [SerializeField] private float interval = 0.35f;
    private float timer;

    private void Awake()
    {
        monster = GetComponent<Monster>();
        baseSpeed = monster.Speed;
        bool boss = monster is Boss;

        tickDamage.SetBoss(boss);
        damageAmp.SetBoss(boss);
        moveControl.SetBoss(boss);
    }

    private void Update()
    {
        if (monster.IsDead)
            return;

        float dt = Time.deltaTime;
        UpdateMove(dt);
        UpdateAmplified(dt);
        UpdateTick(dt);

        UpdateEffect(dt);
    }

    public void Clear()
    {
        tickDamage.Reset();
        damageAmp.Reset();
        moveControl.Reset();

        tickTimer = 0f;
        monster.SetSpeed(baseSpeed);
        monster.SetForward(true);

        effects.Clear();
        index = 0;
        timer = 0f;
    }

    #region 지속 데미지
    public void ActiveTick() => tickDamage.Active();

    public void ApplyTick(int _damage, float _duration, ViewEffect _effect)
    {
        if (tickDamage.Apply(_damage, _duration, _effect))
        {
            tickTimer = 0.5f;
            AddEffect(tickDamage.effect);
        }
    }

    private void UpdateTick(float _deltaTime)
    {
        if (!tickDamage.Update(_deltaTime))
            return;

        tickTimer -= _deltaTime;
        while (tickTimer <= 0f)
        {
            monster.TakeDamage(tickDamage.value, DamageType.DoT, true);
            tickTimer += 1f;
        }
    }
    #endregion

    #region 데미지 증폭
    public void ActiveAmplified() => damageAmp.Active();

    public void ApplyAmplified(int _factor, float _duration, ViewEffect _effect)
    {
        if (damageAmp.Apply(_factor, _duration, _effect))
            AddEffect(damageAmp.effect);
    }

    private void UpdateAmplified(float _deltaTime)
        => damageAmp.Update(_deltaTime);

    public int CalcAmplified(int _damage)
        => damageAmp.IsActive ? _damage * (100 + damageAmp.value) / 100 : _damage;
    #endregion

    #region 이동 제어
    public void ActiveMove() => moveControl.Active();

    public bool CanApplyMove(int _factor, float _duration)
    {
        if (_factor <= 0)
            return moveControl.value <= 0 ? moveControl.CanApply(_duration) : true;
        else if (_factor >= 100)
            return moveControl.value > 0 ? moveControl.CanApply(100, _duration) : false;

        return moveControl.value > 0 && moveControl.value < 100 && moveControl.CanApply(_factor, _duration);
    }

    public void ApplyMove(int _factor, float _duration, ViewEffect _effect)
    {
        if (!CanApplyMove(_factor, _duration))
        {
            if (_effect != null) _effect.Despawn();
            return;
        }

        if (moveControl.Apply(_factor, _duration, _effect, true))
            AddEffect(moveControl.effect);
    }

    private void UpdateMove(float _deltaTime)
    {
        if (!moveControl.Update(_deltaTime))
        {
            monster.SetSpeed(baseSpeed);
            monster.SetForward(true);
            return;
        }

        if (moveControl.value <= 0)
        {
            monster.SetSpeed(baseSpeed);
            monster.SetForward(false);
        }
        else if (moveControl.value >= 100)
        {
            monster.Stop();
        }
        else
        {
            monster.SetSpeed(baseSpeed * (100 - moveControl.value) / 100f);
            monster.SetForward(true);
        }
    }
    #endregion

    #region 이펙트 관리
    private void AddEffect(ViewEffect _effect)
    {
        CleanEffect();

        effects.Remove(_effect);
        effects.Insert(0, _effect);

        index = 0;
        timer = interval;

        ShowEffect();
    }

    private void UpdateEffect(float _deltaTime)
    {
        CleanEffect();

        if (effects.Count <= 0)
        {
            index = 0;
            timer = 0f;
            return;
        }

        timer -= _deltaTime;
        if (timer > 0f) return;

        index = (index + 1) % effects.Count;
        timer = interval;

        ShowEffect();
    }

    private void ShowEffect()
    {
        if (effects.Count <= 0) return;

        if (index >= effects.Count)
            index = 0;

        for (int i = 0; i < effects.Count; i++)
            effects[i].SetVisible(i == index);
    }

    private void CleanEffect()
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            if (!effects[i].IsDespawn) continue;

            effects.RemoveAt(i);

            if (i <= index)
                index--;
        }

        if (index < 0 || index >= effects.Count)
            index = 0;
    }
    #endregion

    #region 프로퍼티
    public bool HasTickDamage => tickDamage.IsActive;
    public bool HasDamageAmp => damageAmp.IsActive;
    public bool HasMoveControl => moveControl.IsActive;
    #endregion
}
