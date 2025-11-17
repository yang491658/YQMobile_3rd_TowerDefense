using UnityEngine;

public class Tower : Entity
{
    [SerializeField] private Transform outLine;
    [SerializeField] private Transform symbol;

    [SerializeField] private int rank = 1;

    [SerializeField] protected TowerData data;

#if UNITY_EDITOR
    private void OnValidate()
    {
        outLine = transform.Find("OutLine");
        symbol = transform.Find("Symbol");
    }
#endif

    public virtual void RankUp(int _amount = 1) => rank += _amount;

    public virtual void Attack(Monster _monster) { }

    #region SET
    public virtual void SetRank(int _rank) => rank = _rank;
    public virtual void SetData(TowerData _data)
    {
        data = _data;

        gameObject.name = data.Name;
        if (data.Image != null) sr.sprite = data.Image;

        outLine.GetComponent<SpriteRenderer>().color = data.Color;
        symbol.GetComponent<SpriteRenderer>().color = data.Color;
    }
    #endregion
}
