using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestManager : MonoBehaviour
{
    public static TestManager Instance { private set; get; }

    [Header("Game Test")]
    [SerializeField] private int testCount = 1;
    [SerializeField] private bool isAuto = false;
    [SerializeField][Min(1f)] private float autoReplay = 1f;
    private Coroutine autoRoutine;

    [Header("Sound Test")]
    [SerializeField] private bool bgmPause = false;

    [Header("Entity Test")]
    [SerializeField] private bool spawn = true;
    [SerializeField][Min(1)] private int rank = 3;

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
        AutoPlay();
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
            AutoPlay();
        if (isAuto)
        {
            AutoMergeTower();

            if (GameManager.Instance?.GetGold() >= EntityManager.Instance?.GetNeedGold())
                if (EntityManager.Instance?.SpawnTower() == null) MergeTower();
            if (GameManager.Instance.IsGameOver && autoRoutine == null)
                autoRoutine = StartCoroutine(AutoReplay());
        }

        if (Input.GetKeyDown(KeyCode.L)) GiveGold();
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

        if (Input.GetKeyDown(KeyCode.D))
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
        isAuto = !isAuto;

        GameManager.Instance?.SetSpeed(isAuto ? GameManager.Instance.GetMaxSpeed() : 1f);
        SoundManager.Instance?.ToggleBGM();
    }

    private IEnumerator AutoReplay()
    {
        yield return new WaitForSecondsRealtime(autoReplay);
        if (GameManager.Instance.IsGameOver)
        {
            testCount++;
            GameManager.Instance?.Replay();
        }
        autoRoutine = null;
    }

    private void GiveGold() => GameManager.Instance?.GoldUp(100_0000);

    private void AutoMergeTower()
    {
        List<Tower> towers = EntityManager.Instance?.GetTowers();
        if (towers == null) return;

        int len = towers.Count;
        if (len < 2) return;

        int maxRank = rank;
        if (maxRank < 1) maxRank = 1;

        for (int r = 1; r < maxRank; r++)
        {
            for (int i = 0; i < len; i++)
            {
                Tower a = towers[i];
                if (a == null || a.IsDragging()) continue;
                if (a.GetRank() != r) continue;

                for (int j = 0; j < len; j++)
                {
                    if (i == j) continue;

                    Tower b = towers[j];
                    if (b == null || b.IsDragging()) continue;
                    if (b.GetRank() != r) continue;

                    if (EntityManager.Instance?.MergeTower(a, b) != null)
                        return;
                }
            }
        }
    }

    private void MergeTower()
    {
        List<Tower> towers = EntityManager.Instance?.GetTowers();
        if (towers == null) return;

        int len = towers.Count;
        if (len < 2) return;

        HashSet<int> rankSet = new HashSet<int>();
        for (int i = 0; i < len; i++)
        {
            Tower t = towers[i];
            if (t == null || t.IsDragging()) continue;
            rankSet.Add(t.GetRank());
        }

        List<int> ranks = new List<int>(rankSet);
        ranks.Sort();

        for (int r = 0; r < ranks.Count; r++)
        {
            int curRank = ranks[r];

            List<int> indices = new List<int>();
            for (int i = 0; i < len; i++)
            {
                Tower t = towers[i];
                if (t == null || t.IsDragging()) continue;
                if (t.GetRank() == curRank)
                    indices.Add(i);
            }

            int count = indices.Count;
            if (count < 2) continue;

            int start = Random.Range(0, count);

            for (int n = 0; n < count; n++)
            {
                int iLocal = indices[(start + n) % count];
                Tower a = towers[iLocal];

                for (int m = 0; m < count; m++)
                {
                    if (n == m) continue;

                    int jLocal = indices[m];
                    Tower b = towers[jLocal];

                    if (EntityManager.Instance?.MergeTower(a, b) != null) return;
                }
            }
        }
    }
}
