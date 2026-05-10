using UnityEngine;

[CreateAssetMenu(fileName = "Charge", menuName = "Skill/Dealing/Charge", order = 104)]
public class Charge : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int factor;
    [SerializeField][Min(0)] private int count;
    [SerializeField][Min(0f)] private float cooldown;

    private int speed;
    private int stack;
    private bool ready;

#if UNITY_EDITOR
    public override ValueType[] GetValues()
        => new[] { ValueType.Factor, ValueType.Count, ValueType.Cooldown };
#endif

    public override void SetValues(Tower _tower)
    {
        factor = _tower.GetValueInt(this, ValueType.Factor);
        count = _tower.GetValueInt(this, ValueType.Count);
        cooldown = _tower.GetValue(this, ValueType.Cooldown);
    }

    public override void OnGenerate(Tower _tower)
    {
        speed = DataManager.Instance.GetTowerStat(_tower.GetRole(), TowerGrade.Normal).attackSpeed;
        stack = 0;
        ready = false;
    }

    public override void OnStat(Tower _tower, ref int _damage, ref int _speed, ref int _chance, ref int _critical)
    {
        _damage = _damage + _speed * factor / 100;
        _speed = speed;
        _chance = !ready ? 0 : _chance;
        _critical = !ready ? 100 : _critical;
    }

    public override void OnAttack(Tower _tower, Monster _target, ref bool _instead)
    {
        if (IsCooldown() || ready)
        { _instead = true; return; }

        ready = ++stack >= count;
        if (ready)
        {
            stack = 0;
            EntityManager.Instance?.MakeEffect(_tower, _tower.transform.position, 1.2f);
            StartCooldown(_tower, cooldown);
        }
    }

    public override void OnHit(Tower _tower, Bullet _bullet, Monster _target, ref bool _instead)
    {
        if (_tower == null) return;

        if (ready)
        {
            _instead = true;

            int critical = _tower.GetCritical();

            float c = Mathf.Min(_tower.GetChance() * count, 100f);
            if (Random.value < c / 100f)
                critical *= _tower.GetRank();

            _tower.HitDamage(_target, _tower.GetDamage(), 100, critical);

            ready = false;
        }
    }

    public override void OnMiss(Tower _tower, Bullet _bullet, Vector3 _pos)
    {
        ready = false;
    }
}
