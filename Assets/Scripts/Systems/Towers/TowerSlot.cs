using UnityEngine;
using UnityEngine.UI;

public class TowerSlot : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform rect;
    [SerializeField] private Image image;
    [SerializeField] private Button btn;
    [Space]
    [SerializeField] private Image outline;
    [SerializeField] private Image icon;

    [Header("Slot")]
    [SerializeField] private TowerData data;
    [SerializeField] private TowerGrade grade;

    public bool IsComplete { private set; get; } = false;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();
        if (image == null)
            image = GetComponent<Image>();
        if (btn == null)
            btn = GetComponent<Button>();
        if (outline == null)
            outline = transform.Find("Outline")?.GetComponent<Image>();
        if (icon == null)
            icon = transform.Find("Icon")?.GetComponent<Image>();
    }
#endif

    private void Update()
    {
        btn.interactable = !IsComplete && GameManager.Instance.EnoughGold();
    }

    #region 슬롯
    public void Init()
    {
        btn.onClick.RemoveListener(OnClickSlot);
        btn.onClick.AddListener(OnClickSlot);

        Reroll();
    }

    public void Move(Vector2 _anchoredPos) => rect.anchoredPosition = _anchoredPos;

    public void Reroll()
    {
        data = DataManager.Instance?.GetRandomTower(out grade);
        Ready();
    }
    #endregion

    #region 상태 및 UI
    public void Ready()
    {
        IsComplete = false;

        rect.localScale = Vector3.one;
        image.color = Color.white;

        SetSlot(data);
    }

    public void Complete()
    {
        IsComplete = true;

        rect.localScale = Vector3.one;
        image.color = SetVisible(Color.gray, false);
        outline.color = SetVisible(Color.gray, false);
        icon.color = SetVisible(Color.gray, false);
    }
    #endregion

    private void OnClickSlot()
    {
        SoundManager.Instance?.Button();
        if (EntityManager.Instance?.SpawnTower(data.ID, grade) != null)
            Complete();
    }

    #region SET
    private void SetSlot(TowerData _data)
    {
        data = _data;

        outline.color = DataManager.Instance.GetTowerColor(grade);
        icon.sprite = _data.Icon;
        icon.color = _data.Color;
    }

    private Color SetVisible(Color _color, bool _visible)
    {
        _color.a = _visible ? 1f : 0.35f;
        return _color;
    }
    #endregion

    #region 프로퍼티
    public Vector3 Pos => rect.anchoredPosition;

    public int ID => data.ID;
    public TowerGrade Grade => grade;
    #endregion
}
