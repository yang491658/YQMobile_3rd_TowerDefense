using UnityEngine;

[CreateAssetMenu(fileName = "DotDamage", menuName = "TowerSkill/D.O.T.", order = 2)]
public class DotDamage : TowerSkill
{
    private float damage;
    private float duration;
    private float interval;

    public override void OnChange(Tower _tower)
    {
        damage = _tower.GetValue(0);
        duration = _tower.GetValue(1);
        interval = _tower.GetValue(2);
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        Instantiate(effect, _target.transform.position, Quaternion.identity, _target.transform)
            .GetComponent<Effect>()
            .SetEffect(_tower.GetColor(), 1f, duration);

        _target.ApplyDot(damage, duration, interval);
    }
}
