using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerStore : MonoBehaviour
{
    public static TowerStore Instance { private set; get; }

    [Header("Slot")]
    [SerializeField] private TowerSlot origin;
    [SerializeField][Min(0)] private int count = 6;
    [SerializeField] private List<TowerSlot> slots = new();
    private Coroutine slotRoutine;

    public bool IsAuto { private set; get; } = false;

    [Header("UI")]
    [SerializeField] private RectTransform slotTrans;
    [SerializeField] private Button buyBtn;
    [SerializeField] private Button autoBtn;
    [SerializeField] private TextMeshProUGUI autoBtnText;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (origin == null)
            origin = GetComponentInChildren<TowerSlot>();
        if (slotTrans == null)
            slotTrans = GameObject.Find("Slots")?.GetComponent<RectTransform>();
        if (buyBtn == null)
            buyBtn = GameObject.Find("BuyBtn")?.GetComponent<Button>();
        if (autoBtn == null)
            autoBtn = GameObject.Find("AutoBtn")?.GetComponent<Button>();
        if (autoBtnText == null)
            autoBtnText = GameObject.Find("AutoBtn/AutoBtnText")?.GetComponent<TextMeshProUGUI>();
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
    }

    private void Update()
    {
        UpdateBtn();
        UpdateSlot();

        if (IsAuto)
            OnClickBuy();
    }

    #region 상점
    private void UpdateBtn()
    {
        buyBtn.interactable = CanBuy;

        autoBtn.image.color = IsAuto ? Color.white : new Color(Color.gray.r, Color.gray.g, Color.gray.b, 0.35f);
        autoBtnText.color = IsAuto ? Color.red : Color.black;
    }

    public void ResetStore()
    {
        IsAuto = false;

        if (slotRoutine != null)
        {
            StopCoroutine(slotRoutine);
            slotRoutine = null;
        }

        foreach (TowerSlot slot in slots)
            Destroy(slot.gameObject);

        origin.gameObject.SetActive(false);
        slots.Clear();
    }
    #endregion

    #region 슬롯
    private void UpdateSlot()
    {
        if (IsMoving) return;

        for (int i = slots.Count - 1; i >= 0; i--)
            if (slots[i] == null)
                slots.RemoveAt(i);

        if (slots.Count >= count) return;

        slotRoutine = StartCoroutine(SlotCoroutine());
    }

    private IEnumerator SlotCoroutine()
    {
        while (slots.Count < count)
        {
            TowerSlot slot = GenerateSlot();
            SortSlot();

            float size = slotTrans.rect.height;
            float limit = -slotTrans.rect.width * 0.5f + size * 0.5f;
            RectTransform rect = slot.transform as RectTransform;

            while (rect.anchoredPosition.x < limit)
                yield return null;
        }

        slotRoutine = null;
    }

    private TowerSlot GenerateSlot()
    {
        float size = slotTrans.rect.height;

        Vector3 worldPos = new Vector3(AutoCamera.WorldRect.xMin, 0f, 0f);
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(slotTrans, screenPos, null, out Vector2 createPos);

        TowerSlot slot = Instantiate(origin, slotTrans);
        slot.gameObject.SetActive(true);
        slot.SetSlot(size, new Vector2(createPos.x - size, 0f));

        slots.Add(slot);

        return slot;
    }

    private void SortSlot()
    {
        for (int i = slots.Count - 1; i >= 0; i--)
            if (slots[i] == null)
                slots.RemoveAt(i);

        float size = slotTrans.rect.height;
        float width = slotTrans.rect.width;
        float start = width * 0.5f - size * 0.5f;
        float end = -width * 0.5f + size * 0.5f;

        for (int i = 0; i < slots.Count; i++)
            slots[i].SetTarget(new Vector2(Mathf.Lerp(start, end, (float)i / (count - 1)), 0f));
    }
    #endregion

    #region 클릭
    public TowerSlot RandomSlot()
    {
        TowerSlot result = null;
        int match = 0;

        foreach (TowerSlot slot in slots)
        {
            if (slot == null || !slot.CanBuyTower) continue;

            if (Random.Range(0, ++match) == 0)
                result = slot;
        }

        return result;
    }

    public void OnClickBuy()
    {
        if (!CanBuy) return;

        TowerSlot slot = RandomSlot();
        if (slot == null) return;

        if (slot.BuyTower())
            slot.Remove();
    }

    public void OnClickAuto() => IsAuto = !IsAuto;
    #endregion

    #region 프로퍼티
    public bool CanBuy => GameManager.Instance.EnoughGold() && EntityManager.Instance.HasEmptyField();
    public bool IsMoving => slotRoutine != null;
    #endregion
}
