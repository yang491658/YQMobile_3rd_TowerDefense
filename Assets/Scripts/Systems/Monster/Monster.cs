using TMPro;
using UnityEngine;

[RequireComponent(typeof(MonsterDebuff))]
public class Monster : Pooling
{
    private static int sorting = 0;

    [Header("UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private TextMeshProUGUI healthText;
    [Space]
    [SerializeField][Min(0f)] private float damageSpeed = 150f;
    [SerializeField][Min(0f)] private float damageDuration = 1.5f;

    [Header("Move")]
    [SerializeField] private Vector3 current;
    [SerializeField] private Vector3 target;
    [SerializeField][Min(0f)] private float moveSpeed = 1f;
    [SerializeField] private Vector3 moveDirection;

    [Header("Battle")]
    [SerializeField][Min(0)] private int health;
    [SerializeField][Min(0)] protected int maxHealth;
    [SerializeField][Min(0)] private int reserve = 0;
    [Space]
    [SerializeField][Min(0)] private int gold;
    [Space]
    [SerializeField] private MonsterDebuff debuff;

    public bool IsDead { private set; get; } = false;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>();
        if (healthText == null)
            healthText = canvas?.GetComponentInChildren<TextMeshProUGUI>();
        if (debuff == null)
            debuff = GetComponent<MonsterDebuff>();
    }
#endif

    protected override void Update()
    {
        base.Update();

        if (IsDead) return;

        float dt = Time.deltaTime;
        UpdateMove(dt);
    }

    #region 이동
    private void UpdateMove(float _deltaTime)
    {
        Vector3Int currentCell = Vector3Int.RoundToInt(current);
        bool direction = debuff.CalcDirection(currentCell, out Vector3Int directionCell);

        if (!direction)
        {
            if (!EntityManager.Instance.GetNextCell(currentCell, out Vector3Int nextCell))
            { OnGoal(); return; }

            target = nextCell;
        }
        else
            target = directionCell;

        Vector3 targetPos = EntityManager.Instance.GetCellPos(Vector3Int.RoundToInt(target));
        Vector3 delta = targetPos - transform.position;

        float arrive = Mathf.Max(moveSpeed * _deltaTime, 0.1f);
        if (delta.sqrMagnitude <= arrive * arrive)
        {
            transform.position = targetPos;

            if (!direction)
                current = Vector3Int.RoundToInt(target);

            target = current;
            moveDirection = Vector3.zero;

            Stop();
            return;
        }

        moveDirection = delta.normalized;
        Move(moveSpeed, moveDirection);
    }
    #endregion

    #region 전투
    public bool TakeDamage(int _damage, bool _isCritical = false, bool _direct = false)
    {
        if (IsDead) return false;

        if (!_direct) ReserveDown(_damage);

        int damage = debuff.CalcDamage(_damage);

#if TEST_Manager
        TestManager.Instance?.AddDamage(damage);

        if (TestManager.Instance?.Mode == TestMode.Solo && this is Boss) { }
        else
#endif
            SetHealth(health - damage);
        CreateDamage(damage, _isCritical);

        if (health <= 0) Die();

        return true;
    }

    private void CreateDamage(int _damage, bool _isCritical = false)
    {
        if (_damage <= 0) return;

        float font = _isCritical ? 65f : 50f;
        Color color = _isCritical ? Color.red : Color.black;

        Vector3 from = transform.position + Vector3.up * 0.5f;
        Vector3 to = new Vector3(0f, AutoCamera.WorldRect.yMax, 0f);
        Vector3 dir = (to - from).normalized;

        TextEffect text = EntityManager.Instance?.MakeText(from);
        if (text == null) return;

        text.SetText(_damage.ToString(), font, color);
        text.SetMove(damageSpeed, dir);
        text.SetDuration(damageDuration);
    }

    public void ReserveUp(int _damage) => reserve += _damage;
    public void ReserveDown(int _damage) => reserve = Mathf.Max(reserve - _damage, 0);

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        OnDeath();

        EntityManager.Instance?.DespawnMonster(this);
    }

    protected virtual void OnDeath()
    {
        GameManager.Instance?.ScoreUp();
        GameManager.Instance?.GoldUp(gold);
    }

    protected virtual void OnGoal()
    {
        GameManager.Instance?.LifeDown();
        EntityManager.Instance?.DespawnMonster(this);
    }
    #endregion

    #region SET
    public void SetMonster(int _set)
    {
        maxHealth = Mathf.Max(50 * _set, 50);
        SetHealth(maxHealth);
        gold = Mathf.Max(10 * _set, 10);
    }

    public void SetMove(Vector3Int _current)
    {
        current = _current;
        target = _current;
        moveDirection = Vector3.zero;
    }
    public float SetSpeed(float _speed) => moveSpeed = Mathf.Max(_speed, 0f);

    public void SetHealth(int _health)
    {
        health = Mathf.Max(_health, 0);
        if (healthText != null)
            healthText.text = UIManager.Instance?.FormatNumber(health);
    }
    #endregion

    #region GET
    public float GetSpeed() => moveSpeed;
    public Vector3 GetDirection() => moveDirection;

    public int GetHealth() => health;
    public int GetMaxHealth() => maxHealth;
    public bool IsExclude() => health < reserve || IsDead || IsDespawn;
    public bool IsInvalid(int _index = -1) => IsDead || IsDespawn || (_index >= 0 && Index != _index);

    public MonsterDebuff GetDebuff() => debuff;
    #endregion

    #region 풀링
    public int Index { private set; get; }

    public override void OnSpawnPool()
    {
        base.OnSpawnPool();

        int order = ++sorting;
        sr.sortingOrder = order;
        if (canvas != null)
            canvas.sortingOrder = order;

        reserve = 0;
        IsDead = false;

        Index = order;
    }

    public override void ResetPool()
    {
        base.ResetPool();

        Pooling[] poolings = GetComponentsInChildren<Pooling>(true);
        for (int i = 0; i < poolings.Length; i++)
        {
            Pooling pooling = poolings[i];
            if (pooling == this) continue;
            if (pooling.IsDespawn) continue;

            EntityManager.Instance?.DespawnPool(pooling);
        }

        current = default;
        target = default;
        moveSpeed = 1f;
        moveDirection = Vector3.zero;

        debuff.Clear();

        Stop();
    }
    #endregion
}
