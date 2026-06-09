using UnityEngine;

[CreateAssetMenu(fileName = "Focus", menuName = "Skill/Dealing/Focus", order = 105)]
public class Focus : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0)] private int delta;
    [SerializeField][Min(0)] private int max;

    [Header("Others")]
    private int index;
    private int stack;

#if UNITY_EDITOR
    public override ValueType[] GetValues()
        => new[] { ValueType.Delta, ValueType.Max };
#endif

    public override void SetValues(Tower _tower)
    {
        delta = _tower.GetValueInt(this, ValueType.Delta);
        max = _tower.GetValueInt(this, ValueType.Max);
    }

    public override void OnGenerate(Tower _tower)
    {
        index = -1;
        stack = 0;
    }

    public override void OnStat(Tower _tower, ref int _damage, ref int _speed, ref int _chance, ref int _critical)
    {
        int limit = Mathf.RoundToInt(_speed * max / 100f);

        _speed += Mathf.Min(delta * stack, limit);
    }

    public override void OnAttack(Tower _tower, Monster _target, ref bool _instead)
    {
        if (_target == null || _target.IsInvalid()) return;

        if (_target.Index != index)
        {
            index = _target.Index;
            stack = 0;
        }

        stack++;
        _tower.UpdateStat();
    }

    public override void OnMiss(Tower _tower, Bullet _bullet, Vector3 _pos)
    {
        stack = 0;
        _tower.UpdateStat();
    }
}
