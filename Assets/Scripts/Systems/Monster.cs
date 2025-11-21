using System.Collections;
using TMPro;
using UnityEngine;

public class Monster : Entity
{
    private static int sorting = 0;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 1f;
    private Transform[] paths;
    private int pathIndex;

    [Header("Battle")]
    [SerializeField] private int health = 5;
    private Canvas canvas;
    private TextMeshProUGUI healthText;
    [Space]
    [SerializeField] private float damageDuration = 1f;
    [SerializeField] private float damageSpeed = 3f;
    [Space]
    [SerializeField] private int dropGold = 1;
    private bool isDead;

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

        SetMonster(1 + GameManager.Instance.GetScore() / 100);
    }

    protected override void Update()
    {
        base.Update();

        if (isDead) return;

        UpdateMove();
    }

    private void OnBecameInvisible()
    {
        if (isDead) return;

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
    public void TakeDamage(int _damage) => TakeDamage(_damage, healthText.color);
    public void TakeDamage(int _damage, Color _color)
    {
        SetHealth(health - _damage);
        CreateDamage(_damage, _color);
        if (health <= 0) Die();
    }

    private void CreateDamage(int _damage, Color _color)
    {
        TextMeshProUGUI t = Instantiate(healthText, canvas.transform);

        t.gameObject.name = "Damage";
        t.transform.localPosition = healthText.transform.localPosition;
        t.text = _damage.ToString();
        t.color = _color;

        StartCoroutine(DamageCoroutine(t, _color));
    }

    private IEnumerator DamageCoroutine(TextMeshProUGUI _text, Color _color)
    {
        float time = 0f;
        Vector3 start = _text.transform.position;

        while (time < damageDuration)
        {
            time += Time.deltaTime;
            float t = time / damageDuration;

            _text.transform.position = start + Vector3.up * damageSpeed * time;

            Color c = _color;
            c.a = Mathf.Lerp(1f, 0f, t);
            _text.color = c;

            yield return null;
        }

        Destroy(_text.gameObject);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

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
    public bool IsDead() => isDead;
    #endregion
}
