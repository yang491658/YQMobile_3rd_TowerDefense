using TMPro;
using UnityEngine;

public class Monster : Entity
{
    private static int sorting = 0;

    [Header("Health")]
    private int health;
    private TextMeshProUGUI healthText;

    [Header("Speed")]
    private float speed;

    [Header("Path")]
    private Transform[] path;
    private int pathIndex;

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

        pathIndex = 0;

        SetHealth(health);
    }

    protected override void Update()
    {
        base.Update();

        if (pathIndex >= path.Length)
        {
            Move(Vector3.right * speed);
            return;
        }

        Transform targetTrans = path[pathIndex];
        Vector3 target = targetTrans.position;
        Vector3 delta = target - transform.position;

        float arrive = Mathf.Max(speed * Time.deltaTime, 0.1f);
        if (delta.sqrMagnitude < arrive * arrive)
        {
            if (++pathIndex >= path.Length)
            {
                Move(Vector3.right * speed);
                return;
            }

            targetTrans = path[pathIndex];
            target = targetTrans.position;
            delta = target - transform.position;
        }

        Vector3 direction = delta.normalized;
        Move(direction * speed);
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
        EntityManager.Instance?.DespawnMonster(this);
    }
    #endregion

    #region SET
    public void SetHealth(int _health)
    {
        health = _health;
        healthText.text = health.ToString();
    }
    public void SetSpeed(float _speed) => speed = _speed;
    public void SetPath(Transform[] _path)
    {
        path = _path;
        pathIndex = 0;
    }
    #endregion
}
