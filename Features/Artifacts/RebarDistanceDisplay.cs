using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HisTools.Patches;
using UnityEngine;

namespace HisTools.Features.Artifacts;

/// <summary>
/// Subclass of ArtifactsInfo that displays distance to returning rebars.
/// </summary>
public class RebarDistanceDisplay : ArtifactsInfo
{
    private readonly List<Projectile_ReturnRebar> _activeRebars = new();
    private static FieldInfo _hasHitSurfaceField;

    public RebarDistanceDisplay() : base("RebarDistance", "Show distance to returning rebars")
    {
        _hasHitSurfaceField ??= typeof(Projectile_ReturnRebar).GetField("hasHitSurface", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    public override void OnEnable()
    {
        // We don't call base.OnEnable() because we don't want to subscribe to WorldUpdate here.
        // The manager (ArtifactsInfo) handles that.
        
        Projectile_ReturnRebarPatch.OnSpawned += HandleRebarSpawned;
        // OnDespawned is no longer fired as per the note in the patch, so we handle it in GatherData via null check.
    }

    public override void OnDisable()
    {
        Projectile_ReturnRebarPatch.OnSpawned -= HandleRebarSpawned;
        _activeRebars.Clear();
    }

    private void HandleRebarSpawned(Projectile_ReturnRebar rebar)
    {
        if (rebar != null && !_activeRebars.Contains(rebar))
        {
            _activeRebars.Add(rebar);
        }
    }

    private bool GetHasHitSurface(Projectile_ReturnRebar rebar)
    {
        if (_hasHitSurfaceField == null) return false;
        try
        {
            return (bool)_hasHitSurfaceField.GetValue(rebar);
        }
        catch
        {
            return false;
        }
    }

    protected override List<ArtifactInfoData> GatherData()
    {
        var result = new List<ArtifactInfoData>();
        var player = ENT_Player.playerObject;
        if (player == null) return result;

        // Cleanup destroyed rebars and collect distances for returning or stuck ones
        for (int i = _activeRebars.Count - 1; i >= 0; i--)
        {
            var rebar = _activeRebars[i];
            if (rebar == null)
            {
                _activeRebars.RemoveAt(i);
                continue;
            }

            bool isReturning = rebar.returning;
            bool isStuck = GetHasHitSurface(rebar);

            if (isReturning || isStuck)
            {
                float dist = Vector3.Distance(player.transform.position, rebar.transform.position);
                string label = isReturning ? "Returning Rebar" : "Stuck Rebar";
                
                result.Add(new ArtifactInfoData
                {
                    Name = label,
                    Value = $"{dist:F1}m",
                    ValueColor = GetDistanceColor(dist)
                });
            }
        }

        return result;
    }

    private Color GetDistanceColor(float distance)
    {
        if (distance < 5f) return new Color(1f, 0.5f, 0f); // Orange for close
        if (distance < 15f) return Color.yellow;
        return Color.green;
    }
}
