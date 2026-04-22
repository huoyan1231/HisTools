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
/// A single timed-perk slot in the horizontal bar.
/// </summary>
internal struct PerkSlot
{
    public Transform Transform;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI TimeText;
}

/// <summary>
/// Data for one active timed perk.
/// </summary>
internal struct TimedPerkData
{
    public string Name;
    public string Id;
    public float SecondsLeft;
    public int StackAmount;
}

/// <summary>
/// Displays limited-time perks with countdown timers,
/// merged into a horizontal bar similar to the buff indicators.
///
/// Detects timed perks via <c>PerkModule_RemovalTimer</c> (the game's
/// native system) with a buff-based fallback.
/// </summary>
public class TimedPerkDisplay : FeatureBase
{
    private Canvas _canvas;
    private HorizontalLayoutGroup _layout;

    private readonly List<PerkSlot> _slots = new();
    private const int MaxSlots = 8;

    // Settings
    private readonly BoolSetting _showOnlyInPause;
    private readonly FloatSliderSetting _position;

    public TimedPerkDisplay() : base("TimedPerks", "Show limited-time perk countdowns")
    {
        _showOnlyInPause = AddSetting(new BoolSetting(this, "ShowOnlyInPause", "Only show when paused", false));
        _position = AddSetting(new FloatSliderSetting(this, "Position", "...", 3f, 1f, 6f, 1f, 0));
    }

    // ────────────────────────────────────────────────
    //  Lifecycle
    // ────────────────────────────────────────────────

    public override void OnEnable()
    {
        EventBus.Subscribe<WorldUpdateEvent>(OnWorldUpdate);
    }

    public override void OnDisable()
    {
        DestroyUI();
        EventBus.Unsubscribe<WorldUpdateEvent>(OnWorldUpdate);
    }

    // ────────────────────────────────────────────────
    //  UI creation
    // ────────────────────────────────────────────────

    private bool EnsureUI()
    {
        if (_canvas != null && _layout != null) return true;

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

        // Remove EventSystem to avoid Unity warning
        var eventSystem = go.GetComponentInChildren<EventSystem>(true);
        if (eventSystem != null) Object.Destroy(eventSystem);

        // Remove existing child buff slots – we only want our own perk slots
        foreach (Transform child in _layout.transform)
            Object.Destroy(child.gameObject);

        Anchor.SetAnchor(_layout.GetComponent<RectTransform>(), (int)_position.Value);

        PrecreateSlots();

        return true;
    }

