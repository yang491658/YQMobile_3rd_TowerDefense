using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Effect : MonoBehaviour
{
    private SpriteRenderer sr;

    [SerializeField] private float duration = 0.5f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    #region SET
    public void SetEffect(Color _color, float _radius)
    {
        Color c = _color;
        c.a = 30f / 255f;
        sr.color = c;

        float currentRadius = sr.bounds.extents.x;
        if (currentRadius <= 0f) return;

        float scaleFactor = _radius / currentRadius;
        transform.localScale *= scaleFactor;

        Destroy(gameObject, duration);
    }
    #endregion
}
