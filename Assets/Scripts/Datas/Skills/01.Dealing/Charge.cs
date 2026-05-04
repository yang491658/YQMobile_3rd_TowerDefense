using UnityEngine;

[CreateAssetMenu(fileName = "Charge", menuName = "Skill/Dealing/Charge", order = 104)]
public class Charge : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int count;
    [SerializeField][Min(0f)] private float cooldown;

    private int speed;
    private int stack;
    private bool ready;

#if UNITY_EDITOR
    public override void SetID() => ID = 104;
    public override ValueType[] GetValues()
        => new[] { ValueType.Count, ValueType.Cooldown };
#endif

    public override void SetValues(Tower _tower)
    {
        count = _tower.GetValueInt(this, ValueType.Count);
        cooldown = _tower.GetValue(this, ValueType.Cooldown);
    }

    public override void OnGenerate(Tower _tower)
    {
        speed = DataManager.Instance.GetBaseStat(_tower.GetRole(), TowerGrade.Normal).attackSpeed;
        stack = 0;
        ready = false;
    }

    public override void OnStat(Tower _tower, ref int _damage, ref int _speed, ref int _chance, ref int _critical)
    {
        int damage = _damage + _speed;
        int critical = _chance + _critical;

        _damage = damage;
        _speed = speed;
        _chance = !ready ? 0 : 100;
        _critical = !ready ? 100 : critical;
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
        ready = false;
    }

    public override void OnImpact(Tower _tower, Bullet _bullet, Vector3 _pos)
    {
        ready = false;
    }
}
