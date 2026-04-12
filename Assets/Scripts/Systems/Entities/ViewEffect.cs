using System.Collections;
using UnityEngine;

public class ViewEffect : Pooling
{
    private Coroutine routine;

    private void OnBecameInvisible()
    {
        Despawn();
    }

    #region SET
    public void SetEffect(Tower _tower, float _scale, float _duration)
    {
        transform.localScale = Vector3.one * _scale;
        sr.color = _tower.GetColor();
        sr.sprite = _tower.GetImage();

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (_duration > 0f)
            routine = StartCoroutine(EffectCoroutine(sr.color.a, _duration));
    }

    private IEnumerator EffectCoroutine(float _startAlpha, float _duration)
    {
        float time = 0f;
        Color color = sr.color;

        while (time < _duration)
        {
            time += Time.deltaTime;
            float t = time / _duration;
            color.a = Mathf.Lerp(_startAlpha, 0f, t);
            sr.color = color;
            yield return null;
        }

        color.a = 0f;
        sr.color = color;

        routine = null;
        Despawn();
    }
    #endregion

    #region 풀링
    public override void OnSpawnPool()
    {
        base.OnSpawnPool();

        col.enabled = false;
        rb.simulated = false;
    }

    public override void ResetPool()
    {
        base.ResetPool();

        transform.localScale = Vector3.one;
        sr.color = Color.white;
        sr.sprite = null;
        sr.sortingLayerID = SortingLayer.NameToID("Effect");
        sr.sortingOrder = 0;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        Stop();
    }
    #endregion
}