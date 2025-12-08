using System.Collections;
using TMPro;
using UnityEngine;

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
    [SerializeField] private int health = 5;
    private Canvas canvas;
    private TextMeshProUGUI healthText;
    [Space]
    [SerializeField] private float damageDuration = 1f;
    [SerializeField] private float damageSpeed = 3f;
    [Space]
    [SerializeField] private int dropGold = 1;
    public bool IsDead { private set; get; } = false;

    [Header("Debuff / Dot")]
    [SerializeField] private float dotDamage;
    [SerializeField] private float dotDuration;
    [SerializeField] private float dotInterval;
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

    protected override void Awake()
    {
        base.Awake();

        canvas = GetComponentInChildren<Canvas>();
        healthText = GetComponentInChildren<TextMeshProUGUI>();

        sr.sortingOrder = sorting;
        canvas.sortingOrder = sorting--;

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

        GameManager.Instance?.LifeDown(health);
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
    public void TakeDamage(float _damage) => TakeDamage((int)_damage);
    public void TakeDamage(int _damage)
    {
        SetHealth(health - _damage);
        CreateDamageText(_damage);
        if (health <= 0) Die();
    }

    private void CreateDamageText(int _damage)
    {
        TextMeshProUGUI t = Instantiate(healthText, canvas.transform);

        t.gameObject.name = "Damage";
        t.transform.localPosition = healthText.transform.localPosition;
        t.text = _damage.ToString();

        StartCoroutine(DamageTextCoroutine(t));
    }

    private IEnumerator DamageTextCoroutine(TextMeshProUGUI _text)
    {
        float time = 0f;
        Vector3 start = _text.transform.position;
        Vector3 dir = Vector3.up;
        if (Mathf.Abs(moveDir.x) < 0.01f)
        {
            if (moveDir.y > 0f)
                dir = Vector3.right;
            else if (moveDir.y < 0f)
                dir = Vector3.left;
        }

        while (time < damageDuration)
        {
            time += Time.deltaTime;
            float t = time / damageDuration;

            _text.transform.position = start + dir * damageSpeed * time;

            Color c = _text.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            _text.color = c;

            yield return null;
        }

        Destroy(_text.gameObject);
    }

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
    public void ApplyDot(float _damage, float _duration, float _interval, Effect _effect)
    {
        dotDamage = Mathf.Max(dotDamage, _damage);
        dotDuration = Mathf.Max(dotDuration, _duration);
        dotInterval = _interval;

        dotTimer = dotDuration;
        dotTickTimer = dotInterval;
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
            dotTickTimer += dotInterval;

            int value = Mathf.CeilToInt(dotDamage * dotInterval);
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

    #region 디버프_감속
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

    public void SetHealth(int _health)
    {
        health = _health;
        healthText.text = health.ToString();
    }
    #endregion

    #region GET
    public int GetHealth() => health;
    public bool HasDebuff() => hasDot || hasSlow;
    #endregion
}
