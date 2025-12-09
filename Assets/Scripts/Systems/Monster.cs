using System.Collections;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Monster : Entity
{
    private static int sorting = 0;

    [Header("Move")]
    [SerializeField] private int pathIndex;
    private Transform[] paths;
    [SerializeField] private float moveSpeed = 3f;
    private float baseMoveSpeed;
    [SerializeField] private Vector3 moveDir;

    [Header("Battle")]
    [SerializeField] private float health = 5;
    [SerializeField] private Canvas healthCanvas;
    [SerializeField] private TextMeshProUGUI healthText;
    [Space]
    [SerializeField] private float damageDuration = 1f;
    [SerializeField] private float damageSpeed = 3f;
    [SerializeField] private Canvas damageCanvas;
    [Space]
    [SerializeField] private int dropGold = 1;
    public bool IsDead { private set; get; } = false;

    [Header("Debuff / DOT")]
    [SerializeField] private float dotDamage;
    [SerializeField] private float dotDuration;
    private float dotTimer;
    private float dotTickTimer;
    private bool hasDot;
    [SerializeField] private Effect dotEffect;

    [Header("Debuff / Slow")]
    [SerializeField] private float slowAmount;
    [SerializeField] private float slowDuration;
    private float slowTimer;
    private bool hasSlow;
    [SerializeField] private Effect slowEffect;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Canvas[] canvases = GetComponentsInChildren<Canvas>();
        healthCanvas = canvases[0];
        damageCanvas = canvases[1];

        healthText = healthCanvas.GetComponentInChildren<TextMeshProUGUI>();
    }
#endif

    protected override void Awake()
    {
        base.Awake();

        sr.sortingOrder = sorting;
        healthCanvas.sortingOrder = sorting--;

        baseMoveSpeed = moveSpeed;
    }

    protected override void Start()
    {
        base.Start();

        SetMonster(GameManager.Instance.GetScore() / 10);
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead) return;

        UpdateMove();
        UpdateDot(Time.deltaTime);
        UpdateSlow(Time.deltaTime);
    }

    private void OnBecameInvisible()
    {
        if (IsDead) return;

        GameManager.Instance?.LifeDown(DisplayDamage(health));
        EntityManager.Instance?.DespawnMonster(this);
    }

    private void UpdateMove()
    {
        if (pathIndex >= paths.Length)
        {
            moveDir = Vector3.right;
            Move(moveDir * moveSpeed);
            return;
        }

        Vector3 delta = paths[pathIndex].position - transform.position;

        float arrive = Mathf.Max(moveSpeed * Time.deltaTime, 0.1f);
        if (delta.sqrMagnitude < arrive * arrive)
        {
            if (++pathIndex >= paths.Length)
            {
                moveDir = Vector3.right;
                Move(moveDir * moveSpeed);
                return;
            }

            delta = paths[pathIndex].position - transform.position;
        }

        moveDir = delta.normalized;
        Move(moveDir * moveSpeed);
    }

    #region 전투_기본
    public void TakeDamage(float _damage, bool _critical = false, float _multiplier = 1f)
    {
        SetHealth(health - _damage);
        CreateDamage(_damage, _critical, _multiplier);
        if (health <= 0) Die();
    }

    private void CreateDamage(float _damage, bool _critical = false, float _multiplier = 1f)
    {
        TextMeshProUGUI t = Instantiate(healthText, damageCanvas.transform);

        t.gameObject.name = "Damage";
        t.transform.localPosition = healthText.transform.localPosition;
        t.text = DisplayDamage(_damage).ToString();
        t.rectTransform.localScale *= Mathf.Max(_multiplier, 1f);

        StartCoroutine(DamageCoroutine(t, _critical));
    }

    private IEnumerator DamageCoroutine(TextMeshProUGUI _text, bool _critical = false)
    {
        float time = 0f;
        Vector3 from = _text.transform.position;
        Vector3 to = new Vector3(0f, AutoCamera.WorldRect.yMax, 0f);
        Vector3 dir = (to - from).normalized;

        while (time < damageDuration)
        {
            time += Time.deltaTime;
            float t = time / damageDuration;

            _text.transform.position = from + dir * damageSpeed * time;

            Color c = _critical ? Color.red : Color.black;
            c.a = Mathf.Lerp(1f, 0f, t);
            _text.color = c;

            yield return null;
        }

        Destroy(_text.gameObject);
    }

    private int DisplayDamage(float _value)
        => _value < 0.5f ? 1 : Mathf.RoundToInt(_value);

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        sr.enabled = false;
        healthText.enabled = false;

        Effect[] effects = GetComponentsInChildren<Effect>();
        for (int i = 0; i < effects.Length; i++)
            Destroy(effects[i].gameObject);

        GameManager.Instance?.ScoreUp();
        GameManager.Instance?.GoldUp(dropGold);
        EntityManager.Instance?.RemoveMonster(this);
        StartCoroutine(DieCoroutine());
    }

    private IEnumerator DieCoroutine()
    {
        yield return new WaitForSeconds(damageDuration);
        EntityManager.Instance?.DespawnMonster(this);
    }
    #endregion

    #region 디버프_도트
    public void ApplyDot(float _damage, float _duration, Effect _effect)
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

            int value = Mathf.CeilToInt(dotDamage);
            TakeDamage(value);
            if (IsDead)
            {
                hasDot = false;
                return;
            }
        }

        if (dotTimer < 0f)
        {
            hasDot = false;
            Destroy(dotEffect.gameObject);
            dotEffect = null;
        }
    }
    #endregion

    #region 디버프_슬로우
    public void ApplySlow(float _slow, float _duration, Effect _effect)
    {
        slowAmount = Mathf.Max(slowAmount, _slow);
        slowDuration = Mathf.Max(slowDuration, _duration);

        slowTimer = slowDuration;
        hasSlow = true;
        moveSpeed = baseMoveSpeed * Mathf.Max(1f - slowAmount / 100f, 0f);

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
            slowAmount = 0f;
            slowDuration = 0f;
            moveSpeed = baseMoveSpeed;

            Destroy(slowEffect.gameObject);
            slowEffect = null;
        }
    }
    #endregion

    #region SET
    public void SetPath(Transform[] _path)
    {
        paths = _path;
        pathIndex = 0;
    }

    public void SetMonster(int _set)
    {
        SetHealth(Mathf.Max(health * _set, 5));
        dropGold = Mathf.Max(dropGold * _set, 1);
    }

    public void SetHealth(float _health)
    {
        health = _health;
        healthText.text = DisplayDamage(health).ToString();
    }
    #endregion

    #region GET
    public float GetHealth() => health;
    public bool HasDebuff() => hasDot || hasSlow;
    #endregion
}
