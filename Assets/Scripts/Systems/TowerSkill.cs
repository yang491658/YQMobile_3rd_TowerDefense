using UnityEngine;

public abstract class TowerSkill : ScriptableObject
{
    public virtual void Initialize(TowerBase _tower) { }

    public virtual void OnUpdate(TowerBase _tower, float _deltaTime) { }

    public virtual void OnAttack(TowerBase _tower) { }

    public virtual void OnHit(TowerBase _tower, Monster _target) { }
}
