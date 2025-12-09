using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "DOT", menuName = "TowerSkill/DOT", order = 2)]
public class DOT : Skill
{
    private float damage;
    private float duration;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (effect == null)
            effect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/Debuff.prefab");
    }
#endif

    public override void SetValues(Tower _tower)
    {
        damage = _tower.GetValue(ValueType.Damage);
        duration = _tower.GetValue(ValueType.Duration);
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        Effect e = Instantiate(effect, _target.transform.position, Quaternion.identity, _target.transform)
            .GetComponent<Effect>();
        e.SetEffect(_tower, _duration: duration);

        _target.ApplyDot(damage, duration, e);
    }
}
