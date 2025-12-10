using UnityEngine;

public class MonsterDebuff : MonoBehaviour
{
    [Header("Origin")]
    [SerializeField] private Monster monster;

    [Header("Debuff / DOT")]
    [SerializeField] private int dotDamage;
    [SerializeField] private float dotDuration;
    private float dotTimer;
    private float dotTickTimer;
    private bool hasDot;
    [SerializeField] private Effect dotEffect;

    [Header("Debuff / Slow")]
    [SerializeField] private int slowPercent;
    [SerializeField] private float slowDuration;
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

        UpdateDot(Time.deltaTime);
        UpdateSlow(Time.deltaTime);
    }

    #region 도트
    public void ApplyDot(int _damage, float _duration, Effect _effect)
    {
        dotDamage = Mathf.Max(dotDamage, _damage);
        dotDuration = Mathf.Max(dotDuration, _duration);

        dotTimer = dotDuration;
        dotTickTimer = 1f;
        hasDot = true;

        if (dotEffect == null)
            dotEffect = _effect;
        else
        {
            Destroy(dotEffect.gameObject);
            dotEffect = _effect;
        }
    }

    private void UpdateDot(float _deltaTime)
    {
        if (!hasDot) return;

        dotTimer -= _deltaTime;
        dotTickTimer -= _deltaTime;

        while (dotTickTimer < 0f)
        {
            dotTickTimer += 1f;

            monster.TakeDamage(dotDamage);
            if (monster.IsDead)
            {
                hasDot = false;
                return;
            }
        }

        if (dotTimer < 0f)
        {
            hasDot = false;
            dotEffect = null;
        }
    }
    #endregion

    #region 슬로우
    public void ApplySlow(int _slow, float _duration, Effect _effect)
    {
        slowPercent = Mathf.Max(slowPercent, _slow);
        slowDuration = Mathf.Max(slowDuration, _duration);

        slowTimer = slowDuration;
        hasSlow = true;
        monster.SetSpeed(baseMoveSpeed * Mathf.Max(1 - slowPercent / 100f, 0f));

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
        }
    }
    #endregion

    #region GET
    public bool HasDebuff() => hasDot || hasSlow;
    #endregion
}
