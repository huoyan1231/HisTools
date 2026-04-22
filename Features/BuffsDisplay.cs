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

internal struct BuffIndicator
{
    public Transform Transform;
    public Image[] Icons;
    public TextMeshProUGUI Value;
    public TextMeshProUGUI Time;
}

public class BuffsDisplay : FeatureBase
{
    private Canvas _canvas;
    private HorizontalLayoutGroup _layout;

    private BuffIndicator? _grub;
    private BuffIndicator? _injector;
    private BuffIndicator? _pills;
    private BuffIndicator? _foodBar;

    private readonly BoolSetting _showOnlyInPause;
    private readonly FloatSliderSetting _buffsPosition;

    public BuffsDisplay() : base("BuffsDisplay", "Display all current buff effects")
    {
        _showOnlyInPause = AddSetting(new BoolSetting(this, "ShowOnlyInPause", "...", false));
        _buffsPosition = AddSetting(new FloatSliderSetting(this, "Position", "...", 2f, 1f, 6f, 1f, 0));
    }

    private static BuffIndicator? FindBuffSlot(Transform layoutRoot, string name)
    {
        if (!layoutRoot) return null;

        var transform = layoutRoot.Find(name);
        var icons = transform?.Find("Icons")?.GetComponentsInChildren<Image>(true);
        var value = transform?.Find("Value")?.GetComponent<TextMeshProUGUI>();
        var time = transform?.Find("Time")?.GetComponent<TextMeshProUGUI>();

        if (icons == null || icons.Length == 0 || !value || !time || !transform)
            return null;

        return new BuffIndicator { Transform = transform, Icons = icons, Value = value, Time = time };
    }

    // ────────────────────────────────────────────────
    //  Prefab initialization
    // ────────────────────────────────────────────────

    private bool EnsurePrefabs()
    {
        if (_grub.HasValue && _injector.HasValue && _pills.HasValue && _foodBar.HasValue
            && _layout && _canvas)
            return true;

        var prefab = PrefabDatabase.Instance.GetObject("histools/UI_BuffsDisplay", true);
        if (!prefab) return false;

        var go = Object.Instantiate(prefab);

        _canvas = go.GetComponent<Canvas>();
        _layout = _canvas.GetComponentInChildren<HorizontalLayoutGroup>(true);

        if (!_canvas || !_layout)
        {
            Object.Destroy(go);
            return false;
        }

        // Remove EventSystem from prefab to avoid "There can be only one active Event System" warning
        var eventSystem = go.GetComponentInChildren<EventSystem>(true);
        if (eventSystem != null)
            Object.Destroy(eventSystem);

        _grub = FindBuffSlot(_layout.transform, "Grub");
        _injector = FindBuffSlot(_layout.transform, "Injector");
        _pills = FindBuffSlot(_layout.transform, "Pills");
        _foodBar = FindBuffSlot(_layout.transform, "FoodBar");

        if (!_grub.HasValue || !_injector.HasValue || !_pills.HasValue || !_foodBar.HasValue)
        {
            Object.Destroy(go);
            _canvas = null;
            _layout = null;
            return false;
        }

        Anchor.SetAnchor(
            _layout.GetComponent<RectTransform>(),
            (int)_buffsPosition.Value
        );

        return true;
    }

    // ────────────────────────────────────────────────
    //  Core update loop
    // ────────────────────────────────────────────────

    public override void OnEnable()
    {
        EventBus.Subscribe<WorldUpdateEvent>(OnWorldUpdate);
    }

    private void OnWorldUpdate(WorldUpdateEvent e)
    {
        if (!EnsurePrefabs())
        {
            Utils.Logger.Error("BuffsDisplay: Failed to ensure prefabs");
            return;
        }

        var player = ENT_Player.GetPlayer();
        if (player == null) return;

        if (_showOnlyInPause.Value && !CL_GameManager.gMan.isPaused)
        {
            SetActive(_grub, false);
            SetActive(_injector, false);
            SetActive(_foodBar, false);
            SetActive(_pills, false);
            return;
        }

        var containers = player.curBuffs.currentBuffs;

        var foodBarContainers = SearchBuff(containers, "roided").Where(HasPilled).ToList();
        var roidedContainers = SearchBuff(containers, "roided").Where(c => !HasPilled(c)).ToList();
        var pilledContainers = SearchBuff(containers, "pilled").Where(c => !HasRoided(c)).ToList();
        var goopedContainers = SearchBuff(containers, "gooped");

        SetActive(_grub, goopedContainers.Count > 0);
        SetActive(_injector, roidedContainers.Count > 0);
        SetActive(_foodBar, foodBarContainers.Count > 0);
        SetActive(_pills, pilledContainers.Count > 0);

        if (goopedContainers.Count > 0) RenderBuff(goopedContainers, _grub.Value);
        if (roidedContainers.Count > 0) RenderBuff(roidedContainers, _injector.Value);
        if (pilledContainers.Count > 0) RenderBuff(pilledContainers, _pills.Value);
        if (foodBarContainers.Count > 0) RenderBuff(foodBarContainers, _foodBar.Value);
    }

    // ────────────────────────────────────────────────
    //  Helpers – buffs
    // ────────────────────────────────────────────────

    private static float CalculateBuffSecondsLeft(BuffContainer container)
    {
        if (container == null) return 0f;
        if (!container.loseOverTime || container.loseRate <= 0f) return float.PositiveInfinity;

        var timeMultBuff = ENT_Player.playerObject != null
            ? ENT_Player.playerObject.curBuffs.GetBuff("buffTimeMult")
            : 0f;
        var timeMultiplier = Mathf.Max(1f + timeMultBuff, 0.1f);
        var effectiveLoseRate = container.loseRate / timeMultiplier;

        return container.buffTime / effectiveLoseRate;
    }

    private static string FormatTime(float s) =>
        float.IsInfinity(s) ? "\u221e" : TimeSpan.FromSeconds(s).ToString(@"mm\:ss");

    private static bool HasBuff(BuffContainer bc, string id) => bc.buffs.Any(b => b.id == id);
    private static bool HasPilled(BuffContainer c) => HasBuff(c, "pilled");
    private static bool HasRoided(BuffContainer c) => HasBuff(c, "roided");
    private static List<BuffContainer> SearchBuff(List<BuffContainer> list, string id) =>
        list.Where(c => HasBuff(c, id)).ToList();

    private static void UpdateIcons(Image[] icons, int count)
    {
        for (var i = 1; i < icons.Length; i++)
            icons[i].gameObject.SetActive(i < count);
    }

    private static float ShortestTime(List<BuffContainer> containers)
    {
        var min = float.PositiveInfinity;
        foreach (var c in containers)
        {
            var t = CalculateBuffSecondsLeft(c);
            if (t < min) min = t;
        }
        return min;
    }

    private static void SetActive(BuffIndicator? slot, bool active)
    {
        if (slot.HasValue) slot.Value.Transform.gameObject.SetActive(active);
    }

    private static void RenderBuff(List<BuffContainer> containers, BuffIndicator obj)
    {
        obj.Value.text = $"x{containers.Count}";
        obj.Time.text = FormatTime(ShortestTime(containers));
        UpdateIcons(obj.Icons, containers.Count);
    }

    // ────────────────────────────────────────────────
    //  Cleanup
    // ────────────────────────────────────────────────

    public override void OnDisable()
    {
        if (_canvas != null)
            Object.Destroy(_canvas.gameObject);

        EventBus.Unsubscribe<WorldUpdateEvent>(OnWorldUpdate);
    }
}
