using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Splash", menuName = "TowerSkill/Splash", order = 1)]
public class Splash : TowerSkill
{
    private float damage;
    private float range;

#if UNITY_EDITOR
    private void OnValidate()
    {
        effect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/Splash.prefab");
    }
#endif

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
        if (effect != null)
            Instantiate(effect, _center, Quaternion.identity, _tower.transform)
                .GetComponent<Effect>()
                .SetEffect(_tower, range, 0.3f);

        var monsters = EntityManager.Instance.GetMonstersInRange(_center, range);
        for (int i = 0; i < monsters.Count; i++)
        {
            Monster m = monsters[i];
            if (m == _target) continue;

            m.TakeDamage(damage);
        }
    }
}
