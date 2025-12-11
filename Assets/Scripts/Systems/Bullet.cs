using UnityEngine;

public class Bullet : Entity
{
    [Header("Origin")]
    [SerializeField] private Tower tower;

    [Header("Battle")]
    [SerializeField][Min(0)] private int damage;
    public bool IsHit { private set; get; } = false;

    [Header("Move")]
    [SerializeField] private Monster target;
    private Vector3 targetPos;
    [SerializeField][Min(0f)] private float moveSpeed = 10f;

    protected override void Update()
    {
        base.Update();

        UpdateMove();
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (target != null && !target.IsDead && target.gameObject == _collision.gameObject)
        {
            IsHit = true;
            tower.HitBullet(target);
            Destroy(gameObject);
            return;
        }
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        tower.RemoveBullet(this);

        if (!IsHit && target != null && damage > 0)
            target.ReservedDown(damage);
    }

    public virtual void UpdateMove()
    {
        if (target != null && !target.IsDead)
            targetPos = target.transform.position;

        Vector3 toBefore = targetPos - transform.position;
        Vector3 dir = toBefore.normalized;

        Move(dir * moveSpeed);

        Vector3 toAfter = targetPos - transform.position;

        if (Vector3.Dot(toBefore, toAfter) < 0.3f)
        {
            if (target == null || target.IsDead)
                tower.HitBullet(targetPos);

            Destroy(gameObject);
        }
    }

    #region SET
    public void SetBullet(Tower _tower)
    {
        tower = _tower;

        Vector3 baseScale = transform.localScale;
        Vector3 towerScale = _tower.transform.lossyScale;
        transform.localScale = new Vector3(
            baseScale.x / towerScale.x,
            baseScale.y / towerScale.y,
            baseScale.z / towerScale.z
        );
        sr.color = _tower.GetColor();

        damage = _tower.GetDamage();
        target = _tower.GetTarget();
        targetPos = target.transform.position;
        target.ReservedUp(damage);

        tower.AddBullet(this);
    }
    #endregion
}
