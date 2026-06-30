using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TowerSlot : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private TowerData data;
    [SerializeField] private TowerGrade grade;

    [Header("Slot")]
    [SerializeField][Min(0f)] private float reroll = 30f;
    private float rerollTimer;
    [SerializeField][Min(0f)] private float move = 1000f;
    private Vector2 moveTarget;
    [SerializeField][Min(0f)] private float remove = 0.3f;
    private Coroutine removeRoutine;

    [Header("UI")]
    [SerializeField] private RectTransform rect;
    [SerializeField] private Image image;
    [Space]
    [SerializeField] private Image outline;
    [SerializeField] private Image icon;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();
        if (image == null)
            image = GetComponent<Image>();
        if (outline == null)
            outline = transform.Find("Outline")?.GetComponent<Image>();
        if (icon == null)
            icon = transform.Find("Icon")?.GetComponent<Image>();
    }
#endif

    private void Update()
    {
        if (GameManager.Instance.IsPaused) return;
        if (IsRemoving) return;

        rerollTimer -= Time.deltaTime;
        if (rerollTimer <= 0f)
        {
            Remove();
            return;
        }

        image.fillAmount = Mathf.Clamp01(rerollTimer / reroll);

        Move();
    }

    public void Move()
    {
        if (!IsMoving) return;

        rect.anchoredPosition = Vector2.MoveTowards(rect.anchoredPosition, moveTarget, move * Time.deltaTime);
    }

    public bool BuyTower() => EntityManager.Instance?.SpawnTower(ID, Grade, _time: remove) != null;

    public void Remove()
    {
        if (IsRemoving) return;

        removeRoutine = StartCoroutine(RemoveCoroutine());
    }

    private IEnumerator RemoveCoroutine()
    {
        Vector3 start = rect.localScale;
        float timer = 0f;

        while (timer < remove)
        {
            timer += Time.deltaTime;
            rect.localScale = Vector3.Lerp(start, Vector3.zero, timer / remove);

            yield return null;
        }

        rect.localScale = Vector3.zero;

        Destroy(gameObject);
    }

    #region SET
    public void SetSlot(float _size, Vector2 _pos)
    {
        data = DataManager.Instance?.GetRandomTower(out grade);
        rerollTimer = reroll;
        moveTarget = _pos;

        rect.sizeDelta = Vector2.one * _size;
        rect.anchoredPosition = _pos;
        rect.localScale = Vector3.one;
        outline.color = DataManager.Instance.GetTowerColor(grade);
        icon.sprite = data.Icon;
        icon.color = data.Color;
    }

    public void SetTarget(Vector2 _target) => moveTarget = _target;
    #endregion

    #region 프로퍼티
    public int ID => data.ID;
    public TowerGrade Grade => grade;

    public bool CanBuyTower => !IsMoving && !IsRemoving;
    public bool IsMoving => rect.anchoredPosition != moveTarget;
    public bool IsRemoving => removeRoutine != null;
    #endregion
}
