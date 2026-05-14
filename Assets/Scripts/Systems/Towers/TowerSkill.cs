using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public abstract class TowerSkill : ScriptableObject
{
    public int ID;
    protected Coroutine cooldownRoutine;

#if UNITY_EDITOR
    private void OnValidate()
        => ID = ((CreateAssetMenuAttribute)System.Attribute.GetCustomAttribute(
            GetType(), typeof(CreateAssetMenuAttribute))).order;

    public virtual ValueType[] GetValues() => default;
#endif

    public virtual System.Predicate<Monster> GetFilter() => null;

    public virtual void SetValues(Tower _tower) { }

    public virtual void OnGenerate(Tower _tower) { }

    public virtual void OnStat(Tower _tower, ref int _damage, ref int _speed, ref int _chance, ref int _critical) { }

    public virtual void OnUpdate(Tower _tower, Monster _target, float _deltaTime) { }

    public virtual void OnAttack(Tower _tower, Monster _target, ref bool _instead) { }

    public virtual void OnHit(Tower _tower, Bullet _bullet, Monster _target, ref bool _instead) { }

    public virtual void OnMiss(Tower _tower, Bullet _bullet, Vector3 _pos) { }

    public virtual void OnMerge(Tower _tower, Tower _target) { }

    public virtual void OnRankUp(Tower _tower, int _amount = 1) { }

    public virtual void OnSell(Tower _tower) { }

    protected bool IsCooldown => cooldownRoutine != null;

    protected void StartCooldown(Tower _tower, float _cooldown)
    {
        if (cooldownRoutine != null)
            EntityManager.Instance?.StopCoroutine(cooldownRoutine);

        cooldownRoutine = EntityManager.Instance?.StartCoroutine(CooldownCoroutine(_tower, _cooldown));
    }

    private IEnumerator CooldownCoroutine(Tower _tower, float _cooldown)
    {
        if (_tower == null)
        { cooldownRoutine = null; yield break; }

        Image timerUI = _tower.TimerUI;
        if (timerUI != null)
        {
            timerUI.gameObject.SetActive(true);
            timerUI.fillAmount = 1f;
        }

        float timer = _cooldown;
        while (timer > 0f)
        {
            if (timerUI == null)
            { cooldownRoutine = null; yield break; }

            timer -= Time.deltaTime;
            timerUI.fillAmount = Mathf.Clamp01(timer / _cooldown);

            yield return null;
        }

        if (timerUI != null)
        {
            timerUI.fillAmount = 0f;
            timerUI.gameObject.SetActive(false);
        }

        cooldownRoutine = null;
    }
}
