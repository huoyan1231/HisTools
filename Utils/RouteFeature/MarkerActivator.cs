using DG.Tweening;
using UnityEngine;

namespace HisTools.Utils.RouteFeature;

public class MarkerActivator : MonoBehaviour
{
    public float bounceDuration = 0.25f;
    public float bounceStrength = 0.40f;

    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void ActivateMarker(Color targetColor)
    {
        if (!this || !_renderer || !_renderer.material)
        {
            return;
        }

        transform.DOPunchScale(Vector3.one * bounceStrength, bounceDuration, 1, 0.5f);

        _renderer.material.DOColor(targetColor, bounceDuration);
    }
}