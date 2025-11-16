using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HandleManager : MonoBehaviour
{
    public static HandleManager Instance { private set; get; }

    private Camera cam => Camera.main;
    private LayerMask layer => LayerMask.GetMask("Tower");

    [Header("Click")]
    private const float doubleClick = 0.25f;
    private bool isDoubleClick;
    private float clickTimer;

    [Header("Drag")]
    [SerializeField][Min(0f)] private float maxDrag = 5f;
    private const float drag = 0.15f;
    private bool canDrag;
    private bool isDragging;
    private Vector3 dragStart;
    private Vector3 dragCurrent;
    private bool isOverUI;

#if UNITY_EDITOR
    [Header("Mark")]
    [SerializeField] private float markDuration = 1f;
    [SerializeField] private float markRadius = 0.5f;
    [SerializeField] private int markSegment = 24;
    private readonly List<Vector3> marks = new();
    private readonly List<float> markTimes = new();
    private readonly List<Color> markColors = new();
    private readonly List<Vector3> dragPath = new();
#endif

#if UNITY_EDITOR
    private void OnValidate()
    {
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

    private void Update()
    {
        if (GameManager.Instance.IsPaused) return;

#if UNITY_EDITOR
        HandleMouse();
        DrawDebug();
#else
        HandleTouch();
#endif
    }

#if UNITY_EDITOR
    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0)) HandleBegin(Input.mousePosition);
        else if (Input.GetMouseButton(0)) HandleMove(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0)) HandleEnd(Input.mousePosition);

        if (Input.GetMouseButtonDown(1)) OnRightClick(ScreenToWorld(Input.mousePosition));
        if (Input.GetMouseButtonDown(2)) OnMiddleClick(ScreenToWorld(Input.mousePosition));
    }
#endif

    private void HandleTouch()
    {
        if (Input.touchCount == 0) return;
        Touch t = Input.GetTouch(0);

        if (t.phase == TouchPhase.Began && !IsOverUI(t.fingerId))
            HandleBegin(t.position, t.fingerId);
        else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            HandleMove(t.position);
        else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            HandleEnd(t.position);
    }

    #region 판정
    private bool IsOverUI(int _fingerID = -1)
        => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(_fingerID);

    private Vector3 ScreenToWorld(Vector3 _screenPos)
    {
        var p = _screenPos;
        p.z = Mathf.Max(-cam.transform.position.z, cam.nearClipPlane);
        return cam.ScreenToWorldPoint(p);
    }

    private bool CanSelect(Collider2D _col)
    {
        if (layer == 0) return true;
        return _col != null;
    }
    #endregion

    #region 구분
    private void HandleBegin(Vector3 _pos, int _fingerID = -1)
    {
        if (IsOverUI(_fingerID))
        {
            isOverUI = true;
            return;
        }
        else isOverUI = false;

        Vector3 worldPos = ScreenToWorld(_pos);
        Collider2D hit = Physics2D.OverlapPoint(worldPos, layer);

        if (CanSelect(hit))
        {
            canDrag = true;
            isDragging = false;
            dragStart = worldPos;
            dragCurrent = dragStart;
#if UNITY_EDITOR
            dragPath.Clear();
            dragPath.Add(dragStart);
#endif
        }
        else
        {
            canDrag = false;
            isDragging = false;
#if UNITY_EDITOR
            dragPath.Clear();
#endif
        }
    }

    private void HandleMove(Vector3 _pos)
    {
        if (isOverUI) return;
        if (!canDrag) return;

        Vector3 worldPos = ScreenToWorld(_pos);
        float distance = Vector3.Distance(dragStart, worldPos);

        if (!isDragging && distance >= drag)
        {
            isDragging = true;
            OnDragBegin(dragStart);
        }

        if (isDragging)
        {
            dragCurrent = ClampDrag(dragStart, worldPos);
            OnDragMove(dragStart, dragCurrent);
#if UNITY_EDITOR
            dragPath.Add(dragCurrent);
#endif
        }
    }

    private void HandleEnd(Vector3 _pos)
    {
        if (isOverUI)
        {
            isOverUI = false;
            canDrag = false;
            isDragging = false;
#if UNITY_EDITOR
            dragPath.Clear();
#endif
            return;
        }

        if (!canDrag)
        {
            canDrag = false;
            isDragging = false;
#if UNITY_EDITOR
            dragPath.Clear();
#endif
            return;
        }

        Vector3 worldPos = ScreenToWorld(_pos);

        if (isDragging)
        {
            worldPos = ClampDrag(dragStart, worldPos);
            float distance = Vector3.Distance(dragStart, worldPos);
            if (distance >= drag)
            {
                isDragging = false;
                OnDragEnd(dragStart, worldPos);
#if UNITY_EDITOR
                dragPath.Clear();
#endif
                return;
            }
        }

        if (Time.time - clickTimer < doubleClick)
        {
            isDoubleClick = false;
            clickTimer = 0;
            OnDouble(worldPos);
        }
        else
        {
            isDoubleClick = true;
            clickTimer = Time.time;
            StartCoroutine(ClickCoroutine(worldPos));
        }

        canDrag = false;
        isDragging = false;
#if UNITY_EDITOR
        dragPath.Clear();
#endif
    }

    private Vector3 ClampDrag(Vector3 _start, Vector3 _current)
    {
        if (maxDrag <= 0f) return _current;
        Vector3 delta = _current - _start;
        return _start + Vector3.ClampMagnitude(delta, maxDrag);
    }

    private IEnumerator ClickCoroutine(Vector3 _pos)
    {
        yield return new WaitForSeconds(doubleClick);
        if (isDoubleClick)
        {
            isDoubleClick = false;
            OnSingle(_pos);
        }
    }
    #endregion

    #region 동작
    private void OnSingle(Vector3 _pos)
    {
        Debug.Log($"단순 터치 : {_pos}"); // TODO : 단순 터치 동작
#if UNITY_EDITOR
        AddClick(_pos, Color.cyan);
#endif
    }

    private void OnDouble(Vector3 _pos)
    {
        Debug.Log($"더블 터치 : {_pos}"); // TODO : 더블 터치 동작
#if UNITY_EDITOR
        AddClick(_pos, Color.blue);
#endif
    }

    private void OnDragBegin(Vector3 _pos)
    {
        Debug.Log($"드래그 시작 : {_pos}"); // TODO : 드래그 시작 동작
    }

    private void OnDragMove(Vector3 _start, Vector3 _current)
    {
        Debug.Log($"드래그 진행"); // TODO : 드래그 진행 동작
    }

    private void OnDragEnd(Vector3 _start, Vector3 _end)
    {
        Debug.Log($"드래그 종료 : {_start} → {_end}"); // TODO : 드래그 종료 동작
    }

