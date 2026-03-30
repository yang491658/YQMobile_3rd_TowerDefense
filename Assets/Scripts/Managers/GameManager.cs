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
    [SerializeField][Min(0)] private int lifeGold = 100;
    public event System.Action<int, int> OnChangeLife;

    [Header("Exp")]
    [SerializeField][Min(0)] private int exp = 0;
    [SerializeField][Min(0)] private int needExp = 100;
    [SerializeField][Min(0)] private int expScore = 100;
    [SerializeField][Min(0)] private int expGold = 100;
    public event System.Action<int, int> OnChangeExp;

    [Header("Level")]
    [SerializeField][Min(1)] private int level = 1;
    [SerializeField][Min(1)] private int maxLevel = 10;
    public event System.Action<int> OnChangeLevel;

    [Header("Gold")]
    [SerializeField][Min(0)] private int gold = 0;
    [SerializeField][Min(0)] private int baseGold = 100;
    [SerializeField][Min(0)] private int needGold = 0;
    public event System.Action<int> OnChangeGold;

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

    #region 생명
    public void LifeUp(int _life = 1)
    {
        life = Mathf.Min(life + _life, maxLife);
        OnChangeLife?.Invoke(life, maxLife);
    }

    public void LifeDown(int _life = 1)
    {
        if (IsGameOver) return;

        life = Mathf.Max(life - _life, 0);
        OnChangeLife?.Invoke(life, maxLife);

        if (life <= 0) GameOver();
    }

    public void BuyLife()
    {
        if (life >= maxLife) return;
        if (gold < lifeGold) return;

        LifeUp();
        GoldDown(lifeGold);
    }

    public void ResetLife()
    {
        life = maxLife;
        OnChangeLife?.Invoke(life, maxLife);
    }
    #endregion

    #region 경험치
    public void ExpUp(int _exp = 1)
    {
        if (level >= maxLevel) return;

        exp += _exp;
        while (level < maxLevel && exp >= needExp)
        {
            exp -= needExp;
            LevelUp();
        }
        if (level >= maxLevel) exp = 0;
        OnChangeExp?.Invoke(exp, needExp);
    }

    public void BuyExp()
    {
        if (level >= maxLevel) return;
        if (gold < expGold) return;

        ExpUp(expGold / 10);
        GoldDown(expGold);
    }
    #endregion

    #region 레벨
    public void LevelUp(int _level = 1)
    {
        if (level >= maxLevel) return;

        level += _level;
        OnChangeLevel?.Invoke(level);

        needExp = 100 * level * (level + 1) / 2;
        OnChangeExp?.Invoke(exp, needExp);

        LevelText();
    }

    private void LevelText()
    {
        TextEffect effect = EntityManager.Instance?.MakeTextEffect();
        if (effect == null) return;

        effect.SetText("레벨업", 150f, Color.blue, 0.3f);
        effect.SetColor(Color.white);
        effect.SetDuration(1f);
    }

    public void ResetLevel()
    {
        level = 1;
        OnChangeLevel?.Invoke(level);

        exp = 0;
        needExp = 100;
        OnChangeExp?.Invoke(exp, needExp);
    }
    #endregion

    #region 골드
    public void GoldUp(int _gold = 1)
    {
        gold += _gold;
        OnChangeGold?.Invoke(gold);
    }

    public void GoldDown(int _gold = 1)
    {
        if (gold < _gold) return;

        gold -= _gold;
        OnChangeGold?.Invoke(gold);
    }

    public void ResetGold()
    {
        gold = baseGold;
        needGold = 0;
        OnChangeGold?.Invoke(gold);
    }

    public void UseGold(bool _useGold)
    {
        if (!_useGold) return;

        GoldDown(needGold);

        needGold += 10;
        OnChangeGold?.Invoke(gold);
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
    public float GetSpeed() => speed;
    public float GetMinSpeed() => minSpeed;
    public float GetMaxSpeed() => maxSpeed;

    public int GetScore() => score;

    public int GetLife() => life;
    public int GetMaxLife() => maxLife;
    public bool EnoughLifeCost() => gold >= lifeGold && life < maxLife;

    public int GetExp() => exp;
    public int GetNeedExp() => needExp;
    public bool EnoughExpCost() => gold >= expGold;

    public int GetLevel() => level;
    public int GetMaxLevel() => maxLevel;
    public bool IsMaxLevel() => level >= maxLevel;

    public int GetGold() => gold;
    public int GetNeedGold() => needGold;
    public bool EnoughGold() => gold >= needGold;
    #endregion
}
