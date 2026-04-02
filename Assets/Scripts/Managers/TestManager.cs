#if TEST_Manager
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct TestResult
{
    [Min(0)] public int score;
    [Min(0f)] public float playTime;

    public TestResult(int _score, float _playTime)
    {
        score = _score;
        playTime = _playTime;
    }
}

[System.Serializable]
public struct SliderConfig
{
    public TextMeshProUGUI TMP;
    public Slider slider;
    public int value;
    public int minValue;
    public int maxValue;
    public string format;

    public SliderConfig(int _value, int _min, int _max, string _format)
    {
        TMP = null;
        slider = null;
        value = _value;
        minValue = _min;
        maxValue = _max;
        format = _format;
    }
}

public class TestManager : MonoBehaviour
{
    public static TestManager Instance { private set; get; }

    [Header("Game Test")]
    [SerializeField] private List<TestResult> testResults = new();
    [SerializeField][Min(0f)] private float playTime = 0f;
    [Space]
    [SerializeField][Min(0f)] private float autoReplay = 0f;
    private Coroutine autoRoutine;

    public bool IsAuto { private set; get; } = false;

    [Header("Sound Test")]
    [SerializeField] private bool onPauseBgm = false;

    [Header("Test UI")]
    [SerializeField] private GameObject testUI;
    [Space]
    [SerializeField] private SliderConfig gameSpeed = new(1, 1, 10, "배속 × {0}");
    [Space]
    [SerializeField] private TextMeshProUGUI testCountNum;
    [SerializeField] private TextMeshProUGUI score10Num;
    [SerializeField] private TextMeshProUGUI averageScoreNum;
    [SerializeField] private TextMeshProUGUI averagePlayNum;
    [Space]
    [SerializeField] private SliderConfig refRank = new(3, 0, 0, "기준 랭크 : {0}");
    [SerializeField] private SliderConfig refTower = new(0, 0, 0, "기준 타워 : {0}");

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (testUI == null)
            testUI = GameObject.Find("TestUI");

        if (gameSpeed.TMP == null)
            gameSpeed.TMP = GameObject.Find("TestUI/GameSpeed/TestText")?.GetComponent<TextMeshProUGUI>();
        if (gameSpeed.slider == null)
            gameSpeed.slider = GameObject.Find("TestUI/GameSpeed/TestSlider")?.GetComponent<Slider>();

