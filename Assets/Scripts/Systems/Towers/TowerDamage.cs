using UnityEngine;

public enum DamageType { Normal, Critical, Dot, Amp, }

[CreateAssetMenu(fileName = "TowerDamage", menuName = "Table/Tower/Damage", order = 54)]
public class TowerDamage : ScriptableObject
{
    [System.Serializable]
    public struct DamageData
    {
        public DamageType type;
        [Min(0f)] public float font;
        public Color color;

        public DamageData(DamageType _type, float _font, Color _color)
        {
            type = _type;
            font = Mathf.Max(_font, 0f);
            color = _color;
        }
    }

    [SerializeField] private DamageData normal = new(DamageType.Normal, 50f, Color.black);
    [SerializeField] private DamageData critical = new(DamageType.Critical, 65f, Color.red);
    [SerializeField] private DamageData dot = new(DamageType.Dot, 45f, Color.green);
    [SerializeField] private DamageData amp = new(DamageType.Amp, 55f, Color.magenta);

    public DamageData GetDamage(DamageType _type) => _type switch
    {
        DamageType.Critical => critical,
        DamageType.Dot => dot,
        DamageType.Amp => amp,
        _ => normal,
    };
}