using UnityEngine;

public abstract class TowerSkill : ScriptableObject
{
    public virtual void SetValues(Tower _tower) { }

    public virtual void OnGenerate(Tower _tower) { }

    public virtual void OnUpdate(Tower _tower, float _deltaTime) { }

    public virtual void OnRankUp(Tower _tower, int _amount = 1) { }

    public virtual void OnAttack(Tower _tower) { }

    public virtual void OnHit(Tower _tower, Monster _target) { }

    public virtual void OnTakeDamage(Tower _tower, Monster _target, ref int _damage, ref bool _critical) { }

    public virtual void OnMerge(Tower _tower, Tower _target) { }

    public virtual void OnSell(Tower _tower) { }
}

