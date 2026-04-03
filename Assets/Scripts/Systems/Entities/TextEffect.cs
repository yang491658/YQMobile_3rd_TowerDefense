using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform), typeof(TextMeshProUGUI))]
public class TextEffect : MonoBehaviour, IPoolable
{
    private RectTransform rect;
    private TextMeshProUGUI text;

    [Header("Text")]
    [SerializeField] private List<Color> colors = new();
    [SerializeField][Min(0f)] private float colorInterval;
    private float colorTimer;

    [Header("Move")]
    [SerializeField][Min(0f)] private float moveSpeed;
    [SerializeField] private Vector3 moveDirection;
    [Space]
    [SerializeField][Min(0f)] private float duration;
    private float timer;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        text = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (IsDespawn) return;

        float dt = Time.deltaTime;

        if (duration > 0f && (timer -= dt) <= 0f)
        {
            Despawn();
            return;
        }

        UpdateColor(dt);
        UpdateMove(dt);
    }

    private void UpdateColor(float _deltaTime)
    {
        if (colors == null || colors.Count == 0) return;

        if (colors.Count == 1)
        {
            Color c = colors[0];

            if (duration > 0f)
                c.a = Mathf.Clamp01(timer / duration);

            text.color = c;
        }
        else if (colorInterval > 0f)
        {
            colorTimer += _deltaTime;

            int count = colors.Count;
            float cycle = count * colorInterval;
            float timeInCycle = Mathf.Repeat(colorTimer, cycle);

            int index = Mathf.FloorToInt(timeInCycle / colorInterval);
            int nextIndex = (index + 1) % count;

            float t2 = (timeInCycle - index * colorInterval) / colorInterval;

            Color from = colors[index];
            Color to = colors[nextIndex];

            text.color = Color.Lerp(from, to, t2);
        }
    }

    private void UpdateMove(float _deltaTime)
    {
        if (moveSpeed <= 0f) return;

        Vector2 dir = moveDirection.normalized;
        rect.anchoredPosition += dir * moveSpeed * _deltaTime;

        if (CameraOut())
            Despawn();
    }

    private bool CameraOut()
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        Camera cam = Camera.main;
        float z = -cam.transform.position.z;

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, corners[i]);
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));

            if (worldPos.x < minX) minX = worldPos.x;
            if (worldPos.x > maxX) maxX = worldPos.x;
            if (worldPos.y < minY) minY = worldPos.y;
            if (worldPos.y > maxY) maxY = worldPos.y;
        }

        Rect worldRect = AutoCamera.WorldRect;
        return maxX < worldRect.xMin || minX > worldRect.xMax ||
            maxY < worldRect.yMin || minY > worldRect.yMax;
    }

    #region SET
    public void SetText(string _text, float _font, Color _color, float _interval = 0f)
    {
        text.text = _text;
        text.fontSize = _font;

        colors.Clear();
        SetColor(_color);
        text.color = _color;
        colorInterval = _interval;
    }
    public void SetColor(Color _color) => colors.Add(_color);

    public void SetPosition(Vector2 _pos) => rect.anchoredPosition = _pos;
    public void SetMove(float _speed, Vector3 _direction)
    {
        moveSpeed = _speed;
        moveDirection = _direction;
    }
    public void SetDuration(float _duration)
    {
        duration = Mathf.Max(_duration, 0f);
        timer = duration;
    }
    #endregion

    #region 풀링
    public bool IsDespawn { get; private set; } = true;

    public void OnSpawnPool()
    {
        IsDespawn = false;

        colorTimer = 0f;
    }

    public void OnDespawnPool()
    {
        IsDespawn = true;

        ResetPool();
    }

    public void ResetPool()
    {
        rect.anchoredPosition = Vector2.zero;

        text.text = string.Empty;
        text.color = Color.white;

        colors.Clear();
        colorInterval = 0f;
        colorTimer = 0f;

        moveSpeed = 0f;
        moveDirection = Vector3.zero;
        duration = 0f;
        timer = 0f;
    }

    public void Despawn()
    {
        if (IsDespawn) return;

        PoolManager.Instance?.Release(gameObject);
    }
    #endregion
}
