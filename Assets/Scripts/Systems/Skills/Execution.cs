using UnityEngine;

[CreateAssetMenu(fileName = "Execution", menuName = "TowerSkill/Execution", order = 4)]
public class Execution : Skill
{
    private float chance;

    public override void SetValues(Tower _tower)
    {
        chance = _tower.GetValue(ValueType.Chance);
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        if (Random.value < chance / 100f)
        {
            EntityManager.Instance.MakeEffect(_tower, _target.transform.position, 1.2f);
            _target.Die();
        }
    }
}