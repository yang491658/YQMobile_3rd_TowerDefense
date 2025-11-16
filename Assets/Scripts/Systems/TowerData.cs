using UnityEngine;

[CreateAssetMenu(fileName = "Tower", menuName = "TowerData", order = 1)]
public class TowerData : ScriptableObject
{
    [Header("Entity")]
    public int ID;
    public string Name;
    public Color32 Color;

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
    }
#endif

    public TowerData Clone()
    {
        TowerData clone = CreateInstance<TowerData>();

        clone.name = this.Name;

        clone.ID = this.ID;
        clone.Name = this.Name;
        clone.Color= this.Color;

        return clone;
    }
}
