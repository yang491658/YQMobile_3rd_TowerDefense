using System.Collections.Generic;
using UnityEngine;

public class Tower : Entity
{
    [Header("Data & Base")]
    [SerializeField] private TowerData data;
    [SerializeField] private Transform outline;
    private SpriteRenderer outlineSR;
    [SerializeField] private Transform symbol;
    private SpriteRenderer symbolSR;

    [Header("Control")]
    public bool IsDragging { private set; get; } = false;

    [Header("Rank")]
    [SerializeField][Min(0)] private int rank;
    public const int MaxRank = 7;

    public bool IsMax { private set; get; } = false;

    [Header("Battle")]
    [SerializeField] private Monster attackTarget;
    private int targetIndex;
    [Space]
    [SerializeField][Min(0)] private int attackDamage;
    [SerializeField][Min(0)] private int attackSpeed;
    private float attackTimer;
    [SerializeField][Min(0)] private int criticalChance;
    [SerializeField][Min(0)] private int criticalDamage;
    [SerializeField] private List<Bullet> bullets = new();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (outline == null) outline = transform.Find("Outline");
        if (symbol == null) symbol = transform.Find("Symbol");
    }
#endif

    protected override void Awake()
    {
        base.Awake();

        outlineSR = outline.GetComponent<SpriteRenderer>();
        symbolSR = symbol.GetComponent<SpriteRenderer>();
    }

    protected override void Update()
    {
        base.Update();

        float dt = Time.deltaTime;
        if (attackSpeed > 0) Attack(dt);
    }

    #region 심볼
    private void UpdateSymbol()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child == symbol || child == outline) continue;

            if (child.name.StartsWith(symbol.name))
                Destroy(child.gameObject);
        }

        if (rank >= MaxRank)
        {
            symbol.localPosition = Vector3.zero;
            if (!IsMax)
            {
                symbol.localScale = Vector3.one * 0.65f;
                symbolSR.sprite = data.Image;
                IsMax = true;
            }
            return;
        }

        symbol.localScale = Vector3.one * 0.18f;
        symbolSR.sprite = DataManager.Instance?.GetRoleSymbol(data.Role);
        IsMax = false;

        Vector2[] positions = SymbolPos(rank);
        symbol.localPosition = positions[0];
        for (int i = 1; i < positions.Length; i++)
        {
            Transform clone = Instantiate(symbol, transform);
            clone.localPosition = positions[i];
        }
    }

    private Vector2[] SymbolPos(int _rank)
    {
        float offset = symbol.localScale.x * 1.25f;

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

    #region 조작
    public void DragOn(bool _on)
    {
        IsDragging = _on;

        int baseOrder = _on ? 1000 : 0;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer r = renderers[i];

            if (r == sr)
                r.sortingOrder = baseOrder;
            else if (r == outlineSR)
                r.sortingOrder = baseOrder + 1;
            else
                r.sortingOrder = baseOrder + 2;
        }
    }

    public Tower Merge(Tower _target)
    {
        if (!EntityManager.Instance.CanMerge(this, _target)) return null;

        return EntityManager.Instance?.MergeTower(this, _target);
    }

    public void RankUp(int _amount = 1)
    {
        if (IsMax) return;

        SetRank(rank + _amount);
    }

    public void Sell()
    {
        EntityManager.Instance?.SellTower(this);
    }
    #endregion

    #region 전투
    private void FindTarget()
    {
        switch (data.Target)
        {
            case AttackTarget.None:
                attackTarget = null; break;
            case AttackTarget.Random:
                attackTarget = EntityManager.Instance?.GetMonsterRandom(); break;
            case AttackTarget.First:
                attackTarget = EntityManager.Instance?.GetMonsterFirst(); break;
            case AttackTarget.Last:
                attackTarget = EntityManager.Instance?.GetMonsterLast(); break;
            case AttackTarget.Near:
                attackTarget = EntityManager.Instance?.GetMonsterNearest(transform.position, 0); break;
            case AttackTarget.Far:
                attackTarget = EntityManager.Instance?.GetMonsterFarthest(transform.position, 0); break;
            case AttackTarget.Strong:
                attackTarget = EntityManager.Instance?.GetMonsterHighHealth(); break;
            case AttackTarget.Weak:
                attackTarget = EntityManager.Instance?.GetMonsterLowHealth(); break;
        }

        targetIndex = attackTarget != null ? attackTarget.Index : 0;
    }

    private void Attack(float _deltaTime)
    {
        attackTimer -= _deltaTime;
        if (attackTimer > 0f) return;

        if (data.Role == TowerRole.Debuff
            || attackTarget == null
            || attackTarget.IsExclude()
            || attackTarget.IsInvalid(targetIndex))
        {
            FindTarget();
            if (attackTarget == null || attackTarget.IsExclude()) return;
        }

        Shoot(attackTarget);

        attackTimer = 60f / attackSpeed;
    }

    public void Shoot(Monster _target)
        => EntityManager.Instance?.MakeBullet(this, _target);

    private void Hit(Monster _target, int _damage)
    {
        if (_target == null) return;

        int damage = _damage;
        int chance = criticalChance;
        int overflow = Mathf.Max(chance - 100, 0);
        chance = Mathf.Min(chance, 100);

        bool critical = false;
        if (Random.value < chance / 100f)
        {
            critical = true;
            damage = damage * criticalDamage / 100;
        }

        bool isHit = _target.TakeDamage(this, damage, critical);
        if (isHit && critical && Random.value < overflow / 1000f)
            GameManager.Instance?.LifeUp();
    }
    #endregion

    #region 불릿
    public void AddBullet(Bullet _bullet) => bullets.Add(_bullet);
    public void RemoveBullet(Bullet _bullet) => bullets.Remove(_bullet);
    public void HitBullet(Monster _target, int _damage) => Hit(_target, _damage);
    #endregion

    #region SET
    public void SetData(TowerData _data)
    {
        data = _data;

        gameObject.name = data.Name;
        outlineSR.color = DataManager.Instance.GetGradeColor(data.Grade);
        symbolSR.color = data.Color;

        SetStat();
    }

    public void SetRank(int _rank)
    {
        rank = Mathf.Clamp(_rank, 1, MaxRank);

        SetStat();
        UpdateSymbol();
    }

    private void SetStat()
    {
        TowerStat.Stat4 stat = DataManager.Instance.GetBaseStat(data.Role, data.Grade);

        if (data.Target == AttackTarget.None)
            attackTarget = null;

        int damage = stat.attackDamage * rank;
        int speed = stat.attackSpeed * rank;
        int chance = stat.criticalChance * rank;
        int critical = stat.criticalDamage;

        attackDamage = damage;
        attackSpeed = speed;

        if (chance <= 0)
        {
            criticalChance = 0;
            criticalDamage = 100;
        }
        else
        {
            criticalChance = chance;
            criticalDamage = critical;
        }
    }
    #endregion

    #region GET
    public TowerData GetData() => data;
    public int GetID() => data.ID;
    public Color GetColor() => data.Color;
    public Sprite GetImage() => data.Image;
    public TowerRole GetRole() => data.Role;
    public TowerGrade GetGrade() => data.Grade;

    public Sprite GetSymbol() => symbolSR.sprite;
    public int GetRank() => rank;

    public Monster GetTarget() => attackTarget;
    public int GetDamage() => attackDamage;
    public int GetSpeed() => attackSpeed;
    public int GetChance() => criticalChance;
    public int GetCritical() => criticalDamage;
    #endregion
}
