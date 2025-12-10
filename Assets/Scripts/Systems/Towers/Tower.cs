using System.Collections.Generic;
using UnityEngine;

public class Tower : Entity
{
    [Header("Data")]
    [SerializeField] private TowerData data;
    [SerializeField] private Transform outLine;
    private SpriteRenderer outLineSR;
    private SpriteRenderer symbolSR;

    [Header("Control")]
    private bool isDragging;
    private Vector3 slot;

    [Header("Rank")]
    [SerializeField] private Transform symbol;
    [SerializeField] private int rank;
    private int maxRank = 7;
    private bool isMax = false;

    [Header("Battle")]
    [SerializeField] private Monster attackTarget;
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackSpeed;
    private float attackTimer;
    [SerializeField] private float criticalChance;
    [SerializeField] private float criticalDamage;
    [SerializeField] private List<Bullet> bullets = new List<Bullet>();

    [Header("Skill")]
    [SerializeField] private List<Skill> skills = new List<Skill>();
    [SerializeField] private List<float> values = new List<float>();
    private Dictionary<ValueType, float> valueDic = new Dictionary<ValueType, float>();

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

        float deltaTime = Time.deltaTime;
        for (int i = 0; i < skills.Count; i++)
            skills[i].OnUpdate(this, deltaTime);

