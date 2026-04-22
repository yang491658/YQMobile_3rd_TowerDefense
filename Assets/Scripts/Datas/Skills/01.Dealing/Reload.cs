using UnityEngine;

[CreateAssetMenu(fileName = "Reload", menuName = "Skill/Dealing/Reload", order = 102)]
public class Reload : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int count;
    [SerializeField][Min(0f)] private float cooldown;

    private int speed;
    private float timer;
    private int stack;
    private bool ready;
    private bool hit;

#if UNITY_EDITOR
    public override void SetID() => ID = 102;
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
        timer = 0f;
        stack = 0;
        ready = false;
        hit = true;
    }

    public override void OnUpdate(Tower _tower, Monster _target, float _deltaTime)
    {
        if (timer > 0f)
            timer -= _deltaTime;
    }

    public override void OnAttack(Tower _tower, Monster _target, ref bool _instead)
    {
        _instead = true;

        if (IsCooldown()) return;
        if (timer > 0f || !hit) return;

        _tower.Shoot(_target);

        ready = ++stack >= count;
        hit = false;
        timer = 60f / speed;
    }

    public override void OnHit(Tower _tower, Monster _target, ref bool _instead)
    {
        _instead = true;

        int damage = _tower.GetDamage() + _tower.GetSpeed();
        int chance = !ready ? 0 : 100;
        int critical = !ready ? 100 : _tower.GetCritical() + _tower.GetChance();

        _tower.HitDamage(_target, damage, chance, critical, false);
        hit = true;

        if (ready)
        {
            stack = 0;
            ready = false;

            StartCooldown(_tower, cooldown);
        }
    }

    public override void OnHit(Tower _tower, Vector3 _pos, ref bool _instead)
    {
        _instead = true;

        if (!ready)
            hit = true;
        else
        {
            stack = count - 1;
            ready = false;
            hit = true;
        }
    }
}
