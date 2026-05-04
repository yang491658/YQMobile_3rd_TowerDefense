using UnityEngine;

[CreateAssetMenu(fileName = "Pierce", menuName = "Skill/Dealing/Pierce", order = 102)]
public class Pierce : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int factor;
    [SerializeField][Min(0)] private int min;

#if UNITY_EDITOR
    public override void SetID() => ID = 102;
    public override ValueType[] GetValues()
        => new[] { ValueType.Factor, ValueType.Min };
#endif

    public override void SetValues(Tower _tower)
    {
        factor = _tower.GetValueInt(this, ValueType.Factor);
        min = _tower.GetValueInt(this, ValueType.Min);
    }

    public override void OnBullet(Tower _tower, Bullet _bullet)
    {
        _bullet.SetTarget(null);
    }

    public override void OnHit(Tower _tower, Bullet _bullet, Monster _target, ref bool _instead)
    {
        _instead = true;

        int count = Mathf.Max(_bullet.GetHitCount() - 1, 0);
        int rate = 100 - factor * count;
        int damage = Mathf.Max(_tower.GetDamage() * rate / 100, min);

        _tower.HitDamage(_target, damage);
    }
}
