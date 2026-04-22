using System;
using System.Collections.Generic;
using System.Linq;
using HisTools.Features.Controllers;
using HisTools.Prefabs;
using HisTools.Utils;
using TMPro;
using UnityEngine;
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

/// <summary>
/// Represents a timed perk's display info.
/// </summary>
internal struct TimedPerkInfo
{
    public string PerkName;
    public string PerkId;
    public float SecondsLeft;
    public int StackAmount;
}

public class BuffsDisplay : FeatureBase
{
    private Canvas _canvas;
    private HorizontalLayoutGroup _layout;

    private BuffIndicator? _grub;
    private BuffIndicator? _injector;
    private BuffIndicator? _pills;
    private BuffIndicator? _foodBar;

    // Timed perk UI elements
    private GameObject _perksContainer;
    private readonly List<GameObject> _perkIndicators = new();
    private const int MaxPerkIndicators = 10;
    private const float PerkIndicatorHeight = 30f;

    private readonly BoolSetting _showOnlyInPause;
    private readonly FloatSliderSetting _buffsPosition;
    private readonly BoolSetting _showTimedPerks;
    private readonly FloatSliderSetting _perksDisplayPosition;

    public BuffsDisplay() : base("BuffsDisplay", "Display all current buff effects")
    {
        _showOnlyInPause = AddSetting(new BoolSetting(this, "ShowOnlyInPause", "...", false));
        _buffsPosition = AddSetting(new FloatSliderSetting(this, "Position", "...", 2f, 1f, 6f, 1f, 0));
        _showTimedPerks = AddSetting(new BoolSetting(this, "ShowTimedPerks", "Show limited-time perk countdowns", true));
        _perksDisplayPosition = AddSetting(new FloatSliderSetting(this, "PerksPos", "...", 3f, 1f, 6f, 1f, 0));
    }

    private BuffIndicator? GetBuffIndicator(string name)
    {
        if (!_layout || !_layout.transform) return null;

        var transform = _layout.transform.Find(name);
        var icons = transform.Find("Icons").GetComponentsInChildren<Image>(true);
        var value = transform.Find("Value").GetComponent<TextMeshProUGUI>();
        var time = transform.Find("Time").GetComponent<TextMeshProUGUI>();

        if (icons.Length == 0 || !value || !time || !transform)
            return null;

        return new BuffIndicator { Transform = transform, Icons = icons, Value = value, Time = time };
    }

    private bool EnsurePrefabs()
    {
        if (_grub.HasValue &&
            _injector.HasValue &&
            _pills.HasValue &&
            _foodBar.HasValue &&
            _layout &&
            _canvas)
        {
            EnsurePerksContainer();
            return true;
        }

        var prefab = PrefabDatabase.Instance.GetObject("histools/UI_BuffsDisplay", true);
        if (!prefab)
            return false;

        var go = Object.Instantiate(prefab);

        _canvas = go.GetComponent<Canvas>();
        _layout = _canvas.GetComponentInChildren<HorizontalLayoutGroup>(true);

        if (!_canvas || !_layout)
        {
            Object.Destroy(go);
            return false;
        }

        var grub = GetBuffIndicator("Grub");
        var injector = GetBuffIndicator("Injector");
        var pills = GetBuffIndicator("Pills");
        var foodBar = GetBuffIndicator("FoodBar");

        if (!grub.HasValue ||
            !injector.HasValue ||
            !pills.HasValue ||
            !foodBar.HasValue)
        {
            Object.Destroy(go);
            _canvas = null;
            _layout = null;
            return false;
        }

        _grub = grub;
        _injector = injector;
        _pills = pills;
        _foodBar = foodBar;

        Anchor.SetAnchor(
            _layout.GetComponent<RectTransform>(),
            (int)_buffsPosition.Value
        );

        EnsurePerksContainer();

        return true;
    }

