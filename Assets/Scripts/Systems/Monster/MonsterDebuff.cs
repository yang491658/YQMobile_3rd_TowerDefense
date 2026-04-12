using UnityEngine;

[RequireComponent(typeof(Monster))]
public class MonsterDebuff : MonoBehaviour
{
    [System.Serializable]
    private struct Debuff
    {
        private int reserve;
        [SerializeField] private int value;
        [SerializeField] private float duration;
        [SerializeField] private float timer;
        private ViewEffect effect;
        private bool boss;
        [SerializeField] private float immune;

        public int Value => value;
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
            return _value * _duration > value * timer;
        }

        public bool Apply(int _value, float _duration, ViewEffect _effect)
        {
            reserve = Mathf.Max(reserve - 1, 0);

            if (!CanApply(_value, _duration))
            {
                if (_effect != null) _effect.Despawn();
                return false;
            }

            value = _value;
            if (!boss || timer <= 0f)
            {
                duration = _duration;
                timer = _duration;
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
    [SerializeField] private Debuff tickDamage;
    private float tickTimer;
    [SerializeField] private Debuff damageAmp;
    [SerializeField] private Debuff speedControl;

    private void Awake()
    {
        monster = GetComponent<Monster>();
        baseSpeed = monster.GetSpeed();
        bool boss = monster is Boss;

        tickDamage.SetBoss(boss);
        damageAmp.SetBoss(boss);
        speedControl.SetBoss(boss);
    }

    private void Update()
    {
        if (monster.IsDead)
            return;

        float dt = Time.deltaTime;
        UpdateSpeed(dt);
        UpdateDamage(dt);
        UpdateTick(dt);
    }

    public void Clear()
    {
        tickDamage.Reset();
        tickTimer = 0f;

        damageAmp.Reset();

        speedControl.Reset();
        monster.SetSpeed(baseSpeed);
    }

    #region 지속 데미지
    public void ActiveTick() => tickDamage.Active();

    public void ApplyTick(int _damage, float _duration, ViewEffect _effect)
    {
        if (tickDamage.Apply(_damage, _duration, _effect))
            tickTimer = 1f;
    }

    private void UpdateTick(float _deltaTime)
    {
        if (!tickDamage.Update(_deltaTime))
            return;

        tickTimer -= _deltaTime;
        while (tickTimer <= 0f)
        {
            monster.TakeDamage(tickDamage.Value, _direct: true);
            tickTimer += 1f;
        }
    }
    #endregion

    #region 데미지 증폭
    public void ActiveDamage() => damageAmp.Active();

    public void ApplyDamage(int _factor, float _duration, ViewEffect _effect)
        => damageAmp.Apply(_factor, _duration, _effect);

    private void UpdateDamage(float _deltaTime)
        => damageAmp.Update(_deltaTime);

    public int CalcDamage(int _damage)
    {
        if (!damageAmp.IsActive) return _damage;

        return _damage * (100 + damageAmp.Value) / 100;
    }
    #endregion

    #region 이동속도 제어
    public void ActiveSpeed() => speedControl.Active();

    public void ApplySpeed(int _factor, float _duration, ViewEffect _effect)
    {
        if (!speedControl.IsActive)
            baseSpeed = monster.GetSpeed();
        else
        {
            if (speedControl.Value == 100 && _factor != 100)
            {
                if (_effect != null) _effect.Despawn();
                return;
            }
        }

        speedControl.Apply(_factor, _duration, _effect);
    }

    private void UpdateSpeed(float _deltaTime)
    {
        if (!speedControl.Update(_deltaTime))
        {
            monster.SetSpeed(baseSpeed);
            return;
        }

        monster.SetSpeed(baseSpeed * (1f - speedControl.Value / 100f));
    }
    #endregion

    #region GET
    public bool HasTickDamage() => tickDamage.IsActive;
    public bool HasDamageAmp() => damageAmp.IsActive;
    public bool HasSpeedControl() => speedControl.IsActive;
    public bool HasDebuff()
        => tickDamage.IsActive || damageAmp.IsActive || speedControl.IsActive;
    #endregion
}
