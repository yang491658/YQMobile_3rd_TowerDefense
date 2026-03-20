using UnityEngine;

public class Bullet : Pooling
{
    [Header("Origin")]
    [SerializeField] private Tower tower;

    [Header("Move")]
    [SerializeField] private Monster target;
    [SerializeField][Min(0f)] private float moveSpeed = 10f;

    public bool IsHit { private set; get; } = false;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
    }

    private void OnBecameInvisible()
    {
        Despawn();
    }

    #region SET
    public void SetBullet(Tower _tower, Monster _target)
    {
        sr.color = _tower.GetColor();
        tower = _tower;
        target = _target;
    }
    #endregion

    #region GET
    public Tower GetTower() => tower;
    public Monster GetTarget() => target;
    #endregion

    #region 풀링
    public override void OnSpawnPool()
    {
        base.OnSpawnPool();

        IsHit = false;
    }

    public override void OnDespawnPool()
    {
        base.OnDespawnPool();
    }

    public override void ResetPool()
    {
        base.ResetPool();

        tower = null;
        target = null;

        Stop();
    }
    #endregion
}
