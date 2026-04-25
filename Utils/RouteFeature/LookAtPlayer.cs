using TMPro;
using UnityEngine;

namespace HisTools.Utils.RouteFeature;

[RequireComponent(typeof(TextMeshPro))]
public class LookAtPlayer : MonoBehaviour
{
    public Transform player;
    public float minDistanceSqr = 0.1f;
    public Color textColor = Color.clear;

    private TextMeshPro _tmp;

    private void Awake()
    {
        _tmp = GetComponent<TextMeshPro>();
    }

    private void Start()
    {
        if (_tmp == null)
            Debug.LogWarning($"[{nameof(LookAtPlayer)}] TextMeshPro missing on {gameObject.name}");
    }

    private void Update()
    {
        if (player == null || _tmp == null) return;

        var lookDir = transform.position - player.position;

        if (!(lookDir.sqrMagnitude > minDistanceSqr)) return;

        transform.rotation = Quaternion.LookRotation(lookDir);
        if (textColor != Color.clear)
            _tmp.color = textColor;
    }
}