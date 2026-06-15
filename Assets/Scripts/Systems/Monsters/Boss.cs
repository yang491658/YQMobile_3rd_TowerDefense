using System.Collections;
using UnityEngine;

public class Boss : Monster
{
    [Header("Reward")]
    [SerializeField][Min(0)] private int reward;
    private Coroutine rewardRoutine;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        scale = 1f;
        moveSpeed = 1f;
    }
#endif

    #region 전투
    protected override void OnDeath()
    {
        GameManager.Instance?.ScoreUp();

        if (rewardRoutine != null)
            GameManager.Instance?.StopCoroutine(rewardRoutine);
        rewardRoutine = GameManager.Instance?.StartCoroutine(RewardCoroutine(reward));
    }

    protected override void OnGoal()
    {
        GameManager.Instance?.GameOver();
    }

    private IEnumerator RewardCoroutine(int _reward)
    {
        while (_reward-- > 0)
        {
            GameManager.Instance?.ExpUp();
            GameManager.Instance?.GoldUp();

            yield return null;
        }
        rewardRoutine = null;
    }
    #endregion

    #region SET
    public void SetBoss()
    {
        int score = GameManager.Instance.Score / 50;
        int wave = MonsterWave.Instance.WaveCount;

        maxHealth = 100 * Mathf.Max(score, 1) * Mathf.Max(wave, 1);
        SetHealth(maxHealth);
        reward = 100 * MonsterWave.Instance.BossCount;
    }
    #endregion

    #region 풀링
    public override void OnSpawnPool()
    {
        base.OnSpawnPool();

        SetBoss();
    }

    public override void ResetPool()
    {
        base.ResetPool();

        moveSpeed = 1f;
    }
    #endregion
}
