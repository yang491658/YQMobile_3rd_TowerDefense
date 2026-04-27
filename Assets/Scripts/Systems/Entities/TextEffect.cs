using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform), typeof(TextMeshProUGUI))]
public class TextEffect : MonoBehaviour, IPoolable
{
    private Camera cam;

    private RectTransform rect;
    private TextMeshProUGUI text;

    [Header("Text")]
    [SerializeField] private List<Color> colors = new();
    [SerializeField][Min(0f)] private float colorInterval;
    private float colorTimer;

    [Header("Move")]
    [SerializeField] private bool moveType;
    [SerializeField][Min(0f)] private float moveSpeed;
    [SerializeField] private Vector3 moveDirection;
    [Space]
    [SerializeField] private Vector3 moveTarget;
    [SerializeField][Min(0f)] private float moveTime;
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

        Color color = colors[0];

        if (colors.Count >= 2 && colorInterval > 0f)
        {
            colorTimer += _deltaTime;

            int count = colors.Count;
            float cycle = count * colorInterval;
            float timeInCycle = Mathf.Repeat(colorTimer, cycle);

            int index = Mathf.FloorToInt(timeInCycle / colorInterval);
            int nextIndex = (index + 1) % count;
            float t = (timeInCycle - index * colorInterval) / colorInterval;

            color = Color.Lerp(colors[index], colors[nextIndex], t);
        }

        if (duration > 0f)
            color.a *= Mathf.Clamp01(timer / duration);

        text.color = color;
    }

    private void UpdateMove(float _deltaTime)
    {
        if (moveType)
        {
            if (moveSpeed <= 0f) return;

            rect.anchoredPosition += (Vector2)moveDirection * moveSpeed * _deltaTime;

            if (CameraOut())
                Despawn();

            return;
        }

        if (moveTime <= 0f)
        {
            rect.anchoredPosition = moveTarget;
            return;
        }

        Vector2 current = rect.anchoredPosition;
        Vector2 target = moveTarget;

        if (_deltaTime >= moveTime)
        {
            rect.anchoredPosition = target;
            moveTime = 0f;
            return;
        }

        rect.anchoredPosition = Vector2.Lerp(current, target, _deltaTime / moveTime);
        moveTime -= _deltaTime;
    }

    private bool CameraOut()
    {
        Rect localRect = rect.rect;
        float z = -cam.transform.position.z;

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        void CheckCorner(Vector3 _localCorner)
        {
            Vector3 worldCorner = rect.TransformPoint(_localCorner);
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, worldCorner);
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));

            if (worldPos.x < minX) minX = worldPos.x;
            if (worldPos.x > maxX) maxX = worldPos.x;
            if (worldPos.y < minY) minY = worldPos.y;
            if (worldPos.y > maxY) maxY = worldPos.y;
        }

        CheckCorner(new Vector3(localRect.xMin, localRect.yMin, 0f));
        CheckCorner(new Vector3(localRect.xMin, localRect.yMax, 0f));
        CheckCorner(new Vector3(localRect.xMax, localRect.yMax, 0f));
        CheckCorner(new Vector3(localRect.xMax, localRect.yMin, 0f));

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
        moveType = true;
        moveSpeed = _speed;
        moveDirection = _direction.normalized;
    }
    public void SetMove(Vector3 _target, float _time)
    {
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, _target);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect.parent as RectTransform, screenPos, null, out Vector2 targetPos);

        moveType = false;
        moveTarget = targetPos;
        moveTime = Mathf.Max(_time, 0f);

        SetDuration(_time);
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

        cam = Camera.main;
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

        moveType = true;
        moveSpeed = 0f;
        moveDirection = Vector3.zero;
        moveTarget = Vector3.zero;
        moveTime = 0f;

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
