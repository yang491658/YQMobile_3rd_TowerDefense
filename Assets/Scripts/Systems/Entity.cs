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
    public void Move(Vector3 _velocity) => rb.linearVelocity = _velocity;
    public void Stop() => Move(Vector3.zero);
    #endregion

    #region SET
    #endregion

    #region GET
    public SpriteRenderer GetSR() => sr;
    #endregion
}
