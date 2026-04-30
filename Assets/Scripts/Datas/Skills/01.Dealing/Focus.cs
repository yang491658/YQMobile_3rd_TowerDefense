using UnityEngine;

[CreateAssetMenu(fileName = "Focus", menuName = "Skill/Dealing/Focus", order = 105)]
public class Focus : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int factor;
    [SerializeField][Min(0)] private int max;
    [SerializeField][Min(0f)] private float duration;
    [SerializeField][Min(0f)] private float cooldown;

    private float timer;
    private int stack;
    private float hold;

#if UNITY_EDITOR
    public override void SetID() => ID = 105;
    public override ValueType[] GetValues()
        => new[] { ValueType.Factor, ValueType.Max, ValueType.Duration, ValueType.Cooldown };
#endif

    public override void SetValues(Tower _tower)
    {
        factor = _tower.GetValueInt(this, ValueType.Factor);
        max = _tower.GetValueInt(this, ValueType.Max);
        duration = _tower.GetValue(this, ValueType.Duration);
        cooldown = _tower.GetValue(this, ValueType.Cooldown);
    }

    public override void OnGenerate(Tower _tower)
    {
        timer = 0f;
        stack = 0;
        hold = 0f;
    }

    public override void OnUpdate(Tower _tower, Monster _target, float _deltaTime)
    {
        if (IsCooldown()) return;

        if (timer > 0f)
            timer -= _deltaTime;

        if (hold > 0f)
        {
            hold -= _deltaTime;

            if (hold <= 0f)
            {
                timer = 0f;
                stack = 0;
                StartCooldown(_tower, cooldown);
            }
        }
    }

    public override void OnAttack(Tower _tower, Monster _target, ref bool _instead)
    {
        _instead = true;

        if (_target == null || _target.IsInvalid()) return;
        if (IsCooldown()) return;
        if (timer > 0f) return;

        _tower.Shoot(_target);
        timer = 60f / CalcSpeed(_tower, stack);
    }

    public override void OnHit(Tower _tower, Monster _target, ref bool _instead)
    {
        if (_tower == null) return;
        if (IsCooldown()) return;
        if (hold > 0f) return;

        stack = Mathf.Min(++stack, max);

        if (stack == max)
        {
            hold = duration;
            EntityManager.Instance?.MakeEffect(_tower, _tower.transform.position, 1.2f);
        }
    }

    private int CalcSpeed(Tower _tower, int _stack)
    {
        int speed = _tower.GetSpeed();
        speed = Mathf.RoundToInt(speed * (100f + factor * _stack) / 100f);
        return Mathf.Min(speed, 6000);
    }
}
