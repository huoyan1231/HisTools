using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HisTools.Features.Controllers;
using HisTools.Utils;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HisTools.Features.Artifacts;

/// <summary>
/// Displays current charge energy of the HandItem_Glove.
/// Shows energy value above the glove and also integrates with the ArtifactsInfo UI list.
/// </summary>
public class GloveEnergyDisplay : ArtifactsInfo
{
    private static FieldInfo _rechargeModuleField;
    private static PropertyInfo _isChargingProperty;
    private static FieldInfo _minChargeField;

    private GameObject _worldLabel;
    private TextMeshPro _worldText;
    private HandItem_Glove _cachedGlove;

    public GloveEnergyDisplay() : base("GloveEnergy", "Show current charge energy of the glove")
    {
        // Reflection for private fields/properties
        _rechargeModuleField ??= typeof(HandItem_Glove).GetField("rechargeAmountModule", BindingFlags.NonPublic | BindingFlags.Instance);
        
        var rechargeType = typeof(ItemExecutionModule_RechargeAmount);
        _isChargingProperty ??= rechargeType.GetProperty("charging", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        _minChargeField ??= rechargeType.GetField("activeBuffMinimumCharge", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    }

    public override void OnEnable()
    {
        // Manager handles WorldUpdate subscription if we were the main ArtifactsInfo,
        // but as a sub-module we just need to ensure our local state is clean.
        CleanupWorldUI();
    }

    public override void OnDisable()
    {
        CleanupWorldUI();
        _cachedGlove = null;
    }

    private void EnsureWorldUI(Transform parent)
    {
        if (_worldLabel != null) return;

        _worldLabel = new GameObject("HisTools_GloveEnergy_Label");
        _worldLabel.transform.SetParent(parent, false);
        _worldLabel.transform.localPosition = Vector3.up * 0.3f; // Slightly above the glove

        _worldText = _worldLabel.AddComponent<TextMeshPro>();
        _worldText.alignment = TextAlignmentOptions.Center;
        _worldText.fontSize = 2f;
        
        // Mirror the text (scale -1 on X) to look correct from behind as in ShowItemInfo
        _worldLabel.transform.localScale = new Vector3(-1, 1, 1);
        
        // Add look-at component
        _worldLabel.AddComponent<UT_LookatPlayer>();
    }

    private void CleanupWorldUI()
    {
        if (_worldLabel != null)
        {
            Object.Destroy(_worldLabel);
            _worldLabel = null;
            _worldText = null;
        }
    }

    protected override List<ArtifactInfoData> GatherData()
    {
        var result = new List<ArtifactInfoData>();
        
        var player = ENT_Player.GetPlayer();
        if (player == null) return result;

        // Try to find the glove in hand items
        var glove = player.GetComponentInChildren<HandItem_Glove>(true);
        if (glove == null)
        {
            CleanupWorldUI();
            return result;
        }

        _cachedGlove = glove;
        
        // 1. Read energy value
        float charge = 0f;
        string chargeStr = glove.item.GetFirstDataStringByType("charge", false);
        if (!string.IsNullOrEmpty(chargeStr))
        {
            float.TryParse(chargeStr, out charge);
        }

        // 2. Read charging state via reflection
        bool isCharging = false;
        float minCharge = 0f;
        
        var module = _rechargeModuleField?.GetValue(glove);
        if (module != null)
        {
            isCharging = (bool)(_isChargingProperty?.GetValue(module) ?? false);
            minCharge = (float)(_minChargeField?.GetValue(module) ?? 0f);
        }

        // 3. Update World UI
        UpdateWorldUI(glove.transform, charge, isCharging);

        // 4. Return data for Screen UI
        string valueStr = isCharging ? $"+{charge:F1}" : $"{charge:F1}";
        Color color = isCharging ? Color.green : Color.white;
        
        result.Add(new ArtifactInfoData
        {
            Name = "Glove Charge",
            Value = valueStr,
            ValueColor = color
        });

        return result;
    }

    private void UpdateWorldUI(Transform parent, float charge, bool isCharging)
    {
        EnsureWorldUI(parent);
        
        if (_worldText != null)
        {
            _worldText.text = isCharging ? $"<color=green>+{charge:F1}</color>" : $"{charge:F1}";
            _worldText.color = isCharging ? Color.green : Color.white;
        }
    }
}
