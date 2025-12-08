using UnityEngine;

public abstract class TowerSkill : ScriptableObject
{
    [SerializeField] protected GameObject effect;

    public virtual void SetValues(Tower _tower) { }

    public virtual void OnUpdate(Tower _tower, float _deltaTime) { }

    public virtual void OnAttack(Tower _tower) { }

    public virtual void OnHit(Tower _tower, Monster _target) { }
}
