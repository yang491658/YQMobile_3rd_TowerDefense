using UnityEngine;

public class Tower : Entity
{
    [Header("Default")]
    [SerializeField] private TowerData data;
    [SerializeField] private Transform outLine;

    [Header("Rank")]
    [SerializeField] private Transform symbol;
    [SerializeField] private int rank;
    private int maxRank = 7;
    private bool isMax = false;

    [Header("Battle")]
    [SerializeField] private Monster target;
    [SerializeField] private int attackDamage;
    [SerializeField] private float attackSpeed;
    private float attackTimer;

    protected override void Awake()
    {
        base.Awake();

        if (outLine == null) outLine = transform.Find("OutLine");
        if (symbol == null) symbol = transform.Find("Symbol");
    }

    protected override void Update()
    {
        base.Update();

        Attack();
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
            Vector3.zero ,
            new Vector3(    -offset ,   -offset ) ,
            new Vector3(         0f ,   -offset ) ,
            new Vector3(    +offset ,   -offset ) ,
            new Vector3(    -offset ,        0f ) ,
            new Vector3(         0f ,        0f ) ,
            new Vector3(    +offset ,        0f ) ,
            new Vector3(    -offset ,   +offset ) ,
            new Vector3(         0f ,   +offset ) ,
            new Vector3(    +offset ,   +offset ) ,
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

        if (target == null)
        {
            Monster nearest = EntityManager.Instance?.GetMonster(transform.position);
            if (nearest == null) return;

            target = nearest;
        }
        Shoot();

        attackTimer = attackSpeed;
    }

    public virtual void Shoot()
    {
        GameObject bulletBase = EntityManager.Instance?.GetBulletBase();
        Bullet bullet = Instantiate(bulletBase, transform.position, Quaternion.identity, transform)
            .GetComponent<Bullet>();

        bullet.SetBullet(this);
    }
    #endregion

    #region SET
    public void IsDragging(bool _on)
    {
        sr.sortingOrder = !_on ? 0 : 1000;
        outLine.GetComponent<SpriteRenderer>().sortingOrder = !_on ? 1 : 1001;
        symbol.GetComponent<SpriteRenderer>().sortingOrder = !_on ? 2 : 1002;
    }

    public void SetRank(int _rank)
    {
        rank = Mathf.Clamp(_rank, 1, maxRank);
        attackDamage = data.Damage * rank;
        attackSpeed = 3f / rank;
        UpdateRank();
    }

    public virtual void SetData(TowerData _data)
    {
        data = _data;

        gameObject.name = data.Name;
        if (data.Image != null) sr.sprite = data.Image;

        outLine.GetComponent<SpriteRenderer>().color = data.Color;
        symbol.GetComponent<SpriteRenderer>().color = data.Color;

        SetRank(1);
    }
    #endregion

    #region GET
    public int GetID() => data.ID;
    public Color GetColor() => data.Color;

    public int GetRank() => rank;
    public bool IsMax() => isMax;

    public Monster GetTarget() => target;
    public int GetDamage() => attackDamage;
    #endregion
}
