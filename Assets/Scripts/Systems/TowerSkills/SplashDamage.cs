using UnityEngine;

[CreateAssetMenu(fileName = "SplashDamage", menuName = "TowerSkill/Splash", order = 1)]
public class SplashDamage : TowerSkill
{
    private float damage;
    private float range;

    public override void OnChange(Tower _tower)
    {
        damage = _tower.GetValue(0);
        range = _tower.GetValue(1);
    }

    public override void OnHit(Tower _tower, Monster _target)
        => HitSplash(_tower, _target.transform.position, _target);

    public void OnHit(Tower _tower, Vector3 _pos)
        => HitSplash(_tower, _pos, null);

    private void HitSplash(Tower _tower, Vector3 _center, Monster _target)
    {
        if (effect != null)
            Instantiate(effect, _center, Quaternion.identity, _tower.transform)
                .GetComponent<Effect>()
                .SetEffect(_tower.GetColor(), range, 0.3f);

        var monsters = EntityManager.Instance.GetMonstersInRange(_center, range);
        for (int i = 0; i < monsters.Count; i++)
        {
            Monster m = monsters[i];
            if (m == _target) continue;

            m.TakeDamage(damage);
        }
    }
}
