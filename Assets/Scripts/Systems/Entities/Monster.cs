using TMPro;
using UnityEngine;

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
    [SerializeField][Min(0f)] private float moveSpeed = 3f;
    [SerializeField][Min(0f)] private float moveCooldown = 1f;
    private float moveTimer = 0f;
    [SerializeField] private Vector3 moveDirection;

    public bool IsMoving { private set; get; } = false;

    [Header("Battle")]
    [SerializeField][Min(0)] private int health;
    [SerializeField][Min(0)] protected int maxHealth;
    [SerializeField][Min(0)] private int reserve = 0;
    [Space]
    [SerializeField][Min(0)] private int gold;

    public bool IsDead { private set; get; } = false;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        if (canvas == null) canvas = canvases[0];
        if (healthText == null)
            healthText = canvas.GetComponentInChildren<TextMeshProUGUI>();
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
        moveTimer -= _deltaTime;

        if (!IsMoving)
        {
            if (moveTimer > 0f) return;

            Vector3Int currentCell = Vector3Int.RoundToInt(current);

            if (!EntityManager.Instance.GetNextCell(currentCell, out Vector3Int nextCell))
            { OnGoal(); return; }

            target = nextCell;
            IsMoving = true;
        }

        Vector3 targetPos = EntityManager.Instance.GetCellPos(Vector3Int.RoundToInt(target));
        Vector3 delta = targetPos - transform.position;

        float arrive = Mathf.Max(moveSpeed * _deltaTime, 0.1f);
        if (delta.sqrMagnitude <= arrive * arrive)
        {
            transform.position = targetPos;

            current = Vector3Int.RoundToInt(target);
            target = current;
            moveDirection = Vector3.zero;
            moveTimer = moveCooldown;
            IsMoving = false;

            Stop();
            return;
        }

        moveDirection = delta.normalized;
        Move(moveSpeed, moveDirection);
    }
    #endregion

    #region 전투
    public bool TakeDamage(Tower _tower, int _damage, bool _critical = false, bool _direct = false)
    {
        if (IsDead) return false;

        if (!_direct) ReserveDown(_damage);

        SetHealth(health - _damage);
        CreateDamage(_damage, _critical);

        if (health <= 0) Die();

        return true;
    }

    private void CreateDamage(int _damage, bool _critical = false)
    {
        if (_damage <= 0) return;

        float font = _critical ? 65f : 50f;
        Color color = _critical ? Color.red : Color.black;

        Vector3 from = transform.position;
        Vector3 to = new Vector3(0f, AutoCamera.WorldRect.yMax, 0f);
        Vector3 dir = (to - from).normalized;

        TextEffect text = EntityManager.Instance?.MakeTextEffect(transform.position);
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
        moveTimer = 0f;
        IsMoving = false;
    }
    public float SetSpeed(float _speed) => moveSpeed = Mathf.Max(_speed, 0f);

    public void SetHealth(int _health)
    {
        health = _health;
        healthText.text = _health < int.MaxValue ? health.ToString() : "ㄱ-";
    }
    #endregion

    #region GET
    public float GetSpeed() => moveSpeed;
    public Vector3 GetDirection() => moveDirection;

    public int GetHealth() => health;
    public int GetMaxHealth() => maxHealth;
    public bool IsExclude() => health < reserve || IsDead || IsDespawn;
    public bool IsInvalid(int _index = -1) => IsDead || IsDespawn || (_index >= 0 && Index != _index);
    #endregion

    #region 풀링
    public int Index { private set; get; }

    public override void OnSpawnPool()
    {
        base.OnSpawnPool();

        Index++;

        int order = ++sorting;
        sr.sortingOrder = order;
        canvas.sortingOrder = order;

        reserve = 0;
        IsDead = false;
    }

    public override void ResetPool()
    {
        base.ResetPool();

        current = default;
        target = default;
        moveSpeed = 3f;
        moveTimer = 0f;
        moveDirection = Vector3.zero;
        IsMoving = false;
        Stop();
    }
    #endregion
}
