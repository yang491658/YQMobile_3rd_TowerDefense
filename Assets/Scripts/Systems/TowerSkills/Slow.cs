using UnityEngine;

[CreateAssetMenu(fileName = "Slow", menuName = "TowerSkill/Slow", order = 3)]
public class Slow : TowerSkill
{
    private float amount;
    private float duration;

    public override void OnChange(Tower _tower)
    {
        amount = _tower.GetValue(0);
        duration = _tower.GetValue(1);
    }

    public override void OnHit(Tower _tower, Monster _target)
    {
        Effect e = Instantiate(effect, _target.transform.position, Quaternion.identity, _target.transform)
            .GetComponent<Effect>();
        e.SetEffect(_tower, 1f);

        _target.ApplySlow(amount, duration, e);
    }
}
