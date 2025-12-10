using UnityEngine;

[CreateAssetMenu(fileName = "Splash", menuName = "TowerSkill/Splash", order = 1)]
public class Splash : Skill
{
    [Header("Skill")]
    [SerializeField] private float damage;
    [SerializeField] private float range;

    public override void SetValues(Tower _tower)
    {
        damage = _tower.GetValue(ValueType.Damage);
        range = _tower.GetValue(ValueType.Range);
    }

    public override void OnHit(Tower _tower, Monster _target)
        => HitSplash(_tower, _target.transform.position, _target);

    public void OnHit(Tower _tower, Vector3 _pos)
        => HitSplash(_tower, _pos, null);

    private void HitSplash(Tower _tower, Vector3 _center, Monster _target)
    {
        EntityManager.Instance.MakeEffect(_tower, _center, range);

        var monsters = EntityManager.Instance.GetMonstersInRange(_center, range);
        for (int i = 0; i < monsters.Count; i++)
        {
            Monster m = monsters[i];
            if (m == _target) continue;

            m.TakeDamage(damage);
        }
    }
}
