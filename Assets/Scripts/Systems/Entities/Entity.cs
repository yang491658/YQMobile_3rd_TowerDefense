using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(Rigidbody2D))]
public class Entity : MonoBehaviour
{
    protected SpriteRenderer sr;
    protected Collider2D col;
    protected Rigidbody2D rb;

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

    #region GET
    public SpriteRenderer GetSR() => sr;
    public Collider2D GetCol() => col;
    public Rigidbody2D GetRb() => rb;
    #endregion
}
