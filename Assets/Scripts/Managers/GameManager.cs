using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { private set; get; }

    [Header("Speed")]
    [SerializeField][Min(0f)] private float speed = 1f;
    [SerializeField][Min(0f)] private float minSpeed = 0.5f;
    [SerializeField][Min(0f)] private float maxSpeed = 3f;
    public event System.Action<float> OnChangeSpeed;

    public bool IsPaused { private set; get; } = false;
    public bool IsGameOver { private set; get; } = false;

    [Header("Score")]
    [SerializeField] private int score = 0;
    private int scoreStack = 0;
    public event System.Action<int> OnChangeScore;

    [Header("Life")]
    [SerializeField][Min(0)] private int life = 0;
    [SerializeField][Min(0)] private int maxLife = 20;
    [SerializeField][Min(0f)] private float lifeCooldown = 3f;
    private float lifeTimer = 0f;
    public event System.Action<int, int> OnChangeLife;

    [Header("Exp")]
    [SerializeField][Min(0)] private int exp = 0;
    [SerializeField][Min(0)] private int needExp = 0;
    [SerializeField][Min(0)] private int expScore = 1000;
    public event System.Action<int, int> OnChangeExp;

    [Header("Level")]
    [SerializeField][Min(1)] private int level = 1;
    [SerializeField][Min(1)] private int maxLevel = 10;
    public event System.Action<int> OnChangeLevel;

    [Header("Gold")]
    [SerializeField] private int gold = 100;
    [SerializeField][Min(0)] private int needGold = 0;
    public event System.Action<int, int> OnChangeGold;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void GameOverReact();
    [DllImport("__Internal")] private static extern void ReplayReact();
#endif

#if UNITY_EDITOR
    private void OnValidate()
    {
        minSpeed = Mathf.Clamp(minSpeed, 0.05f, 1f);
        maxSpeed = Mathf.Clamp(maxSpeed, 1f, 100f);
        if (minSpeed > maxSpeed) minSpeed = maxSpeed;
        speed = Mathf.Clamp(speed, minSpeed, maxSpeed);
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
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += LoadGame;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= LoadGame;
    }

    private void LoadGame(Scene _scene, LoadSceneMode _mode)
    {
        Pause(false);
        IsGameOver = false;

        ResetScore();
        ResetLife();
        ResetLevel();
        ResetGold();

        EntityManager.Instance?.ResetEntity();
        UIManager.Instance?.ResetUI();

#if TEST_Manager
        if (TestManager.Instance.IsAuto) TestManager.Instance?.SetAuto();
#endif
    }

    #region 진행
    public void Pause(bool _pause)
    {
        if (IsPaused == _pause) return;

        IsPaused = _pause;
        Time.timeScale = _pause ? 0f : speed;

        if (_pause)
            HandleManager.Instance?.CancelDrag();
    }

    private void ActWithReward(System.Action _act)
    {
        if (ADManager.Instance != null) ADManager.Instance?.ShowReward(_act);
        else _act?.Invoke();
    }

    public void Replay()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ReplayReact();
#else
        ActWithReward(ReplayGame);
#endif
    }
    private void ReplayGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    public void Quit() => ActWithReward(QuitGame);
    private void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        Pause(true);
        SoundManager.Instance?.GameOver();
        UIManager.Instance?.OpenResult(true);

#if UNITY_WEBGL && !UNITY_EDITOR
        GameOverReact();