    /// <summary>
    /// Creates and manages the container for timed perk indicators.
    /// Uses the main buffs canvas to avoid duplicate Event Systems.
    /// </summary>
    private void EnsurePerksContainer()
    {
        if (!_showTimedPerks.Value)
        {
            _perksContainer?.SetActive(false);
            return;
        }

        if (_perksContainer != null)
        {
            _perksContainer.SetActive(true);
            return;
        }

        // Reuse the main buffs canvas to avoid duplicate Event Systems
        if (_canvas == null)
        {
            // Canvas not ready yet, will be created in EnsurePrefabs
            return;
        }

        // Create container for perk indicators inside the existing canvas
        _perksContainer = new GameObject("HisTools_PerksContainer");
        _perksContainer.transform.SetParent(_canvas.transform, false);

        var rect = _perksContainer.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.one; // Top-right
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-10, -10);

        var vLayout = _perksContainer.AddComponent<VerticalLayoutGroup>();
        vLayout.childAlignment = TextAnchor.UpperRight;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childForceExpandWidth = false;
        vLayout.childForceExpandHeight = false;
        vLayout.spacing = 5f;

        // Position based on settings
        Anchor.SetAnchor(rect, (int)_perksDisplayPosition.Value);

        // Pre-create perk indicator slots
        for (int i = 0; i < MaxPerkIndicators; i++)
        {
            var indicator = CreatePerkIndicator(_perksContainer.transform, i);
            _perkIndicators.Add(indicator);
            indicator.SetActive(false);
        }
    }

    /// <summary>
    /// Creates a single perk indicator GameObject with icon, name, and countdown timer.
    /// </summary>
    private GameObject CreatePerkIndicator(Transform parent, int index)
    {
        var indicatorGO = new GameObject($"PerkIndicator_{index}");
        indicatorGO.transform.SetParent(parent, false);

        // Layout element for sizing
        var layoutEl = indicatorGO.AddComponent<LayoutElement>();
        layoutEl.minHeight = PerkIndicatorHeight;
        layoutEl.preferredWidth = 180f;
        layoutEl.flexibleWidth = 0f;

        // Background image
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(indicatorGO.transform, false);
        var bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

        // Perk name label
        var nameGO = new GameObject("PerkName");
        nameGO.transform.SetParent(indicatorGO.transform, false);
        var nameRect = nameGO.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.5f);
        nameRect.anchorMax = new Vector2(0, 0.5f);
        nameRect.pivot = new Vector2(0, 0.5f);
        nameRect.anchoredPosition = new Vector2(5, 0);
        nameRect.sizeDelta = new Vector2(100, 20);
        var nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = "";
        nameText.fontSize = 12f;
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.Left;

        // Timer label
        var timerGO = new GameObject("Timer");
        timerGO.transform.SetParent(indicatorGO.transform, false);
        var timerRect = timerGO.AddComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(1, 0.5f);
        timerRect.anchorMax = new Vector2(1, 0.5f);
        timerRect.pivot = new Vector2(1, 0.5f);
        timerRect.anchoredPosition = new Vector2(-5, 0);
        timerRect.sizeDelta = new Vector2(70, 20);
        var timerText = timerGO.AddComponent<TextMeshProUGUI>();
        timerText.text = "";
        timerText.fontSize = 12f;
        timerText.color = new Color(1f, 0.8f, 0.2f); // Yellow/gold for timers
        timerText.alignment = TextAlignmentOptions.Right;
        timerText.fontStyle = FontStyles.Bold;

        return indicatorGO;
    }


    public override void OnEnable()
    {
        EventBus.Subscribe<WorldUpdateEvent>(OnWorldUpdate);
    }

    private static float CalculateBuffSecondsLeft(BuffContainer container)
    {
        if (container == null)
            return 0f;

        if (!container.loseOverTime || container.loseRate <= 0f)
            return float.PositiveInfinity;

        // buffTime is a [0,1] normalized timer that drains over time.
        // Rate: buffTime -= deltaTime * loseRate / (1 + GetBuff("buffTimeMult"))
        // Therefore: secondsLeft = buffTime / (loseRate / timeMultiplier)
        var timeMultBuff = ENT_Player.playerObject != null
            ? ENT_Player.playerObject.curBuffs.GetBuff("buffTimeMult")
            : 0f;
        var timeMultiplier = Mathf.Max(1f + timeMultBuff, 0.1f);
        var effectiveLoseRate = container.loseRate / timeMultiplier;

        return container.buffTime / effectiveLoseRate;
    }

    private static string GetFormattedTime(float secondsLeft)
    {
        return float.IsInfinity(secondsLeft) ? "∞" : TimeSpan.FromSeconds(secondsLeft).ToString(@"mm\:ss\:ff");
    }

    private static bool HaveBuff(BuffContainer bc, string id)
    {
        return bc.buffs.Any(buff => buff.id == id);
    }

    private static List<BuffContainer> SearchBuff(List<BuffContainer> bcList, string id)
    {
        return bcList.Where(container => HaveBuff(container, id)).ToList();
    }

    private static void UpdateIconsStack(Image[] icons, int count)
    {
        for (var i = 1; i < icons.Length; i++)
            icons[i].gameObject.SetActive(i < count);
    }

    private static float SummaryBuffSecondsLeft(List<BuffContainer> containers)
    {
        // Return the shortest remaining time among all active containers of this buff type.
        // This ensures the countdown always shows the soonest expiry, preventing the timer
        // from appearing frozen when one container expires while others remain.
        var minSecondsLeft = float.PositiveInfinity;
        foreach (var container in containers)
        {
            var secondsLeft = CalculateBuffSecondsLeft(container);
            if (secondsLeft < minSecondsLeft)
                minSecondsLeft = secondsLeft;
        }
        return minSecondsLeft;
    }

    /// <summary>
    /// Gets all timed perks with their remaining duration.
    /// A perk is considered "timed" if its associated buff has loseOverTime enabled.
    /// </summary>
    private List<TimedPerkInfo> GetTimedPerks()
    {
        var timedPerks = new List<TimedPerkInfo>();

        var player = ENT_Player.GetPlayer();
        if (player == null || player.perks == null)
            return timedPerks;

        foreach (var perk in player.perks)
        {
            if (perk == null || perk.buff == null || string.IsNullOrEmpty(perk.buff.id))
                continue;

            // Get the buff container for this perk
            var container = player.curBuffs.GetBuffContainer(perk.buff.id);
            if (container == null)
                continue;

            // Only show perks with active timers (loseOverTime)
            if (!container.loseOverTime || container.loseRate <= 0f)
                continue;

            var secondsLeft = CalculateBuffSecondsLeft(container);
            if (float.IsInfinity(secondsLeft) || secondsLeft <= 0f)
                continue;

            // Get perk title - use a shortened version
            var perkName = perk.title;
            if (string.IsNullOrEmpty(perkName))
                perkName = perk.id;

            // Shorten long names
            if (perkName.Length > 12)
                perkName = perkName[..12] + "...";

            timedPerks.Add(new TimedPerkInfo
            {
                PerkName = perkName,
                PerkId = perk.id,
                SecondsLeft = secondsLeft,
                StackAmount = perk.stackAmount
            });
        }

        // Sort by remaining time (shortest first)
        timedPerks.Sort((a, b) => a.SecondsLeft.CompareTo(b.SecondsLeft));

        return timedPerks;
    }

    /// <summary>
    /// Updates the timed perks UI indicators.
    /// </summary>
    private void UpdateTimedPerksUI(List<TimedPerkInfo> timedPerks)
    {
        if (!_showTimedPerks.Value || _perkIndicators.Count == 0)
            return;

        for (int i = 0; i < MaxPerkIndicators; i++)
        {
            var indicator = _perkIndicators[i];
            if (indicator == null)
                continue;

            if (i < timedPerks.Count)
            {
                var perk = timedPerks[i];
                indicator.SetActive(true);

                // Update name
                var nameText = indicator.transform.Find("PerkName")?.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = perk.StackAmount > 1
                        ? $"{perk.PerkName} x{perk.StackAmount}"
                        : perk.PerkName;
                }

                // Update timer
                var timerText = indicator.transform.Find("Timer")?.GetComponent<TextMeshProUGUI>();
                if (timerText != null)
                {
                    timerText.text = GetFormattedTime(perk.SecondsLeft);

                    // Color based on urgency
                    if (perk.SecondsLeft < 10f)
                        timerText.color = new Color(1f, 0.2f, 0.2f); // Red when < 10s
                    else if (perk.SecondsLeft < 30f)
                        timerText.color = new Color(1f, 0.6f, 0.2f); // Orange when < 30s
                    else
                        timerText.color = new Color(1f, 0.9f, 0.2f); // Yellow otherwise
                }
            }
            else
            {
                indicator.SetActive(false);
            }
        }
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
            _grub?.Transform.gameObject.SetActive(false);
            _injector?.Transform.gameObject.SetActive(false);
            _foodBar?.Transform.gameObject.SetActive(false);
            _pills?.Transform.gameObject.SetActive(false);
            _perksContainer?.SetActive(false);
            return;
        }

        // Show/hide perks container based on setting
        if (_showTimedPerks.Value)
            _perksContainer?.SetActive(true);

        var containers = player.curBuffs.currentBuffs;

        var foodBarContainers = SearchBuff(containers, "roided")
            .Where(container => HaveBuff(container, "pilled"))
            .ToList();

        var roidedContainers = SearchBuff(containers, "roided")
            .Where(container => !HaveBuff(container, "pilled"))
            .ToList();

        var pilledContainers = SearchBuff(containers, "pilled").Where(container => !HaveBuff(container, "roided"))
            .ToList();
        var goopedContainers = SearchBuff(containers, "gooped");
        
        _grub?.Transform.gameObject.SetActive(goopedContainers.Count > 0);
        _injector?.Transform.gameObject.SetActive(roidedContainers.Count > 0);
        _foodBar?.Transform.gameObject.SetActive(foodBarContainers.Count > 0);
        _pills?.Transform.gameObject.SetActive(pilledContainers.Count > 0);
        
        if (goopedContainers.Count > 0)
            RenderBuff(goopedContainers, _grub.GetValueOrDefault());

        if (roidedContainers.Count > 0)
            RenderBuff(roidedContainers, _injector.GetValueOrDefault());

        if (pilledContainers.Count > 0)
            RenderBuff(pilledContainers, _pills.GetValueOrDefault());

        if (foodBarContainers.Count > 0)
            RenderBuff(foodBarContainers, _foodBar.GetValueOrDefault());

        // Update timed perks display
        if (_showTimedPerks.Value)
        {
            var timedPerks = GetTimedPerks();
            UpdateTimedPerksUI(timedPerks);
        }
    }

    private void RenderBuff(List<BuffContainer> containers, BuffIndicator obj)
    {
        var secondsLeft = SummaryBuffSecondsLeft(containers);

        obj.Value.text = $"x{containers.Count}";
        obj.Time.text = GetFormattedTime(secondsLeft);

        UpdateIconsStack(obj.Icons, containers.Count);
    }

    public override void OnDisable()
    {
        // Destroy the main buffs canvas (which also destroys perks container if parented)
        if (_canvas != null)
            Object.Destroy(_canvas.gameObject);
        
        // Also destroy perks container if it exists separately
        if (_perksContainer != null && _perksContainer.transform.parent != _canvas?.transform)
            Object.Destroy(_perksContainer.transform.parent?.gameObject);

        EventBus.Unsubscribe<WorldUpdateEvent>(OnWorldUpdate);
    }
}