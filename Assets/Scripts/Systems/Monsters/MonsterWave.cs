using System.Collections;
using UnityEngine;

public enum Phase { None, Normal, Boss, Waiting, Warning }

public class MonsterWave : MonoBehaviour
{
    public static MonsterWave Instance { private set; get; }

    [Header("Wave")]
    [SerializeField] private Phase phase;
    [SerializeField][Min(0)] private int waveCount = 1;

    public bool IsPause { private set; get; } = false;

    [Header("Normal")]
    [SerializeField][Min(0.1f)] private float normalTime = 180f;
    private float normalTimer;
    [SerializeField] private Vector2 spawnRange = new Vector2(0.3f, 3f);
    private float spawnDelay;
    private float spawnTimer;
    [SerializeField][Min(0f)] private float spawnPeak = 30f;
    private float spawnDecrease;

    [Header("Boss")]
    [SerializeField][Min(1)] private int bossInterval = 5;
    [Space]
    [SerializeField] private Boss boss;

    public bool IsSpawned { private set; get; } = false;

    [Header("Waiting / Warning")]
    [SerializeField][Min(0.1f)] private float waitingTime = 10f;
    private float waitingTimer;
    [SerializeField][Min(0.1f)] private float warningInterval = 0.3f;
    private float warningTextTimer;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (spawnPeak > normalTime)
            spawnPeak = normalTime;

        if (spawnRange.x < 0f)
            spawnRange.x = 0f;

        if (spawnRange.y < spawnRange.x)
            spawnRange.y = spawnRange.x;
    }