#endif
    }
    #endregion

    #region 점수
    public void ScoreUp(int _score = 1)
    {
        score += _score;
        OnChangeScore?.Invoke(score);

        if (level < maxLevel)
        {
            scoreStack += _score;
            while (scoreStack >= expScore)
            {
                scoreStack -= expScore;
                ExpUp(expScore / 10);
            }
        }
    }

    public void ResetScore()
    {
        score = 0;
        scoreStack = 0;
        OnChangeScore?.Invoke(score);
    }
    #endregion

    #region 생명력
    public void LifeUp(int _life = 1)
    {
        life = Mathf.Min(life + _life, maxLife);
        OnChangeLife?.Invoke(life, maxLife);
    }

    public void LifeDown(int _life = 1)
    {
        if (IsGameOver) return;
        if (lifeTimer > 0f) return;

        life = Mathf.Max(life - _life, 0);
        lifeTimer = lifeCooldown;
        OnChangeLife?.Invoke(life, maxLife);

        if (life <= 0) GameOver();
    }

    public void ResetLife()
    {
        maxLife = 20;
        life = maxLife;
        lifeTimer = 0f;
        OnChangeLife?.Invoke(life, maxLife);
    }
    #endregion

    #region 경험치
    public void ExpUp(int _exp = 1)
    {
        if (level >= maxLevel) return;

        exp += _exp;
        while (CanLevelUp && level < maxLevel && exp >= needExp)
        {
            exp -= needExp;
            LevelUp();
        }
        if (level >= maxLevel) exp = 0;
        OnChangeExp?.Invoke(exp, needExp);
    }

    public bool BuyExp()
    {
        if (!CanBuyExp) return false;

        int cost = needExp;
        ExpUp(cost / 10);
        GoldDown(cost);

        return true;
    }
    #endregion

    #region 레벨
    public void LevelUp(int _level = 1)
    {
        if (level >= maxLevel) return;

        level = Mathf.Min(level + _level, maxLevel);
        OnChangeLevel?.Invoke(level);

        needExp = NeedExp;
        OnChangeExp?.Invoke(exp, needExp);

        LevelText();
    }

    private void LevelText()
    {
        Rect mapRect = UIManager.Instance.GetMapAreaRect();
        Vector3 pos = new Vector3(0f, mapRect.yMin, 0f);
        Vector3 offset = UIManager.Instance.GetPlayerOffset();

        TextEffect effect = EntityManager.Instance?.MakeText(pos + offset);
        if (effect == null) return;

        effect.SetText("레벨업", 80f, Color.blue, 0.1f);
        effect.SetColor(Color.white);
        effect.SetMove(150f, Vector3.up);
        effect.SetDuration(1f);
    }

    public void ResetLevel()
    {
        level = 1;
        OnChangeLevel?.Invoke(level);

        exp = 0;
        needExp = NeedExp;
        OnChangeExp?.Invoke(exp, needExp);
    }
    #endregion

    #region 골드
    public void GoldUp(int _gold = 1)
    {
        gold += _gold;
        OnChangeGold?.Invoke(gold, needGold);
    }

    public void GoldDown(int _gold = 1, bool _force = false)
    {
        if (!_force && gold < _gold) return;

        gold -= _gold;
        OnChangeGold?.Invoke(gold, needGold);
    }

    public void ResetGold()
    {
        gold = 100;
        needGold = 0;
        OnChangeGold?.Invoke(gold, needGold);
    }

    public bool UseGold()
    {
        if (!EnoughGold()) return false;

        gold -= needGold;
        needGold += 10;
        OnChangeGold?.Invoke(gold, needGold);

        return true;
    }
    #endregion

    #region SET
    public void SetSpeed(float _speed) => SetSpeed(_speed, false);
    public void SetSpeed(float _speed, bool _force)
    {
        speed = _force ? _speed : Mathf.Clamp(_speed, minSpeed, maxSpeed);
        if (!IsPaused) Time.timeScale = speed;
        OnChangeSpeed?.Invoke(speed);
    }
    #endregion

    #region GET
    public int GetSellGold(Tower _tower)
    {
        float rate = 0.8f / Mathf.Sqrt(DataManager.Instance.GetGradeStat(_tower.Grade));
        return Mathf.FloorToInt(needGold * _tower.Rank * rate);
    }
    public bool EnoughGold() => gold >= needGold;
    #endregion

    #region 프로퍼티
    public float Speed => speed;
    public float MinSpeed => minSpeed;
    public float MaxSpeed => maxSpeed;

    public int Score => score;

    public int Life => life;
    public int MaxLife => maxLife;
    public float LifeCooldown => lifeCooldown;

    public int Exp => exp;
    public int NeedExp => 100 * level * (level + 1) / 2;
    public bool CanBuyExp => gold >= needExp && exp < needExp && level < maxLevel;
    private bool CanLevelUp => MonsterWave.Instance?.BossOrder > level;

    public int Level => level;
    public int MaxLevel => maxLevel;
    public bool IsMaxLevel => level >= maxLevel;

    public int Gold => gold;
    public int NeedGold => needGold;
    #endregion
}
