using UnityEngine;

public enum DamageType { Normal, Critical, DoT, Bonus, }

[CreateAssetMenu(fileName = "TowerDamage", menuName = "Table/Tower/Damage", order = 54)]
public class TowerDamage : ScriptableObject
{
    [System.Serializable]
    public struct DamageData
    {
        public DamageType type;
        public Color color;
        [Min(0f)] public int font;

        public DamageData(DamageType _type, Color _color, int _font = 50)
        {
            type = _type;
            color = _color;
            font = Mathf.Max(_font, 0);
        }
    }

    [SerializeField] private DamageData normal = new(DamageType.Normal, Color.black, 50);
    [SerializeField] private DamageData critical = new(DamageType.Critical, Color.red, 65);
    [SerializeField] private DamageData dot = new(DamageType.DoT, Color.green, 35);
    [SerializeField] private DamageData bonus = new(DamageType.Bonus, Color.magenta, 45);

    public DamageData GetDamage(DamageType _type) => _type switch
    {
        DamageType.Critical => critical,
        DamageType.DoT => dot,
        DamageType.Bonus => bonus,
        _ => normal,
    };
}