#endif

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
            case Phase.Boss: BossPhase(dt); break;
            case Phase.Waiting: WaitingPhase(dt); break;
            case Phase.Warning: WarningPhase(dt); break;
        }
    }

    #region 웨이브 진행
    public void ResetWave()
    {
        waveCount = 1;
        IsPause = false;

        normalTimer = normalTime;
#if TEST_Manager
        if (TestManager.Instance?.Mode == TestMode.Tower)
            spawnDelay = spawnRange.x;
        else
#endif
            spawnDelay = spawnRange.y;
        spawnTimer = 0f;
        spawnDecrease = (normalTime - spawnPeak) / (spawnRange.y - spawnRange.x);

        boss = null;
        IsSpawned = false;

        waitingTimer = 0f;
        warningTextTimer = 0f;
    }

    public void StartWave()
    {
        ResetWave();
        phase = Phase.Normal;
    }

    public void PauseWave(bool _on) => IsPause = _on;
    public void PauseWave(float _time) => StartCoroutine(PauseCoroutine(_time));
    private IEnumerator PauseCoroutine(float _time)
    {
        PauseWave(true);
        yield return new WaitForSeconds(_time);
        PauseWave(false);
    }

    public void StopWave()
    {
        ResetWave();
        phase = Phase.None;
    }
    #endregion

    #region 노말/보스 페이즈
    private void NormalPhase(float _deltaTime)
    {
        spawnTimer -= _deltaTime;
        if (spawnTimer <= 0f)
        {
            EntityManager.Instance?.SpawnMonster();
            spawnTimer = spawnDelay;
        }

#if TEST_Manager
        if (TestManager.Instance?.Mode == TestMode.Tower)
            spawnDelay = spawnRange.x;
        else
#endif
            if (spawnDelay > spawnRange.x)
                spawnDelay = Mathf.Max(spawnDelay - _deltaTime / spawnDecrease, spawnRange.x);

#if TEST_Manager
        if (TestManager.Instance?.Mode == TestMode.Wave) return;
#endif

        normalTimer -= _deltaTime;
        if (normalTimer > 0f) return;

        if ((waveCount + 1) % bossInterval != 0)
            phase = Phase.Waiting;
        else
            phase = Phase.Warning;

        waitingTimer = waitingTime;
        warningTextTimer = 0f;
    }

    private void BossPhase(float _deltaTime)
    {
        if (!IsSpawned)
        {
            boss = EntityManager.Instance?.SpawnBoss();
            IsSpawned = true;
            return;
        }

        if (EntityManager.Instance?.GetMonsterCount() == 0)
        {
            ClearText();

            phase = Phase.Waiting;
            boss = null;
            IsSpawned = false;
            waitingTimer = waitingTime;
        }
    }
    #endregion

    #region 대기/경고 페이즈
    private void WaitingPhase(float _deltaTime)
    {
        waitingTimer -= _deltaTime;
        if (waitingTimer > 0f) return;

        if (++waveCount % bossInterval != 0)
        {
            phase = Phase.Normal;
            normalTimer = normalTime;
            spawnDelay = spawnRange.y;
            spawnTimer = 0f;
        }
        else phase = Phase.Boss;
    }

    private void WarningPhase(float _deltaTime)
    {
        waitingTimer -= _deltaTime;
        warningTextTimer -= _deltaTime;

        if (waitingTimer > 0f)
        {
            if (warningTextTimer > 0f) return;

            WarningText();
            warningTextTimer = warningInterval;
            return;
        }

        waveCount++;
        phase = Phase.Boss;
    }

    private void WarningText()
    {
        Rect worldRect = AutoCamera.WorldRect;
        Rect mapRect = UIManager.Instance.GetMapAreaRect();
        float y = Random.Range(mapRect.yMin, mapRect.yMax);
        Vector3 pos = new Vector3(worldRect.xMax, y, 0f);

        float scale = Random.Range(50f, 120f);
        float speed = Random.Range(150f, 500f);

        TextEffect effect = EntityManager.Instance?.MakeText(pos);
        if (effect == null) return;

        effect.SetText("경고", scale, Color.yellow, 0.1f);
        effect.SetColor(Color.black);
        effect.SetMove(speed, Vector3.left);
        effect.SetDuration(waitingTimer);
    }

    private void ClearText()
    {
        Rect mapRect = UIManager.Instance.GetMapAreaRect();
        Vector3 pos = new Vector3(0f, mapRect.center.y, 0f);
        Vector3 target = new Vector3(0f, mapRect.yMax * 0.85f, 0f);

        TextEffect effect = EntityManager.Instance?.MakeText(pos);
        if (effect == null) return;

        effect.SetText("클리어", 250f, Color.green, 0.3f);
        effect.SetColor(Color.white);
        effect.SetMove(target, waitingTime * 0.8f);
        effect.SetDuration(waitingTime / 3f);
    }
    #endregion

    #region SET
    public void SetDelay(float _delay)
        => spawnDelay = Mathf.Clamp(_delay, spawnRange.x, spawnRange.y);
    #endregion

    #region GET
    public void GetPhaseValue(out Color _color, out float _value, out float _maxValue, out string _text)
    {
        _color = Color.clear;
        _value = 0f;
        _maxValue = 1f;
        _text = string.Empty;

        switch (phase)
        {
            case Phase.Normal:
                _color = Color.green;
                if (normalTime > 0f)
                {
                    _value = normalTimer;
                    _maxValue = normalTime;
                }
                _text = (waveCount + 1) % bossInterval != 0
                    ? $"웨이브 {waveCount}"
                    : $"웨이브 {waveCount} | 곧 보스 등장";
                break;

            case Phase.Boss:
                _color = Color.red;
                if (boss != null)
                {
                    _value = boss.Health;
                    _maxValue = boss.MaxHealth;

                    if (!boss.IsInvalid())
                        _text = $"{UIManager.Instance?.FormatNumber(boss.Health)} / {UIManager.Instance?.FormatNumber(boss.MaxHealth)}";
                }
                break;

            case Phase.Waiting:
                _color = Color.magenta;
                if (waitingTimer > 0f)
                {
                    _value = waitingTime - waitingTimer;
                    _maxValue = waitingTime;
                }
                _text = "웨이브 대기 중...";
                break;

            case Phase.Warning:
                _color = Color.yellow;
                if (waitingTimer > 0f)
                {
                    _value = waitingTime - waitingTimer;
                    _maxValue = waitingTime;
                }
                break;
        }
    }
    #endregion

    #region 프로퍼티
    public bool IsRunning => phase != Phase.None;
    public int WaveCount => waveCount;
    #endregion
}
