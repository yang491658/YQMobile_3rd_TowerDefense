using UnityEngine;

public class TowerBuff : MonoBehaviour
{
    [Header("Damage Buff")]
    [SerializeField][Min(0)] private int damagePercent;
    [SerializeField][Min(0f)] private float damageDuration;
    private float damageTimer;
    private bool damageActive;

    private void Update()
    {
        if (!damageActive) return;

        damageTimer -= Time.deltaTime;

        if (damageTimer <= 0f)
        {
            damageActive = false;
            damagePercent = 0;
            damageDuration = 0f;
            damageTimer = 0f;
        }
    }

    public void ApplyDamageBuff(int _percent, float _duration)
    {
        damagePercent = Mathf.Max(_percent, damagePercent);
        damageDuration = Mathf.Max(_duration, damageDuration);
        damageTimer = damageDuration;
        damageActive = damagePercent > 0 && damageDuration > 0f;
    }

    public int GetBuffDamage(int _damage)
    {
        if (!damageActive || damagePercent <= 0) return _damage;

        float ratio = 1f + damagePercent / 100f;
        return Mathf.RoundToInt(_damage * ratio);
    }
}
