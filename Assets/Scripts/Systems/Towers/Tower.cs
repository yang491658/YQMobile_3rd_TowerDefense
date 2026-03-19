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

        symbol.localScale = Vector3.one * 0.15f;
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

    #region SET
    public void SetData(TowerData _data)
    {
        data = _data;

        gameObject.name = data.Name;
        outlineSR.color = DataManager.Instance.GetGradeColor(data.Grade);
        symbolSR.color = data.Color;
    }

    public void SetSymbolColor(Color _color) => symbolSR.color = _color;

    public void SetRank(int _rank)
    {
        rank = Mathf.Clamp(_rank, 1, MaxRank);

        UpdateSymbol();
    }
    #endregion

    #region GET
    public TowerData GetData() => data;
    public int GetID() => data.ID;
    public Color GetColor() => data.Color;
    public Sprite GetImage() => data.Image;
    public TowerRole GetRole() => data.Role;
    public TowerGrade GetGrade() => data.Grade;

    public int GetRank() => rank;
    #endregion
}
