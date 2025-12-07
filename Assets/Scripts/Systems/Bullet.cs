using UnityEngine;

public class Bullet : Entity
{
    [Header("Move")]
    [SerializeField] private Monster target;
    [Space]
    [SerializeField] private float moveSpeed = 10f;
    private Vector3 moveDir;

    [Header("Battle")]
    [SerializeField] private Tower tower;

    protected override void Update()
    {
        base.Update();

        UpdateMove();
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (target != null && !target.IsDead && target.gameObject == _collision.gameObject)
        {
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
    }

    public virtual void UpdateMove()
    {
        if (target != null && !target.IsDead)
            moveDir = (target.transform.position - transform.position).normalized;

        Move(moveDir * moveSpeed);
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
        target = _tower.GetTarget();
        moveDir = (target.transform.position - transform.position).normalized;

        tower.AddBullet(this);
    }
    #endregion
}
