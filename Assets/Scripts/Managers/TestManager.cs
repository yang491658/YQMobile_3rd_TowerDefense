#if TEST_Manager
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct TestResult
{
    [Min(0)] public long value;
    [Min(0f)] public float playTime;

    public TestResult(long _value, float _playTime)
    {
        value = _value;
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

public enum TestMode { None, Wave, Solo, Boss, Snake, Full, }

public class TestManager : MonoBehaviour
{
    public static TestManager Instance { private set; get; }

    [Header("Game Test")]
    [SerializeField] private List<TestResult> testResults = new();
    [SerializeField][Min(0f)] private float playTime = 0f;
    [SerializeField][Min(0)] private long runDamage = 0;
    [Space]
    [SerializeField][Min(0f)] private float autoReplay = 0f;
    private Coroutine autoRoutine;

    public bool IsAuto { private set; get; } = false;

    [Header("Sound Test")]
    [SerializeField] private bool onPauseBgm = false;

    [Header("Test Text")]
    [SerializeField] private TextMeshProUGUI testText;
    private float timer = 0f;
    private int frame = 0;

    [Header("Test UI")]
    [SerializeField] private GameObject testUI;
    [Space]
    [SerializeField] private SliderConfig gameSpeed = new(1, 1, 20, "배속 × {0}");
    [Space]
    [SerializeField] private TextMeshProUGUI testCountNum;
    [SerializeField] private TextMeshProUGUI averagePlayNum;
    [SerializeField] private TextMeshProUGUI averageValueName;
    [SerializeField] private TextMeshProUGUI averageValueNum;
    [SerializeField] private TextMeshProUGUI value10Num;
    [Space]
    [SerializeField] private SliderConfig refRank = new(3, 0, 0, "기준 랭크 : {0}");
    [SerializeField] private SliderConfig refTower = new(0, 0, 0, "기준 타워 : {0}");
    [SerializeField] private SliderConfig refBoss = new(0, 0, 0, "기준 보스 : {0}");
    public int RefBoss => refBoss.value;
    private bool bossInit = false;
    private int needTower = 1;

    public TestMode Mode { private set; get; } = TestMode.None;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (testText == null)
            testText = GameObject.Find("TestText")?.GetComponent<TextMeshProUGUI>();
        if (testUI == null)
            testUI = GameObject.Find("TestUI");

        if (gameSpeed.TMP == null)
            gameSpeed.TMP = GameObject.Find("TestUI/GameSpeed/TestText")?.GetComponent<TextMeshProUGUI>();
        if (gameSpeed.slider == null)
            gameSpeed.slider = GameObject.Find("TestUI/GameSpeed/TestSlider")?.GetComponent<Slider>();

        if (testCountNum == null)
            testCountNum = GameObject.Find("TestUI/TestCount/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (averagePlayNum == null)
            averagePlayNum = GameObject.Find("TestUI/AveragePlay/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (averageValueName == null)
            averageValueName = GameObject.Find("TestUI/AverageValue/TestName")?.GetComponent<TextMeshProUGUI>();
        if (averageValueNum == null)
            averageValueNum = GameObject.Find("TestUI/AverageValue/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (value10Num == null)
            value10Num = GameObject.Find("TestUI/Value10/TestNum")?.GetComponent<TextMeshProUGUI>();

        if (refRank.TMP == null)
            refRank.TMP = GameObject.Find("TestUI/RefRank/TestText")?.GetComponent<TextMeshProUGUI>();
        if (refRank.slider == null)
            refRank.slider = GameObject.Find("TestUI/RefRank/TestSlider")?.GetComponent<Slider>();
        if (refTower.TMP == null)
            refTower.TMP = GameObject.Find("TestUI/RefTower/TestText")?.GetComponent<TextMeshProUGUI>();
        if (refTower.slider == null)
            refTower.slider = GameObject.Find("TestUI/RefTower/TestSlider")?.GetComponent<Slider>();
        if (refBoss.TMP == null)
            refBoss.TMP = GameObject.Find("TestUI/RefBoss/TestText")?.GetComponent<TextMeshProUGUI>();
        if (refBoss.slider == null)
            refBoss.slider = GameObject.Find("TestUI/RefBoss/TestSlider")?.GetComponent<Slider>();
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

        if (Input.GetKey(KeyCode.T))
        {
            int refID = DataManager.Instance.GetTowerID(refTower.value);
            Vector3 pos = Input.mousePosition;
            pos.z = -Camera.main.transform.position.z;
            pos = Camera.main.ScreenToWorldPoint(pos);

            EntityManager.Instance?.SpawnTower(refID, refRank.value, pos, _useGold: false);
        }
        if (Input.GetKeyDown(KeyCode.E))
            EntityManager.Instance?.ToggleSpawn(MonsterWave.Instance.IsPause);
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
            ChangeGameSpeed(gameSpeed.value == gameSpeed.maxValue
                ? GameManager.Instance.GetMaxSpeed()
                : gameSpeed.maxValue);
        if (Input.GetKeyDown(KeyCode.DownArrow))
            ChangeGameSpeed(gameSpeed.value == gameSpeed.minValue
                ? GameManager.Instance.GetMaxSpeed()
                : gameSpeed.minValue);
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            if (Mode != TestMode.Boss) ChangeRefTower(--refTower.value);
            else ChangeRefBoss(--refBoss.value);
        if (Input.GetKeyDown(KeyCode.RightArrow))
            if (Mode != TestMode.Boss) ChangeRefTower(++refTower.value);
            else ChangeRefBoss(++refBoss.value);

        if (Input.GetKeyDown(KeyCode.J)) ToggleMode(TestMode.Wave);
        if (Input.GetKeyDown(KeyCode.K)) ToggleMode(TestMode.Solo);
        if (Input.GetKeyDown(KeyCode.L)) ToggleMode(TestMode.Boss);
        if (Input.GetKeyDown(KeyCode.Semicolon)) ToggleMode(TestMode.Snake);
        if (Input.GetKeyDown(KeyCode.Quote)) ToggleMode(TestMode.Full);

        UpdateTestText();
        #endregion
    }

    #region 자동 테스트
    public void SetAuto(bool _on = true)
    {
        IsAuto = _on;

        if (!_on) Mode = TestMode.None;
    }

    private void AutoPlay()
    {
        playTime += Time.deltaTime;

        AutoMerge();
        if (Mode == TestMode.None)
        {
            if (GameManager.Instance.EnoughGold())
            {
                if (EntityManager.Instance.HasEmptyField())
                    TowerStore.Instance?.AutoPurchase
                        (refTower.value == 0 ? 0 : DataManager.Instance.GetTowerID(refTower.value));
                else MergeRandom();
            }
        }
        else TestPlay();
    }

    private void TestPlay()
    {
        int refID = DataManager.Instance.GetTowerID(refTower.value);

        switch (Mode)
        {
            case TestMode.Wave:
                Vector3? posWave = EntityManager.Instance?.GetLShapePos();
                if (posWave.HasValue)
                    EntityManager.Instance?.SpawnTower(refID, refRank.value, posWave.Value, _useGold: false);
                SyncBasic();
                break;

            case TestMode.Solo:
                MonsterWave.Instance?.StopWave();
                Vector3? posSolo = EntityManager.Instance?.GetLShapePos();
                if (posSolo.HasValue)
                    EntityManager.Instance?.SpawnTower(refID, refRank.value, posSolo.Value, _useGold: false);
                if (EntityManager.Instance?.GetMonsterCount() == 0)
                    EntityManager.Instance?.SpawnBoss();
                SyncBasic();
                break;

            case TestMode.Boss:
                if (!bossInit)
                {
                    for (int i = 0; i < needTower; i++)
                        EntityManager.Instance?.SpawnTower(0, 1, _useGold: false);
                    bossInit = true;
                }
                break;

            case TestMode.Snake:
                Vector3? posSnake = EntityManager.Instance?.GetSnakePos();
                if (posSnake.HasValue)
                    EntityManager.Instance?.SpawnTower(refID, refRank.value, posSnake.Value, _useGold: false);
                break;

            case TestMode.Full:
                if (EntityManager.Instance.HasEmptyField())
                    EntityManager.Instance?.SpawnTower(refID, refRank.value, _useGold: false);
                SyncBasic();
                break;
        }
    }

    private IEnumerator AutoReplay()
    {
        yield return new WaitForSecondsRealtime(autoReplay);

        if (GameManager.Instance.IsGameOver)
        {
            long value = GameManager.Instance.GetScore();

            if (Mode == TestMode.Boss)
            {
                if (EntityManager.Instance?.GetMonsterCount() > 0)
                {
                    playTime = 0f;
                    runDamage = 0;
                    needTower++;
                }
                else
                {
                    testResults.Add(new TestResult(needTower, playTime));
                    playTime = 0f;
                    runDamage = 0;
                    needTower = Mathf.Max(needTower - 1, 1);
                }
                bossInit = false;
            }
            else
            {
                if (Mode == TestMode.Solo)
                    value = runDamage;

                testResults.Add(new TestResult(value, playTime));

                playTime = 0f;
                runDamage = 0;
            }

            GameManager.Instance?.Replay();
            UpdateTestUI();
        }
        autoRoutine = null;
    }

    private void ToggleMode(TestMode _mode)
    {
        if (Mode != _mode)
            switch (_mode)
            {
                case TestMode.Wave:
                case TestMode.Solo:
                    ChangeRefBoss(0);
                    break;
                case TestMode.Boss:
                    ChangeRefTower(0);
                    if (refBoss.value == 0) ChangeRefBoss(1);
                    break;
            }

        Mode = Mode == _mode ? TestMode.None : _mode;
        OnClickReset();
        GameManager.Instance?.Replay();
    }

    private void AutoMerge()
    {
        List<Tower> towers = EntityManager.Instance?.GetTowers();
        if (towers == null || towers.Count < 2) return;

        int limitRank = refRank.value;
        if (limitRank < 1) limitRank = 1;

        for (int r = 1; r < limitRank; r++)
        {
            List<Tower> matches = new();

            for (int i = 0; i < towers.Count; i++)
            {
                Tower tower = towers[i];
                if (tower == null || tower.IsDragging || tower.IsMax) continue;
                if (tower.GetRank() != r) continue;

                matches.Add(tower);
            }

            for (int i = 0; i < matches.Count; i++)
            {
                Tower a = matches[i];
                if (a == null) continue;

                for (int j = i + 1; j < matches.Count; j++)
                {
                    Tower b = matches[j];
                    if (b == null) continue;
                    if (a.GetID() != b.GetID()) continue;

                    if (a.Merge(b) != null) return;
                }
            }
        }
    }

    private void MergeRandom()
    {
        List<Tower> towers = EntityManager.Instance?.GetTowers();
        if (towers == null || towers.Count < 2) return;

        HashSet<int> rankSet = new();
        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null || tower.IsDragging || tower.IsMax) continue;

            rankSet.Add(tower.GetRank());
        }

        List<int> ranks = new(rankSet);
        ranks.Sort();

        for (int r = 0; r < ranks.Count; r++)
        {
            int curRank = ranks[r];
            List<Tower> matches = new();

            for (int i = 0; i < towers.Count; i++)
            {
                Tower tower = towers[i];
                if (tower == null || tower.IsDragging || tower.IsMax) continue;
                if (tower.GetRank() != curRank) continue;

                matches.Add(tower);
            }

            int count = matches.Count;
            if (count < 2) continue;

            int start = Random.Range(0, count);

            for (int n = 0; n < count; n++)
            {
                Tower a = matches[(start + n) % count];
                if (a == null) continue;

                for (int m = 1; m < count; m++)
                {
                    Tower b = matches[(start + n + m) % count];
                    if (b == null) continue;
                    if (a.GetID() != b.GetID()) continue;

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
            basics[r] = new();

        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null || tower.IsDragging) continue;

            int id = tower.GetID();
            int rank = tower.GetRank();

            if (id == refID) target[rank]++;
            else if (id == 0) basics[rank].Add(tower);
        }

        for (int rank = 1; rank <= maxRank; rank++)
        {
            List<Tower> list = basics[rank];
            int need = target[rank];

            while (list.Count < need)
            {
                Vector3? pos = EntityManager.Instance?.GetLShapePos();
                if (!pos.HasValue) break;

                Tower spawned = EntityManager.Instance?.SpawnTower(0, rank, pos.Value, _useGold: false);
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
                    int last = list.Count - 1; Tower remove = list[last]; list.RemoveAt(last);

                    EntityManager.Instance?.DespawnTower(remove);
                }
            }
        }
    }

    public void AddDamage(int _damage) => runDamage += _damage;
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
        refBoss.maxValue = DataManager.Instance.GetBossDatas().Length;
        InitSlider(refBoss, ChangeRefBoss);
    }

    private void OnDisable()
    {
        gameSpeed.slider.onValueChanged.RemoveListener(ChangeGameSpeed);

        refRank.slider.onValueChanged.RemoveListener(ChangeRefRank);
        refTower.slider.onValueChanged.RemoveListener(ChangeRefTower);
        refBoss.slider.onValueChanged.RemoveListener(ChangeRefBoss);
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

    private void ChangeGameSpeed(float _value)
        => ApplySlider(ref gameSpeed, _value, _v => GameManager.Instance?.SetSpeed(_v, true));

    private void ChangeRefRank(float _value) => ApplySlider(ref refRank, _value);
    private void ChangeRefTower(float _value) => ApplySlider(ref refTower, _value, _v =>
    {
        if (Mode == TestMode.None) return;
        OnClickReset();
        GameManager.Instance?.Replay();
    });
    private void ChangeRefBoss(float _value) => ApplySlider(ref refBoss, _value, _v =>
    {
        if (_v == 0) { ToggleMode(TestMode.None); return; }
        OnClickReset();
        GameManager.Instance?.Replay();
    });

    private void UpdateTestText()
    {
        timer += Time.unscaledDeltaTime;
        frame++;

        if (timer < 0.1f) return;

        testText.text =
            $"FPS : {frame / timer:0.0}\n" +
            $"MS : {timer * 1000f / frame:0.0}\n\n" +
            $"Tower : {EntityManager.Instance?.GetTowerCount()}\n" +
            $"Monster : {EntityManager.Instance?.GetMonsterCount()}\n" +
            $"Others : {PoolManager.Instance?.GetOtherCount()}";

        timer = 0f;
        frame = 0;
    }

    private void UpdateTestUI()
    {
        int count = testResults.Count;

        List<long> values = new(count);
        long totalValue = 0;
        float totalPlay = 0f;
        double valueSqSum = 0d;

        for (int i = 0; i < count; i++)
        {
            TestResult r = testResults[i];

            values.Add(r.value);
            totalValue += r.value;
            totalPlay += r.playTime;
            valueSqSum += (double)r.value * r.value;
        }

        long topAvg = 0;
        long bottomAvg = 0;
        if (count > 0)
        {
            values.Sort();
            int group = Mathf.Max(Mathf.CeilToInt(count * 0.1f), 1);

            long sumBottom = 0;
            for (int i = 0; i < group; i++) sumBottom += values[i];

            long sumTop = 0;
            for (int i = count - group; i < count; i++) sumTop += values[i];

            bottomAvg = (long)System.Math.Round((double)sumBottom / group);
            topAvg = (long)System.Math.Round((double)sumTop / group);
        }

        long averageValue = count > 0 ? (long)System.Math.Round((double)totalValue / count) : 0;
        float averagePlay = count > 0 ? totalPlay / count : 0f;

        double cvValue = 0d;
        if (count > 1)
        {
            double meanValue = (double)totalValue / count;
            double varValue = valueSqSum / count - meanValue * meanValue;
            if (varValue < 0d) varValue = 0d;
            double stdValue = System.Math.Sqrt(varValue);
            cvValue = meanValue != 0d ? (stdValue / meanValue) * 100d : 0d;
        }

        int totalSeconds = Mathf.RoundToInt(averagePlay);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        testCountNum.text = count.ToString();
        averagePlayNum.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        averageValueName.text = Mode switch
        {
            TestMode.Solo => "누적 데미지",
            TestMode.Boss => "필요 타워 수",
            _ => "평균점수"
        };
        averageValueNum.text = Mode == TestMode.Boss
            ? $"{averageValue:#,0}"
            : $"{averageValue:#,0} ({cvValue:0.#}%)";
        value10Num.text = $"{topAvg:#,0} / {bottomAvg:#,0}";

        UpdateSliderUI(gameSpeed);
        UpdateSliderUI(refRank);
        UpdateSliderUI(refTower);
        UpdateSliderUI(refBoss);
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
        runDamage = 0;

        if (autoRoutine != null)
        {
            StopCoroutine(autoRoutine);
            autoRoutine = null;
        }

        needTower = 1;
        bossInit = false;

        UpdateTestUI();
    }
    public void OnClickReplay() => GameManager.Instance?.Replay();
    #endregion
}
#endif