        Attack();
    }

    #region 심볼
    private void UpdateSymbol()
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
                symbol.localScale = Vector3.one * 0.65f;
                symbolSR.sprite = data.Symbol;
                isMax = true;
            }
            return;
        }

        isMax = false;

        Vector2[] positions = SymbolPos(rank);

        for (int i = 0; i < positions.Length; i++)
        {
            if (i == 0)
                symbol.localPosition = positions[i];
            else
            {
                Transform clone = Instantiate(symbol, transform);
                clone.localPosition = positions[i];
            }
        }
    }

    private Vector2[] SymbolPos(int _rank)
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
    public void Attack()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        SetTarget();
        if (attackTarget == null || attackTarget.IsDead) return;

        for (int i = 0; i < skills.Count; i++)
            skills[i].OnAttack(this);

        Shoot();
        attackTimer = attackSpeed;
    }

    public void Shoot()
    {
        GameObject bulletBase = EntityManager.Instance?.GetBulletBase();
        Bullet bullet = Instantiate(bulletBase, transform.position, Quaternion.identity, transform)
            .GetComponent<Bullet>();

        bullet.SetBullet(this);
    }
    #endregion

    #region 불릿
    public void AddBullet(Bullet _bullet) => bullets.Add(_bullet);
    public void RemoveBullet(Bullet _bullet) => bullets.Remove(_bullet);
    public void SortBullets(Vector3 _delta)
    {
        for (int i = 0; i < bullets.Count; i++)
        {
            Bullet b = bullets[i];
            b.transform.position -= _delta;
        }
    }

    public void HitBullet(Monster _target) => HitBullet(_target, _target.transform.position);
    public void HitBullet(Vector3 _pos) => HitBullet(null, _pos);
    private void HitBullet(Monster _target, Vector3 _pos)
    {
        for (int i = 0; i < skills.Count; i++)
        {
            Skill skill = skills[i];

            if (_target != null)
                skill.OnHit(this, _target);
            else if (skill is Splash _splash)
                _splash.OnHit(this, _pos);
        }

        if (_target == null) return;

        float damage = attackDamage;
        bool critical = false;

        if (Random.value < criticalChance / 100f)
        {
            critical = true;
            damage *= criticalDamage / 100f;
        }

        for (int i = 0; i < skills.Count; i++)
            skills[i].OnTakeDamage(this, _target, ref damage, ref critical);

        _target.TakeDamage(damage, critical);
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

    public Tower Merge(Tower _target, int _id = 0)
    {
        if (!EntityManager.Instance.CanMerge(this, _target)) return null;

        for (int i = 0; i < skills.Count; i++)
            skills[i].OnMerge(this, _target);

        return EntityManager.Instance?.MergeTower(this, _target, _id);
    }

    public void RankUp(int _amount = 1)
    {
        SetRank(rank + _amount);

        for (int i = 0; i < skills.Count; i++)
            skills[i].OnRankUp(this, _amount);
    }

    public void Sell()
    {
        for (int i = 0; i < skills.Count; i++)
            skills[i].OnSell(this);

        GameManager.Instance?.GoldUp(GameManager.Instance.GetNeedGold() * rank);
        EntityManager.Instance?.IsSell(Vector3.one);
        EntityManager.Instance?.DespawnTower(this);
    }
    #endregion

    #region SET
    public void SetData(TowerData _data)
    {
        data = _data;

        gameObject.name = data.Name;

        outLineSR.color = data.Color;
        symbolSR.color = data.Color;

        skills.Clear();
        for (int i = 0; i < data.Skills.Count; i++)
        {
            Skill skill = Instantiate(data.Skills[i]);
            skills.Add(skill);
        }

        SetRank(1);

        for (int i = 0; i < skills.Count; i++)
            skills[i].OnGenerate(this);
    }

    public void SetRank(int _rank)
    {
        rank = Mathf.Clamp(_rank, 1, maxRank);
        UpdateSymbol();

        attackDamage = data.AttackDamage * rank;
        attackSpeed = data.AttackSpeed / rank;
        criticalChance = data.CriticalChance * rank;
        criticalDamage = data.CriticalDamage;

        values.Clear();
        valueDic.Clear();
        for (int i = 0; i < data.Values.Count; i++)
        {
            SkillValue value = data.Values[i];

            float v = SetValue(value);
            values.Add(v);
            valueDic[value.valueType] = v;
        }

        for (int i = 0; i < skills.Count; i++)
            skills[i].SetValues(this);
    }

    private void SetTarget()
    {
        bool noDebuff = data.Role == TowerRole.Debuff;

        switch (data.AttackTarget)
        {
            case AttackTarget.None:
                attackTarget = null;
                return;
            case AttackTarget.Random:
                attackTarget = EntityManager.Instance?.GetMonsterRandom(noDebuff);
                break;
            case AttackTarget.First:
                attackTarget = EntityManager.Instance?.GetMonsterFirst(noDebuff);
                break;
            case AttackTarget.Last:
                attackTarget = EntityManager.Instance?.GetMonsterLast(noDebuff);
                break;
            case AttackTarget.Near:
                attackTarget = EntityManager.Instance?.GetMonsterNearest(transform.position, 0, noDebuff);
                break;
            case AttackTarget.Far:
                attackTarget = EntityManager.Instance?.GetMonsterFarthest(transform.position, 0, noDebuff);
                break;
            case AttackTarget.Strong:
                attackTarget = EntityManager.Instance?.GetMonsterHighHealth(noDebuff);
                break;
            case AttackTarget.Weak:
                attackTarget = EntityManager.Instance?.GetMonsterLowHealth(noDebuff);
                break;
        }
    }

    private float SetValue(SkillValue _value)
    {
        float value = _value.baseValue;
        float bonus = _value.rankBonus;

        switch (_value.rankType)
        {
            case RankType.Add: return value + bonus * rank;
            case RankType.Subtract: return value - bonus * rank;
            case RankType.Multiply: return value * rank;
            case RankType.Divide: return value / Mathf.Max(rank, 1);
            default: return value;
        }
    }
    #endregion

    #region GET
    public TowerData GetData() => data;
    public int GetID() => data.ID;
    public Color GetColor() => data.Color;
    public Sprite GetSymbol() => data.Symbol;

    public bool IsDragging() => isDragging;
    public Vector3 GetSlot() => slot;

    public int GetRank() => rank;
    public bool IsMax() => isMax;
    public Monster GetTarget() => attackTarget;
    public float GetValue(ValueType _type) => valueDic[_type];
    #endregion
}
