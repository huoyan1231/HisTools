using System;
using System.Collections.Generic;
using System.Linq;
using HisTools.Features.Controllers;
using HisTools.Prefabs;
using HisTools.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HisTools.Features;

/// <summary>
/// A single artifact info slot in the horizontal bar.
/// </summary>
public struct ArtifactSlot
{
    public Transform Transform;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI ValueText;
}

/// <summary>
/// Data for one active artifact info.
/// </summary>
public struct ArtifactInfoData
{
    public string Name;
    public string Value;
    public Color ValueColor;
}

/// <summary>
/// Base class for artifact info features and the main entry manager.
/// Inherits from FeatureBase to provide common settings and lifecycle.
/// </summary>
public class ArtifactsInfo : FeatureBase
{
    protected static Canvas _canvas;
    protected static HorizontalLayoutGroup _layout;
    protected static readonly List<ArtifactSlot> _slots = new();
    private const int MaxSlots = 8;

    // Static list of all artifact modules (subclasses)
    private static readonly List<ArtifactsInfo> _modules = new();
    
    // Settings (Shared by subclasses)
    protected readonly FloatSliderSetting _uiPosition;
    protected readonly BoolSetting _showOnlyInPause;

    protected ArtifactsInfo(string name, string description) : base(name, description)
    {
        _uiPosition = AddSetting(new FloatSliderSetting(this, "UI Position", "...", 3f, 1f, 6f, 1f, 0));
        _showOnlyInPause = AddSetting(new BoolSetting(this, "ShowOnlyInPause", "Only show when paused", false));
    }

    // Default constructor for the "Main Entry" feature
    public ArtifactsInfo() : base("ArtifactsInfo", "Unified entrance for all artifacts info")
    {
        _uiPosition = AddSetting(new FloatSliderSetting(this, "UI Position", "...", 3f, 1f, 6f, 1f, 0));
        _showOnlyInPause = AddSetting(new BoolSetting(this, "ShowOnlyInPause", "Only show when paused", false));
        
        // Find and instantiate all subclasses (except this one if it's not the manager)
        InitializeSubModules();
    }

