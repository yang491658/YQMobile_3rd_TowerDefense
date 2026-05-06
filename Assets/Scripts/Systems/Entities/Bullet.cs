using System.Collections.Generic;
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
    [SerializeField] private Vector2 moveDirection;
    private readonly List<int> hits = new();

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

        if (target == null)
        {
            rb.MovePosition(rb.position + moveDirection * step);
            return;
        }

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

        if (target == null && _collision.TryGetComponent(out Monster _monster))
        {
            if (hits.Contains(_monster.Index)) return;

            hits.Add(_monster.Index);
            tower.Hit(this, _monster, _monster.Index, _monster.transform.position);
            return;
        }

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
        sr.sprite = _tower.GetSymbol();
        sr.color = _tower.GetColor();

        tower = _tower;
        tower.AddBullet(this);
        reserve = _tower.GetDamage();

        SetTarget(_target);
    }

    public void SetTarget(Monster _target)
    {
        if (!IsHit && reserve > 0
            && target != null && !target.IsInvalid(targetIndex))
            target.ReserveDown(reserve);

        target = _target;
        if (target == null) return;

        target.ReserveUp(reserve);
        targetIndex = target.Index;
        targetPos = target.transform.position;
        moveDirection = (target.transform.position - transform.position).normalized;
    }
    #endregion

    #region GET
    public int GetHitCount() => hits.Count;
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
        moveDirection = default;
        hits.Clear();

        Stop();
    }
    #endregion
}
