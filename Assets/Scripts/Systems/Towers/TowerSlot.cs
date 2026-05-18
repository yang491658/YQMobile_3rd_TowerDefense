using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TowerSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private enum SlotState { Ready, Select, Complete }

    [Header("UI")]
    [SerializeField] private RectTransform rect;
    [SerializeField] private Image image;
    [SerializeField] private Button btn;
    [Space]
    [SerializeField] private Image outline;
    [SerializeField] private Image icon;

    [Header("Slot")]
    [SerializeField] private TowerData data;
    [SerializeField] private SlotState state;

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
        btn.interactable = state != SlotState.Complete && GameManager.Instance.EnoughGold();
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
        data = DataManager.Instance?.GetRandomTower();
        Ready();
    }

    public bool Purchase(Vector3 _pos)
    {
        if (EntityManager.Instance?.SpawnTower(data.ID, 1, _pos) == null)
            return false;

        Complete();
        return true;
    }
    #endregion

    #region 상태 및 UI
    public void Ready()
    {
        state = SlotState.Ready;

        rect.localScale = Vector3.one;
        image.color = Color.white;

        SetSlot(data);
    }

    public void Select(bool _on)
    {
        if (state == SlotState.Complete) return;

        state = _on ? SlotState.Select : SlotState.Ready;

        rect.localScale = _on ? Vector3.one * 1.2f : Vector3.one;
        image.color = SetVisible(image.color, _on);
        outline.color = SetVisible(outline.color, _on);
        icon.color = SetVisible(icon.color, _on);
    }

    public void Complete()
    {
        state = SlotState.Complete;

        rect.localScale = Vector3.one;
        image.color = SetVisible(Color.gray, false);
        outline.color = SetVisible(Color.gray, false);
        icon.color = SetVisible(Color.gray, false);
    }
    #endregion

    #region 클릭 및 드래그
    private void OnClickSlot()
    {
        SoundManager.Instance?.Button();
        TowerStore.Instance?.SelectSlot(this);
    }

    public void OnBeginDrag(PointerEventData _eventData)
    {
        if (state == SlotState.Complete) return;
        if (!GameManager.Instance.EnoughGold()) return;

        SoundManager.Instance?.Button();
        TowerStore.Instance?.SelectSlot(this, true);

        Vector3 pos = HandleManager.Instance.ScreenToWorld(_eventData.position);
        EntityManager.Instance?.ShowPlaceField(true, pos);
    }

    public void OnDrag(PointerEventData _eventData)
    {
        if (!TowerStore.Instance.IsPlacing) return;

        Vector3 pos = HandleManager.Instance.ScreenToWorld(_eventData.position);
        EntityManager.Instance?.ShowPlaceField(true, pos);
    }

    public void OnEndDrag(PointerEventData _eventData)
    {
        if (!TowerStore.Instance.IsPlacing) return;

        Vector3 pos = HandleManager.Instance.ScreenToWorld(_eventData.position);
        Rect mapRect = UIManager.Instance.GetMapAreaRect(pos.z);

        if (mapRect.Contains(new Vector2(pos.x, pos.y)))
            TowerStore.Instance?.PurchaseSlot(pos);
        else
            TowerStore.Instance?.CancelSlot();
    }
    #endregion

    #region SET
    private void SetSlot(TowerData _data)
    {
        data = _data;

        outline.color = DataManager.Instance.GetTowerColor(_data.Grade);
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
    public TowerGrade Grade => data.Grade;

    public bool IsComplete => state == SlotState.Complete;
    #endregion
}
