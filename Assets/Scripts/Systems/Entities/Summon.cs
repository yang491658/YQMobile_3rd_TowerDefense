using UnityEngine;

public class Summon : Pooling
{
    public int ID { private set; get; }

    [Header("Origin")]
    [SerializeField] private Tower tower;
    [SerializeField] private Monster target;
    private int targetIndex;
    private Vector3 targetPos;

    [Header("Move")]
    [SerializeField][Min(0f)] private float speed;
    [SerializeField][Min(0f)] private float rate;
    [SerializeField] private Vector3[] path;
    [SerializeField][Min(0)] private int pathIndex;
    private bool loop = false;

    [Header("Battle")]
    [SerializeField][Min(0)] private int reserve;
    private bool isHit = false;
    private bool onHit = false;

    [Header("Life")]
    [SerializeField][Min(0f)] private float duration;
    private float timer;
    [SerializeField][Min(0f)] private float rotate;

    protected override void Update()
    {
        base.Update();

        if (tower == null)
        { Despawn(); return; }

        if (target != null && !target.IsInvalid(targetIndex))
            targetPos = target.transform.position;

        if (duration > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                Despawn();
        }

        transform.Rotate(0f, 0f, rotate * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (IsDespawn) return;

        UpdateMove(Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (IsDespawn) return;

        if (_collision.TryGetComponent(out Monster _monster))
        {
            if (target != null)
            {
                if (isHit) return;

                if (!target.IsInvalid(targetIndex) && target == _monster)
                {
                    isHit = true;
                    tower.Hit(_monster, onHit);
                }
            }
            else tower.Hit(_monster, onHit);
        }
    }

    private void OnBecameInvisible()
    {
        Despawn();
    }

    private void UpdateMove(float _deltaTime)
    {
        speed = tower.GetSpeed() * rate;
        if (speed <= 0f) return;

        Vector3 nextPos;
        bool hasTarget = target != null;

        if (hasTarget)
            nextPos = targetPos;
        else
        {
            if (path == null || path.Length == 0) return;

            if (pathIndex >= path.Length)
            {
                if (loop) pathIndex = 0;
                else { Despawn(); return; }
            }

            nextPos = path[pathIndex];
        }

        Vector2 current = rb.position;
        Vector2 move = Vector2.MoveTowards(current, nextPos, speed * _deltaTime);
        rb.MovePosition(move);

        Vector2 delta = (Vector2)nextPos - move;
        if (delta.sqrMagnitude > 0.0001f) return;

        if (hasTarget)
        {
            target = null;
            targetIndex = 0;
            targetPos = default;
            return;
        }

        if (++pathIndex < path.Length) return;

        if (loop) pathIndex = 0;
        else Despawn();
    }

    #region SET
    public void SetSummon(TowerSkill _skill, Tower _tower, float _scale = 1f, float _rate = 1f)
    {
        transform.localScale = _tower.transform.localScale * _scale;
        sr.sprite = _tower.GetIcon();
        sr.color = _tower.GetColor();

        ID = _skill.ID;
        tower = _tower;
        tower.AddSummon(this);
        rate = _rate;
    }

    public void SetOrbit(float _radius, float _angle)
    {
        const int count = 36;

        path = new Vector3[count];
        Vector3 center = tower.transform.position;

        for (int i = 0; i < count; i++)
        {
            float angle = (_angle + 360f / count * i) * Mathf.Deg2Rad;
            path[i] = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * _radius;
        }

        transform.position = path[0];
        pathIndex = 1;
        loop = true;
    }
    public void SetBounce(Monster _target, float _rotate = 0f)
    {
        target = _target;
        targetIndex = _target.Index;
        targetPos = _target.transform.position;

        path = new[] { tower.transform.position };
        pathIndex = 0;
        loop = false;

        reserve = tower.GetDamage();
        isHit = false;
        onHit = true;
        target.ReserveUp(reserve);

        rotate = _rotate;
    }
    public void SetZone(float _duration, float _rotate)
    {
        duration = _duration;
        timer = _duration;
        rotate = _rotate;
    }
    #endregion

    #region 풀링
    public override void OnSpawnPool()
    {
        base.OnSpawnPool();

        transform.rotation = Quaternion.identity;
    }

    public override void OnDespawnPool()
    {
        if (tower != null)
            tower.RemoveSummon(this);

        if (!isHit && reserve > 0 && target != null && !target.IsInvalid(targetIndex))
            target.ReserveDown(reserve);

        base.OnDespawnPool();
    }

    public override void ResetPool()
    {
        base.ResetPool();

        ID = 0;

        tower = null;
        target = null;
        targetIndex = 0;
        targetPos = default;

        speed = 0f;
        rate = 0f;
        path = null;
        pathIndex = 0;
        loop = false;

        reserve = 0;
        isHit = false;
        onHit = false;

        duration = 0f;
        timer = 0f;
        rotate = 0f;

        Stop();
    }
    #endregion
}