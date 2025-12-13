using UnityEngine;

public class MonsterDebuff : MonoBehaviour
{
    [Header("Origin")]
    [SerializeField] private Monster monster;

    [Header("Debuff / DOT")]
    [SerializeField][Min(0)] private int dotDamage;
    [SerializeField][Min(0f)] private float dotDuration;
    private float dotTimer;
    private float dotTickTimer;
    private bool hasDOT;
    [SerializeField] private Effect dotEffect;

    [Header("Debuff / Slow")]
    [SerializeField][Min(0)] private int slowPercent;
    [SerializeField][Min(0f)] private float slowDuration;
    private float slowTimer;
    private float baseMoveSpeed;
    private bool hasSlow;
    [SerializeField] private Effect slowEffect;

    [Header("Debuff / Curse")]
    [SerializeField][Min(0)] private int cursePercent;
    [SerializeField][Min(0f)] private float curseDuration;
    private float curseTimer;
    private bool hasCurse;
    [SerializeField] private Effect curseEffect;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (monster == null)
            monster = GetComponent<Monster>();
    }
#endif

    private void Awake()
    {
        baseMoveSpeed = monster.GetSpeed();
    }

    private void Update()
    {
        if (monster.IsDead) return;

        UpdateDOT(Time.deltaTime);
        UpdateSlow(Time.deltaTime);
        UpdateCurse(Time.deltaTime);
    }

    #region 도트 (지속 데미지)
    public void ApplyDOT(int _damage, float _duration, Effect _effect)
    {
        dotDamage = Mathf.Max(_damage, dotDamage);
        dotDuration = Mathf.Max(_duration, dotDuration);

        dotTimer = dotDuration;
        dotTickTimer = 1f;
        hasDOT = true;

        if (dotEffect == null)
            dotEffect = _effect;
        else
        {
            Destroy(dotEffect.gameObject);
            dotEffect = _effect;
        }
    }

    private void UpdateDOT(float _deltaTime)
    {
        if (!hasDOT) return;

        dotTimer -= _deltaTime;
        dotTickTimer -= _deltaTime;

        while (dotTickTimer < 0f)
        {
            dotTickTimer += 1f;

            monster.TakeDamage(dotDamage, _direct: true);
            if (monster.IsDead)
            {
                hasDOT = false;
                return;
            }
        }

        if (dotTimer < 0f)
        {
            hasDOT = false;
            dotEffect = null;
        }
    }
    #endregion

    #region 슬로우 (이속 감소)
    public void ApplySlow(int _factor, float _duration, Effect _effect)
    {
        slowPercent = Mathf.Max(_factor, slowPercent);
        slowDuration = Mathf.Max(_duration, slowDuration);

        slowTimer = slowDuration;
        hasSlow = true;

        if (slowDuration > 0f)
        {
            float slow = slowPercent * Mathf.Clamp01(slowTimer / slowDuration);
            float ratio = Mathf.Max(1f - slow / 100f, 0f);
            monster.SetSpeed(baseMoveSpeed * ratio);
        }
        else monster.SetSpeed(baseMoveSpeed);

        if (slowEffect == null)
            slowEffect = _effect;
        else
        {
            Destroy(slowEffect.gameObject);
            slowEffect = _effect;
        }
    }

    private void UpdateSlow(float _deltaTime)
    {
        if (!hasSlow) return;

        slowTimer -= _deltaTime;

        if (slowTimer < 0f)
        {
            hasSlow = false;
            slowPercent = 0;
            slowDuration = 0f;
            monster.SetSpeed(baseMoveSpeed);

            slowEffect = null;
            return;
        }

        if (slowDuration > 0f)
        {
            float slow = slowPercent * Mathf.Clamp01(slowTimer / slowDuration);
            float ratio = Mathf.Max(1f - slow / 100f, 0f);
            monster.SetSpeed(baseMoveSpeed * ratio);
        }
        else monster.SetSpeed(baseMoveSpeed);
    }
    #endregion 

    #region 저주 (피해 증폭)
    public void ApplyCurse(int _factor, float _duration, Effect _effect)
    {
        cursePercent = Mathf.Max(_factor, cursePercent);
        curseDuration = Mathf.Max(_duration, curseDuration);

        curseTimer = curseDuration;
        hasCurse = true;

        if (curseEffect == null)
            curseEffect = _effect;
        else
        {
            Destroy(curseEffect.gameObject);
            curseEffect = _effect;
        }
    }

    private void UpdateCurse(float _deltaTime)
    {
        if (!hasCurse) return;

        curseTimer -= _deltaTime;

        if (curseTimer < 0f)
        {
            hasCurse = false;
            cursePercent = 0;
            curseDuration = 0f;

            curseEffect = null;
        }
    }

    public int CalcDamage(int _damage)
    {
        if (!hasCurse) return _damage;

        int mul = 100 + cursePercent;
        return _damage * mul / 100;
    }
    #endregion

    #region GET
    public bool HasDebuff() => hasDOT || hasSlow || hasCurse;
    #endregion
}
