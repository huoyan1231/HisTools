using System.Text;
using HisTools.Features.Controllers;
using HisTools.Prefabs;
using TMPro;
using UnityEngine;
using Logger = HisTools.Utils.Logger;

namespace HisTools.Features;

public class DebugInfo : FeatureBase
{
    private TextMeshProUGUI _uiText;
    private Canvas _uiCanvas;
    
    private readonly StringBuilder _summary = new();
    private float _speedValue;

    private string _fgTextColor;
    private string _bgTextColor;

    public DebugInfo() : base("DebugInfo", "Show various debug info on screen")
    {
        AddSettings();
    }

    private void EnsurePrefab()
    {
        if (_uiCanvas && _uiText) return;

        var prefab = PrefabDatabase.Instance.GetObject("histools/Feature_DebugInfo", false);
        if (!prefab) return;

        var go = Object.Instantiate(prefab);
        _uiCanvas = go.GetComponent<Canvas>();
        _uiText = go.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void AddSettings()
    {
        AddSetting(new BoolSetting(this, "Color from palette", "Prefer color from accent palette", true));
        AddSetting(new BoolSetting(this, "Crosshair pos", "Copy crosshair world position into clipboard", false));
        AddSetting(new BoolSetting(this, "Level name", "Show level name", true));
        AddSetting(new BoolSetting(this, "Level flipped", "Show if level is flipped", true));
        AddSetting(new BoolSetting(this, "Horizontal speed", "Show player speed indicator", true));
        AddSetting(new BoolSetting(this, "In world pos", "Show player position relative to world", true));
        AddSetting(new BoolSetting(this, "In level pos", "Show player position relative to level", true));
    }

    public override void OnEnable()
    {
        EventBus.Subscribe<WorldUpdateEvent>(OnWorldUpdate);
        EventBus.Subscribe<PlayerLateUpdateEvent>(OnPlayerLateUpdate);
        
        var usePalette = GetSetting<BoolSetting>("Color from palette").Value;
        _bgTextColor = usePalette
            ? Utils.Palette.HtmlTransparent(Plugin.BackgroundHtml.Value, 0.5f)
            : "#000000AA";
        _fgTextColor = usePalette
            ? Utils.Palette.RGBAToHex(Utils.Palette.HtmlColorLight(Plugin.AccentHtml.Value, 1.8f))
            : "green";
    }

    public override void OnDisable()
    {
        EventBus.Unsubscribe<WorldUpdateEvent>(OnWorldUpdate);
        EventBus.Unsubscribe<PlayerLateUpdateEvent>(OnPlayerLateUpdate);

        _uiCanvas?.gameObject?.SetActive(false);
    }

    private void OnWorldUpdate(WorldUpdateEvent e)
    {
        var level = CL_EventManager.currentLevel;

        if (!level) return;
        var player = ENT_Player.GetPlayer();
        if (player == null) return;
        EnsurePrefab();

        _uiCanvas.gameObject.SetActive(true);

        var absolutePos = player.transform.position;
        var levelPos = Utils.Vectors.ConvertPointToLocal(absolutePos);

        var copyCrosshair = GetSetting<BoolSetting>("Crosshair pos").Value;
        var showLevelName = GetSetting<BoolSetting>("Level name").Value;
        var showLevelFlipped = GetSetting<BoolSetting>("Level flipped").Value;
        var showSpeed = GetSetting<BoolSetting>("Horizontal speed").Value;
        var showWorldPos = GetSetting<BoolSetting>("In world pos").Value;
        var showLevelPos = GetSetting<BoolSetting>("In level pos").Value;

        if (copyCrosshair && Input.GetMouseButtonDown(2))
        {
            if (Camera.main)
            {
                var pos = Utils.Raycast.GetLookTarget(Camera.main.transform, 100f);
                var json = $"{{ \"x\": {pos.x:F2}, \"y\": {pos.y:F2}, \"z\": {pos.z:F2} }}";
                GUIUtility.systemCopyBuffer = json;
                Logger.Info($"Copied to clipboard: {json}");
            }
        }

        _summary.Clear();

        var bg = _bgTextColor;
        var fg = _fgTextColor;

        if (showLevelName)
            _summary.Append($"levelName: <mark={bg}><b><color={fg}>{level.levelName}</color></b></mark> ");

        if (showLevelFlipped)
            _summary.Append(
                $"levelFlipped: <mark={bg}><b><color={fg}>{level.transform.lossyScale.x < 0f}</color></b></mark> ");

        if (showLevelPos)
            _summary.Append($"levelPos: <mark={bg}><b><color={fg}>{levelPos}</color></b></mark> ");

        if (showSpeed)
            _summary.Append($"speed: <mark={bg}><b><color={fg}>{_speedValue:F1}</color></b></mark> ");

        if (showWorldPos)
            _summary.Append($"absolutePos: <mark={bg}><b><color={fg}>{absolutePos}</color></b></mark> ");

        _uiText.text = _summary.ToString();
    }

    private void OnPlayerLateUpdate(PlayerLateUpdateEvent e)
    {
        var showSpeed = GetSetting<BoolSetting>("Horizontal speed")?.Value ?? true;
        if (!showSpeed) return;

        var hv = new Vector3(e.Vel.x, 0, e.Vel.z);
        var speed = hv.magnitude;

        const float maxComponent = 0.05f;
        var maxMagnitude = Mathf.Sqrt(2) * maxComponent;

        _speedValue = Mathf.Clamp(speed / maxMagnitude * 300f, 0, 300);
    }
}