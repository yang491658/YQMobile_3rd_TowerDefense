using UnityEngine;

public class Bullet : Entity
{
    [Header("Battle")]
    [SerializeField] private Monster target;
    private Vector3 targetPos;
    [SerializeField] private float moveSpeed = 10f;

    [SerializeField] private int attackDamage;

    protected override void Update()
    {
        base.Update();

        Chase();
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (target != null)
        {
            if (target.gameObject == _collision.gameObject)
            {
                target.TakeDamage(attackDamage);

                Destroy(gameObject);
                return;
            }
        }
    }

    #region 전투
    public virtual void Chase()
    {
        if (target != null)
            targetPos = target.transform.position;

        Vector3 toTarget = targetPos - transform.position;

        if (target == null)
        {
            if (toTarget.sqrMagnitude < 0.01f)
            {
                Destroy(gameObject);
                return;
            }
        }

        Move(toTarget.normalized * moveSpeed);
    }
    #endregion

    #region SET
    public void SetBullet(Tower _tower)
    {
        Vector3 baseScale = transform.localScale;
        Vector3 towerScale = _tower.transform.lossyScale;
        transform.localScale = new Vector3(
            baseScale.x / towerScale.x,
            baseScale.y / towerScale.y,
            baseScale.z / towerScale.z
        );

        sr.color = _tower.GetColor();
        target = _tower.GetTarget();
        attackDamage = _tower.GetDamage();
    }
    #endregion

    #region GET
    #endregion
}
