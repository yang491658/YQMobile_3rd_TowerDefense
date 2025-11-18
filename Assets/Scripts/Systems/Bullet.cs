using UnityEngine;

public class Bullet : Entity
{
    [Header("Battle")]
    private Monster target;
    private float speed = 15f;
    private int damage;
    private float hitDistance = 0.1f;

    private Vector3 targetPosition;

    protected override void Start()
    {
        base.Start();

        if (target != null)
            targetPosition = target.transform.position;
    }

    protected override void Update()
    {
        base.Update();

        Shoot();
    }

    #region ¿¸≈ı
    private void Shoot()
    {
        if (target != null)
            targetPosition = target.transform.position;

        Vector3 toTarget = targetPosition - transform.position;
        float sqrDistance = toTarget.sqrMagnitude;
        float hitSqr = hitDistance * hitDistance;

        if (sqrDistance <= hitSqr)
        {
            if (target != null)
                target.TakeDamage(damage);

            EntityManager.Instance.DespawnBullet(this);
            return;
        }

        Vector3 direction = toTarget.normalized;
        Move(direction * speed);
    }
    #endregion

    #region SET
    public void SetBullet(Transform _symbol)
    {
        transform.localScale = _symbol.localScale * 2f;
        sr.color = _symbol.GetComponent<SpriteRenderer>().color;
    }
    public void SetTarget(Monster _mon)
    {
        target = _mon;

        if (target != null)
            targetPosition = target.transform.position;
    }

    public void SetDamage(int _damage) => damage = _damage;
    #endregion

    #region GET
    #endregion
}
