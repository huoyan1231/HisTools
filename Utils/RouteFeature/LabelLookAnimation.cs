using DG.Tweening;
using TMPro;
using UnityEngine;

namespace HisTools.Utils.RouteFeature;

[RequireComponent(typeof(TextMeshPro))]
public class LabelLookAnimation : MonoBehaviour
{
    public float maxDistance = 3f;
    public float tweenDuration = 0.3f;
    public float scaleFactor = 1.6f;
    public float boundsExpansion = 0.5f;
    public Color targetColor = Color.white;
    
    private Transform _cam;
    private Color _baseColor;
    private Vector3 _baseScale;
    private Tween _currentTween;
    private bool _isActive;

    private TextMeshPro _tmp;
    private Renderer _renderer;
    private void Awake()
    {
        _tmp = GetComponent<TextMeshPro>();
        _renderer = GetComponent<Renderer>();
        _cam = Camera.main?.transform;

        _baseColor = _tmp.color;
        _baseScale = _tmp.transform.localScale;
    }

    private void Update()
    {
        if (!_cam || !_tmp || !_renderer)
            return;

        var hitThis = false;
        var ray = new Ray(_cam.position, _cam.forward);

        var bounds = _renderer.bounds;
        bounds.Expand(boundsExpansion);

        if (bounds.IntersectRay(ray, out var distance))
        {
            if (distance <= maxDistance)
                hitThis = true;
        }

        switch (hitThis)
        {
            case true when !_isActive:
                Activate();
                break;
            case false when _isActive:
                Deactivate();
                break;
        }
    }

    private void Activate()
    {
        if (!this || !transform || !_tmp)
        {
            return;
        }

        _isActive = true;
        _currentTween?.Kill();

        _currentTween = DOTween.Sequence()
            .Join(_tmp.DOColor(targetColor, tweenDuration))
            .Join(_tmp.transform.DOScale(_baseScale * scaleFactor, tweenDuration).SetEase(Ease.OutBack));
    }

    private void Deactivate()
    {
        if (!this || !transform || !_tmp)
        {
            return;
        }

        _isActive = false;
        _currentTween?.Kill();

        _currentTween = DOTween.Sequence()
            .Join(_tmp.DOColor(_baseColor, tweenDuration))
            .Join(_tmp.transform.DOScale(_baseScale, tweenDuration).SetEase(Ease.InOutSine));
    }
}