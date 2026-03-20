using TMPro;
using UnityEngine;

public class Monster : Pooling
{
    private static int sorting = 0;

    [Header("Text UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Move")]
    [SerializeField][Min(0f)] private float moveSpeed = 3f;
    [SerializeField] private Vector3 moveDirection;

    [Header("Battle")]
    [SerializeField][Min(0)] private int health;
    [SerializeField][Min(0)] protected int maxHealth;
    [Space]
    [SerializeField][Min(0)] private int gold;

    public bool IsDead { private set; get; } = false;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        if (canvas == null) canvas = canvases[0];
        if (healthText == null)
            healthText = canvas.GetComponentInChildren<TextMeshProUGUI>();
    }
#endif

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();
    }

    #region SET
    public void SetMonster(int _set)
    {
        maxHealth = Mathf.Max(50 * _set, 50);
        SetHealth(maxHealth);
        gold = Mathf.Max(10 * _set, 10);
    }

    public float SetSpeed(float _speed) => moveSpeed = Mathf.Max(_speed, 0f);

    public void SetHealth(int _health)
    {
        health = _health;
        healthText.text = _health < int.MaxValue ? health.ToString() : "ㄱ-";
    }
    #endregion

    #region GET
    public float GetSpeed() => moveSpeed;
    public Vector3 GetDirection() => moveDirection;

    public int GetHealth() => health;
    public int GetMaxHealth() => maxHealth;
    #endregion

    #region 풀링
    public int Index { private set; get; }

    public override void OnSpawnPool()
    {
        base.OnSpawnPool();

        Index++;

        int order = ++sorting;
        sr.sortingOrder = order;
        canvas.sortingOrder = order;

        IsDead = false;
    }

    public override void ResetPool()
    {
        base.ResetPool();

        moveSpeed = 3f;
    }
    #endregion
}
