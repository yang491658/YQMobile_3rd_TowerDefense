using UnityEngine;

[ExecuteAlways]
public class AutoBackground : MonoBehaviour
{
    private SpriteRenderer sr;
    private int lastW, lastH;
    private float lastAspect, lastOrthoSize;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Init();
        if (enabled) Fit();
    }
#endif

    private void Awake()
    {
        Init();
        Fit();
    }

    private void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH ||
            !Mathf.Approximately(AutoCamera.Aspect, lastAspect) ||
            !Mathf.Approximately(AutoCamera.OrthoSize, lastOrthoSize))
            Fit();
    }

    private void OnEnable()
    {
        Init();
        Fit();
    }

    private void Init()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Fit()
    {
        if (sr == null) return;

        Sprite sp = sr.sprite;
        if (sp == null) return;

        lastW = Screen.width;
        lastH = Screen.height;
        lastAspect = AutoCamera.Aspect;
        lastOrthoSize = AutoCamera.OrthoSize;

        float camH = lastOrthoSize * 2f;
        float camW = camH * lastAspect;
        Vector2 size = sp.bounds.size;
        float scale = Mathf.Max(camW / size.x, camH / size.y);

        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
