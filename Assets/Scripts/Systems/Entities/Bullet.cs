using UnityEngine;

public class Bullet : Pooling
{
    [Header("Origin")]
    [SerializeField] private Tower tower;

    [Header("Battle")]
    [SerializeField][Min(0)] private int reserve;

    [Header("Move")]
    [SerializeField] private Monster target;
    private int targetIndex;
    private Vector3 targetPos;
    [SerializeField][Min(0f)] private float moveSpeed = 10f;

    public bool IsHit { private set; get; } = false;

    protected override void Update()
    {
        base.Update();

        if (target != null && !target.IsInvalid(targetIndex))
            targetPos = target.transform.position;
    }

    private void FixedUpdate()
    {
        if (IsDespawn || IsHit) return;

        float step = moveSpeed * Time.fixedDeltaTime;

        Vector2 from = RB.position;
        Vector2 to = Vector2.MoveTowards(from, targetPos, step);

        RB.MovePosition(to);

        Vector2 delta = (Vector2)targetPos - to;
        if (delta.sqrMagnitude > 0.0001f) return;

        Hit();
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (IsDespawn || IsHit) return;

        if (target != null && !target.IsInvalid(targetIndex)
            && target.gameObject == _collision.gameObject)
            Hit();
    }

    private void Hit()
    {
        IsHit = true;
        tower.Hit(this, target, targetIndex, targetPos);

        Despawn();
    }

    private void OnBecameInvisible()
    {
        Despawn();
    }

    #region SET
    public void SetBullet(Tower _tower, Monster _target)
    {
        transform.localScale = _tower.transform.localScale * 0.3f;
        SR.sprite = _tower.Symbol;
        SR.color = _tower.Color;

        tower = _tower;
        tower.AddBullet(this);
        reserve = _tower.Damage;

        target = _target;
        targetIndex = target.Index;
        targetPos = target.transform.position;
        target.ReserveUp(reserve);
    }
    #endregion

    #region 풀링
    public override void OnSpawnPool()
    {
        base.OnSpawnPool();

        IsHit = false;
    }

    public override void OnDespawnPool()
    {
        if (tower != null)
            tower.RemoveBullet(this);

        if (!IsHit && reserve > 0
            && target != null && !target.IsInvalid(targetIndex))
            target.ReserveDown(reserve);

        base.OnDespawnPool();
    }

    public override void ResetPool()
    {
        base.ResetPool();

        tower = null;
        reserve = 0;

        target = null;
        targetIndex = 0;
        targetPos = default;
        moveSpeed = 10f;

        Stop();
    }
    #endregion
}
