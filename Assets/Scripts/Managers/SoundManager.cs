using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum SoundType { BGM, SFX }

[System.Serializable]
public struct SoundClip
{
    public AudioClip[] bgmClips;
    public AudioClip[] sfxClips;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { private set; get; }

    [Header("Source")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    private readonly HashSet<AudioSource> sfxLoops = new();

    [Header("Volume")]
    [SerializeField][Range(0f, 1f)] private float bgmVol = 1f;
    [SerializeField][Range(0f, 1f)] private float sfxVol = 1f;
    private float prevBgmVol;
    private float prevSfxVol;
    public event System.Action<SoundType, float> OnChangeVolume;

    [Header("Clip")]
    [SerializeField] private SoundClip soundClips;
    private readonly Dictionary<string, AudioClip> bgmDict = new();
    private readonly Dictionary<string, AudioClip> sfxDict = new();

#if UNITY_EDITOR
    private void OnValidate()
    {
        var bgmList = new List<AudioClip>();
        LoadSound(bgmList, SoundType.BGM);
        soundClips.bgmClips = bgmList.ToArray();

        var sfxList = new List<AudioClip>();
        LoadSound(sfxList, SoundType.SFX);
        soundClips.sfxClips = sfxList.ToArray();
    }

    private void LoadSound(List<AudioClip> _list, SoundType _type)
    {
        _list.Clear();
        string path = "Sounds/" + (_type == SoundType.BGM ? "BGMs" : "SFXs");
        _list.AddRange(Resources.LoadAll<AudioClip>(path));
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

        SetAudio();

        foreach (var btn in FindObjectsByType<Button>(FindObjectsSortMode.None))
            btn.onClick.AddListener(Button);
    }

    public void PauseSound(bool _on) => AudioListener.pause = _on;
    public void PauseSound(string _on) => AudioListener.pause = bool.TryParse(_on, out var _v) && _v;

    #region 배경음
    public void PlayBGM(AudioClip _clip)
    {
        if (bgmSource == null || IsBGMMuted) return;

        bgmSource.clip = _clip;
        bgmSource.Play();
    }

    public void PlayBGM(string _name)
    {
        if (bgmDict.TryGetValue(_name, out var _clip))
            PlayBGM(_clip);
    }

    public void PauseBGM(bool _on)
    {
        if (_on) bgmSource.Pause();
        else bgmSource.UnPause();
    }

    public void StopBGM() => bgmSource.Stop();

    public void ToggleBGM()
    {
        if (!IsBGMMuted && bgmVol > 0f)
        {
            prevBgmVol = bgmVol;
            SetBGMVolume(0f);
        }
        else SetBGMVolume(prevBgmVol);
    }
    #endregion

    #region 효과음
    public void PlaySFX(AudioClip _clip)
    {
        if (sfxSource == null || IsSFXMuted) return;

        sfxSource.PlayOneShot(_clip);
    }

    public void PlaySFX(string _name)
    {
        if (sfxDict.TryGetValue(_name, out var _clip))
            PlaySFX(_clip);
    }

    public AudioSource PlaySFXLoop(AudioClip _clip, Transform _owner)
    {
        var src = _owner.gameObject.AddComponent<AudioSource>();
        src.clip = _clip;
        src.loop = true;
        src.playOnAwake = false;
        src.volume = sfxVol;
        src.mute = (sfxVol <= 0f);
        src.spatialBlend = 0f;
        src.Play();
        sfxLoops.Add(src);
        return src;
    }

    public AudioSource PlaySFXLoop(string _name, Transform _owner)
    {
        if (!sfxDict.TryGetValue(_name, out var _clip)) return null;
        return PlaySFXLoop(_clip, _owner);
    }

    public void PauseSFXLoop(bool _on)
    {
        sfxLoops.RemoveWhere(_src => _src == null);

        if (_on)
            foreach (var src in sfxLoops) src.Pause();
        else
            foreach (var src in sfxLoops) src.UnPause();
    }

    public void StopSFXLoop(AudioSource _src)
    {
        sfxLoops.Remove(_src);
        if (_src.isPlaying) _src.Stop();
        Destroy(_src);
    }

    public void ToggleSFX()
    {
        if (!IsSFXMuted && sfxVol > 0f)
        {
            prevSfxVol = sfxVol;
            SetSFXVolume(0f);
        }
        else SetSFXVolume(prevSfxVol);
    }

    public void Button() => PlaySFX("Button");
    public void GameOver()
    {
        PlaySFX("GameOver");
        PlayBGM("GameOver");
    }
    #endregion

    #region SET
    private void SetAudio()
    {
        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        bgmSource.playOnAwake = false;
        sfxSource.playOnAwake = false;
        bgmSource.loop = true;

        SetBGMVolume(bgmVol);
        SetSFXVolume(sfxVol);
        SetDictionary();
    }

    public void SetBGMVolume(float _volume = 1f)
    {
        bgmVol = Mathf.Clamp01(_volume);
        bgmSource.volume = bgmVol;
        bgmSource.mute = (bgmVol <= 0f);

        if (bgmVol > 0f) prevBgmVol = bgmVol;

        OnChangeVolume?.Invoke(SoundType.BGM, bgmVol);
    }

    public void SetSFXVolume(float _volume = 1f)
    {
        sfxVol = Mathf.Clamp01(_volume);
        bool isMuted = sfxVol <= 0f;

        sfxSource.volume = sfxVol;
        sfxSource.mute = isMuted;

        sfxLoops.RemoveWhere(_src => _src == null);

        foreach (var src in sfxLoops)
        {
            src.volume = sfxVol;
            src.mute = isMuted;
        }

        if (sfxVol > 0f) prevSfxVol = sfxVol;

        OnChangeVolume?.Invoke(SoundType.SFX, sfxVol);
    }

    private void SetDictionary()
    {
        bgmDict.Clear();
        if (soundClips.bgmClips != null)
            foreach (var c in soundClips.bgmClips)
                if (c != null) bgmDict.TryAdd(c.name, c);

        sfxDict.Clear();
        if (soundClips.sfxClips != null)
            foreach (var c in soundClips.sfxClips)
                if (c != null) sfxDict.TryAdd(c.name, c);
    }
    #endregion

    #region 프로퍼티
    public float BGMVolume => bgmVol;
    public float SFXVolume => sfxVol;

    public bool IsBGMMuted => bgmSource != null && bgmSource.mute;
    public bool IsSFXMuted => sfxSource != null && sfxSource.mute;
    #endregion
}
