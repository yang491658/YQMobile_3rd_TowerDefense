using UnityEngine;

public enum SummonType { None, Orbit = 401, Bounce = 402, Zone = 403 }

public class Summon : Pooling
{
    public int ID { private set; get; }

    [Header("Base")]
    [SerializeField] private Tower tower;
    private Vector3 towerPos;
    [SerializeField][Min(0f)] private float speed;
    [SerializeField] private SummonType type;

    [Header("Orbit")]
    [SerializeField][Min(0f)] private float radius;
    [SerializeField][Min(0f)] private float angle;

    [Header("Bounce")]
    [SerializeField] private Monster target;
    private int targetIndex;
    private Vector3 targetPos;
    [SerializeField][Min(0)] private int reserve;

    public bool IsHit { private set; get; } = false;

    [Header("Zone")]
    [SerializeField][Min(0f)] private float duration;
    private float timer;

    protected override void Update()
    {
        base.Update();

        if (IsDespawn) return;

        if (tower == null && type == SummonType.Orbit)
        { Despawn(); return; }

        switch (type)
        {
            case SummonType.Orbit: MoveOrbit(); break;
            case SummonType.Bounce: MoveBounce(); break;
            case SummonType.Zone: MoveZone(); break;
        }
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (IsDespawn) return;

        if (_collision.TryGetComponent(out Monster _monster))
        {
            switch (type)
            {
                case SummonType.Orbit:
                case SummonType.Zone:
                    tower.Hit(_monster, _monster.Index, _monster.transform.position, false);
                    break;

                case SummonType.Bounce:
                    if (IsHit) return;
                    if (target != null && !target.IsInvalid(targetIndex) && target == _monster)
                    {
                        IsHit = true;
                        tower.Hit(target, targetIndex, targetPos, false);
                    }
                    break;
            }
        }
    }

    #region 스킬
    private void MoveOrbit()
    {
        angle += speed * Time.deltaTime;

        Vector3 pos = tower.transform.position;
        float rad = angle * Mathf.Deg2Rad;

        pos.x += Mathf.Cos(rad) * radius;
        pos.y += Mathf.Sin(rad) * radius;

        transform.position = pos;
    }

    private void MoveBounce()
    {
        transform.Rotate(Vector3.forward * speed * 180f * Time.deltaTime);

        if (!IsHit)
        {
            if (target != null && !target.IsInvalid(targetIndex))
                targetPos = target.transform.position;

            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            if ((transform.position - targetPos).sqrMagnitude <= 0.0001f)
                IsHit = true;
        }
        else
        {
            if (tower != null)
                towerPos = tower.transform.position;

            transform.position = Vector3.MoveTowards(transform.position, towerPos, speed * Time.deltaTime);

            if ((transform.position - towerPos).sqrMagnitude <= 0.0001f)
                Despawn();
        }
    }

    private void MoveZone()
    {
        transform.Rotate(Vector3.forward * speed * Time.deltaTime);

        timer -= Time.deltaTime;
        if (timer <= 0f)
            Despawn();
    }
    #endregion

    #region SET
    public void SetSummon(TowerSkill _skill, Tower _tower, float _scale, float _speed)
    {
        transform.localScale = _tower.transform.localScale * _scale;
        SR.sprite = _tower.Icon;
        SR.color = _tower.Color;

        ID = _skill.ID;
        tower = _tower;
        towerPos = _tower.transform.position;
        tower.AddSummon(this);
        speed = _speed;
    }

    public void SetOrbit(float _radius, float _angle)
    {
        type = SummonType.Orbit;
        radius = _radius;
        angle = _angle;
    }

    public void SetBounce(Monster _target)
    {
        type = SummonType.Bounce;
        target = _target;
        targetIndex = _target.Index;
        targetPos = _target.transform.position;

        reserve = tower.Damage;
        target.ReserveUp(reserve);

        IsHit = false;
    }

    public void SetZone(float _duration)
    {
        type = SummonType.Zone;
        duration = _duration;
        timer = _duration;
    }
    #endregion

    #region 프로퍼티
    public SummonType Type => type;
    #endregion

    #region 풀링
    public override void OnSpawnPool()
    {
        base.OnSpawnPool();

        transform.rotation = Quaternion.identity;

        ID = 0;
        IsHit = false;
    }

    public override void OnDespawnPool()
    {
        if (tower != null)
            tower.RemoveSummon(this);

        if (type == SummonType.Bounce && !IsHit
            && target != null && !target.IsInvalid(targetIndex))
            target.ReserveDown(reserve);

        base.OnDespawnPool();
    }

    public override void ResetPool()
    {
        base.ResetPool();

        tower = null;
        towerPos = default;
        speed = 0f;
        type = SummonType.None;

        radius = 0f;
        angle = 0f;

        target = null;
        targetIndex = 0;
        targetPos = default;
        reserve = 0;

        duration = 0f;
        timer = 0f;

        Stop();
    }
    #endregion
}
