using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Slow", menuName = "TowerSkill/Slow", order = 3)]
public class Slow : TowerSkill
{
    private float percent;
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
        percent = _tower.GetValue(ValueType.Percent);
        duration = _tower.GetValue(ValueType.Duration);
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        Effect e = Instantiate(effect, _target.transform.position, Quaternion.identity, _target.transform)
            .GetComponent<Effect>();
        e.SetEffect(_tower, 1f);

        _target.ApplySlow(percent, duration, e);
    }
}
