using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AutoCamera : MonoBehaviour
{
    private Camera cam;
    private int lastW, lastH;

    [SerializeField] private Vector2 res = new Vector2(1080, 1920);
    [SerializeField][Min(0f)] private float minSize = 10f;

    public static float OrthoSize { private set; get; }
    public static Vector2 Resolution { private set; get; }
    public static float Aspect { private set; get; }

    public static Rect WorldRect { private set; get; }
    public static float SizeDelta { private set; get; } = 0f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            cam = GetComponent<Camera>();

            OrthoSize = cam.orthographicSize;
            Resolution = res;
            Aspect = res.x / res.y;
        }
    }
#endif

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;

        OrthoSize = cam.orthographicSize;
        Resolution = res;
        Aspect = res.x / res.y;

        Apply(true);
    }

    private void Update()
    {
        Apply(false);
    }

    private void Apply(bool _force)
    {
        int cw = Screen.width;
        int ch = Screen.height;
        if (!_force && cw == lastW && ch == lastH) return;

        lastW = cw;
        lastH = ch;
        if (ch == 0) return;

        float currentAspect = (float)cw / ch;
        float size = OrthoSize * (Aspect / currentAspect);
        size = Mathf.Max(size, minSize);
        cam.orthographicSize = size;

        float worldH = size * 2f;
        float worldW = worldH * currentAspect;
        Vector3 pos = cam.transform.position;
        WorldRect = new Rect(pos.x - worldW * 0.5f, pos.y - worldH * 0.5f, worldW, worldH);

        SizeDelta = size - OrthoSize;
    }
}
