#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestManager : MonoBehaviour
{
    public static TestManager Instance { private set; get; }

    [Header("Game Test")]
    [SerializeField] private int testCount = 1;
    [SerializeField][Min(1f)] private float autoDelay = 1f;
    private bool isAuto = false;
    private Coroutine autoRoutine;

    [Header("Sound Test")]
    [SerializeField] private bool bgmPause = false;

    [Header("Entity Test")]
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
        if (Input.GetKeyDown(KeyCode.R))
            GameManager.Instance?.Replay();
        if (Input.GetKeyDown(KeyCode.Q))
            GameManager.Instance?.Quit();

        if (Input.GetKeyDown(KeyCode.O))
        {
            isAuto = !isAuto;

            GameManager.Instance?.SetSpeed(isAuto ? GameManager.Instance.GetMaxSpeed() : 1f);
            GameManager.Instance?.Replay();
        }
        if (isAuto && !GameManager.Instance.IsPaused)
        {
            if (GameManager.Instance?.GetGold() >= EntityManager.Instance?.GetNeedGold())
                if (EntityManager.Instance?.SpawnTower() == null) MergeTower();
            if (GameManager.Instance.IsGameOver && autoRoutine == null)
                autoRoutine = StartCoroutine(AutoReplay());
        }

        if (Input.GetKeyDown(KeyCode.L))
            GameManager.Instance?.GoldUp(10000);
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
            if (Input.GetKey(key))
            {
                EntityManager.Instance?.SpawnTower(i, null, false);
                break;
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            spawn = !spawn;
            EntityManager.Instance?.ToggleSpawnMonster(spawn);
        }

        if (Input.GetKey(KeyCode.T))
            EntityManager.Instance?.SpawnTower(0, null, false);
        if (Input.GetKey(KeyCode.Y))
            MergeTower();

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

    private IEnumerator AutoReplay()
    {
        yield return new WaitForSecondsRealtime(autoDelay);
        if (GameManager.Instance.IsGameOver)
        {
            testCount++;
            GameManager.Instance?.Replay();
        }
        autoRoutine = null;
    }

    private void MergeTower()
    {
        List<Tower> towers = EntityManager.Instance?.GetTowers();
        int len = towers.Count;
        if (len < 2) return;

        int start = Random.Range(0, len);

        for (int n = 0; n < len; n++)
        {
            int i = (start + n) % len;

            for (int j = 0; j < len; j++)
            {
                if (i == j) continue;

                if (EntityManager.Instance?.MergeTower(towers[i], towers[j]) != null) return;
            }
        }
    }
}
#endif
