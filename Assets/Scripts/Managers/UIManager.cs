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

    [Header("Count UI")]
    [SerializeField] private TextMeshProUGUI countText;
    private Coroutine countRoutine;
    [SerializeField] private int countStart = 3;
    [SerializeField] private float countDuration = 1f;
    [SerializeField] private float countScale = 10f;
    [SerializeField] private bool countSkip = true;

    [Header("InGame UI")]
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private TextMeshProUGUI playTimeText;
    private bool onPlayTime = false;
    private float playTime = 0f;
    [SerializeField] private TextMeshProUGUI scoreNum;
    [Space]
    [SerializeField] private TextMeshProUGUI lifeText;
    [Space]
    [SerializeField] private Button goldbtn;
    [SerializeField] private TextMeshProUGUI currentGoldNum;
    [SerializeField] private TextMeshProUGUI needGoldNum;

    [Header("Setting UI")]
    [SerializeField] private GameObject settingUI;
    [SerializeField] private TextMeshProUGUI settingScoreNum;
    [SerializeField] private Slider speedSlider;

    [Header("Sound UI")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Image bgmIcon;
    [SerializeField] private Image sfxIcon;
    [SerializeField] private List<Sprite> bgmIcons = new List<Sprite>();
    [SerializeField] private List<Sprite> sfxIcons = new List<Sprite>();

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

        if (lifeText == null)
            lifeText = GameObject.Find("InGameUI/Life/LifeText")?.GetComponent<TextMeshProUGUI>();

        if (goldbtn == null)
            goldbtn = GameObject.Find("InGameUI/GoldBtn")?.GetComponent<Button>();
        if (currentGoldNum == null)
            currentGoldNum = GameObject.Find("InGameUI/GoldBtn/CurrentGoldNum")?.GetComponent<TextMeshProUGUI>();
        if (needGoldNum == null)
            needGoldNum = GameObject.Find("InGameUI/GoldBtn/NeedGoldNum")?.GetComponent<TextMeshProUGUI>();

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
        LoadSprite(bgmIcons, "White Music");
        LoadSprite(bgmIcons, "White Music Off");
        sfxIcons.Clear();
        LoadSprite(sfxIcons, "White Sound On");
        LoadSprite(sfxIcons, "White Sound Icon");
        LoadSprite(sfxIcons, "White Sound Off 2");

        if (confirmUI == null)
            confirmUI = GameObject.Find("ConfirmUI");
        if (confirmTitle == null)
            confirmTitle = GameObject.Find("ConfirmUI/Box/ConfirmTitle")?.GetComponent<TextMeshProUGUI>();

        if (resultUI == null)
            resultUI = GameObject.Find("ResultUI");
        if (resultScoreNum == null)
            resultScoreNum = GameObject.Find("ResultUI/Score/ScoreNum")?.GetComponent<TextMeshProUGUI>();
    }

    static private void LoadSprite(List<Sprite> _list, string _sprite)
    {
        if (string.IsNullOrEmpty(_sprite)) return;
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Imports/Dark UI/Icons" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in assets)
            {
                var s = obj as Sprite;
                if (s != null && s.name == _sprite)
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
    }

    private void Start()
    {
        UpdateScore(GameManager.Instance.GetScore());
        UpdateLife(GameManager.Instance.GetLife());
        UpdateGold(GameManager.Instance.GetGold());
    }

    private void Update()
    {
        if (GameManager.Instance.IsGameOver) return;

        if (onPlayTime)
            onPlayTime = false;
        else
            playTime += Time.unscaledDeltaTime;

        UpdatePlayTime();
    }

    private void OnEnable()
    {
        GameManager.Instance.OnChangeSpeed += UpdateSpeed;
        speedSlider.minValue = GameManager.Instance.GetMinSpeed();
        speedSlider.maxValue = GameManager.Instance.GetMaxSpeed();
        speedSlider.wholeNumbers = false;
        speedSlider.value = GameManager.Instance.GetSpeed();
        speedSlider.onValueChanged.AddListener(GameManager.Instance.SetSpeed);

        GameManager.Instance.OnChangeScore += UpdateScore;
        GameManager.Instance.OnChangeLife += UpdateLife;
        GameManager.Instance.OnChangeGold += UpdateGold;

        SoundManager.Instance.OnChangeVolume += UpdateVolume;
        bgmSlider.value = SoundManager.Instance.GetBGMVolume();
        bgmSlider.onValueChanged.AddListener(SoundManager.Instance.SetBGMVolume);
        sfxSlider.value = SoundManager.Instance.GetSFXVolume();
        sfxSlider.onValueChanged.AddListener(SoundManager.Instance.SetSFXVolume);

        OnOpenUI += GameManager.Instance.Pause;
        OnOpenUI += SoundManager.Instance.PauseSFXLoop;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnChangeSpeed -= UpdateSpeed;
        speedSlider.onValueChanged.RemoveListener(GameManager.Instance.SetSpeed);

        GameManager.Instance.OnChangeScore -= UpdateScore;
        GameManager.Instance.OnChangeLife -= UpdateLife;
        GameManager.Instance.OnChangeGold -= UpdateGold;

        SoundManager.Instance.OnChangeVolume -= UpdateVolume;
        bgmSlider.onValueChanged.RemoveListener(SoundManager.Instance.SetBGMVolume);
        sfxSlider.onValueChanged.RemoveListener(SoundManager.Instance.SetSFXVolume);

        OnOpenUI -= GameManager.Instance.Pause;
        OnOpenUI -= SoundManager.Instance.PauseSFXLoop;
    }

    #region 기타
    public void StartCountdown()
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

                    if (elapsed >= duration)
                        break;

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

    private string FormatNumber(int _number, bool _full)
    {
        if (_full && _number < 10000)
            return _number.ToString("0000");

        for (int i = units.Length; i > 0; i--)
        {
            float n = Mathf.Pow(1000f, i);
            if (_number >= n)
            {
                float value = _number / n;

                if (value >= 100f)
                    return ((int)value).ToString() + units[i - 1];

                if (value >= 10f)
                {
                    float v10 = Mathf.Floor(value * 10f) / 10f;
                    return v10.ToString("0.0") + units[i - 1];
                }

                float v100 = Mathf.Floor(value * 100f) / 100f;
                return v100.ToString("0.00") + units[i - 1];
            }
        }

        return _full ? _number.ToString("0000") : _number.ToString();
    }
    #endregion

    #region 오픈
    public void OpenUI(bool _on)
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

        if (!_pass)
        {
            confirmUI.SetActive(_on);
            if (_on)
            {
                confirmTitle.text = $"{_text}하시겠습니까?";
                confirmAction = _action;
            }
        }

        if (!_on)
        {
            confirmTitle.text = string.Empty;
            confirmAction = null;
        }

        if (_pass) _action?.Invoke();
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
        playTime = 0;
        onPlayTime = true;
        UpdatePlayTime();
        UpdateGold(GameManager.Instance.GetGold());
    }

    public void UpdateSpeed(float _speed)
    {
        if (!Mathf.Approximately(speedSlider.value, _speed))
            speedSlider.value = _speed;
    }

    public void UpdatePlayTime()
    {
        int total = Mathf.FloorToInt(playTime);
        string s = (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        playTimeText.text = s;
    }

    public void UpdateScore(int _score)
    {
        string s = FormatNumber(_score, true);
        scoreNum.text = s;
        settingScoreNum.text = s;
        resultScoreNum.text = s;
    }

    public void UpdateLife(int _life)
    => lifeText.text = _life.ToString("0000");

    public void UpdateGold(int _gold)
    {
        currentGoldNum.text = FormatNumber(_gold, false);
        needGoldNum.text = "/ " + FormatNumber(GameManager.Instance.GetNeedGold(), false);

        goldbtn.interactable = GameManager.Instance.EnoughGold();
    }

    public void UpdateVolume(SoundType _type, float _volume)
    {
        switch (_type)
        {
            case SoundType.BGM:
                if (!Mathf.Approximately(bgmSlider.value, _volume))
                    bgmSlider.value = _volume;
                break;

            case SoundType.SFX:
                if (!Mathf.Approximately(sfxSlider.value, _volume))
                    sfxSlider.value = _volume;
                break;

            default:
                return;
        }
        UpdateSoundIcon();
    }

    public void UpdateSoundIcon()
    {
        if (bgmIcons.Count >= 2)
            bgmIcon.sprite = SoundManager.Instance.IsBGMMuted() ? bgmIcons[1] : bgmIcons[0];

        if (sfxIcons.Count >= 3)
        {
            if (SoundManager.Instance.IsSFXMuted())
                sfxIcon.sprite = sfxIcons[2];
            else if (SoundManager.Instance?.GetSFXVolume() < 0.2f)
                sfxIcon.sprite = sfxIcons[1];
            else
                sfxIcon.sprite = sfxIcons[0];
        }
    }
    #endregion

    #region 버튼
    public void OnClickSetting() => OpenSetting(true);
    public void OnClickGold() => EntityManager.Instance?.SpawnTower();

    public void OnClickClose() => OpenUI(false);
    public void OnClickSpeed()
    {
        if (speedSlider.value != 1f)
            speedSlider.value = 1f;
        else
            speedSlider.value = speedSlider.maxValue;
    }
    public void OnClickBGM() => SoundManager.Instance?.ToggleBGM();
    public void OnClickSFX() => SoundManager.Instance?.ToggleSFX();

    public void OnClickReplay() => OpenConfirm(true, "다시", GameManager.Instance.Replay);
    public void OnClickQuit() => OpenConfirm(true, "종료", GameManager.Instance.Quit);

    public void OnClickOkay()
    {
        var action = confirmAction;
        OpenConfirm(false);
        action?.Invoke();
    }
    public void OnClickCancel() => OpenConfirm(false);

    public void OnClickReplayDirect() => OpenConfirm(true, "다시", GameManager.Instance.Replay, true);
    public void OnClickQuitDirect() => OpenConfirm(true, "종료", GameManager.Instance.Quit, true);
    #endregion

    #region SET
    public void SetCountdown(bool _skip) => countSkip = _skip;
    public void SetInGameUI(float _margin)
    {
        var rt = inGameUI.GetComponent<RectTransform>();
        rt.offsetMax = new Vector3(rt.offsetMax.x, -_margin);
    }
    #endregion

    #region GET
    public bool GetOnSetting() => settingUI.activeSelf;
    public bool GetOnConfirm() => confirmUI.activeSelf;
    public bool GetOnResult() => resultUI.activeSelf;
    #endregion
}
