using UnityEngine;

public class Tower : Entity
{
    [SerializeField] private Transform rank;
    [SerializeField] private int rankCount = 1;

    [SerializeField] protected TowerData data;

#if UNITY_EDITOR
    private void OnValidate()
    {
        rank = transform.Find("Rank");
    }
#endif

    protected override void Start()
    {
        base.Start();

        rank.GetComponent<SpriteRenderer>()
            .color = data.Color;
    }

    public virtual void Attack(Monster _monster) { }

    #region SET
    public virtual void SetRank(int _rank) => rankCount = _rank;
    public virtual void SetData(TowerData _data)
    {
        data = _data;

        gameObject.name = data.Name;
        if (data.Image != null) sr.sprite = data.Image;
    }
    #endregion
}
