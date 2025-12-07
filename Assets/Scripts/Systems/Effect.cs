using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Effect : MonoBehaviour
{
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    #region SET
    public void SetEffect(Color _color, float _radius, float _duration)
    {
        Color c = _color;
        c.a = 0.1f;
        sr.color = c;

        transform.localScale *= _radius / sr.size.x;

        Destroy(gameObject, _duration);
    }
    #endregion
}
