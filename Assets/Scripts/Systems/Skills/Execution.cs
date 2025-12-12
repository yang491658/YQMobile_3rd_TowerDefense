using UnityEngine;

[CreateAssetMenu(fileName = "Execution", menuName = "TowerSkill/Dealing/Execution", order = 104)]
public class Execution : TowerSkill
{
    [Header("Skill Value")]
    [SerializeField][Min(0)] private int chance;

    public override void SetValues(Tower _tower)
    {
        chance = _tower.GetValueInt(ValueType.Chance);
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        if (Random.value < chance / 100f)
        {
            EntityManager.Instance?.MakeEffect(_tower, _target.transform.position, _duration: 0.3f);
            _target.Die();
        }
    }
}