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
    public void SetEffect(Color _color, float _scale, float _duration = 0)
    {
        Color c = _color;
        c.a = 0.15f;
        sr.color = c;

        transform.localScale *= _scale;

        if (_duration > 0)
            Destroy(gameObject, _duration);
    }
    #endregion
}
