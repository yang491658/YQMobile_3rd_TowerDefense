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
    [SerializeField] private Debuff tickDamage = new();
    private float tickTimer;
    [SerializeField] private Debuff bonusDamage = new();
    [SerializeField] private Debuff speedControl = new();
    [SerializeField] private Debuff directionControl = new();

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
        bonusDamage.SetBoss(boss);
        speedControl.SetBoss(boss);
        directionControl.SetBoss(boss);
    }

    private void Update()
    {
        if (monster.IsDead)
            return;

        float dt = Time.deltaTime;
        UpdateDirection(dt);
        UpdateSpeed(dt);
        UpdateBonus(dt);
        UpdateTick(dt);

        UpdateEffect(dt);
    }

    public void Clear()
    {
        tickDamage.Reset();
        bonusDamage.Reset();
        speedControl.Reset();
        directionControl.Reset();

        tickTimer = 0f;
        monster.SetSpeed(baseSpeed);

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
            tickTimer = 1f;
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

    #region 추가 데미지
    public void ActiveBonus() => bonusDamage.Active();

    public void ApplyBonus(int _factor, float _duration, ViewEffect _effect)
    {
        if (bonusDamage.Apply(_factor, _duration, _effect))
            AddEffect(bonusDamage.effect);
    }

    private void UpdateBonus(float _deltaTime)
        => bonusDamage.Update(_deltaTime);

    public int CalcBonus(int _damage)
        => bonusDamage.IsActive ? _damage * bonusDamage.value / 100 : 0;
    #endregion

    #region 이동속도 제어
    public void ActiveSpeed() => speedControl.Active();

    public void ApplySpeed(int _factor, float _duration, ViewEffect _effect)
    {
        if (!speedControl.IsActive)
            baseSpeed = monster.Speed;
        else
        {
            if (speedControl.value == 100 && _factor != 100)
            {
                if (_effect != null) _effect.Despawn();
                return;
            }

            if (!(monster is Boss) && speedControl.value != 100 && _factor == 100)
                speedControl.Reset();
        }

        if (speedControl.Apply(_factor, _duration, _effect))
            AddEffect(speedControl.effect);
    }

    private void UpdateSpeed(float _deltaTime)
    {
        if (!speedControl.Update(_deltaTime))
        {
            monster.SetSpeed(baseSpeed);
            return;
        }

        monster.SetSpeed(baseSpeed * (1f - speedControl.value / 100f));
    }
    #endregion

    #region 이동방향 제어
    public void ActiveDirection() => directionControl.Active();

    public void ApplyDirection(int _dir, float _duration, ViewEffect _effect)
    {
        if (_dir == 0)
            _dir = Random.Range(1, 5);

        if (directionControl.Apply(_dir, _duration, _effect))
            AddEffect(directionControl.effect);
    }

    private void UpdateDirection(float _deltaTime)
        => directionControl.Update(_deltaTime);

    public bool CalcDirection(Vector3Int _current, out Vector3Int _next)
    {
        _next = default;

        if (!directionControl.IsActive) return false;

        Vector3Int offset = directionControl.value switch
        {
            1 => Vector3Int.right,
            2 => Vector3Int.down,
            3 => Vector3Int.left,
            4 => Vector3Int.up,
            _ => default,
        };

        if (offset == default) return false;

        Vector3Int next = _current + offset;
        if (!EntityManager.Instance.CanMoveCell(next)) return false;

        _next = next;
        return true;
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
    public bool HasBonusDamage => bonusDamage.IsActive;
    public bool HasSpeedControl => speedControl.IsActive;
    public bool HasDirectionControl => directionControl.IsActive;
    public bool HasDebuff => tickDamage.IsActive || bonusDamage.IsActive || speedControl.IsActive || directionControl.IsActive;
    #endregion
}
