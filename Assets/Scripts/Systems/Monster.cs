using TMPro;
using UnityEngine;

public class Monster : Entity
{
    private static int sorting = 0;

    [Header("Health")]
    [SerializeField] private int health;
    private TextMeshProUGUI healthText;

    [Header("Move")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private int index;
    [SerializeField] private Transform[] path;

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

        index = 0;

        SetHealth(health);
    }

    protected override void Update()
    {
        base.Update();

        if (index >= path.Length)
        {
            Move(Vector3.right * speed);
            return;
        }

        Vector3 delta = path[index].position - transform.position;

        float arrive = Mathf.Max(speed * Time.deltaTime, 0.1f);
        if (delta.sqrMagnitude < arrive * arrive)
        {
            if (++index >= path.Length)
            {
                Move(Vector3.right * speed);
                return;
            }

            delta = path[index].position - transform.position;
        }

        Move(delta.normalized * speed);
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
        GameManager.Instance?.GoldUp();
        EntityManager.Instance?.DespawnMonster(this);
    }
    #endregion

    #region SET
    public void SetHealth(int _health)
    {
        health = _health;
        healthText.text = health.ToString();
    }
    public void SetPath(Transform[] _path)
    {
        path = _path;
        index = 0;
    }
    #endregion
}
