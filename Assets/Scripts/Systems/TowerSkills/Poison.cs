using UnityEngine;

[CreateAssetMenu(fileName = "Poison", menuName = "TowerSkill/Poison", order = 2)]
public class Poison : TowerSkill
{
    private float damage;
    private float duration;

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
