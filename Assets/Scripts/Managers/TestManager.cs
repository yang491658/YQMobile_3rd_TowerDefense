#if UNITY_EDITOR
using System.Collections;
using UnityEngine;

public class TestManager : MonoBehaviour
{
    public static TestManager Instance { private set; get; }

    [Header("Game Test")]
    [SerializeField] private int testCount = 1;
    [SerializeField] private bool isAutoPlay = false;
    [SerializeField] private bool isAutoReplay = false;
    [SerializeField][Min(1f)] private float replayTime = 5f;
    private Coroutine replayRoutine;

    [Header("Sound Test")]
    [SerializeField] private bool bgmPause = false;

    [Header("Entity  Test")]
    [SerializeField] private bool spawn = true;

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

    private void Start()
    {
        SoundManager.Instance?.ToggleBGM();
    }

    private void Update()
    {
        #region 게임 테스트
        if (Input.GetKeyDown(KeyCode.P))
            GameManager.Instance?.Pause(!GameManager.Instance.IsPaused);
        if (Input.GetKeyDown(KeyCode.G))
            GameManager.Instance?.GameOver();

        if (Input.GetKeyDown(KeyCode.O))
        {
            isAutoReplay = !isAutoReplay;
            AutoPlay();
        }
        if (isAutoReplay && GameManager.Instance.IsGameOver && replayRoutine == null)
            replayRoutine = StartCoroutine(AutoReplay());

        if (Input.GetKeyDown(KeyCode.R))
            GameManager.Instance?.Replay();
        if (Input.GetKeyDown(KeyCode.Q))
            GameManager.Instance?.Quit();
        if (Input.GetKey(KeyCode.L))
            GameManager.Instance?.ScoreUp();
        #endregion

        #region 사운드 테스트
        if (Input.GetKeyDown(KeyCode.B))
        {
            bgmPause = !bgmPause;
            SoundManager.Instance?.PauseSound(bgmPause);
        }
        if (Input.GetKeyDown(KeyCode.M))
            SoundManager.Instance?.ToggleBGM();
        if (Input.GetKeyDown(KeyCode.N))
            SoundManager.Instance?.ToggleSFX();
        #endregion

        #region 엔티티 테스트
        for (int i = 1; i <= 10; i++)
        {
            KeyCode key = (i == 10) ? KeyCode.Alpha0 : (KeyCode)((int)KeyCode.Alpha0 + i);
            if (Input.GetKeyDown(key))
            {
                break;
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            spawn = !spawn;
            EntityManager.Instance?.ToggleSpawn(spawn);
        }

        if (Input.GetKeyDown(KeyCode.Delete))
            EntityManager.Instance?.DespawnAll();
        #endregion

        #region UI 테스트
        if (Input.GetKeyDown(KeyCode.Z))
            UIManager.Instance?.OpenSetting(!UIManager.Instance.GetOnSetting());
        if (Input.GetKeyDown(KeyCode.X))
            UIManager.Instance?.OpenConfirm(!UIManager.Instance.GetOnConfirm());
        if (Input.GetKeyDown(KeyCode.C))
            UIManager.Instance?.OpenResult(!UIManager.Instance.GetOnResult());
        #endregion
    }

    private void AutoPlay()
    {
        if (!isAutoPlay)
        {
            isAutoPlay = true;
        }
        else
        {
            isAutoPlay = false;
        }
    }

    private IEnumerator AutoReplay()
    {
        yield return new WaitForSecondsRealtime(replayTime);
        if (GameManager.Instance.IsGameOver)
        {
            testCount++;
            GameManager.Instance?.Replay();
        }
        replayRoutine = null;
    }
}
#endif
