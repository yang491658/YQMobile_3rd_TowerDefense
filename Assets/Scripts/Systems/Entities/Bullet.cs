using UnityEngine;

public class Bullet : Pooling
{
    [Header("Origin")]
    [SerializeField] private Tower tower;
    [SerializeField][Min(0)] private int damage;

    [Header("Move")]
    [SerializeField] private Monster target;
    private int targetIndex;
    private Vector3 targetPos;
    [SerializeField][Min(0f)] private float moveSpeed = 10f;

    public bool IsHit { private set; get; } = false;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();

        if (target != null && !target.IsInvalid(targetIndex))
            targetPos = target.transform.position;

        Vector3 direction = targetPos - transform.position;
        if (direction.sqrMagnitude <= 0.01f)
        {
            Despawn();
            return;
        }

        Move(moveSpeed, direction);
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (target != null && !target.IsInvalid(targetIndex)
            && target.gameObject == _collision.gameObject)
        {
            IsHit = true;
            tower.HitBullet(target, damage);
            Despawn();
        }
    }

    private void OnBecameInvisible()
    {
        Despawn();
    }

    #region SET
    public void SetBullet(Tower _tower, Monster _target)
    {
        sr.color = _tower.GetColor();

        tower = _tower;
        damage = _tower.GetDamage();

        target = _target;
        targetIndex = _target.Index;
        targetPos = _target.transform.position;
    }
    #endregion

    #region GET
    public Tower GetTower() => tower;
    public int GetDamage() => damage;

    public Monster GetTarget() => target;
    #endregion

    #region 풀링
    public override void OnSpawnPool()
    {
        base.OnSpawnPool();

        IsHit = false;
    }

    public override void OnDespawnPool()
    {
        base.OnDespawnPool();
    }

    public override void ResetPool()
    {
        base.ResetPool();

        tower = null;
        damage = 0;

        target = null;
        targetIndex = 0;
        targetPos = default;
        moveSpeed = 10f;

        Stop();
    }
    #endregion
}
