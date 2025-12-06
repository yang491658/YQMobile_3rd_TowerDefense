using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { private set; get; }

    [Header("Speed")]
    [SerializeField] private float speed = 1f;
    [SerializeField] private float minSpeed = 0.5f;
    [SerializeField] private float maxSpeed = 3f;
    public event System.Action<float> OnChangeSpeed;

    [Header("Score")]
    [SerializeField] private int score = 0;
    public event System.Action<int> OnChangeScore;

    [Header("Life")]
    [SerializeField] private int life = 0;
    [SerializeField][Min(0)] private int defaultLife = 1000;
    public event System.Action<int> OnChangeLife;

    [Header("Gold")]
    [SerializeField] private int gold = 0;
    public event System.Action<int> OnChangeGold;

    public bool IsPaused { private set; get; } = false;
    public bool IsGameOver { private set; get; } = false;

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
        ResetGold();

        EntityManager.Instance?.ResetEntity();
        EntityManager.Instance?.SetEntity();
        EntityManager.Instance?.ToggleSpawnMonster(true);

        UIManager.Instance?.ResetUI();
        UIManager.Instance?.OpenUI(false);
        UIManager.Instance?.StartCountdown();
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
    }

    public void ResetScore()
    {
        score = 0;
        OnChangeScore?.Invoke(score);
    }
    #endregion

    #region 생명
    public void LifeUp(int _life = 1)
    {
        life += _life;
        OnChangeLife?.Invoke(life);
    }

    public void LifeDown(int _life = 1)
    {
        life -= _life;
        OnChangeLife?.Invoke(life);

        //if (life < 0) GameOver(); // 임시
    }

    public void ResetLife()
    {
        life = defaultLife;
        OnChangeLife?.Invoke(life);
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
        gold = 0;
        OnChangeGold?.Invoke(gold);
    }
    #endregion

    #region SET
    public void SetSpeed(float _speed)
    {
        speed = Mathf.Clamp(_speed, minSpeed, maxSpeed);
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
    public int GetMaxLife() => defaultLife;

    public int GetGold() => gold;
    #endregion
}
