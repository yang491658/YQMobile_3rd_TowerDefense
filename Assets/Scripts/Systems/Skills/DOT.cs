using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "DOT", menuName = "TowerSkill/DOT", order = 2)]
public class DOT : TowerSkill
{
    private float damage;
    private float duration;

#if UNITY_EDITOR
    private void OnValidate()
    {
        effect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/Debuff.prefab");
    }
#endif

    public override void OnChange(Tower _tower)
    {
        damage = _tower.GetValue(0);
        duration = _tower.GetValue(1);
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        Effect e = Instantiate(effect, _target.transform.position, Quaternion.identity, _target.transform)
            .GetComponent<Effect>();
        e.SetEffect(_tower, 1f);

        _target.ApplyDot(damage, duration, e);
    }
}
