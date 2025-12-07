using System.Collections;
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

    #region 전투
    public void TakeDamage(float _damage) => TakeDamage((int)_damage);
    public void TakeDamage(int _damage)
    {
        SetHealth(health - _damage);
        CreateDamage(_damage);
        if (health <= 0) Die();
    }

    private void CreateDamage(int _damage)
    {
        TextMeshProUGUI t = Instantiate(healthText, canvas.transform);

        t.gameObject.name = "Damage";
        t.transform.localPosition = healthText.transform.localPosition;
        t.text = _damage.ToString();

        StartCoroutine(DamageCoroutine(t));
    }

    private IEnumerator DamageCoroutine(TextMeshProUGUI _text)
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

    #region SET
    public void SetMonster(int _set)
    {
        SetHealth(health * _set);

        pathIndex = 0;
        dropGold *= _set;
    }

    public void SetHealth(int _health)
    {
        health = _health;
        healthText.text = health.ToString();
    }

    public void SetPath(Transform[] _path)
    {
        paths = _path;
        pathIndex = 0;
    }
    #endregion

    #region GET
    public int GetHealth() => health;
    #endregion
}