        if (testCountNum == null)
            testCountNum = GameObject.Find("TestUI/TestCount/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (score10Num == null)
            score10Num = GameObject.Find("TestUI/Score10/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (averageScoreNum == null)
            averageScoreNum = GameObject.Find("TestUI/AverageScore/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (averagePlayNum == null)
            averagePlayNum = GameObject.Find("TestUI/AveragePlay/TestNum")?.GetComponent<TextMeshProUGUI>();

        if (refTower.TMP == null)
            refTower.TMP = GameObject.Find("TestUI/RefTower/TestText")?.GetComponent<TextMeshProUGUI>();
        if (refTower.slider == null)
            refTower.slider = GameObject.Find("TestUI/RefTower/TestSlider")?.GetComponent<Slider>();
        if (refRank.TMP == null)
            refRank.TMP = GameObject.Find("TestUI/RefRank/TestText")?.GetComponent<TextMeshProUGUI>();
        if (refRank.slider == null)
            refRank.slider = GameObject.Find("TestUI/RefRank/TestSlider")?.GetComponent<Slider>();
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

        testUI.SetActive(false);
    }

    private void Start()
    {
        SoundManager.Instance?.ToggleBGM();

        SetAuto();
        UpdateTestUI();
    }

    private void Update()
    {
        #region 게임 매니저
        if (Input.GetKeyDown(KeyCode.P)) GameManager.Instance?.Pause(!GameManager.Instance.IsPaused);
        if (Input.GetKeyDown(KeyCode.G)) GameManager.Instance?.GameOver();
        if (Input.GetKeyDown(KeyCode.R)) GameManager.Instance?.Replay();
        if (Input.GetKeyDown(KeyCode.Q)) GameManager.Instance?.Quit();
        #endregion

        #region 사운드 매니저
        if (Input.GetKeyDown(KeyCode.B))
        {
            onPauseBgm = !onPauseBgm;
            SoundManager.Instance?.PauseSound(onPauseBgm);
        }
        if (Input.GetKeyDown(KeyCode.M)) SoundManager.Instance?.ToggleBGM();
        if (Input.GetKeyDown(KeyCode.N)) SoundManager.Instance?.ToggleSFX();
        #endregion

        #region 엔티티 매니저
        for (int i = 1; i <= 10; i++)
        {
            KeyCode key = i == 10 ? KeyCode.Alpha0 : (KeyCode)((int)KeyCode.Alpha0 + i);
            int digit = i == 10 ? 0 : i;

            if (Input.GetKeyDown(key))
            {
                bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                int current = refTower.value;
                int prefix = current / 100;
                int tens = (current / 10) % 10;
                int ones = current % 10;

                if (isShift) tens = digit;
                else ones = digit;

                int newValue = prefix * 100 + tens * 10 + ones;

                ChangeRefTower(newValue);
                break;
            }
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            int refID = DataManager.Instance.GetTowerID(refTower.value);
            Vector3 pos = Input.mousePosition;
            pos.z = -Camera.main.transform.position.z;
            pos = Camera.main.ScreenToWorldPoint(pos);

            EntityManager.Instance?.SpawnTower(refID, refRank.value, pos, _useGold: false);
        }
        if (Input.GetKeyDown(KeyCode.E))
            EntityManager.Instance?.ToggleSpawn(!EntityManager.Instance.IsSpawning);
        if (Input.GetKeyDown(KeyCode.Delete))
            EntityManager.Instance?.DespawnAll();
        #endregion

        #region UI 매니저
        if (Input.GetKeyDown(KeyCode.Z)) UIManager.Instance?.OpenSetting(!UIManager.Instance.GetOnSetting());
        if (Input.GetKeyDown(KeyCode.X)) UIManager.Instance?.OpenConfirm(!UIManager.Instance.GetOnConfirm());
        if (Input.GetKeyDown(KeyCode.C)) UIManager.Instance?.OpenResult(!UIManager.Instance.GetOnResult());
        #endregion

        #region 테스트 매니저
        if (Input.GetKeyDown(KeyCode.BackQuote)) OnClickTest();
        if (Input.GetKeyDown(KeyCode.O)) SetAuto(!IsAuto);
        if (IsAuto)
        {
            if (GameManager.Instance.IsGameOver)
            {
                if (autoRoutine == null)
                    autoRoutine = StartCoroutine(AutoReplay());
            }
            else AutoPlay();
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
            ChangeGameSpeed(gameSpeed.value == gameSpeed.maxValue ? GameManager.Instance.GetMaxSpeed() : gameSpeed.maxValue);
        if (Input.GetKeyDown(KeyCode.DownArrow))
            ChangeGameSpeed(gameSpeed.value == gameSpeed.minValue ? GameManager.Instance.GetMaxSpeed() : gameSpeed.minValue);
        #endregion
    }

    #region 자동 테스트
    public void SetAuto(bool _on = true)
    {
        IsAuto = _on;

        GameManager.Instance?.SetSpeed(_on ? GameManager.Instance.GetMaxSpeed() : 1f);
    }

    private void AutoPlay()
    {
        playTime += Time.deltaTime;

        AutoMerge(); SyncBasic();
        if (GameManager.Instance.EnoughGold())
        {
            if (EntityManager.Instance.HasEmptyField())
                EntityManager.Instance?.SpawnTower(refTower.value == 0 ? 0 : DataManager.Instance.GetTowerID(refTower.value));
            else MergeRandom();
        }

        int towerCount = EntityManager.Instance.GetTowerCount();
        int monsterCount = EntityManager.Instance.GetMonsterCount();
        if (towerCount < 30 && monsterCount < 50)
            ChangeGameSpeed(gameSpeed.maxValue);
        else if (towerCount < 100 && monsterCount < 200)
            ChangeGameSpeed(GameManager.Instance.GetMaxSpeed());
        else
            ChangeGameSpeed(1f);
    }

    private IEnumerator AutoReplay()
    {
        yield return new WaitForSecondsRealtime(autoReplay);

        if (GameManager.Instance.IsGameOver)
        {
            int score = GameManager.Instance.GetScore();

            testResults.Add(new TestResult(score, playTime));
            playTime = 0f;

            GameManager.Instance?.Replay();

            UpdateTestUI();
        }
        autoRoutine = null;
    }

    private void AutoMerge()
    {
        var towers = EntityManager.Instance?.GetTowers();
        if (towers == null) return;

        int len = towers.Count; if (len < 2) return;
        int limitRank = refRank.value; if (limitRank < 1) limitRank = 1;

        for (int r = 1; r < limitRank; r++)
        {
            for (int i = 0; i < len; i++)
            {
                Tower a = towers[i];
                if (a == null || a.IsDragging) continue;
                if (a.GetRank() != r) continue;

                for (int j = 0; j < len; j++)
                {
                    if (i == j) continue;

                    Tower b = towers[j];
                    if (b == null || b.IsDragging) continue;
                    if (b.GetRank() != r) continue;

                    if (a.Merge(b) != null) return;
                }
            }
        }
    }

    private void SyncBasic()
    {
        if (refTower.value == 0) return;

        int refID = DataManager.Instance.GetTowerID(refTower.value);
        TowerData refData = DataManager.Instance?.SearchTower(refID);
        if (refData.Role != TowerRole.Buff && refData.Role != TowerRole.Debuff) return;

        List<Tower> towers = EntityManager.Instance?.GetTowers();
        int maxRank = Tower.MaxRank;

        int[] target = new int[maxRank + 1];
        List<Tower>[] basics = new List<Tower>[maxRank + 1];

        for (int r = 0; r <= maxRank; r++)
            basics[r] = new List<Tower>();

        for (int i = 0; i < towers.Count; i++)
        {
            Tower t = towers[i];
            if (t == null || t.IsDragging) continue;

            int rank = t.GetRank(); int id = t.GetID();

            if (id == refID) target[rank]++;
            else if (id == 999) basics[rank].Add(t);
        }

        for (int rank = 1; rank <= maxRank; rank++)
        {
            List<Tower> list = basics[rank];
            int need = target[rank];

            while (list.Count < need)
            {
                Tower spawned = EntityManager.Instance?.SpawnTower(999, rank, _useGold: false);
                if (spawned == null) break;

                list.Add(spawned);
            }

            while (list.Count > need)
            {
                if (rank < maxRank && list.Count - need >= 2)
                {
                    int last = list.Count - 1; Tower a = list[last]; list.RemoveAt(last);
                    last = list.Count - 1; Tower b = list[last]; list.RemoveAt(last);

                    Tower merged = a.Merge(b);
                    if (merged != null) basics[rank + 1].Add(merged);
                    else break;
                }
                else
                {
                    int last = list.Count - 1;
                    Tower remove = list[last];
                    list.RemoveAt(last);
                    EntityManager.Instance?.DespawnTower(remove);
                }
            }
        }
    }

    private void MergeRandom()
    {
        List<Tower> towers = EntityManager.Instance?.GetTowers();
        if (towers == null) return;

        int len = towers.Count; if (len < 2) return;

        HashSet<int> rankSet = new();
        for (int i = 0; i < len; i++)
        {
            Tower t = towers[i];
            if (t == null || t.IsDragging) continue;

            rankSet.Add(t.GetRank());
        }

        List<int> ranks = new(rankSet); ranks.Sort();
        for (int r = 0; r < ranks.Count; r++)
        {
            int curRank = ranks[r];
            List<int> indices = new List<int>();
            for (int i = 0; i < len; i++)
            {
                Tower t = towers[i];
                if (t == null || t.IsDragging) continue;

                if (t.GetRank() == curRank) indices.Add(i);
            }

            int count = indices.Count; if (count < 2) continue;
            int start = Random.Range(0, count);

            for (int n = 0; n < count; n++)
            {
                Tower a = towers[indices[(start + n) % count]];
                for (int m = 0; m < count; m++)
                {
                    if (n == m) continue;
                    Tower b = towers[indices[m]];

                    if (a.Merge(b) != null) return;
                }
            }
        }
    }
    #endregion

    #region 테스트 UI
    private void OnEnable()
    {
        gameSpeed.value = (int)GameManager.Instance?.GetSpeed();
        InitSlider(gameSpeed, ChangeGameSpeed);

        refRank.minValue = 1;
        refRank.maxValue = Tower.MaxRank;
        InitSlider(refRank, ChangeRefRank);
        refTower.maxValue = DataManager.Instance.GetTowerDatas().Length;
        InitSlider(refTower, ChangeRefTower);
    }

    private void OnDisable()
    {
        gameSpeed.slider.onValueChanged.RemoveListener(ChangeGameSpeed);

        refRank.slider.onValueChanged.RemoveListener(ChangeRefRank);
        refTower.slider.onValueChanged.RemoveListener(ChangeRefTower);
    }

    private void InitSlider(SliderConfig _config, UnityEngine.Events.UnityAction<float> _action)
    {
        if (_config.slider == null) return;

        _config.slider.minValue = _config.minValue;
        _config.slider.maxValue = _config.maxValue;
        _config.slider.wholeNumbers = true;
        _config.slider.value = Mathf.Clamp(_config.value, _config.minValue, _config.maxValue);

        _action.Invoke(_config.slider.value);
        _config.slider.onValueChanged.AddListener(_action);
    }

    private int ChangeSlider(float _value, SliderConfig _config)
        => Mathf.Clamp(Mathf.RoundToInt(_value), _config.minValue, _config.maxValue);

    private void ApplySlider(ref SliderConfig _config, float _value, System.Action<int> _afterAction = null)
    {
        _config.value = ChangeSlider(_value, _config);
        UpdateSliderUI(_config);
        _afterAction?.Invoke(_config.value);
    }

    private void UpdateSliderUI(SliderConfig _config)
    {
        _config.TMP.text = string.IsNullOrEmpty(_config.format)
            ? _config.value.ToString()
            : string.Format(_config.format, _config.value);
        _config.slider.value = _config.value;
    }

    private void ChangeGameSpeed(float _value) => ApplySlider(ref gameSpeed, _value, _v => GameManager.Instance?.SetSpeed(_v, true));

    private void ChangeRefRank(float _value) => ApplySlider(ref refRank, _value);
    private void ChangeRefTower(float _value) => ApplySlider(ref refTower, _value);

    private void UpdateTestUI()
    {
        int count = testResults.Count;

        List<int> scores = new(count);
        int totalScore = 0;
        float totalPlay = 0f;
        double scoreSqSum = 0d;

        for (int i = 0; i < count; i++)
        {
            TestResult r = testResults[i];

            scores.Add(r.score);
            totalScore += r.score;
            totalPlay += r.playTime;
            scoreSqSum += (double)r.score * r.score;
        }

        int topAvg = 0; int bottomAvg = 0;
        if (count > 0)
        {
            scores.Sort();
            int group = Mathf.Max(Mathf.CeilToInt(count * 0.1f), 1);

            long sumBottom = 0;
            for (int i = 0; i < group; i++) sumBottom += scores[i];

            long sumTop = 0;
            for (int i = count - group; i < count; i++) sumTop += scores[i];

            bottomAvg = Mathf.RoundToInt((float)sumBottom / group);
            topAvg = Mathf.RoundToInt((float)sumTop / group);
        }

        int averageScore = count > 0 ? totalScore / count : 0;
        float averagePlay = count > 0 ? totalPlay / count : 0f;

        double cvScore = 0d;
        if (count > 1)
        {
            double meanScore = (double)totalScore / count;
            double varScore = scoreSqSum / count - meanScore * meanScore;
            if (varScore < 0d) varScore = 0d;
            double stdScore = System.Math.Sqrt(varScore);
            cvScore = meanScore != 0d ? (stdScore / meanScore) * 100d : 0d;
        }

        int totalSeconds = Mathf.RoundToInt(averagePlay);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        testCountNum.text = count.ToString();
        score10Num.text = $"{topAvg:#,0} / {bottomAvg:#,0}";
        averageScoreNum.text = $"{averageScore:#,0} ({cvScore:0.#}%)";
        averagePlayNum.text = minutes.ToString("00") + ":" + seconds.ToString("00");

        UpdateSliderUI(gameSpeed);
    }

    public void OnClickTest()
    {
        testUI.SetActive(!testUI.activeSelf);
        UpdateTestUI();
    }
    public void OnClickReset()
    {
        testResults.Clear();
        playTime = 0f;

        UpdateTestUI();
    }
    public void OnClickReplay() => GameManager.Instance?.Replay();
    #endregion
}
#endif
