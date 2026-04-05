using UnityEngine;

public enum Phase { None, Normal, Warning, Boss, Reward }

public sealed class MonsterWave : MonoBehaviour
{
    public static MonsterWave Instance { private set; get; }

    [Header("Wave")]
    [SerializeField] private Phase phase;

    public bool IsPause { private set; get; } = false;
    public bool IsRunning => phase != Phase.None;

    [Header("Normal")]
    [SerializeField][Min(0.1f)] private float normalTime = 180f;
    private float normalTimer;
    [SerializeField] private Vector2 spawnRange = new Vector2(1f, 3f);
    private float spawnDelay;
    private float spawnTimer;
    [SerializeField] private float spawnPeak = 30f;
    private float spawnDecrease;

    [Header("Warning")]
    [SerializeField][Min(0.1f)] private float warningTime = 5f;
    private float warningTimer;
    [SerializeField][Min(0f)] private float warningInterval = 0.3f;
    private float warningTextTimer;

    [Header("Boss")]
    [SerializeField][Min(0f)] private float bossCome = 1.5f;
    [SerializeField][Min(1)] private int bossOrder = 1;
    [SerializeField] private Boss boss;
    private bool onBoss = false;

    public bool IsSpawned { private set; get; } = false;
    public bool IsFinished { private set; get; } = false;

    [Header("Reward")]
    [SerializeField][Min(0.1f)] private float rewardTime = 3f;
    private float rewardTimer;
    [SerializeField][Min(0)] private int rewardExp;
    [SerializeField][Min(0)] private int rewardGold;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (GameManager.Instance.IsGameOver) return;
        if (IsPause) return;

