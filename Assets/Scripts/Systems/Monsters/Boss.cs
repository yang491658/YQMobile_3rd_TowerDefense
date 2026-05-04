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

#if TEST_Manager
        if (TestManager.Instance?.Mode == TestMode.Solo)
        {
            SetSpeed(1f);
            maxHealth = int.MaxValue;
            SetHealth(maxHealth);
            return;
        }
#endif

        data = _data;

        maxHealth = data.MaxHealth;
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
