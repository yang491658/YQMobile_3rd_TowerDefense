using UnityEngine;

public class Tower : Entity
{
    [Header("Data")]
    [SerializeField] private TowerData data;
    [SerializeField] private Transform outLine;
    private SpriteRenderer outLineSR;
    private SpriteRenderer symbolSR;
    private Vector3 slot;

    [Header("Rank")]
    [SerializeField] private Transform symbol;
    [SerializeField] private int rank;
    private int maxRank = 7;
    private bool isMax = false;

    [Header("Battle")]
    [SerializeField] private Monster target;
    [Space]
    [SerializeField] private int attackDamage;
    [SerializeField] private float attackSpeed;
    private float attackTimer;

    [Header("Control")]
    private bool isDragging;

    protected override void Awake()
    {
        base.Awake();

        if (outLine == null) outLine = transform.Find("OutLine");
        if (symbol == null) symbol = transform.Find("Symbol");

        outLineSR = outLine.GetComponent<SpriteRenderer>();
        symbolSR = symbol.GetComponent<SpriteRenderer>();

        slot = transform.position;
    }

    protected override void Update()
    {
        base.Update();

        if (!isDragging) Attack();
    }

    #region 랭크
    public void RankUp(int _amount = 1) => SetRank(rank + _amount);

    private void UpdateRank()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != symbol && child != outLine)
                Destroy(child.gameObject);
        }

        if (rank == maxRank)
        {
            symbol.localPosition = Vector3.zero;
            if (!isMax)
            {
                symbol.localScale = Vector3.one * .5f;
                symbolSR.sprite = data.SymbolImage;
                isMax = true;
            }
            return;
        }

        Vector2[] positions = GetArray(rank);

        for (int i = 0; i < positions.Length; i++)
        {
            if (i == 0)
                symbol.localPosition = positions[0];
            else
            {
                Transform clone = Instantiate(symbol, transform);
                clone.localPosition = positions[i];
            }
        }
    }

    private Vector2[] GetArray(int _rank)
    {
        float offset = symbol.localScale.x * 1.2f;

        Vector2[] grid =
        {
            Vector2.zero ,
            new Vector2(    -offset ,   -offset ) ,
            new Vector2(         0f ,   -offset ) ,
            new Vector2(    +offset ,   -offset ) ,
            new Vector2(    -offset ,        0f ) ,
            new Vector2(         0f ,        0f ) ,
            new Vector2(    +offset ,        0f ) ,
            new Vector2(    -offset ,   +offset ) ,
            new Vector2(         0f ,   +offset ) ,
            new Vector2(    +offset ,   +offset ) ,
        };

        switch (_rank)
        {
            case 1: return new[] { grid[5] };
            case 2: return new[] { grid[4], grid[6] };
            case 3: return new[] { grid[1], grid[3], grid[8] };
            case 4: return new[] { grid[1], grid[3], grid[7], grid[9] };
            case 5: return new[] { grid[1], grid[3], grid[5], grid[7], grid[9] };
            case 6: return new[] { grid[1], grid[3], grid[4], grid[6], grid[7], grid[9] };
            default: return grid;
        }
    }
    #endregion

    #region 전투
    public virtual void Attack()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        if (target == null || target.IsDead())
        {
            target = EntityManager.Instance?.GetMonster(transform.position);
            if (target == null || target.IsDead()) return;
        }

        Shoot();
        attackTimer = 1f / attackSpeed;
    }

    public virtual void Shoot()
    {
        GameObject bulletBase = EntityManager.Instance?.GetBulletBase();
        Bullet bullet = Instantiate(bulletBase, transform.position, Quaternion.identity, transform)
            .GetComponent<Bullet>();

        bullet.SetBullet(this);
    }
    #endregion

    #region 조작
    public void DragOn(bool _on)
    {
        isDragging = _on;

        int baseOrder = _on ? 1000 : 0;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer r = renderers[i];

            if (r == sr)
                r.sortingOrder = baseOrder;
            else if (r == outLineSR)
                r.sortingOrder = baseOrder + 1;
            else
                r.sortingOrder = baseOrder + 2;
        }
    }

    public void Sell()
    {
        GameManager.Instance?.GoldUp(GetRank());
        EntityManager.Instance?.IsSell(Vector3.one);
        EntityManager.Instance?.DespawnTower(this);
    }
    #endregion

    #region SET
    public void SetRank(int _rank)
    {
        rank = Mathf.Clamp(_rank, 1, maxRank);
        UpdateRank();

        attackDamage = data.AttackDamage * rank;
        attackSpeed = data.AttackSpeed * rank;
    }

    public virtual void SetData(TowerData _data)
    {
        data = _data;

        gameObject.name = data.Name;
        if (data.BaseImage != null) sr.sprite = data.BaseImage;

        outLineSR.color = data.Color;
        symbolSR.color = data.Color;

        SetRank(1);
    }
    #endregion

    #region GET
    public int GetID() => data.ID;
    public Color GetColor() => data.Color;

    public Vector3 GetSlot() => slot;

    public int GetRank() => rank;
    public bool IsMax() => isMax;

    public Monster GetTarget() => target;
    public int GetDamage() => attackDamage;
    public bool IsDragging() => isDragging;
    #endregion
}