    /// <summary>
    /// Pre-create empty slots up to MaxSlots, all hidden initially.
    /// </summary>
    private void PrecreateSlots()
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            var slot = CreateSlot(_layout.transform, i);
            slot.Transform.gameObject.SetActive(false);
            _slots.Add(slot);
        }
    }

    /// <summary>
    /// Build one perk slot: [Name] [Time].
    /// </summary>
    private static PerkSlot CreateSlot(Transform parent, int index)
    {
        var root = new GameObject($"Perk_{index}");
        root.transform.SetParent(parent, false);

        // ── Background ──
        var bgImg = root.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);

        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(280f, 44f);

        // ── Name label ──
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(root.transform, false);
        var nameRect = nameGo.AddComponent<RectTransform>();
        nameRect.anchorMin = Vector2.zero;
        nameRect.anchorMax = Vector2.one;
        nameRect.offsetMin = new Vector2(6f, 0f);
        nameRect.offsetMax = new Vector2(-64f, 0f);
        var nameText = nameGo.AddComponent<TextMeshProUGUI>();
        nameText.text = "";
        nameText.fontSize = 28f;
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.enableWordWrapping = false;

        // ── Timer label ──
        var timeGo = new GameObject("Time");
        timeGo.transform.SetParent(root.transform, false);
        var timeRect = timeGo.AddComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(1f, 0f);
        timeRect.anchorMax = new Vector2(1f, 1f);
        timeRect.pivot = new Vector2(1f, 0.5f);
        timeRect.sizeDelta = new Vector2(60f, 0f);
        var timeText = timeGo.AddComponent<TextMeshProUGUI>();
        timeText.text = "";
        timeText.fontSize = 24f;
        timeText.color = new Color(1f, 0.85f, 0.2f);
        timeText.alignment = TextAlignmentOptions.MidlineRight;
        timeText.fontStyle = FontStyles.Bold;
        timeText.enableWordWrapping = false;

        return new PerkSlot
        {
            Transform = root.transform,
            NameText = nameText,
            TimeText = timeText
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

    // ────────────────────────────────────────────────
    //  Update loop
    // ────────────────────────────────────────────────

    private void OnWorldUpdate(WorldUpdateEvent e)
    {
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

        var perks = GatherTimedPerks(player);
        SyncSlotVisibility(perks.Count);
        RenderSlots(perks);
    }

    private void HideAll()
    {
        foreach (var s in _slots)
        {
            if (s.Transform != null)
                s.Transform.gameObject.SetActive(false);
        }
    }

    // ────────────────────────────────────────────────
    //  Data gathering
    // ────────────────────────────────────────────────

    /// <summary>
    /// Collect all currently-active timed perks with remaining duration.
    /// Uses PerkModule_RemovalTimer when available, falls back to buff analysis.
    /// </summary>
    private static List<TimedPerkData> GatherTimedPerks(ENT_Player player)
    {
        var result = new List<TimedPerkData>();
        if (player.perks == null) return result;

        foreach (var perk in player.perks)
        {
            if (perk == null) continue;

            var id = GetId(perk);
            if (string.IsNullOrEmpty(id)) continue;

            float? seconds = GetRemovalTimerTime(perk);
            if (!seconds.HasValue || seconds.Value <= 0f)
                seconds = GetBuffBasedTime(player, perk);
            if (!seconds.HasValue || seconds.Value <= 0f || float.IsInfinity(seconds.Value)) continue;

            var name = GetDisplayName(perk);
            result.Add(new TimedPerkData
            {
                Name = name.Length > 20 ? name[..20] + "..." : name,
                Id = id,
                SecondsLeft = seconds.Value,
                StackAmount = GetStack(perk)
            });
        }

        result.Sort((a, b) => a.SecondsLeft.CompareTo(b.SecondsLeft));
        return result;
    }

    // ────────────────────────────────────────────────
    //  Rendering
    // ────────────────────────────────────────────────

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

    private void RenderSlots(List<TimedPerkData> perks)
    {
        for (int i = 0; i < Math.Min(perks.Count, MaxSlots); i++)
        {
            var slot = _slots[i];
            if (slot.Transform == null) continue;
            var p = perks[i];

            slot.NameText.text = p.StackAmount > 1
                ? $"{p.Name} x{p.StackAmount}"
                : p.Name;

            slot.TimeText.text = FormatTime(p.SecondsLeft);

            // Urgency colouring
            slot.TimeText.color = p.SecondsLeft switch
            {
                < 10f => new Color(1f, 0.25f, 0.25f),
                < 30f => new Color(1f, 0.6f, 0.2f),
                _ => new Color(1f, 0.9f, 0.2f)
            };
        }
    }

    private static string FormatTime(float s) =>
        float.IsInfinity(s) ? "\u221e" : TimeSpan.FromSeconds(s).ToString(@"mm\:ss");

    // ════════════════════════════════════════════════
    //  Reflection helpers – perk data
    // ════════════════════════════════════════════════

    #region Reflection helpers

    private static string GetId(object perk)
    {
        if (perk == null) return "";
        var p = perk.GetType().GetProperty("id");
        if (p != null) return p.GetValue(perk)?.ToString() ?? "";
        var f = perk.GetType().GetField("id");
        return f?.GetValue(perk)?.ToString() ?? "";
    }

    private static string GetDisplayName(object perk)
    {
        if (perk == null) return "";
        foreach (var n in new[] { "title", "displayName", "name" })
        {
            var p = perk.GetType().GetProperty(n);
            if (p != null)
            {
                var v = p.GetValue(perk)?.ToString();
                if (!string.IsNullOrEmpty(v)) return v;
            }
        }
        return GetId(perk);
    }

    private static int GetStack(object perk)
    {
        if (perk == null) return 1;
        foreach (var n in new[] { "stackAmount", "stack" })
        {
            var p = perk.GetType().GetProperty(n);
            if (p != null)
            {
                var v = p.GetValue(perk);
                if (v is int i) return i;
                if (v is float fl) return (int)fl;
            }
            var f = perk.GetType().GetField(n);
            if (f != null)
            {
                var v = f.GetValue(perk);
                if (v is int i) return i;
                if (v is float fl) return (int)fl;
            }
        }
        return 1;
    }

    /// <summary>
    /// Try reading remaining time from <c>PerkModule_RemovalTimer</c>.
    /// </summary>
    private static float? GetRemovalTimerTime(object perk)
    {
        try
        {
            if (perk == null) return null;
            var t = perk.GetType();

            // Generic GetModule&lt;T&gt;()
            var gm = t.GetMethod("GetModule");
            if (gm != null && gm.IsGenericMethod)
            {
                var rtType = t.Assembly.GetType("PerkModule_RemovalTimer")
                         ?? typeof(ENT_Player).Assembly.GetType("PerkModule_RemovalTimer");
                if (rtType != null)
                {
                    var mod = gm.MakeGenericMethod(rtType).Invoke(perk, null);
                    if (mod != null) return ExtractTime(mod);
                }
            }

            // Enumerate .modules list
            var mp = t.GetProperty("modules");
            var mf = t.GetField("modules");
            var mods = mp != null ? mp.GetValue(perk) : mf?.GetValue(perk);
            if (mods is System.Collections.IEnumerable e)
            {
                foreach (var m in e)
                    if (m?.GetType().Name.Contains("RemovalTimer") == true)
                        return ExtractTime(m);
            }

            return null;
        }
        catch (System.Exception ex)
        {
            Utils.Logger.Debug($"GetRemovalTimerTime: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Read a float time field/property from a module instance by probing common names.
    /// </summary>
    private static float? ExtractTime(object mod)
    {
        if (mod == null) return null;
        var mt = mod.GetType();
        var bf = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

        var names = new[] { "timeToRemove", "removeTime", "removalTime", "duration",
                            "secondsLeft", "remainingTime", "timer", "timeLeft", "initialTime" };

        foreach (var n in names)
        {
            var f = mt.GetField(n, bf);
            if (f?.FieldType == typeof(float))
            {
                var v = (float)f.GetValue(mod);
                if (v > 0f) return v;
            }
            var p = mt.GetProperty(n, bf);
            if (p?.PropertyType == typeof(float))
            {
                var v = (float)p.GetValue(mod);
                if (v > 0f) return v;
            }
        }

        return null;
    }

    /// <summary>
    /// Fallback: detect timed status from the buff system when RemovalTimer is unavailable.
    /// </summary>
    private static float? GetBuffBasedTime(ENT_Player player, object perk)
    {
        try
        {
            var pt = perk.GetType();
            var bp = pt.GetProperty("buff");
            if (bp == null) return null;

            var bo = bp.GetValue(perk);
            if (bo == null) return null;

            var bip = bo.GetType().GetProperty("id");
            if (bip == null) return null;

            var buffId = bip.GetValue(bo)?.ToString();
            if (string.IsNullOrEmpty(buffId)) return null;

            var container = player.curBuffs.GetBuffContainer(buffId);
            if (container == null || !container.loseOverTime || container.loseRate <= 0f) return null;

            var sec = CalcBuffSeconds(container);
            return (float.IsInfinity(sec) || sec <= 0f) ? null : (float?)sec;
        }
        catch (System.Exception ex)
        {
            Utils.Logger.Debug($"GetBuffBasedTime: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Calculate remaining seconds from a buff container's normalised timer.
    /// </summary>
    private static float CalcBuffSeconds(BuffContainer c)
    {
        if (c == null || !c.loseOverTime || c.loseRate <= 0f) return 0f;

        var timeMultBuff = ENT_Player.playerObject != null
            ? ENT_Player.playerObject.curBuffs.GetBuff("buffTimeMult")
            : 0f;
        var mult = Mathf.Max(1f + timeMultBuff, 0.1f);
        return c.buffTime / (c.loseRate / mult);
    }

    #endregion
}
