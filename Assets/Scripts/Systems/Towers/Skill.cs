using UnityEngine;

public abstract class Skill : ScriptableObject
{
    [SerializeField] protected GameObject effect;

    public virtual void SetValues(Tower _tower) { }

    public virtual void OnGenerate(Tower _tower) { }

    public virtual void OnUpdate(Tower _tower, float _deltaTime) { }

    public virtual void OnRankUp(Tower _tower, int _amount = 1) { }

    public virtual void OnAttack(Tower _tower) { }

    public virtual void OnHit(Tower _tower, Monster _target) { }

    public virtual void OnMerge(Tower _tower, Tower _target) { }

    public virtual void OnSell(Tower _tower) { }
}

