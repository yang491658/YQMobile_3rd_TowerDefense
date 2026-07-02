using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { private set; get; }

    public event System.Action<bool> OnOpenUI;
    private static readonly string[] units = { "K", "M", "B", "T" };

    public static float BannerHeightPx { private set; get; } = 0f;

    [Header("Count UI")]
    [SerializeField] private TextMeshProUGUI countText;
    private Coroutine countRoutine;
    [SerializeField][Min(0)] private int countStart = 3;
    [SerializeField][Min(0f)] private float countDuration = 1f;
    [SerializeField][Min(0f)] private float countScale = 10f;
    [SerializeField] private bool countSkip = true;

    [Header("InGame UI")]
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private TextMeshProUGUI playTimeText;
    private bool onPlayTime = false;
    private float playTime = 0f;
    private int playTimeSec = -1;
    [SerializeField] private TextMeshProUGUI scoreNum;

    [Header("InGame UI / Wave")]
    [SerializeField] private GameObject waveUI;
    [SerializeField] private SliderUI wave;

    [Header("InGame UI / Player")]
    [SerializeField] private RectTransform mapUI;
    [SerializeField] private RectTransform playerUI;
    private float playerHeight = 0f;
    private bool onStore = false;
    private int storeGold = 0;
    private Image storeImage;
    private Color storeColor;
    [Space]
    [SerializeField] private SliderUI life;
    [SerializeField] private SliderUI exp;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private GameObject goldImage;
    [SerializeField] private GameObject loanImage;
    [SerializeField] private TextMeshProUGUI[] chanceText;

    [Header("InGame UI / Drag")]
    [SerializeField] private RectTransform drag;
    [SerializeField] private Image dragOutline;
    [SerializeField] private Image dragSymbol;

    [System.Serializable]
    private struct SliderUI
    {
        public Slider slider;
        [HideInInspector] public Image fill;
        [HideInInspector] public Color color;
        [HideInInspector] public int prev;

        public TextMeshProUGUI text;
        public Button btn;

        [HideInInspector] public Coroutine routine;
    }

    [Header("Setting UI")]
    [SerializeField] private GameObject settingUI;
    [SerializeField] private TextMeshProUGUI settingScoreNum;
    [SerializeField] private Slider speedSlider;

    [Header("Sound UI")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Image bgmIcon;
    [SerializeField] private Image sfxIcon;
    [SerializeField] private List<Sprite> bgmIcons = new();
    [SerializeField] private List<Sprite> sfxIcons = new();

    [Header("Confirm UI")]
    [SerializeField] private GameObject confirmUI;
    [SerializeField] private TextMeshProUGUI confirmTitle;
    private System.Action confirmAction;

    [Header("Result UI")]
    [SerializeField] private GameObject resultUI;
    [SerializeField] private TextMeshProUGUI resultScoreNum;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (countText == null)
            countText = GameObject.Find("CountText")?.GetComponent<TextMeshProUGUI>();

        if (inGameUI == null)
            inGameUI = GameObject.Find("InGameUI");
        if (playTimeText == null)
            playTimeText = GameObject.Find("InGameUI/Score/PlayTimeText")?.GetComponent<TextMeshProUGUI>();
        if (scoreNum == null)
            scoreNum = GameObject.Find("InGameUI/Score/ScoreNum")?.GetComponent<TextMeshProUGUI>();

        if (waveUI == null)
            waveUI = GameObject.Find("InGameUI/Wave");
        if (wave.slider == null)
            wave.slider = GameObject.Find("InGameUI/Wave/WaveSlider")?.GetComponent<Slider>();
        if (wave.text == null)
            wave.text = GameObject.Find("InGameUI/Wave/WaveText")?.GetComponent<TextMeshProUGUI>();

        if (mapUI == null)
            mapUI = GameObject.Find("InGameUI/Map")?.GetComponent<RectTransform>();
        if (playerUI == null)
            playerUI = GameObject.Find("InGameUI/Player")?.GetComponent<RectTransform>();

        if (life.slider == null)
            life.slider = GameObject.Find("InGameUI/Player/Life/LifeSlider")?.GetComponent<Slider>();
        if (life.text == null)
            life.text = GameObject.Find("InGameUI/Player/Life/LifeSlider/LifeText")?.GetComponent<TextMeshProUGUI>();

        if (exp.slider == null)
            exp.slider = GameObject.Find("InGameUI/Player/Exp/ExpSlider")?.GetComponent<Slider>();
        if (exp.text == null)
            exp.text = GameObject.Find("InGameUI/Player/Exp/ExpSlider/ExpText")?.GetComponent<TextMeshProUGUI>();
        if (exp.btn == null)
            exp.btn = GameObject.Find("InGameUI/Player/Exp/ExpBtn")?.GetComponent<Button>();

        if (levelText == null)
            levelText = GameObject.Find("InGameUI/Player/Level+Gold/LevelText")?.GetComponent<TextMeshProUGUI>();
        if (goldText == null)
            goldText = GameObject.Find("InGameUI/Player/Level+Gold/GoldText")?.GetComponent<TextMeshProUGUI>();
        if (goldImage == null)
            goldImage = GameObject.Find("InGameUI/Player/Level+Gold/GoldImage");
        if (loanImage == null)
            loanImage = GameObject.Find("InGameUI/Player/Level+Gold/LoanImage");
        if (chanceText == null || chanceText.Length == 0)
            chanceText = GameObject.Find("InGameUI/Player/Tower/Chance").GetComponentsInChildren<TextMeshProUGUI>();

        if (drag == null)
            drag = GameObject.Find("InGameUI/Drag")?.GetComponent<RectTransform>();
        if (dragOutline == null)
            dragOutline = GameObject.Find("InGameUI/Drag/Outline")?.GetComponent<Image>();
        if (dragSymbol == null)
            dragSymbol = GameObject.Find("InGameUI/Drag/Symbol")?.GetComponent<Image>();

        if (settingUI == null)
            settingUI = GameObject.Find("SettingUI");
        if (settingScoreNum == null)
            settingScoreNum = GameObject.Find("SettingUI/Box/Score/ScoreNum")?.GetComponent<TextMeshProUGUI>();
        if (speedSlider == null)
            speedSlider = GameObject.Find("Speed/SpeedSlider")?.GetComponent<Slider>();

        if (bgmSlider == null)
            bgmSlider = GameObject.Find("BGM/BgmSlider")?.GetComponent<Slider>();
        if (sfxSlider == null)
            sfxSlider = GameObject.Find("SFX/SfxSlider")?.GetComponent<Slider>();
        if (bgmIcon == null)
            bgmIcon = GameObject.Find("BGM/BgmBtn/BgmIcon")?.GetComponent<Image>();
        if (sfxIcon == null)
            sfxIcon = GameObject.Find("SFX/SfxBtn/SfxIcon")?.GetComponent<Image>();

        bgmIcons.Clear();
        LoadSprite(bgmIcons, "Music");
        LoadSprite(bgmIcons, "Music Off");
        sfxIcons.Clear();
        LoadSprite(sfxIcons, "Sound On");
        LoadSprite(sfxIcons, "Sound Icon");
        LoadSprite(sfxIcons, "Sound Off 2");

        if (confirmUI == null)
            confirmUI = GameObject.Find("ConfirmUI");
        if (confirmTitle == null)
            confirmTitle = GameObject.Find("ConfirmUI/Box/ConfirmTitle")?.GetComponent<TextMeshProUGUI>();

        if (resultUI == null)
            resultUI = GameObject.Find("ResultUI");
        if (resultScoreNum == null)
            resultScoreNum = GameObject.Find("ResultUI/Score/ScoreNum")?.GetComponent<TextMeshProUGUI>();
    }

    private static void LoadSprite(List<Sprite> _list, string _sprite)
    {
        if (string.IsNullOrEmpty(_sprite)) return;
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Imports/Dark UI/Icons" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in assets)
            {
                if (obj is Sprite s && s.name == _sprite)
                {
                    _list.Add(s);
                    return;
                }
            }
        }
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

        wave.fill = wave.slider.fillRect.GetComponent<Image>();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(playerUI);
        playerHeight = playerUI.rect.height;
        storeImage = playerUI.GetComponent<Image>();
        storeColor = storeImage.color;

        life.fill = life.slider.fillRect.GetComponent<Image>();
        life.color = life.fill.color;
        exp.fill = exp.slider.fillRect.GetComponent<Image>();
        exp.color = exp.fill.color;
    }

    private void Start()
    {
        ResetUI();
    }

    private void Update()
    {
        if (GameManager.Instance.IsGameOver) return;

        if (onPlayTime)
            onPlayTime = false;
        else
            playTime += Time.unscaledDeltaTime;

        UpdatePlayTime();
        UpdateWave();

        if (exp.btn.gameObject.activeSelf)
            exp.btn.interactable = GameManager.Instance.CanBuyExp;
    }

    private void OnEnable()
    {
        GameManager.Instance.OnChangeSpeed += UpdateSpeed;
        speedSlider.minValue = GameManager.Instance.MinSpeed;
        speedSlider.maxValue = GameManager.Instance.MaxSpeed;
        speedSlider.value = GameManager.Instance.Speed;
        speedSlider.onValueChanged.AddListener(GameManager.Instance.SetSpeed);

        GameManager.Instance.OnChangeScore += UpdateScore;
        GameManager.Instance.OnChangeLife += UpdateLife;
        life.slider.maxValue = GameManager.Instance.MaxLife;
        life.slider.value = GameManager.Instance.Life;
        GameManager.Instance.OnChangeExp += UpdateExp;
        GameManager.Instance.OnChangeLevel += UpdateLevel;
        GameManager.Instance.OnChangeGold += UpdateGold;

        SoundManager.Instance.OnChangeVolume += UpdateVolume;
        bgmSlider.value = SoundManager.Instance.BGMVolume;
        bgmSlider.onValueChanged.AddListener(SoundManager.Instance.SetBGMVolume);
        sfxSlider.value = SoundManager.Instance.SFXVolume;
        sfxSlider.onValueChanged.AddListener(SoundManager.Instance.SetSFXVolume);

        OnOpenUI += GameManager.Instance.Pause;
        OnOpenUI += SoundManager.Instance.PauseSFXLoop;

        UpdateSoundIcon();
    }

    private void OnDisable()
    {
        GameManager.Instance.OnChangeSpeed -= UpdateSpeed;
        speedSlider.onValueChanged.RemoveListener(GameManager.Instance.SetSpeed);

        GameManager.Instance.OnChangeScore -= UpdateScore;
        GameManager.Instance.OnChangeLife -= UpdateLife;
        GameManager.Instance.OnChangeExp -= UpdateExp;
        GameManager.Instance.OnChangeLevel -= UpdateLevel;
        GameManager.Instance.OnChangeGold -= UpdateGold;

        SoundManager.Instance.OnChangeVolume -= UpdateVolume;
        bgmSlider.onValueChanged.RemoveListener(SoundManager.Instance.SetBGMVolume);
        sfxSlider.onValueChanged.RemoveListener(SoundManager.Instance.SetSFXVolume);

        OnOpenUI -= GameManager.Instance.Pause;
        OnOpenUI -= SoundManager.Instance.PauseSFXLoop;
    }

    #region 기타
    private void StartCountdown()
    {
        if (countRoutine != null) StopCoroutine(countRoutine);
        countRoutine = StartCoroutine(CountCoroutine());
    }

    private IEnumerator CountCoroutine()
    {
        if (!countSkip)
        {
            GameManager.Instance?.Pause(true);
            SoundManager.Instance?.StopBGM();
            inGameUI.SetActive(false);

            float duration = countDuration;
            float maxScale = countScale;

            countText.gameObject.SetActive(true);

            for (int i = countStart; i > 0; i--)
            {
                countText.text = i.ToString();
                countText.rectTransform.localScale = Vector3.one;

                SoundManager.Instance?.PlaySFX("Count");

                float start = Time.realtimeSinceStartup;

                while (true)
                {
                    float elapsed = Time.realtimeSinceStartup - start;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float scale = 1f + Mathf.Sin(t * Mathf.PI) * (maxScale - 1f);
                    countText.rectTransform.localScale = Vector3.one * scale;

                    if (elapsed >= duration) break;

                    yield return null;
                }
            }
        }

        countText.gameObject.SetActive(false);
        countText.rectTransform.localScale = Vector3.one;

        GameManager.Instance?.Pause(false);
        SoundManager.Instance?.PlayBGM("Default");
        inGameUI.SetActive(true);

        countRoutine = null;
    }

    public string FormatNumber(int _number, bool _full = false)
    {
        long number = _number;
        bool negative = number < 0L;
        if (negative) number = -number;

        if (number < 10000L)
        {
            string text = _full ? number.ToString("0000") : number.ToString();
            return negative ? $"-{text}" : text;
        }

        double value = number;
        int unitIndex = -1;

        while (value >= 1000d && unitIndex < units.Length - 1)
        {
            value /= 1000d;
            unitIndex++;
        }

        string result;
        if (value >= 100d)
            result = Mathf.RoundToInt((float)value).ToString() + units[unitIndex];
        else if (value >= 10d)
            result = value.ToString("0.0") + units[unitIndex];
        else
            result = value.ToString("0.00") + units[unitIndex];

        return negative ? "-" + result : result;
    }
    #endregion

    #region 오픈
    private void OpenUI(bool _on)
    {
        OpenResult(_on);
        OpenConfirm(_on);
        OpenSetting(_on);
    }

    public void OpenSetting(bool _on)
    {
        if (settingUI == null) return;

        inGameUI.SetActive(!_on);
        settingUI.SetActive(_on);
        OnOpenUI?.Invoke(_on);
    }

    public void OpenConfirm(bool _on, string _text = null, System.Action _action = null, bool _pass = false)
    {
        if (confirmUI == null) return;

        if (_pass)
        {
            confirmUI.SetActive(false);
            confirmTitle.text = string.Empty;
            confirmAction = null;
            _action?.Invoke();
            return;
        }

        confirmUI.SetActive(_on);
        confirmTitle.text = _on ? $"{_text}하시겠습니까?" : string.Empty;
        confirmAction = _on ? _action : null;
    }

    public void OpenResult(bool _on)
    {
        if (resultUI == null) return;

        inGameUI.SetActive(!_on);
        resultUI.SetActive(_on);
        OnOpenUI?.Invoke(_on);
    }
    #endregion

    #region 업데이트
    public void ResetUI()
    {
        onPlayTime = true;
        playTime = 0f;
        playTimeSec = -1;

        ResetSlider(ref life);
        ResetSlider(ref exp);

        UpdateScore(GameManager.Instance.Score);
        UpdateLife(GameManager.Instance.Life, GameManager.Instance.MaxLife);
        UpdateExp(GameManager.Instance.Exp, GameManager.Instance.NeedExp);
        UpdateLevel(GameManager.Instance.Level);
        UpdateStore(false);
        UpdateDrag(null);

        OpenUI(false);
        StartCountdown();
    }

    private void ResetSlider(ref SliderUI _slider)
    {
        _slider.fill.color = _slider.color;
        _slider.prev = int.MinValue;
        _slider.text.color = Color.white;

        if (_slider.routine != null)
            StopCoroutine(_slider.routine);
        _slider.routine = null;
    }

    private void UpdateSpeed(float _speed)
    {
        float value = Mathf.Clamp(_speed, speedSlider.minValue, speedSlider.maxValue);

        if (!Mathf.Approximately(speedSlider.value, value))
            speedSlider.SetValueWithoutNotify(value);
    }

    private void UpdatePlayTime()
    {
        int total = Mathf.FloorToInt(playTime);
        if (total == playTimeSec) return;
        playTimeSec = total;

        playTimeText.text = $"{total / 60:00}:{total % 60:00}";
    }

    private void UpdateScore(int _score)
    {
        string s = FormatNumber(_score, true);
        scoreNum.text = s;
        settingScoreNum.text = s;
        resultScoreNum.text = s;
    }

    private void UpdateWave()
    {
        if (!MonsterWave.Instance.IsRunning)
        {
            waveUI.SetActive(false);
            return;
        }
        waveUI.SetActive(true);

        MonsterWave.Instance.GetPhaseValue(out Color color, out float value, out float maxValue, out string text);
        wave.fill.color = color;
        wave.slider.value = value;
        wave.slider.maxValue = maxValue;

        bool onText = !string.IsNullOrEmpty(text);
        wave.text.gameObject.SetActive(onText);
        if (onText) wave.text.text = text;
    }

    private IEnumerator FlashCoroutine(SliderUI _ui, Color? _color = null, float _duration = 0.3f)
    {
        Color color = _color.HasValue ? _color.Value : Color.white;
        _ui.fill.color = color;
        _ui.text.color = Color.black;

        yield return new WaitForSecondsRealtime(0.05f);

        float start = Time.realtimeSinceStartup;
        while (true)
        {
            float t = (Time.realtimeSinceStartup - start) / _duration;
            if (t >= 1f) break;

            _ui.fill.color = Color.Lerp(color, _ui.color, t);
            _ui.text.color = Color.Lerp(Color.black, Color.white, t);
            yield return null;
        }

        _ui.fill.color = _ui.color;
        _ui.text.color = Color.white;
    }

    private void UpdateSlider(ref SliderUI _ui, int _value, int _maxValue, bool _btn, bool _flash = true)
    {
        _ui.slider.maxValue = _maxValue;
        _ui.slider.value = Mathf.Min(_value, _maxValue);
        _ui.text.text = $"{FormatNumber(_value)} / {FormatNumber(_maxValue)}";
        if (_ui.btn != null) _ui.btn.interactable = _btn;

        if (_ui.prev == int.MinValue)
        { _ui.prev = _value; return; }

        if (_value == _ui.prev) return;
        _ui.prev = _value;

        if (!_flash) return;

        if (_ui.routine != null)
            StopCoroutine(_ui.routine);
        _ui.routine = StartCoroutine(FlashCoroutine(_ui));
    }

    private void UpdateLife(int _life, int _maxLife)
    {
        int prev = life.prev;

        UpdateSlider(ref life, _life, _maxLife, false, prev != int.MinValue && _life > prev);

        if (prev == int.MinValue || _life >= prev) return;

        if (life.routine != null)
            StopCoroutine(life.routine);
        life.routine = StartCoroutine(FlashCoroutine(life, Color.black, GameManager.Instance.LifeCooldown));
    }

    private void UpdateExp(int _exp, int _needExp)
    {
        if (GameManager.Instance.IsMaxLevel)
        {
            exp.slider.maxValue = 1;
            exp.slider.value = 1;
            ResetSlider(ref exp);
            return;
        }

        UpdateSlider(ref exp, _exp, _needExp, GameManager.Instance.CanBuyExp);
    }

    private void UpdateLevel(int _level)
    {
        bool isMax = GameManager.Instance.IsMaxLevel;

        exp.text.gameObject.SetActive(!isMax);
        exp.btn.gameObject.SetActive(!isMax);
        levelText.text = isMax ? "Lv.MAX" : $"Lv.{_level}";

        UpdateChance();
    }

    private void UpdateGold(int _gold, int _needGold)
    {
        string need = FormatNumber(_needGold);

        if (onStore)
        {
            int showGold = _gold + storeGold;
            goldText.text = $"{FormatNumber(showGold)}(+{FormatNumber(storeGold)})/{need}";
            goldText.color = showGold >= 0 ? Color.blue : Color.red;
        }
        else
        {
            goldText.text = $"{FormatNumber(_gold)}/{need}";
            goldText.color = _gold >= 0 ? Color.white : Color.red;
        }

        goldImage.SetActive(_gold >= 0);
        loanImage.SetActive(_gold < 0);
    }

    public bool IsStore(Vector3 _pos)
    {
        Camera cam = Camera.main;
        Vector3 pos = cam.WorldToScreenPoint(_pos);
        return RectTransformUtility.RectangleContainsScreenPoint(playerUI, pos);
    }

    public void UpdateStore(bool _on, int _gold = 0)
    {
        onStore = _on;
        storeGold = _gold;
        storeImage.color = onStore ? Color.cyan : storeColor;

        UpdateGold(GameManager.Instance.Gold, GameManager.Instance.NeedGold);
    }

    private void UpdateChance()
    {
        int i = -1;
        foreach (KeyValuePair<TowerGrade, float> chance in TowerChance.Instance?.GetChances())
        {
            if (++i >= chanceText.Length) return;

            chanceText[i].text = $"{chance.Value:0}%";
            chanceText[i].color = DataManager.Instance.GetTowerColor(chance.Key);
        }
    }

    public void UpdateDrag(Tower _tower, Vector3 _worldPos = default)
    {
        if (_tower == null)
        {
            drag.gameObject.SetActive(false);
            drag.localScale = Vector3.one;
            return;
        }

        if (!drag.gameObject.activeSelf)
        {
            _tower.SetDrag(dragOutline, dragSymbol);
            drag.gameObject.SetActive(true);
        }

        Vector3 scale = _tower.transform.lossyScale;
        drag.localScale = new Vector3(scale.x, scale.y, 1f);
        drag.position = RectTransformUtility.WorldToScreenPoint(Camera.main, _worldPos);
    }

    private void UpdateVolume(SoundType _type, float _volume)
    {
        switch (_type)
        {
            case SoundType.BGM:
                if (!Mathf.Approximately(bgmSlider.value, _volume))
                    bgmSlider.SetValueWithoutNotify(_volume);
                break;

            case SoundType.SFX:
                if (!Mathf.Approximately(sfxSlider.value, _volume))
                    sfxSlider.SetValueWithoutNotify(_volume);
                break;

            default: return;
        }
        UpdateSoundIcon();
    }

    private void UpdateSoundIcon()
    {
        if (bgmIcons.Count >= 2)
            bgmIcon.sprite = SoundManager.Instance.IsBGMMuted ? bgmIcons[1] : bgmIcons[0];

        if (sfxIcons.Count >= 3)
        {
            if (SoundManager.Instance.IsSFXMuted)
                sfxIcon.sprite = sfxIcons[2];
            else if (SoundManager.Instance?.SFXVolume < 0.2f)
                sfxIcon.sprite = sfxIcons[1];
            else
                sfxIcon.sprite = sfxIcons[0];
        }
    }
    #endregion

    #region 버튼
    public void OnClickSetting() => OpenSetting(true);
    public void OnClickExp() => GameManager.Instance?.BuyExp();

    public void OnClickClose() => OpenUI(false);
    public void OnClickSpeed() => speedSlider.value = speedSlider.value != 1f ? 1f : speedSlider.maxValue;
    public void OnClickBGM() => SoundManager.Instance?.ToggleBGM();
    public void OnClickSFX() => SoundManager.Instance?.ToggleSFX();

    public void OnClickReplay() => OpenConfirm(true, "다시", GameManager.Instance.Replay);
    public void OnClickQuit() => OpenConfirm(true, "종료", GameManager.Instance.Quit);

    public void OnClickOkay()
    {
        System.Action action = confirmAction;
        OpenConfirm(false);
        action?.Invoke();
    }
    public void OnClickCancel() => OpenConfirm(false);

    public void OnClickReplayDirect() => OpenConfirm(true, "다시", GameManager.Instance.Replay, true);
    public void OnClickQuitDirect() => OpenConfirm(true, "종료", GameManager.Instance.Quit, true);
    #endregion

    #region SET
    public void SetSkipCountdown(bool _skip) => countSkip = _skip;
    public void SetMargin(float _margin)
    {
        RectTransform rt = inGameUI.GetComponent<RectTransform>();
        rt.offsetMax = new Vector2(rt.offsetMax.x, -_margin);

        Canvas canvas = inGameUI.GetComponentInParent<Canvas>();
        BannerHeightPx = _margin * canvas.scaleFactor;

        EntityManager.Instance?.SetEntity();
    }
    #endregion

    #region GET
    public Rect GetMapAreaRect(float _z = 0f)
    {
        Rect rect = mapUI.rect;
        Canvas canvas = mapUI.GetComponentInParent<Canvas>();
        Camera uiCam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = canvas.worldCamera;

        Camera worldCam = Camera.main;
        float depth = Mathf.Abs(_z - worldCam.transform.position.z);

        Vector3 minCorner = mapUI.TransformPoint(new Vector3(rect.xMin, rect.yMin, 0f));
        Vector3 maxCorner = mapUI.TransformPoint(new Vector3(rect.xMax, rect.yMax, 0f));

        Vector3 minScreen = RectTransformUtility.WorldToScreenPoint(uiCam, minCorner);
        Vector3 maxScreen = RectTransformUtility.WorldToScreenPoint(uiCam, maxCorner);

        Vector3 minWorld = worldCam.ScreenToWorldPoint(new Vector3(minScreen.x, minScreen.y, depth));
        Vector3 maxWorld = worldCam.ScreenToWorldPoint(new Vector3(maxScreen.x, maxScreen.y, depth));

        return Rect.MinMaxRect(minWorld.x, minWorld.y, maxWorld.x, maxWorld.y);
    }

    public Vector3 GetPlayerOffset(float _z = 0f)
    {
        UpdateWave();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(playerUI);

        float height = playerHeight - playerUI.rect.height;
        if (Mathf.Approximately(height, 0f))
            return Vector3.zero;

        float scaleFactor = playerUI.GetComponentInParent<Canvas>().scaleFactor;
        float heightPx = height * scaleFactor;

        Camera worldCam = Camera.main;
        float depth = Mathf.Abs(_z - worldCam.transform.position.z);

        Vector3 origin = worldCam.ScreenToWorldPoint(new Vector3(0f, 0f, depth));
        Vector3 move = worldCam.ScreenToWorldPoint(new Vector3(0f, heightPx, depth));

        return origin - move;
    }
    #endregion

#if TEST_Manager
    #region 프로퍼티
    public bool OnSetting => settingUI.activeSelf;
    public bool OnConfirm => confirmUI.activeSelf;
    public bool OnResult => resultUI.activeSelf;
    #endregion
#endif
}