    private void InitializeSubModules()
    {
        if (GetType() != typeof(ArtifactsInfo)) return;
        
        var subTypes = GetType().Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ArtifactsInfo)) && !t.IsAbstract);
            
        foreach (var type in subTypes)
        {
            try
            {
                var module = (ArtifactsInfo)Activator.CreateInstance(type);
                _modules.Add(module);
                
                // Add a toggle setting for this module in the main feature
                AddSetting(new BoolSetting(this, $"Enable {type.Name}", $"Show info for {type.Name}", true));
                
                Utils.Logger.Info($"ArtifactsInfo: Loaded sub-module {type.Name}");
            }
            catch (Exception e)
            {
                Utils.Logger.Error($"ArtifactsInfo: Failed to load sub-module {type.Name}: {e}");
            }
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        if (GetType() == typeof(ArtifactsInfo))
        {
            EventBus.Subscribe<WorldUpdateEvent>(OnWorldUpdate);
            foreach (var module in _modules) module.OnEnable();
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (GetType() == typeof(ArtifactsInfo))
        {
            DestroyUI();
            EventBus.Unsubscribe<WorldUpdateEvent>(OnWorldUpdate);
            foreach (var module in _modules) module.OnDisable();
        }
    }

    protected virtual void OnWorldUpdate(WorldUpdateEvent e)
    {
        if (GetType() != typeof(ArtifactsInfo)) return;
        
        if (!EnsureUI()) return;

        var player = ENT_Player.GetPlayer();
        if (player == null)
        {
            HideAll();
            return;
        }

        if (_showOnlyInPause.Value && !CL_GameManager.gMan.isPaused)
        {
            HideAll();
            return;
        }

        // Gather data from all enabled modules
        var allData = new List<ArtifactInfoData>();
        foreach (var module in _modules)
        {
            var toggleSetting = GetSetting<BoolSetting>($"Enable {module.GetType().Name}");
            if (toggleSetting != null && toggleSetting.Value)
            {
                allData.AddRange(module.GatherData());
            }
        }

        SyncSlotVisibility(allData.Count);
        RenderSlots(allData);
    }

    /// <summary>
    /// Subclasses override this to provide data to be displayed.
    /// </summary>
    protected virtual List<ArtifactInfoData> GatherData() => new();

    private bool EnsureUI()
    {
        if (_canvas != null && _layout != null) return true;

        var prefab = PrefabDatabase.Instance.GetObject("histools/UI_BuffsDisplay", true);
        if (!prefab) return false;

        var go = Object.Instantiate(prefab);
        go.name = "HisTools_ArtifactsUI";

        _canvas = go.GetComponent<Canvas>();
        _layout = _canvas.GetComponentInChildren<HorizontalLayoutGroup>(true);
        if (!_canvas || !_layout)
        {
            Object.Destroy(go);
            return false;
        }

        // Remove EventSystem to avoid Unity warning
        var eventSystem = go.GetComponentInChildren<EventSystem>(true);
        if (eventSystem != null) Object.Destroy(eventSystem);

        // Remove existing child buff slots
        foreach (Transform child in _layout.transform)
            Object.Destroy(child.gameObject);

        Anchor.SetAnchor(_layout.GetComponent<RectTransform>(), (int)_uiPosition.Value);

        PrecreateSlots();

        return true;
    }

    private void PrecreateSlots()
    {
        _slots.Clear();
        for (int i = 0; i < MaxSlots; i++)
        {
            var slot = CreateSlot(_layout.transform, i);
            slot.Transform.gameObject.SetActive(false);
            _slots.Add(slot);
        }
    }

    private static ArtifactSlot CreateSlot(Transform parent, int index)
    {
        var root = new GameObject($"Artifact_{index}");
        root.transform.SetParent(parent, false);

        // Background (Reuse style from TimedPerkDisplay)
        var bgImg = root.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);

        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(280f, 44f);

        // Name label
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(root.transform, false);
        var nameRect = nameGo.AddComponent<RectTransform>();
        nameRect.anchorMin = Vector2.zero;
        nameRect.anchorMax = Vector2.one;
        nameRect.offsetMin = new Vector2(6f, 0f);
        nameRect.offsetMax = new Vector2(-100f, 0f);
        var nameText = nameGo.AddComponent<TextMeshProUGUI>();
        nameText.text = "";
        nameText.fontSize = 24f;
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.enableWordWrapping = false;

        // Value label
        var valueGo = new GameObject("Value");
        valueGo.transform.SetParent(root.transform, false);
        var valueRect = valueGo.AddComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(1f, 0f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.sizeDelta = new Vector2(100f, 0f);
        var valueText = valueGo.AddComponent<TextMeshProUGUI>();
        valueText.text = "";
        valueText.fontSize = 24f;
        valueText.color = new Color(1f, 0.85f, 0.2f);
        valueText.alignment = TextAlignmentOptions.MidlineRight;
        valueText.fontStyle = FontStyles.Bold;
        valueText.enableWordWrapping = false;

        return new ArtifactSlot
        {
            Transform = root.transform,
            NameText = nameText,
            ValueText = valueText
        };
    }

    private void DestroyUI()
    {
        foreach (var s in _slots)
        {
            if (s.Transform != null && s.Transform.gameObject != null)
                Object.Destroy(s.Transform.gameObject);
        }
        _slots.Clear();

        if (_canvas != null)
        {
            Object.Destroy(_canvas.gameObject);
            _canvas = null;
            _layout = null;
        }
    }

    private void SyncSlotVisibility(int visibleCount)
    {
        var count = Math.Min(visibleCount, MaxSlots);
        for (int i = 0; i < _slots.Count; i++)
        {
            var s = _slots[i];
            if (s.Transform == null) continue;
            s.Transform.gameObject.SetActive(i < count);
        }
    }

    private void RenderSlots(List<ArtifactInfoData> data)
    {
        for (int i = 0; i < Math.Min(data.Count, MaxSlots); i++)
        {
            var slot = _slots[i];
            if (slot.Transform == null) continue;
            var d = data[i];

            slot.NameText.text = d.Name;
            slot.ValueText.text = d.Value;
            slot.ValueText.color = d.ValueColor;
        }
    }

    private void HideAll()
    {
        foreach (var s in _slots)
        {
            if (s.Transform != null)
                s.Transform.gameObject.SetActive(false);
        }
    }
}
