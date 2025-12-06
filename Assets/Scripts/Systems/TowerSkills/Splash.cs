using UnityEngine;

[CreateAssetMenu(fileName = "01_Splash", menuName = "TowerSkill/Splash", order = 1)]
public class Splash : TowerSkill
{
    public override void Initialize(Tower _tower)
    {
    }

    public override void OnUpdate(Tower _tower, float _deltaTime)
    {
    }

    public override void OnAttack(Tower _tower)
    {
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        Vector3 center = _target.transform.position;
        int damage = Mathf.Max(Mathf.RoundToInt(_tower.GetValue(1)), 1);
        float radius = Mathf.Max(_tower.GetValue(2), 1f);

        if (effect != null)
            Instantiate(effect, center, Quaternion.identity, _tower.transform)
                .GetComponent<Effect>()
                .SetEffect(_tower.GetColor(), radius);

        var monsters = EntityManager.Instance.GetMonstersInRange(center, radius);
        for (int i = 0; i < monsters.Count; i++)
        {
            Monster m = monsters[i];
            if (m == _target) continue;

            m.TakeDamage(damage, _tower.GetColor());
        }
    }
}
