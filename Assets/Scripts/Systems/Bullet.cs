using UnityEngine;

public class Bullet : Entity
{
    [Header("Battle")]
    private Monster target;
    private Vector3 targetPos;
    private float speed = 10f;
    private int damage;

    protected override void Update()
    {
        base.Update();

        Shoot();
    }

    #region 전투
    private void Shoot()
    {
        if (target != null)
            targetPos = target.transform.position;

        Vector3 toTarget = targetPos - transform.position;

        if (toTarget.sqrMagnitude < 0.01f)
        {
            if (target != null)
                target.TakeDamage(damage);

            EntityManager.Instance?.DespawnBullet(this);
            return;
        }

        Move(toTarget.normalized * speed);
    }
    #endregion

    #region SET
    public void SetBullet(Transform _symbol)
    {
        transform.localScale = _symbol.localScale * 3f;
        sr.color = _symbol.GetComponent<SpriteRenderer>().color;
    }
    public void SetTarget(Monster _mon) => target = _mon;
    public void SetDamage(int _damage) => damage = _damage;
    #endregion

    #region GET
    #endregion
}
