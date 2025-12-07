using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Monster : Entity
{
    private static int sorting = 0;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 3f;
    private Transform[] paths;
    private int pathIndex;

    [Header("Battle")]
    [SerializeField] private int health = 5;
    private Canvas canvas;
    private TextMeshProUGUI healthText;
    [Space]
    [SerializeField] private float damageDuration = 1.5f;
    [SerializeField] private float damageSpeed = 3.5f;
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

    protected override void Awake()
    {
        base.Awake();

        canvas = GetComponentInChildren<Canvas>();
        healthText = GetComponentInChildren<TextMeshProUGUI>();

        sr.sortingOrder = sorting;
        canvas.sortingOrder = sorting--;
    }

    protected override void Start()
    {
        base.Start();

        SetMonster(1 + GameManager.Instance.GetScore() / 30);
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead) return;

        UpdateMove();
        UpdateDot(Time.deltaTime);
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
            Move(Vector3.right * moveSpeed);
            return;
        }

        Vector3 delta = paths[pathIndex].position - transform.position;

        float arrive = Mathf.Max(moveSpeed * Time.deltaTime, 0.1f);
        if (delta.sqrMagnitude < arrive * arrive)
        {
            if (++pathIndex >= paths.Length)
            {
                Move(Vector3.right * moveSpeed);
                return;
            }

            delta = paths[pathIndex].position - transform.position;
        }

        Move(delta.normalized * moveSpeed);
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

        while (time < damageDuration)
        {
            time += Time.deltaTime;
            float t = time / damageDuration;

            _text.transform.position = start + Vector3.up * damageSpeed * time;

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
    public void ApplyDot(float _damage, float _duration, float _interval)
    {
        dotDamage = _damage;
        dotDuration = _duration;
        dotInterval = _interval;

        dotTimer = 0f;
        dotTickTimer = 0f;
        hasDot = true;
    }

    private void UpdateDot(float _deltaTime)
    {
        if (!hasDot) return;

        dotTimer += _deltaTime;
        dotTickTimer += _deltaTime;

        while (dotTickTimer >= dotInterval)
        {
            dotTickTimer -= dotInterval;

            int value = Mathf.CeilToInt(dotDamage * dotInterval);
            TakeDamage(value);
            if (IsDead)
            {
                hasDot = false;
                return;
            }
        }

        if (dotTimer >= dotDuration)
            hasDot = false;
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
        SetHealth(health * _set);
        dropGold *= _set;
    }

    public void SetHealth(int _health)
    {
        health = _health;
        healthText.text = health.ToString();
    }
    #endregion

    #region GET
    public int GetHealth() => health;
    public bool HasDebuff() => hasDot;
    #endregion
}
