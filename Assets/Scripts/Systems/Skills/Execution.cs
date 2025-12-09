using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Execution", menuName = "TowerSkill/Execution", order = 4)]
public class Execution : TowerSkill
{
    private float chance;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (effect == null)
            effect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/Execution.prefab");
    }
#endif

    public override void SetValues(Tower _tower)
    {
        chance = _tower.GetValue(ValueType.Chance);
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        if (Random.value < chance / 100f)
        {
            Effect e = Instantiate(effect, _target.transform.position, Quaternion.identity, EntityManager.Instance?.GetEffectTrans())
                .GetComponent<Effect>();
            e.SetEffect(_tower, 1.2f, 0.3f);

            _target.Die();
        }
    }
}