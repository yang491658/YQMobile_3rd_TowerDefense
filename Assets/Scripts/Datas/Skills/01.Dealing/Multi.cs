using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Multi", menuName = "Skill/Dealing/Multi", order = 102)]
public class Multi : TowerSkill
{
    [Header("Value")]
    [SerializeField][Min(0f)] private float cooldown;
    [SerializeField][Min(0)] private int count;

    private const float interval = 0.1f;

#if UNITY_EDITOR
    public override ValueType[] GetValues()
        => new[] { ValueType.Cooldown, ValueType.Count };
#endif

    public override void SetValues(Tower _tower)
    {
        cooldown = _tower.GetValue(this, ValueType.Cooldown);
        count = _tower.GetValueInt(this, ValueType.Count);
    }

    public override void OnUpdate(Tower _tower, Monster _target, float _deltaTime)
    {
        if (_target == null || _target.IsInvalid()) return;
        if (IsCooldown) return;

        StartCooldown(_tower, cooldown);
        EntityManager.Instance?.StartCoroutine(MultiCoroutine(_tower));
    }

    private IEnumerator MultiCoroutine(Tower _tower)
    {
        EntityManager.Instance?.MakeEffect(_tower, _tower.transform.position, 1.2f);

        int hit = 0;
        while (hit < count)
        {
            if (_tower == null) yield break;

            Monster target = EntityManager.Instance?.GetMonsterRandom();
            if (target == null || target.IsInvalid()) yield break;

            _tower.Shoot(target);

            if (++hit < count)
                yield return new WaitForSeconds(interval);
        }
    }
}
