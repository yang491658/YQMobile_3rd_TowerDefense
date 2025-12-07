using UnityEngine;

[CreateAssetMenu(fileName = "01_Splash", menuName = "TowerSkill/Splash", order = 1)]
public class Splash : TowerSkill
{
    private float damage;
    private float range;

    public override void OnChange(Tower _tower)
    {
        damage = _tower.GetValue(0);
        range = _tower.GetValue(1);
    }

    public override void OnUpdate(Tower _tower, float _deltaTime) { }
    public override void OnAttack(Tower _tower) { }

    public override void OnHit(Tower _tower, Monster _target)
    {
        Vector3 center = _target.transform.position;

        if (effect != null)
            Instantiate(effect, center, Quaternion.identity, _tower.transform)
                .GetComponent<Effect>()
                .SetEffect(_tower.GetColor(), range, 0.5f);

        var monsters = EntityManager.Instance.GetMonstersInRange(center, range);
        for (int i = 0; i < monsters.Count; i++)
        {
            Monster m = monsters[i];
            if (m == _target) continue;

            m.TakeDamage(damage, _tower.GetColor());
        }
    }
}
