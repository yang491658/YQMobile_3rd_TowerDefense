using UnityEngine;

[ExecuteAlways]
public class AutoBackground : MonoBehaviour
{
    private Camera cam;
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
        if (cam == null) cam = Camera.main;

        if (Screen.width != lastW || Screen.height != lastH ||
            !Mathf.Approximately(cam.aspect, lastAspect) ||
            !Mathf.Approximately(cam.orthographicSize, lastOrthoSize))
            Fit();
    }

    private void OnEnable()
    {
        Init();
        Fit();
    }

    private void Init()
    {
        cam = Camera.main;
        sr = GetComponent<SpriteRenderer>();
    }

    private void Fit()
    {
        if (cam == null || !cam.orthographic || sr.sprite == null) return;

        lastW = Screen.width;
        lastH = Screen.height;
        lastAspect = cam.aspect;
        lastOrthoSize = cam.orthographicSize;

        Sprite sp = sr.sprite;
        float ppu = sp.pixelsPerUnit;
        if (ppu <= 0f) return;

        Rect worldRect = AutoCamera.WorldRect;
        float worldW = worldRect.width;
        float worldH = worldRect.height;

        float spriteW = sp.rect.width / ppu;
        float spriteH = sp.rect.height / ppu;
        if (spriteW <= 0f || spriteH <= 0f) return;

        Transform tr = transform;
        Transform parent = tr.parent;
        Vector3 parentLossy = (parent != null) ? parent.lossyScale : Vector3.one;
        float parentScaleX = (parentLossy.x == 0f) ? 1f : parentLossy.x;
        float parentScaleY = (parentLossy.y == 0f) ? 1f : parentLossy.y;

        float localX = (worldW / spriteW) / parentScaleX;
        float localY = (worldH / spriteH) / parentScaleY;
        tr.localScale = new Vector3(localX, localY, (localX + localY) / 2f);

        Bounds b = sr.bounds;
        Vector2 center = worldRect.center;
        tr.position += new Vector3(center.x - b.center.x, center.y - b.center.y, 0f);
    }
}
