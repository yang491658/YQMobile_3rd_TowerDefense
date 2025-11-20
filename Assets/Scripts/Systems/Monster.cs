using TMPro;
using UnityEngine;

public class Monster : Entity
{
    private static int sorting = 0;

    [Header("Path")]
    [SerializeField] private float moveSpeed = 3f;
    private Transform[] paths;
    private int pathIndex;

    [Header("Battle")]
    [SerializeField] private int health = 5;
    private TextMeshProUGUI healthText;
    [SerializeField] private int dropGold = 1;


    protected override void Awake()
    {
        base.Awake();

        Canvas canvas = GetComponentInChildren<Canvas>();
        healthText = GetComponentInChildren<TextMeshProUGUI>();

        sr.sortingOrder = sorting;
        canvas.sortingOrder = sorting--;
    }

    protected override void Start()
    {
        base.Start();

        SetMonster(1 + GameManager.Instance.GetScore() / 100);
    }

    protected override void Update()
    {
        base.Update();

        if (pathIndex >= paths.Length)
        {
            Move(Vector3.right * moveSpeed);
            return;
        }

        Vector3 delta = paths[pathIndex].position - transform.position;

        float arrive = Mathf.Max(moveSpeed * Time.deltaTime, 0.1f);
        if (delta.sqrMagnitude < arrive * arrive)
        {
            if (++pathIndex >= paths.Length)
            {
                Move(Vector3.right * moveSpeed);
                return;
            }

            delta = paths[pathIndex].position - transform.position;
        }

        Move(delta.normalized * moveSpeed);
    }

    private void OnBecameInvisible()
    {
        EntityManager.Instance?.DespawnMonster(this);
    }

    #region 전투
    public void TakeDamage(int _damage)
    {
        SetHealth(health - _damage);

        if (health <= 0)
            Die();
    }

    public void Die()
    {
        GameManager.Instance?.ScoreUp();
        GameManager.Instance?.GoldUp(dropGold);
        EntityManager.Instance?.DespawnMonster(this);
    }
    #endregion

    #region SET
    public void SetMonster(int _set)
    {
        SetHealth(health * _set);

        pathIndex = 0;
        dropGold *= _set;
    }

    public void SetHealth(int _health)
    {
        health = _health;
        healthText.text = health.ToString();
    }

    public void SetPath(Transform[] _path)
    {
        paths = _path;
        pathIndex = 0;
    }
    #endregion
}
