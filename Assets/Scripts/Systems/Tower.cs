using UnityEngine;

public class Tower : Entity
{
    [SerializeField] protected TowerData data;

    public virtual void Attack(Monster _monster) { }

    #region SET
    public virtual void SetData(TowerData _data)
    {
        data = _data;
        gameObject.name = data.Name;
    }
    #endregion
}
