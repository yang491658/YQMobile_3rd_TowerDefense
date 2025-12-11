using UnityEngine;

[CreateAssetMenu(fileName = "DamageUp", menuName = "TowerSkill/Buff/DamageUp", order = 301)]
public class DamageUp : TowerSkill
{
    [Header("Skill")]
    [SerializeField][Min(0)] private int percent;
    [SerializeField][Min(0)] private int count;
    [SerializeField][Min(0f)] private float duration;
    [SerializeField][Min(0f)] private float cooldown;

    private float timer;

    public override void SetValues(Tower _tower)
    {
        percent = _tower.GetValueInt(ValueType.Percent);
        count = _tower.GetValueInt(ValueType.Count);
        duration = _tower.GetValue(ValueType.Duration);
        cooldown = _tower.GetValue(ValueType.Cooldown);
    }

    public override void OnGenerate(Tower _tower)
    {
        timer = 0f;
    }

    public override void OnUpdate(Tower _tower, float _deltaTime)
    {
        timer -= _deltaTime;
        if (timer > 0f) return;

        ApplyBuff(_tower);
    }

    private void ApplyBuff(Tower _tower)
    {
        int towerCount = EntityManager.Instance.GetTowerCount();
        int maxCount = Mathf.Min(count, towerCount);

        for (int i = 0; i < maxCount; i++)
        {
            Tower target = EntityManager.Instance.GetTowerRandom();
            EntityManager.Instance?.MakeEffect(_tower, target.transform, 0.5f, duration);
            target.ApplyDamageBuff(percent, duration);
        }

        timer = duration + cooldown;
    }
}
