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
    }

    #region 도트
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

    #region 슬로우
    public void ApplySlow(int _slow, float _duration, Effect _effect)
    {
        slowPercent = Mathf.Max(_slow, slowPercent);
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

    #region GET
    public bool HasDebuff() => hasDOT || hasSlow;
    #endregion
}
