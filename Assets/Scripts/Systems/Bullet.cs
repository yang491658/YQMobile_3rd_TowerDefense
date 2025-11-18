using UnityEngine;

public class Bullet : Entity
{
    [Header("Battle")]
    [SerializeField] private Monster target;
    private Vector3 targetPos;
    [SerializeField] private int damage;
    [SerializeField] private float speed = 10f;

    protected override void Update()
    {
        base.Update();

        Shoot();
    }

    #region 전투
    public virtual void Shoot()
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
    public void SetColor(Color _color) => sr.color = _color;
    public void SetTarget(Monster _mon) => target = _mon;
    public void SetDamage(int _damage) => damage = _damage;
    #endregion

    #region GET
    #endregion
}
