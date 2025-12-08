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
    public void SetEffect(Tower _tower, float _scale, float _duration = 0)
    {
        gameObject.name = _tower.name + "'s Debuff";

        Color c = _tower.GetColor();
        c.a = 30f/ 255f;
        sr.color = c;

        transform.localScale *= _scale;

        if (_duration > 0)
            Destroy(gameObject, _duration);
    }
    #endregion
}
