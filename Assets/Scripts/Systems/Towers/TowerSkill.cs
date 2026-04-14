using System.Collections;
using UnityEngine;

public abstract class TowerSkill : ScriptableObject
{
    public int ID;
    protected Coroutine cooldownRoutine;

#if UNITY_EDITOR
    private void OnValidate() => SetID();

    public virtual void SetID() => ID = 0;

    public virtual ValueType[] GetValues() => default;
#endif

    public virtual System.Predicate<Monster> GetFilter() => null;

    public virtual void SetValues(Tower _tower) { }

    public virtual void OnGenerate(Tower _tower) { }

    public virtual void OnUpdate(Tower _tower, float _deltaTime) { }

    public virtual void OnRankUp(Tower _tower, int _amount = 1) { }

    public virtual void OnAttack(Tower _tower, Monster _target, ref bool _instead) { }

    public virtual void OnHit(Tower _tower, Monster _target, ref bool _instead) { }

    public virtual void OnHit(Tower _tower, Vector3 _pos, ref bool _instead) { }

    public virtual void OnMerge(Tower _tower, Tower _target) { }

    public virtual void OnSell(Tower _tower) { }

    protected bool IsCooldown() => cooldownRoutine != null;

    protected void StartCooldown(Tower _tower, float _time)
    {
        if (cooldownRoutine != null)
            _tower.StopCoroutine(cooldownRoutine);

        cooldownRoutine = _tower.StartCoroutine(CooldownCoroutine(_time));
    }

    protected IEnumerator CooldownCoroutine(float _time)
    {
        yield return new WaitForSeconds(_time);
        cooldownRoutine = null;
    }
}