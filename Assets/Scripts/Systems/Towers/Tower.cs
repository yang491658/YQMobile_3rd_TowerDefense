using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TowerBuff))]
public class Tower : Entity
{
    [Header("Data + Base")]
    [SerializeField] private TowerData data;
    [SerializeField] private Transform outline;
    private SpriteRenderer outlineSR;

    [Header("Symbol")]
    [SerializeField] private Transform symbol;
    private SpriteRenderer symbolSR;
    [SerializeField] private float interval = 1.25f;
    [SerializeField] private float baseSize = 0.18f;
    [SerializeField] private float maxSize = 0.65f;

    [Header("Control")]
    [SerializeField] private Vector3Int cell;

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

    [Header("Skill")]
    [SerializeField] private List<TowerSkill> skills = new();
    private readonly Dictionary<TowerSkill, Dictionary<ValueType, float>> valueDic = new();
    [Space]
    [SerializeField] private Image timerUI;
    [SerializeField] private TowerBuff buff;
    [SerializeField] private List<Summon> summons = new();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (outline == null) outline = transform.Find("Outline");
        if (symbol == null) symbol = transform.Find("Symbol");
        if (timerUI == null) timerUI = GetComponentInChildren<Image>();
    }
#endif

    protected override void Awake()
    {
        base.Awake();

        outlineSR = outline.GetComponent<SpriteRenderer>();
        symbolSR = symbol.GetComponent<SpriteRenderer>();
        timerUI.gameObject.SetActive(false);
        buff = GetComponent<TowerBuff>();
    }

    protected override void Update()
    {
        if (EntityManager.Instance.IsMoving) return;

        base.Update();

        float dt = Time.deltaTime;
        for (int i = 0; i < skills.Count; i++)
            skills[i].OnUpdate(this, attackTarget, dt);

        UpdateBuff();
        Attack(dt);
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
                symbol.localScale = Vector3.one * maxSize;
                symbolSR.sprite = data.Icon;
                IsMax = true;
            }
            return;
        }

        symbol.localScale = Vector3.one * baseSize;
        symbolSR.sprite = data.Symbol;
        IsMax = false;

        Vector2[] positions = SymbolPos(rank, symbol.localScale.x);
        symbol.localPosition = positions[0];
        for (int i = 1; i < positions.Length; i++)
        {
            Transform clone = Instantiate(symbol, transform);
            clone.localPosition = positions[i];
        }
    }

    private Vector2[] SymbolPos(int _rank, float _standard)
    {
        float offset = _standard * interval;

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
    public void SetDrag(Image _outline, Image _symbol)
    {
        _outline.sprite = outlineSR.sprite;
        _outline.color = outlineSR.color;

        Transform parent = _symbol.transform.parent;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child == _symbol.transform) continue;
            if (child.name == _symbol.name)
                Destroy(child.gameObject);
        }

        _symbol.sprite = symbolSR.sprite;
        _symbol.color = symbolSR.color;

        if (rank >= MaxRank)
        {
            _symbol.rectTransform.localScale = Vector3.one * maxSize;
            _symbol.rectTransform.anchoredPosition = Vector2.zero;
            return;
        }

        _symbol.rectTransform.localScale = Vector3.one * baseSize;

        Vector2[] positions = SymbolPos(rank, _symbol.rectTransform.localScale.x * 100f);
        _symbol.rectTransform.anchoredPosition = positions[0];

        for (int i = 1; i < positions.Length; i++)
        {
            Image clone = Instantiate(_symbol, parent);
            clone.name = _symbol.name;
            clone.rectTransform.anchoredPosition = positions[i];
        }
    }

    public void Drag(bool _on)
    {
        IsDragging = _on;

        if (!_on)
            transform.position = EntityManager.Instance.GetCellPos(cell);

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

        for (int i = 0; i < skills.Count; i++)
            skills[i].OnMerge(this, _target);

        for (int i = 0; i < _target.skills.Count; i++)
            _target.skills[i].OnMerge(_target, this);

        return EntityManager.Instance?.MergeTower(this, _target);
    }

    public void RankUp(int _amount = 1)
    {
        if (IsMax) return;

        SetRank(rank + _amount);

        for (int i = 0; i < skills.Count; i++)
            skills[i].OnRankUp(this, _amount);
    }

    public void Sell()
    {
        for (int i = 0; i < skills.Count; i++)
            skills[i].OnSell(this);

        EntityManager.Instance?.SellTower(this);
    }
    #endregion

    #region 전투
    private void FindTarget()
    {
        System.Predicate<Monster> filter = null;
        for (int i = 0; i < skills.Count; i++)
        {
            filter = skills[i].GetFilter();
            if (filter != null) break;
        }

        attackTarget = data.Target switch
        {
            AttackTarget.None => null,
            AttackTarget.Random => EntityManager.Instance?.GetMonsterRandom(filter),
            AttackTarget.First => EntityManager.Instance?.GetMonsterFirst(filter),
            AttackTarget.Last => EntityManager.Instance?.GetMonsterLast(filter),
            AttackTarget.Near => EntityManager.Instance?.GetMonsterNearest(transform.position, 0, filter),
            AttackTarget.Far => EntityManager.Instance?.GetMonsterFarthest(transform.position, 0, filter),
            AttackTarget.Strong => EntityManager.Instance?.GetMonsterHighHealth(filter),
            AttackTarget.Weak => EntityManager.Instance?.GetMonsterLowHealth(filter),
            _ => attackTarget
        };

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

        bool instead = data.Role == TowerRole.Summon;
        for (int i = 0; i < skills.Count; i++)
            skills[i].OnAttack(this, attackTarget, ref instead);

        if (!instead)
            Shoot(attackTarget);

        if (!instead || data.Role == TowerRole.Summon)
            attackTimer = 60f / attackSpeed;
    }

    public void Shoot(Monster _target)
        => EntityManager.Instance?.MakeBullet(this, _target);

    public void Hit(Monster _target, int _index, Vector3 _pos, bool _onHit = true)
    {
        bool valid = _target != null && !_target.IsInvalid(_index);
        bool instead = false;

        if (_onHit)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                if (valid) skills[i].OnHit(this, _target, ref instead);
                else skills[i].OnImpact(this, _pos);
            }
        }

        if (!valid || instead) return;

        HitDamage(_target);
    }

    public void HitDamage(Monster _target, int _damage = -1, int _chance = -1, int _critical = -1,
        bool _lifeUp = true, DamageType _type = DamageType.Normal)
    {
        int damage = _damage < 0 ? attackDamage : _damage;
        int chance = _chance < 0 ? criticalChance : _chance;
        int critical = _critical < 0 ? criticalDamage : _critical;

        int overflow = Mathf.Max(chance - 100, 0);
        chance = Mathf.Min(chance, 100);

        DamageType type = _type;
        if (Random.value < chance / 100f)
        {
            type = DamageType.Critical;
            damage = damage * critical / 100;
        }

        bool isHit = _target.TakeDamage(damage, type);
        if (_lifeUp && isHit && type == DamageType.Critical
            && Random.value < overflow / 1000f)
            GameManager.Instance?.LifeUp();
    }
    #endregion

    #region 불릿
    public void AddBullet(Bullet _bullet) => bullets.Add(_bullet);
    public void RemoveBullet(Bullet _bullet) => bullets.Remove(_bullet);
    #endregion

    #region 스킬
    public void AddSummon(Summon _summon) => summons.Add(_summon);
    public void RemoveSummon(Summon _summon) => summons.Remove(_summon);
    public void ClearSummon(TowerSkill _skill = null)
    {
        for (int i = summons.Count - 1; i >= 0; i--)
        {
            Summon summon = summons[i];
            if (summon == null) continue;
            if (_skill != null && summon.ID != _skill.ID) continue;

            summon.Despawn();
        }

        if (_skill == null)
            summons.Clear();
    }
    #endregion

    private void UpdateBuff()
    {
        TowerStat.Stat4 stat = DataManager.Instance.GetBaseStat(data.Role, data.Grade);

        if (data.Target == AttackTarget.None)
            attackTarget = null;

        int damage = stat.attackDamage * rank;
        int speed = stat.attackSpeed * rank;
        int chance = stat.criticalChance * rank;
        int critical = stat.criticalDamage;

        attackDamage = buff.CalcStat(TowerBuff.SubType.Damage, damage);
        attackSpeed = buff.CalcStat(TowerBuff.SubType.Speed, speed);
        chance = buff.CalcStat(TowerBuff.SubType.Chance, chance);
        critical = buff.CalcStat(TowerBuff.SubType.Critical, critical);

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

    #region SET
    private TowerData BasicData()
    {
        TowerData basic = ScriptableObject.CreateInstance<TowerData>();

        basic.ID = 0;
        basic.Name = "Basic";
        basic.Icon = symbolSR.sprite;
        basic.Symbol = symbolSR.sprite;
        basic.Color = Color.black;

        basic.Grade = TowerGrade.Normal;
        basic.Role = TowerRole.Dealer;
        basic.Target = AttackTarget.First;
        basic.Skills = new();

        return basic;
    }

    public void SetTower(TowerData _data, int _rank = 1)
    {
        data = _data != null ? _data : BasicData();

        gameObject.name = data.Name;
        outlineSR.color = DataManager.Instance.GetGradeColor(data.Grade);
        symbolSR.color = data.Color;

        cell = EntityManager.Instance.GetCell(transform.position);

        for (int i = 0; i < skills.Count; i++)
            Destroy(skills[i]);

        skills.Clear();
        for (int i = 0; i < data.Skills.Count; i++)
        {
            SkillConfig config = data.Skills[i];
            if (config.skill == null) continue;

            TowerSkill skill = Instantiate(config.skill);
            skills.Add(skill);
        }
        buff.Clear();

        SetRank(_rank);

        for (int i = 0; i < skills.Count; i++)
            skills[i].OnGenerate(this);
    }

    public void SetRank(int _rank)
    {
        rank = Mathf.Clamp(_rank, 1, MaxRank);

        UpdateBuff();
        UpdateSymbol();

        valueDic.Clear(); int index = 0;
        for (int i = 0; i < data.Skills.Count && index < skills.Count; i++)
        {
            SkillConfig config = data.Skills[i];
            if (config.skill == null) continue;

            if (config.values != null)
            {
                TowerSkill skill = skills[index];

                Dictionary<ValueType, float> dic = new(config.values.Count);
                valueDic.Add(skill, dic);

                for (int j = 0; j < config.values.Count; j++)
                {
                    SkillValue value = config.values[j];
                    dic[value.valueType] = SetValue(value);
                }
            }

            index++;
        }

        for (int i = 0; i < skills.Count; i++)
            skills[i].SetValues(this);
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
            case RankType.Divide: return value / rank;
            default: return value;
        }
    }
    #endregion

    #region GET
    public TowerData GetData() => data;
    public Sprite GetIcon() => data.Icon;
    public int GetID() => data.ID;
    public Sprite GetSymbol() => data.Symbol;
    public Color GetColor() => data.Color;
    public TowerRole GetRole() => data.Role;
    public TowerGrade GetGrade() => data.Grade;

    public int GetRank() => rank;
    public Monster GetTarget() => attackTarget;
    public int GetDamage() => attackDamage;
    public int GetSpeed() => attackSpeed;
    public int GetChance() => criticalChance;
    public int GetCritical() => criticalDamage;

    public float GetValue(TowerSkill _skill, ValueType _type)
    {
        if (!valueDic.TryGetValue(_skill, out var dic)) return 0f;
        return dic.TryGetValue(_type, out var value) ? value : 0f;
    }
    public int GetValueInt(TowerSkill _skill, ValueType _type)
        => Mathf.FloorToInt(GetValue(_skill, _type) + 0.5f);

    public Image GetTimerUI() => timerUI;
    public TowerBuff GetBuff() => buff;
    public int GetSummonCount(TowerSkill _skill = null)
    {
        if (_skill == null)
            return summons.Count;

        int count = 0;
        for (int i = 0; i < summons.Count; i++)
        {
            Summon summon = summons[i];
            if (summon == null) continue;
            if (summon.ID != _skill.ID) continue;

            count++;
        }

        return count;
    }
    #endregion
}
