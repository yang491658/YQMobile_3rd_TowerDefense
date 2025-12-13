using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField][Min(0)] private int testCount = 0;
    [SerializeField][Min(0)] private int maxScore = 0;
    private int totalScore = 0;
    [SerializeField][Min(0)] private int averageScore = 0;
    [SerializeField] private bool isAuto = false;
    [SerializeField][Min(0f)] private float autoReplay = 0f;
    private Coroutine autoRoutine;

    [Header("Sound Test")]
    [SerializeField] private bool bgmPause = false;

    [Header("Entity Test")]
    [SerializeField] private bool spawn = true;
    [SerializeField] private float totalDPS = 0f;
    [Space]
    [SerializeField] private float overAverageDPS = 0f;
    [SerializeField] private int overCount = 0;
    [Space]
    [SerializeField] private float underMaxDPS = 0f;
    [SerializeField] private int underCount = 0;

    [Header("Test UI")]
    [SerializeField] private GameObject testUI;
    [Space]
    [SerializeField] private SliderConfig gameSpeed = new SliderConfig(1, 1, 10, "배속 × {0}");
    [Space]
    [SerializeField] private TextMeshProUGUI testCountNum;
    [SerializeField] private TextMeshProUGUI maxScoreNum;
    [SerializeField] private TextMeshProUGUI averageScoreNum;
    [SerializeField] private TextMeshProUGUI totalDPSText;
    [SerializeField] private TextMeshProUGUI overDPSText;
    [SerializeField] private TextMeshProUGUI underDPSText;
    [Space]
    [SerializeField] private SliderConfig refTower = new SliderConfig(0, 0, 1, "기준타워 : {0}");
    [SerializeField] private SliderConfig refRank = new SliderConfig(3, 1, 7, "기준랭크 : {0}");

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
        if (maxScoreNum == null)
            maxScoreNum = GameObject.Find("TestUI/MaxScore/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (averageScoreNum == null)
            averageScoreNum = GameObject.Find("TestUI/AverageScore/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (totalDPSText == null)
            totalDPSText = GameObject.Find("TestUI/TotalDPS/TestText")?.GetComponent<TextMeshProUGUI>();
        if (overDPSText == null)
            overDPSText = GameObject.Find("TestUI/OverDPS/TestText")?.GetComponent<TextMeshProUGUI>();
        if (underDPSText == null)
            underDPSText = GameObject.Find("TestUI/UnderDPS/TestText")?.GetComponent<TextMeshProUGUI>();

        if (refTower.TMP == null)
            refTower.TMP = GameObject.Find("TestUI/RefID/TestText")?.GetComponent<TextMeshProUGUI>();
        if (refTower.slider == null)
            refTower.slider = GameObject.Find("TestUI/RefID/TestSlider")?.GetComponent<Slider>();
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

        AutoPlay();
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
            bgmPause = !bgmPause;
            SoundManager.Instance?.PauseSound(bgmPause);
        }
        if (Input.GetKeyDown(KeyCode.M)) SoundManager.Instance?.ToggleBGM();
        if (Input.GetKeyDown(KeyCode.N)) SoundManager.Instance?.ToggleSFX();
        #endregion

        #region 엔티티 매니저
        for (int i = 1; i <= 10; i++)
        {
            KeyCode key = (i == 10) ? KeyCode.Alpha0 : (KeyCode)((int)KeyCode.Alpha0 + i);
            if (Input.GetKeyDown(key))
            {
                EntityManager.Instance?.SpawnTowerByIndex(i, 1, _useGold: false);
                break;
            }
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            spawn = !spawn;
            EntityManager.Instance?.ToggleSpawnMonster(spawn);
        }
        if (Input.GetKeyDown(KeyCode.T)) EntityManager.Instance?.SpawnTowerByIndex(refTower.value, 1, _useGold: false);
        if (Input.GetKeyDown(KeyCode.Y)) MergeTower();
        if (Input.GetKeyDown(KeyCode.U))
        {
            var list = EntityManager.Instance?.GetTowers();
            foreach (var tower in list)
                tower.RankUp();
        }

        if (Input.GetKeyDown(KeyCode.Delete)) EntityManager.Instance?.DespawnAll();
        #endregion

        #region UI 매니저
        if (Input.GetKeyDown(KeyCode.Z)) UIManager.Instance?.OpenSetting(!UIManager.Instance.GetOnSetting());
        if (Input.GetKeyDown(KeyCode.X)) UIManager.Instance?.OpenConfirm(!UIManager.Instance.GetOnConfirm());
        if (Input.GetKeyDown(KeyCode.C)) UIManager.Instance?.OpenResult(!UIManager.Instance.GetOnResult());
        #endregion

        #region 테스트 매니저
        if (Input.GetKeyDown(KeyCode.L)) GiveGold();
        if (Input.GetKeyDown(KeyCode.O)) AutoPlay();
        if (isAuto)
        {
            if (GameManager.Instance.IsGameOver)
            {
                if (autoRoutine == null)
                    autoRoutine = StartCoroutine(AutoReplay());
            }
            else
            {
                AutoMergeTower();
                UpdateTotalDPS();
                if (GameManager.Instance?.GetScore() > 1000)
                    GameManager.Instance?.GameOver();
                if (GameManager.Instance.EnoughGold())
                    if (EntityManager.Instance?.SpawnTowerByIndex(refTower.value) == null) MergeTower();
                if (EntityManager.Instance?.GetAttackTowers().Count <= 0)
                    GameManager.Instance?.Replay();
            }
        }
        if (Input.GetKeyDown(KeyCode.BackQuote)) OnClickTest();
        if (Input.GetKey(KeyCode.RightShift))
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
                ChangeGameSpeed(gameSpeed.value == gameSpeed.maxValue ? GameManager.Instance.GetMaxSpeed() : gameSpeed.maxValue);
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                ChangeGameSpeed(gameSpeed.value == gameSpeed.minValue ? GameManager.Instance.GetMaxSpeed() : gameSpeed.minValue);
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) ChangeGameSpeed(++gameSpeed.value);
            else if (Input.GetKeyDown(KeyCode.DownArrow)) ChangeGameSpeed(--gameSpeed.value);
        }
        #endregion
    }

    #region 테스트
    private void AutoPlay()
    {
        isAuto = !isAuto;

        GameManager.Instance?.SetSpeed(isAuto ? GameManager.Instance.GetMaxSpeed() : 1f);
    }

    private IEnumerator AutoReplay()
    {
        yield return new WaitForSecondsRealtime(autoReplay);
        if (GameManager.Instance.IsGameOver)
        {
            int score = GameManager.Instance.GetScore();

            totalScore += score;
            maxScore = Mathf.Max(score, maxScore);
            averageScore = totalScore / ++testCount;

            if (score > 1000)
            {
                float preTotal = overAverageDPS * overCount++;
                overAverageDPS = (preTotal + totalDPS) / overCount;
            }
            else underMaxDPS = Mathf.Max(totalDPS, underMaxDPS);

            UpdateTestUI();
            GameManager.Instance?.Replay();
        }
        autoRoutine = null;
    }

    private void GiveGold() => GameManager.Instance?.GoldUp(100_0000);

    private void AutoMergeTower()
    {
        var towers = EntityManager.Instance?.GetTowers();
        if (towers == null) return;

        int len = towers.Count;
        if (len < 2) return;

        int limitRank = refRank.value;
        if (limitRank < 1) limitRank = 1;

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

                    if (a.Merge(b, _index: refTower.value) != null) return;
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
            if (t == null || t.IsDragging) continue;
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
                if (t == null || t.IsDragging) continue;
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

                    if (a.Merge(b, _index: refTower.value) != null) return;
                }
            }
        }
    }
    #endregion

    #region 테스트 UI
    private void OnEnable()
    {
        gameSpeed.value = (int)GameManager.Instance.GetSpeed();
        InitSlider(gameSpeed, ChangeGameSpeed);
        refTower.maxValue = EntityManager.Instance.GetTowerDataCount();
        InitSlider(refTower, ChangeRefTower);
        InitSlider(refRank, ChangeRefRank);
    }

    private void OnDisable()
    {
        gameSpeed.slider.onValueChanged.RemoveListener(ChangeGameSpeed);
        refTower.slider.onValueChanged.RemoveListener(ChangeRefTower);
        refRank.slider.onValueChanged.RemoveListener(ChangeRefRank);
    }

    private void InitSlider(SliderConfig _config, UnityEngine.Events.UnityAction<float> _action)
    {
        if (_config.slider == null) return;

        _config.slider.minValue = _config.minValue;
        _config.slider.maxValue = _config.maxValue;
        _config.slider.wholeNumbers = true;

        float v = _config.value;
        if (v < _config.minValue) v = _config.minValue;
        else if (v > _config.maxValue) v = _config.maxValue;
        _config.slider.value = v;

        _action.Invoke(_config.slider.value);
        _config.slider.onValueChanged.AddListener(_action);
    }

    private int ChangeSlider(float _value, SliderConfig _config)
    {
        int v = Mathf.RoundToInt(_value);
        if (v < _config.minValue) v = _config.minValue;
        else if (v > _config.maxValue) v = _config.maxValue;
        return v;
    }

    private void ApplySlider(ref SliderConfig _config, float _value, System.Action<int> _afterAction = null)
    {
        _config.value = ChangeSlider(_value, _config);
        UpdateSliderUI(_config);
        _afterAction?.Invoke(_config.value);
    }

    private void UpdateSliderUI(SliderConfig _config)
    {
        if (string.IsNullOrEmpty(_config.format))
            _config.TMP.text = _config.value.ToString();
        else
            _config.TMP.text = string.Format(_config.format, _config.value);

        _config.slider.value = _config.value;
    }
    private void ChangeGameSpeed(float _value) => ApplySlider(ref gameSpeed, _value, _v => GameManager.Instance?.SetSpeed(_v, true));
    private void ChangeRefTower(float _value) => ApplySlider(ref refTower, _value);
    private void ChangeRefRank(float _value) => ApplySlider(ref refRank, _value);

    private void UpdateTestUI()
    {
        testCountNum.text = testCount.ToString();
        maxScoreNum.text = maxScore.ToString();
        averageScoreNum.text = averageScore.ToString();

        UpdateSliderUI(gameSpeed);
        UpdateSliderUI(refTower);
        UpdateSliderUI(refRank);

        UpdateTotalDPS();
        overDPSText.text = $"{overAverageDPS.ToString("#,0.0")} ({overCount.ToString()})";
        underDPSText.text = $"{underMaxDPS.ToString("#,0.0")} ({underCount.ToString()})";
    }

    private void UpdateTotalDPS()
    {
        List<Tower> towers = EntityManager.Instance?.GetAttackTowers();

        if (towers == null || towers.Count == 0)
        {
            totalDPS = 0f;
        }
        else
        {
            float sumDps = 0f;

            for (int i = 0; i < towers.Count; i++)
            {
                Tower t = towers[i];
                if (t == null) continue;

                float damage = t.GetDamage();
                float speed = t.GetSpeed();
                float critChance = t.GetCriticalChance();
                float critDamage = t.GetCriticalDamage();

                if (speed <= 0f || damage <= 0f)
                    continue;

                float chance = critChance / 100f;
                float critMul = critDamage / 100f;
                float expectedPerHit = damage * (1f + (critMul - 1f) * chance);
                float attacksPerSecond = speed / 60f;
                float dps = expectedPerHit * attacksPerSecond;

                sumDps += dps;
            }

            totalDPS = sumDps;
        }

        totalDPSText.text = totalDPS.ToString("#,0.0");
    }

    public void OnClickTest()
    {
        testUI.SetActive(!testUI.activeSelf);
        UpdateTestUI();
    }
    public void OnClickReset()
    {
        testCount = 0;
        maxScore = 0;
        totalScore = 0;
        averageScore = 0;

        overAverageDPS = 0f;
        overCount = 0;
        underMaxDPS = 0f;
        underCount = 0;

        UpdateTestUI();
    }
    public void OnClickReplay() => GameManager.Instance?.Replay();
    #endregion
}