#if UNITY_EDITOR
    private void OnRightClick(Vector3 _pos)
    {
        Debug.Log($"우클릭 : {_pos}"); // TODO : 우클릭 동작
        AddClick(_pos, Color.yellow);
    }

    private void OnMiddleClick(Vector3 _pos)
    {
        Debug.Log($"휠클릭 : {_pos}"); // TODO : 휠클릭 동작
        AddClick(_pos, Color.red);
    }

    private void AddClick(Vector3 _pos, Color _color)
    {
        marks.Add(_pos);
        markTimes.Add(Time.time + markDuration);
        markColors.Add(_color);
    }

    private void DrawDebug()
    {
        for (int i = markTimes.Count - 1; i >= 0; i--)
        {
            if (Time.time > markTimes[i])
            {
                int last = markTimes.Count - 1;
                (markTimes[i], markTimes[last]) = (markTimes[last], markTimes[i]);
                (marks[i], marks[last]) = (marks[last], marks[i]);
                (markColors[i], markColors[last]) = (markColors[last], markColors[i]);
                markTimes.RemoveAt(last);
                marks.RemoveAt(last);
                markColors.RemoveAt(last);
                continue;
            }

            Vector3 center = marks[i];
            Color c = markColors[i];
            for (int s = 0; s < markSegment; s++)
            {
                float a0 = (Mathf.PI * 2f) * s / markSegment;
                float a1 = (Mathf.PI * 2f) * (s + 1) / markSegment;
                Vector3 p0 = center + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0)) * markRadius;
                Vector3 p1 = center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * markRadius;
                Debug.DrawLine(p0, p1, c);
            }
        }

        if (isDragging)
        {
            Debug.DrawLine(dragStart, dragCurrent, Color.green);

            for (int i = 1; i < dragPath.Count; i++)
                Debug.DrawLine(dragPath[i - 1], dragPath[i], Color.magenta);
        }
    }
#endif
    #endregion
}
