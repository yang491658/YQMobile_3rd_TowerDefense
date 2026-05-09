using UnityEngine;

[CreateAssetMenu(fileName = "Pierce", menuName = "Skill/Dealing/Pierce", order = 102)]
public class Pierce : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int factor;
    [SerializeField][Min(0)] private int min;

#if UNITY_EDITOR
    public override ValueType[] GetValues()
        => new[] { ValueType.Factor, ValueType.Min };
#endif

    public override void SetValues(Tower _tower)
    {
        factor = _tower.GetValueInt(this, ValueType.Factor);
        min = _tower.GetValueInt(this, ValueType.Min);
    }

    public override void OnHit(Tower _tower, Bullet _bullet, Monster _target, ref bool _instead)
    {
        int count = _bullet.GetHitCount();

        if (count <= 1)
        {
            _bullet.SetTarget(null);
            return;
        }

        _instead = true;

        int rate = 100 - factor * (count - 1);
        int damage = Mathf.Max(_tower.GetDamage() * rate / 100, min);

        _tower.HitDamage(_target, damage, 0);
    }
}
