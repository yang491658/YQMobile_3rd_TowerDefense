using UnityEngine;

public class Bullet : Entity
{
    [Header("Move")]
    [SerializeField] private Monster target;
    private Vector3 targetPos;
    [Space]
    [SerializeField] private float moveSpeed = 10f;

    [Header("Battle")]
    private TowerBase tower;
    [SerializeField] private int attackDamage;

    protected override void Update()
    {
        base.Update();

        UpdateMove();
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (target != null && target.gameObject == _collision.gameObject)
        {
            tower.HitBullet(target);
            target.TakeDamage(attackDamage, sr.color);

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
        if (target != null)
            targetPos = target.transform.position;

        Vector3 toBefore = targetPos - transform.position;
        Vector3 dir = toBefore.normalized;

        Move(dir * moveSpeed);

        Vector3 toAfter = targetPos - transform.position;

        if (toBefore != Vector3.zero && Vector3.Dot(toBefore, toAfter) < 0.3f)
            Destroy(gameObject);
    }

    #region SET
    public void SetBullet(TowerBase _tower)
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
        targetPos = target.transform.position;
        attackDamage = _tower.GetDamage();

        tower.AddBullet(this);
    }
    #endregion

    #region GET
    #endregion
}
