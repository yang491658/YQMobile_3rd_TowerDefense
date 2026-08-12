using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(Rigidbody2D))]
public class Entity : MonoBehaviour
{
    private SpriteRenderer sr;
    private Collider2D col;
    private Rigidbody2D rb;

    protected virtual void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Start() { }

    protected virtual void Update() { }

    #region 이동
    public void Move(float _speed, Vector3 _direction) => rb.linearVelocity = _speed * _direction.normalized;
    public void Stop() => rb.linearVelocity = Vector2.zero;
    #endregion

    #region 프로퍼티
    public SpriteRenderer SR => sr;
    public Collider2D Col => col;
    public Rigidbody2D RB => rb;
    #endregion
}
