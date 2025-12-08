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
    [SerializeField] private bool isDragging;
    [SerializeField] private Vector3 slot;

    [Header("Rank")]
    [SerializeField] private Transform symbol;
    [SerializeField] private int rank;
    private int maxRank = 7;
    private bool isMax = false;

    [Header("Battle")]
    [SerializeField] private int attackDamage;
    [SerializeField] private float attackSpeed;
    [SerializeField] private Monster attackTarget;
    private float attackTimer;
    [SerializeField] private List<Bullet> bullets = new List<Bullet>();

    [Header("Skill")]
    [SerializeField] private List<TowerSkill> skills = new List<TowerSkill>();
    [SerializeField] private List<float> values = new List<float>();

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
                symbol.localScale = Vector3.one * 0.65f;
                symbolSR.sprite = data.Symbol;
                isMax = true;
            }
            return;
        }

        isMax = false;

        Vector2[] positions = GetArray(rank);

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

    private void HitBullet(Monster _target, Vector3 _pos)
    {
        for (int i = 0; i < skills.Count; i++)
        {
            TowerSkill skill = skills[i];

            if (_target != null)
                skill.OnHit(this, _target);
            else if (skill is SplashDamage _splash)
                _splash.OnHit(this, _pos);
        }

        _target?.TakeDamage(attackDamage);
    }
    public void HitBullet(Monster _target) => HitBullet(_target, _target.transform.position);
    public void HitBullet(Vector3 _pos) => HitBullet(null, _pos);
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
    public void SetData(TowerData _data)
    {
        data = _data;

        gameObject.name = data.Name;

        outLineSR.color = data.Color;
        symbolSR.color = data.Color;

        skills.Clear();
        for (int i = 0; i < data.Skills.Count; i++)
        {
            TowerSkill skill = Instantiate(data.Skills[i]);
            skills.Add(skill);
        }

        SetRank(1);
    }

    public void SetRank(int _rank)
    {
        rank = Mathf.Clamp(_rank, 1, maxRank);
        UpdateRank();

        attackDamage = data.AttackDamage * rank;
        attackSpeed = data.AttackSpeed / rank;

        values.Clear();
        for (int i = 0; i < data.Values.Count; i++)
            values.Add(SetValue(i));

        for (int i = 0; i < skills.Count; i++)
            skills[i].OnChange(this);
    }

    public float SetValue(int _index)
    {
        Vector3 value = data.Values[_index];

        if (value.y != 0f)
        {
            if (value.z == 0f)
                return value.x + value.y * (rank - 1);
            else if (value.z == 1f)
                return value.x * rank;
        }

        return value.x;
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
            case AttackTarget.Weak:
                attackTarget = EntityManager.Instance?.GetMonsterLowHealth(noDebuff);
                break;
            case AttackTarget.Strong:
                attackTarget = EntityManager.Instance?.GetMonsterHighHealth(noDebuff);
                break;
        }
    }
    #endregion

    #region GET
    public TowerData GetData() => data;
    public int GetID() => data.ID;
    public Color GetColor() => data.Color;

    public bool IsDragging() => isDragging;
    public Vector3 GetSlot() => slot;

    public int GetRank() => rank;
    public bool IsMax() => isMax;
    public int GetDamage() => attackDamage;
    public Monster GetTarget() => attackTarget;
    public float GetValue(int _index) => values[_index];
    #endregion
}
