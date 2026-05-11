using System.Collections;
using UnityEngine;

public class ViewEffect : Pooling
{
    private Coroutine routine;

    private IEnumerator EffectCoroutine(float _startAlpha, float _duration)
    {
        float timer = _duration;
        Color color = SR.color;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(timer / _duration);
            color.a = Mathf.Lerp(_startAlpha, 0f, t);
            SR.color = color;
            yield return null;
        }

        color.a = 0f;
        SR.color = color;

        routine = null;
        Despawn();
    }

    #region SET
    public void SetEffect(Tower _tower, float _scale, float _duration)
    {
        transform.localScale = Vector3.one * _scale;
        SR.color = _tower.Color;
        SR.sprite = _tower.Icon;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (_duration > 0f)
            routine = StartCoroutine(EffectCoroutine(SR.color.a, _duration));
    }

    public void SetVisible(bool _visible) => SR.enabled = _visible;
    #endregion

    #region 풀링
    public override void OnSpawnPool()
    {
        base.OnSpawnPool();

        SR.enabled = true;
        Col.enabled = false;
        RB.simulated = false;
    }

    public override void ResetPool()
    {
        base.ResetPool();

        transform.localScale = Vector3.one;
        SR.color = Color.white;
        SR.sprite = null;
        SR.sortingLayerID = SortingLayer.NameToID("Effect");
        SR.sortingOrder = 0;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        Stop();
    }
    #endregion
}
