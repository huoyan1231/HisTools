using System.Text;
using HisTools.Features.Controllers;
using HisTools.Utils;
using HisTools.Utils.RouteFeature;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace HisTools.Features;

public class ShowItemInfo : FeatureBase
{
    private GameObject _itemInfoPrefab;

    public ShowItemInfo() : base("ShowItemInfo", "Show spawn chances")
    {
        AddSetting(new BoolSetting(this, "Color from palette", "Prefer color from accent palette", true));
        AddSetting(new BoolSetting(this, "Item name", "Show item text label", true));
        AddSetting(new BoolSetting(this, "Spawn chance", "Show item spawn chance", true));
        AddSetting(new FloatSliderSetting(this, "Label size", "...", 0.5f, 0.1f, 2f, 0.05f, 2));
        AddSetting(new ColorSetting(this, "Label color", "...", Color.gray));
    }

    private void EnsurePrefabs()
    {
        if (_itemInfoPrefab != null) return;

        _itemInfoPrefab = new GameObject($"HisTools_ItemInfo_Prefab");
        var tmp = _itemInfoPrefab.AddComponent<TextMeshPro>();
        tmp.text = "ItemInfo";
        tmp.fontSize = GetSetting<FloatSliderSetting>("Label size").Value;
        tmp.color = GetSetting<ColorSetting>("Label color").Value;
        tmp.alignment = TextAlignmentOptions.Center;
        // Mirror the text (scale -1 on X) so it looks correct when viewed from behind
        _itemInfoPrefab.transform.localScale = new Vector3(-1, 1, 1);
        // LookAtPlayer was renamed to UT_LookatPlayer in game update and no longer needs a player reference
        _itemInfoPrefab.AddComponent<UT_LookatPlayer>();
        _itemInfoPrefab.SetActive(false);
    }

    public override void OnEnable()
    {
        EventBus.Subscribe<EnterLevelEvent>(OnEnterLevel);
    }

    public override void OnDisable()
    {
        EventBus.Unsubscribe<EnterLevelEvent>(OnEnterLevel);
    }

    private void OnEnterLevel(EnterLevelEvent e)
    {
        EnsurePrefabs();

        var entities = e.Level.GetComponentsInChildren<GameEntity>();
        if (entities == null) return;
        foreach (var entity in entities)
        {
            var spawnChance = entity.GetComponent<UT_SpawnChance>();
            if (!spawnChance || spawnChance.spawnSettings == null) continue;

            var label = Object.Instantiate(_itemInfoPrefab, entity.transform.position + Vector3.up * 0.5f,
                Quaternion.identity);
            var tmp = label.GetComponent<TextMeshPro>();
            var finalText = new StringBuilder();

            if (GetSetting<BoolSetting>("Color from palette").Value)
                tmp.color = Palette.FromHtml(Plugin.AccentHtml.Value);
            if (GetSetting<BoolSetting>("Item name").Value)
                finalText.Append(entity.name).Append(" - ");
            if (GetSetting<BoolSetting>("Spawn chance").Value)
                finalText.Append(spawnChance.spawnSettings.GetEffectiveSpawnChance() * 100f).Append("%");

            tmp.fontSize = GetSetting<FloatSliderSetting>("Label size").Value;
            tmp.text = finalText.ToString();
            label.SetActive(true);
        }
    }
}