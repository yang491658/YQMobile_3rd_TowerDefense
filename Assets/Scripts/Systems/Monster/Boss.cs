using UnityEngine;

public class Boss : Monster
{
    [Header("Boss / Data")]
    [SerializeField] private BossData data;

    #region 전투
    protected override void OnDeath()
    {
    }

    protected override void OnGoal()
    {
        GameManager.Instance?.GameOver();
    }
    #endregion

    #region SET
    public void SetBoss(BossData _data)
    {
        StopAllCoroutines();

        data = _data;

        SetSpeed(data.MoveSpeed);
        maxHealth = data.TotalHealth;
        SetHealth(maxHealth);
    }
    #endregion

    #region GET
    public BossData GetData() => data;
    #endregion

    #region 풀링
    public override void ResetPool()
    {
        base.ResetPool();

        StopAllCoroutines();

        data = null;
    }
    #endregion
}
