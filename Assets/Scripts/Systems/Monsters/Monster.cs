using System.Collections;
using TMPro;
using UnityEngine;

public class Monster : Entity
{
    private static int sorting = 0;

    [Header("Text UI")]
    [SerializeField] private Canvas healthCanvas;
    [SerializeField] private TextMeshProUGUI healthText;
    [Space]
    [SerializeField][Min(0f)] private float textDuration = 1f;
    [SerializeField][Min(0f)] private float textSpeed = 3f;
    [SerializeField] private Canvas textCanvas;

    [Header("Move")]
    [SerializeField][Min(0)] private int pathIndex;
    private Transform[] paths;
    [SerializeField][Min(0f)] private float moveSpeed = 3f;
    [SerializeField] private Vector3 moveDir;

    [Header("Battle")]
    [SerializeField][Min(0)] private int health = 50;
    [SerializeField][Min(0)] private int reservedDamage = 0;
    public bool IsDead { private set; get; } = false;
    [SerializeField][Min(0)] private int dropGold = 1;
    [Space]
    [SerializeField] private MonsterDebuff debuff;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Canvas[] canvases = GetComponentsInChildren<Canvas>();
        if (healthCanvas == null) healthCanvas = canvases[0];
        if (textCanvas == null) textCanvas = canvases[1];
        if (healthText == null)
            healthText = healthCanvas.GetComponentInChildren<TextMeshProUGUI>();

        if (debuff == null)
            debuff = GetComponent<MonsterDebuff>();
    }
#endif

    protected override void Awake()
    {
        base.Awake();

        sr.sortingOrder = sorting;
        healthCanvas.sortingOrder = sorting--;
    }

    protected override void Start()
    {
        base.Start();

        SetMonster(GameManager.Instance.GetScore() / 10);
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead) return;

        UpdateMove();
    }

    private void OnBecameInvisible()
    {
        if (IsDead) return;

        GameManager.Instance?.LifeDown(Mathf.Max(health / 10, 1));
        EntityManager.Instance?.DespawnMonster(this);
    }

    #region 이동
    private void UpdateMove()
    {
        if (pathIndex >= paths.Length)
        {
            moveDir = Vector3.right;
            Move(moveDir * moveSpeed);
            return;
        }

        Vector3 delta = paths[pathIndex].position - transform.position;

        float arrive = Mathf.Max(moveSpeed * Time.deltaTime, 0.1f);
        if (delta.sqrMagnitude < arrive * arrive)
        {
            if (++pathIndex >= paths.Length)
            {
                moveDir = Vector3.right;
                Move(moveDir * moveSpeed);
                return;
            }

            delta = paths[pathIndex].position - transform.position;
        }

        moveDir = delta.normalized;
        Move(moveDir * moveSpeed);
    }
    #endregion

    #region 전투
    public void TakeDamage(int _damage, bool _critical = false, bool _direct = false)
    {
        if (IsDead) return;

        if (!_direct)
        {
            reservedDamage -= _damage;
            if (reservedDamage < 0) reservedDamage = 0;
        }

        SetHealth(health - _damage);
        CreateDamage(_damage, _critical);
        if (health <= 0) Die();
    }

    public void ReservedUp(int _damage)
    {
        if (_damage < 0) return;
        reservedDamage += _damage;
    }

    public void ReservedDown(int _damage)
    {
        if (_damage < 0) return;
        reservedDamage -= _damage;
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        sr.enabled = false;
        healthText.enabled = false;

        Effect[] effects = GetComponentsInChildren<Effect>();
        for (int i = 0; i < effects.Length; i++)
            Destroy(effects[i].gameObject);

        GameManager.Instance?.ScoreUp();
        GameManager.Instance?.GoldUp(dropGold);
        StartCoroutine(DieCoroutine());
    }

    private IEnumerator DieCoroutine()
    {
        yield return new WaitForSeconds(textDuration);
        EntityManager.Instance?.DespawnMonster(this);
    }
    #endregion

    #region 데미지 UI
    private void CreateDamage(int _damage, bool _critical = false)
    {
        TextMeshProUGUI t = Instantiate(healthText, textCanvas.transform);

        t.gameObject.name = "Damage";
        t.transform.localPosition = healthText.transform.localPosition;
        t.text = _damage.ToString();
        if (_critical) t.rectTransform.localScale *= 1.2f;

        StartCoroutine(DamageCoroutine(t, _critical));
    }

    private IEnumerator DamageCoroutine(TextMeshProUGUI _text, bool _critical = false)
    {
        float time = 0f;
        Vector3 from = _text.transform.position;
        Vector3 to = new Vector3(0f, AutoCamera.WorldRect.yMax, 0f);
        Vector3 dir = (to - from).normalized;

        while (time < textDuration)
        {
            time += Time.deltaTime;
            float t = time / textDuration;

            _text.transform.position = from + dir * textSpeed * time;

            Color c = _critical ? Color.red : Color.black;
            c.a = Mathf.Lerp(1f, 0f, t);
            _text.color = c;

            yield return null;
        }

        Destroy(_text.gameObject);
    }
    #endregion

    #region 디버프
    public void ApplyDot(int _damage, float _duration, Effect _effect)
        => debuff.ApplyDot(_damage, _duration, _effect);

    public void ApplySlow(int _slow, float _duration, Effect _effect)
        => debuff.ApplySlow(_slow, _duration, _effect);
    #endregion

    #region SET
    public void SetMonster(int _set)
    {
        SetHealth(Mathf.Max(health * _set, health));
        dropGold = Mathf.Max(dropGold * _set, 1);
    }

    public void SetPath(Transform[] _path)
    {
        paths = _path;
        pathIndex = 0;
    }
    public float SetSpeed(float _speed) => moveSpeed = _speed;
    public void SetHealth(int _health)
    {
        health = _health;
        healthText.text = health.ToString();
    }
    #endregion

    #region GET
    public float GetSpeed() => moveSpeed;
    public int GetHealth() => health;

    public bool CanTarget() => !IsDead && health > reservedDamage;
    public bool HasDebuff() => debuff.HasDebuff();
    #endregion
}
