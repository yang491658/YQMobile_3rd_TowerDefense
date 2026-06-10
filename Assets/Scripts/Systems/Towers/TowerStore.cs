using UnityEngine;

public class TowerStore : MonoBehaviour
{
    public static TowerStore Instance { private set; get; }

    [Header("Slot")]
    [SerializeField] private TowerSlot origin;
    [Space]
    [SerializeField][Min(0)] private int count = 7;
    [SerializeField] private TowerSlot[] slots;

    [Header("Move")]
    [SerializeField][Min(0f)] private float time = 10f;
    [SerializeField] private RectTransform enter;
    [SerializeField] private RectTransform exit;

    public bool IsMoving { private set; get; }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (enter == null)
            enter = transform.Find("Enter")?.GetComponent<RectTransform>();
        if (exit == null)
            exit = transform.Find("Exit")?.GetComponent<RectTransform>();
        if (origin == null)
            origin = GetComponentInChildren<TowerSlot>();
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
        if (IsMoving)
            MoveSlot();
    }

    #region 상점
    public void ResetStore()
    {
        IsMoving = false;

        for (int i = 0; i < slots.Length; i++)
            Destroy(slots[i].gameObject);

        slots = new TowerSlot[0];

        InitStore();
    }

    private void InitStore()
    {
        origin.gameObject.SetActive(false);

        slots = new TowerSlot[count];

        Vector2 start = enter.anchoredPosition;
        Vector2 end = exit.anchoredPosition;

        int len = slots.Length;
        for (int i = 0; i < len; i++)
        {
            TowerSlot slot = Instantiate(origin, transform);
            slot.gameObject.SetActive(true);
            slot.Init();
            slot.Move(Vector2.Lerp(start, end, (float)i / len));

            slots[i] = slot;
        }

        IsMoving = true;
    }
    #endregion

    #region 슬롯
    private void MoveSlot()
    {
        Vector2 start = enter.anchoredPosition;
        Vector2 target = exit.anchoredPosition;
        float distance = Vector2.Distance(start, target);
        float delta = distance / time * Time.deltaTime;

        for (int i = 0; i < slots.Length; i++)
        {
            TowerSlot slot = slots[i];
            Vector2 pos = slot.Pos;
            float remain = delta;

            while (remain > 0f)
            {
                float toExit = Vector2.Distance(pos, target);

                if (remain < toExit)
                {
                    pos = Vector2.MoveTowards(pos, target, remain);
                    remain = 0f;
                }
                else
                {
                    remain -= toExit;
                    slot.Reroll();
                    pos = start;

                    if (distance <= 0f)
                    { remain = 0f; break; }
                }
            }

            slot.Move(pos);
        }
    }
    #endregion

    #region 자동 구매
    public TowerSlot AutoSlot(int _id, TowerGrade _grade)
    {
        TowerSlot result = null;

        int match = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            TowerSlot slot = slots[i];

            if (slot.IsComplete) continue;
            if (_id != 0 && slot.ID != _id) continue;
            if (_grade != 0 && slot.Grade != _grade) continue;

            match++;
            if (Random.Range(0, match) == 0)
                result = slot;
        }

        return result;
    }

    public bool AutoPurchase(int _id, TowerGrade _grade, TowerSlot _slot = null)
    {
        TowerSlot slot = _slot != null ? _slot : AutoSlot(_id, _grade);
        if (slot == null) return false;

        if (EntityManager.Instance?.SpawnTower(slot.ID, slot.Grade) == null)
            return false;

        slot.Complete();
        return true;
    }
    #endregion
}
