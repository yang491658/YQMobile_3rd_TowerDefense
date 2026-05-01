using UnityEngine;

public class Bullet : Pooling
{
    [Header("Origin")]
    [SerializeField] private Tower tower;

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

        Vector2 from = rb.position;
        Vector2 to = Vector2.MoveTowards(from, targetPos, step);

        rb.MovePosition(to);

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
        tower.Hit(target, targetIndex, targetPos);
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
        sr.sprite = _tower.GetSymbol();
        sr.color = _tower.GetColor();

        tower = _tower;
        tower.AddBullet(this);

        target = _target;
        targetIndex = _target.Index;
        targetPos = _target.transform.position;
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

        base.OnDespawnPool();
    }

    public override void ResetPool()
    {
        base.ResetPool();

        tower = null;
        target = null;
        targetIndex = 0;
        targetPos = default;
        moveSpeed = 10f;

        Stop();
    }
    #endregion
}
