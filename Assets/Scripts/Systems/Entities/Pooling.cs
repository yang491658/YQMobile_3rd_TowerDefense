public abstract class Pooling : Entity, IPoolable
{
    public bool IsDespawn { protected set; get; } = true;

    public virtual void OnSpawnPool()
    {
        IsDespawn = false;
    }

    public virtual void OnDespawnPool()
    {
        IsDespawn = true;

        ResetPool();
    }

    public virtual void ResetPool() { }

    public void Despawn()
    {
        if (IsDespawn) return;

        EntityManager.Instance?.DespawnPool(this);
    }
}
