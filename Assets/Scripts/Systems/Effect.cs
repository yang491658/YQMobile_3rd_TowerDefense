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
    public void SetEffect(Tower _tower, float _alpha = 0.5f, float _scale = 1f, float _duration = 0f)
    {
        gameObject.name = _tower.name + "'s Debuff";

        Color c = _tower.GetColor();
        c.a = _alpha;
        sr.color = c;

        transform.localScale *= _scale;

        float startAlpha;
        float fadeDuration;

        if (_duration > 0f)
        {
            startAlpha = 0.5f;
            fadeDuration = _duration;

            c.a = startAlpha;
            sr.color = c;
        }
        else
        {
            startAlpha = _alpha;
            fadeDuration = 0.3f;
        }

        StartCoroutine(EffectCoroutine(startAlpha, fadeDuration));
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
