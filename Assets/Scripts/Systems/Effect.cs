using System.Collections;
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
    public void SetEffect(Tower _tower, float _scale = 1f, float _duration = 0f)
    {
        sr.color = _tower.GetColor();
        sr.sprite = _tower.GetSymbol();

        transform.localScale *= _scale;

        float startAlpha = sr.color.a;
        float duration = _duration > 0f ? _duration : 0.3f;

        StartCoroutine(EffectCoroutine(startAlpha, duration));
    }
    #endregion

    private IEnumerator EffectCoroutine(float _startAlpha, float _duration)
    {
        float time = 0f;
        Color c = sr.color;

        while (time < _duration)
        {
            time += Time.deltaTime;
            float t = time / _duration;
            c.a = Mathf.Lerp(_startAlpha, 0f, t);
            sr.color = c;
            yield return null;
        }

        c.a = 0f;
        sr.color = c;
        Destroy(gameObject);
    }
}