        float dt = Time.deltaTime;
        switch (phase)
        {
            case Phase.Normal: NormalPhase(dt); break;
            case Phase.Warning: WarningPhase(dt); break;
            case Phase.Boss: BossPhase(dt); break;
            case Phase.Reward: RewardPhase(dt); break;
        }
    }

    #region 웨이브
    public void ResetWave()
    {
        IsPause = false;

        normalTimer = normalTime;
        spawnDelay = spawnRange.y;
        spawnTimer = 0f;
        spawnDecrease = (normalTime - spawnPeak) / (spawnRange.y - spawnRange.x);

        warningTimer = 0f;
        warningTextTimer = 0f;

        onBoss = false;
        bossOrder = 1;
        boss = null;
        IsSpawned = false;
        IsFinished = false;

        rewardTimer = 0f;
        rewardExp = 0;
        rewardGold = 0;
    }

    public void StartWave()
    {
        ResetWave();
        phase = Phase.Normal;
    }
    public void PauseWave(bool _on) => IsPause = _on;
    public void StopWave()
    {
        ResetWave();
        phase = Phase.None;
    }
    #endregion

    #region 노말 페이즈
    private void NormalPhase(float _deltaTime)
    {
        spawnTimer -= _deltaTime;
        if (spawnTimer <= 0f)
        {
            EntityManager.Instance?.SpawnMonster();
            spawnTimer = spawnDelay;
        }

        if (spawnDelay > spawnRange.x)
            spawnDelay = Mathf.Max(spawnDelay - _deltaTime / spawnDecrease, spawnRange.x);

        if (IsFinished) return;

        normalTimer -= _deltaTime;
        if (normalTimer <= 0f)
        {
            phase = Phase.Warning;
            warningTimer = warningTime;
            warningTextTimer = 0f;
        }
    }
    #endregion

    #region 경고 페이즈
    private void WarningPhase(float _deltaTime)
    {
        warningTimer -= _deltaTime;
        warningTextTimer -= _deltaTime;

        if (warningTimer > 0f && warningTextTimer <= 0f)
        {
            WarningText();
            warningTextTimer = warningInterval;
        }

        if (warningTimer <= 0f)
        {
            phase = Phase.Boss;
            IsSpawned = false;
        }
    }

    private void WarningText()
    {
        Rect worldRect = AutoCamera.WorldRect;
        Rect mapRect = UIManager.Instance.GetMapAreaRect();
        float y = Random.Range(mapRect.yMin, mapRect.yMax);
        Vector3 pos = new Vector3(worldRect.xMax, y, 0f);

        float scale = Random.Range(50f, 120f);
        float speed = Random.Range(150f, 500f);

        TextEffect effect = EntityManager.Instance?.MakeTextEffect(pos);
        if (effect == null) return;

        effect.SetText("경고", scale, Color.yellow, 0.15f);
        effect.SetColor(Color.black);
        effect.SetMove(speed, Vector3.left);
    }
    #endregion

    #region 보스 페이즈
    private void BossPhase(float _deltaTime)
    {
        if (!onBoss)
        {
            Vector3 offset = UIManager.Instance.GetPlayerOffset();
            EntityManager.Instance?.MoveInGame(offset, bossCome);
            onBoss = true;
        }

        if (!IsSpawned)
        {
            BossText();
            boss = EntityManager.Instance?.SpawnBoss(bossOrder);
            IsSpawned = true;

            BossData data = boss.GetData();
            rewardExp = data.Exp;
            rewardGold = data.Gold;
            return;
        }

        if (boss.IsDead)
        {
            RewardText();
            phase = Phase.Reward;
            rewardTimer = rewardTime;
            boss = null;
            IsSpawned = false;
            if (DataManager.Instance?.GetBossID(++bossOrder) == 0) IsFinished = true;
        }
    }

    private void BossText()
    {
        TextEffect effect = EntityManager.Instance?.MakeTextEffect();
        if (effect == null) return;

        effect.SetText("보스 등장", 250f, Color.red, 0.05f);
        effect.SetColor(Color.black);
        effect.SetDuration(1f);
    }
    #endregion

    #region 보상 페이즈
    private void RewardPhase(float _deltaTime)
    {
        if (onBoss)
        {
            EntityManager.Instance?.MoveInGame(Vector3.zero, bossCome);
            onBoss = false;
        }

        float ratio = _deltaTime * rewardTimer;

        if (rewardExp > 0)
        {
            int delta = Mathf.FloorToInt(rewardExp * ratio);
            delta = Mathf.Clamp(delta, 1, rewardExp);

            GameManager.Instance?.ExpUp(delta);
            rewardExp -= delta;
        }

        if (rewardGold > 0)
        {
            int delta = Mathf.FloorToInt(rewardGold * ratio);
            delta = Mathf.Clamp(delta, 1, rewardGold);

            GameManager.Instance?.GoldUp(delta);
            rewardGold -= delta;
        }

        rewardTimer -= _deltaTime;
        if (rewardTimer <= 0f && rewardExp <= 0 && rewardGold <= 0)
        {
            phase = Phase.Normal;
            normalTimer = normalTime;
            spawnDelay = spawnRange.y;
            spawnTimer = 0f;
        }
    }

    private void RewardText()
    {
        TextEffect effect = EntityManager.Instance?.MakeTextEffect();
        if (effect == null) return;

        effect.SetText("클리어", 250f, Color.green, 0.3f);
        effect.SetColor(Color.white);
        effect.SetDuration(1f);
    }
    #endregion

    #region SET
    public void SetDelay(float _delay)
        => spawnDelay = Mathf.Clamp(_delay, spawnRange.x, spawnRange.y);
    #endregion

    #region GET
    public void GetPhaseValue(out Phase _phase, out float _value, out float _maxValue, out Color _color)
    {
        _phase = phase;
        _value = 0f;
        _maxValue = 1f;
        _color = Color.clear;

        switch (phase)
        {
            case Phase.Normal:
                if (normalTime > 0f)
                {
                    _value = normalTimer;
                    _maxValue = normalTime;
                }
                _color = Color.green;
                break;

            case Phase.Warning:
                if (warningTime > 0f)
                {
                    _value = warningTime - warningTimer;
                    _maxValue = warningTime;
                }
                _color = Color.yellow;
                break;

            case Phase.Boss:
                if (boss != null)
                {
                    _value = boss.GetHealth();
                    _maxValue = boss.GetMaxHealth();
                }
                _color = Color.red;
                break;

            case Phase.Reward:
                if (rewardTime > 0f)
                {
                    _value = rewardTime - rewardTimer;
                    _maxValue = rewardTime;
                }
                _color = Color.magenta;
                break;
        }
    }
    #endregion
}
