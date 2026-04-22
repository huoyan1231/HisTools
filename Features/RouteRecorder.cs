using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HisTools.Features.Controllers;
using HisTools.Prefabs;
using HisTools.UI.Controllers;
using HisTools.Utils;
using HisTools.Utils.RouteFeature;
using LibBSP;
using Newtonsoft.Json;
using Steamworks;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HisTools.Features;

public class RouteRecorder : FeatureBase
{
    private struct HistoryChunk
    {
        public int From;
        public int To;
        public GameObject Go;
    }

    private const int MinPoints = 10;
    private const int MinHistoryStep = 3;
    private const string JumpButton = "Jump";
    private bool _stopRequested;
    private int _stepCounter;

    private readonly List<Vector3> _points = [];
    private HashSet<int> _jumpIndices = [];
    private readonly List<Note> _notes = [];

    private GameObject _previewRoot;
    private LineRenderer _previewLine;

    private ENT_Player _player;
    private TextMeshPro _notePrefab;

    private GameObject _recorderMenu;
    private GameObject _recorderMenuPlayButton;
    private GameObject _recorderMenuPauseButton;

    private TextMeshProUGUI _recorderMenuPointsTmp;
    private PopupController _addNotePopup;
    private PopupController _saveRecordPopup;

    private GameObject _recorderHistory;
    private Transform _historyContent;
    private GameObject _historyPoint;

    private readonly Stack<HistoryChunk> _historyStack = new();


    private bool _recording;


    public RouteRecorder() : base("RouteRecorder", "Record route for current level and save to json")
    {
        AddSettings();
    }

    private void AddSettings()
    {
        AddSetting(new FloatSliderSetting(this, "Record quality", "How much points have to be recorded", 3.3f, 0.5f,
            4.0f, 0.1f, 1));
        AddSetting(new FloatSliderSetting(this, "Preview line width", "Size of preview trail from player", 0.15f, 0.05f,
            0.3f, 0.05f, 2));
        AddSetting(new FloatSliderSetting(this, "Preview markers size", "Size of jump points markers", 0.3f, 0.05f,
            0.4f, 0.05f, 2));
        AddSetting(new BoolSetting(this, "Show preview while recording", "...", true));
        AddSetting(new BoolSetting(this, "Auto stop", "Stop recording automatically on level end", true));
        AddSetting(new FloatSliderSetting(this, "Auto stop distance", "Distance to level exit to stop recording", 5.5f,
            1f, 15f, 0.1f, 1));
    }

    public override void OnEnable()
    {
        var player = ENT_Player.GetPlayer();
        if (!player) return;
        _player = player;
        Cleanup();
        _recording = true;
        _previewRoot = new GameObject("HisTools_PreviewRoot");

        // Note
        var notePrefab = PrefabDatabase.Instance.GetObject("histools/InfoLabel", false);
        if (notePrefab)
        {
            var noteGo = Object.Instantiate(notePrefab);
            var tmp = noteGo.GetComponent<TextMeshPro>();
            tmp.fontSize = 3;

            var look = noteGo.AddComponent<LookAtPlayer>();
            look.player = _player.transform;

            _notePrefab = tmp;
        }

        // Line
        var lineObj = new GameObject("HisTools_RecordedPath");
        _previewLine = lineObj.AddComponent<LineRenderer>();
        _previewLine.positionCount = 0;
        _previewLine.material = new Material(Shader.Find("Sprites/Default"));
        _previewLine.widthMultiplier = GetSetting<FloatSliderSetting>("Preview line width").Value;
        _previewLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _previewLine.receiveShadows = false;

        lineObj.transform.SetParent(_previewRoot.transform);
        var gradient = new Gradient();
        gradient.SetKeys(
            [new GradientColorKey(Color.green, 0f), new GradientColorKey(Color.red, 1f)],
            [new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)]
        );

        _previewLine.colorGradient = gradient;

        // Recorder Menu
        var recorderUI = PrefabDatabase.Instance.GetObject("histools/UI_RouteRecorder", true);
        if (recorderUI)
        {
            _recorderMenu = Object.Instantiate(recorderUI);
            _recorderMenuPointsTmp = _recorderMenu.GetComponentInChildren<TextMeshProUGUI>();
            _recorderMenuPlayButton = _recorderMenu.transform.Find("Menu/Controls/PlayPause/PlayButton")?.gameObject;
            _recorderMenuPauseButton = _recorderMenu.transform.Find("Menu/Controls/PlayPause/PauseButton")?.gameObject;
        }

        // Note Popup
        var notePopupPrefab = PrefabDatabase.Instance.GetObject("histools/UI_Popup_Input", true);
        if (!notePopupPrefab) return;

        var notePopup = Object.Instantiate(notePopupPrefab);
        _addNotePopup = notePopup.AddComponent<PopupController>();
        _addNotePopup.title.text = "RouteRecorder";
        _addNotePopup.description.text = "You are about to add a new note at the location the camera is pointing at";

        _addNotePopup.applyButton.onClick.AddListener(() =>
        {
            if (!Camera.main) return;

            var pos = Raycast.GetLookTarget(Camera.main.transform, 100f);
            var text = _addNotePopup.inputField!.text;
            AddNote(pos + Vector3.up * 0.5f, text);

            Utils.Logger.Info($"RecordPath: Added note at {pos}: {text}");
            _addNotePopup?.Hide();
        });

        _addNotePopup.cancelButton.onClick.AddListener(() => Utils.Logger.Debug("AddNotePopup CANCEL clicked"));


        // SaveRecord Popup
        if (_saveRecordPopup) Object.Destroy(_saveRecordPopup.gameObject);
        var savePopupPrefab = PrefabDatabase.Instance.GetObject("histools/UI_Popup_SaveRecord", true);
        if (!savePopupPrefab) return;

        var savePopup = Object.Instantiate(savePopupPrefab);
        _saveRecordPopup = savePopup.AddComponent<PopupController>();

        var folderPath = Path.Combine(Constants.Paths.ConfigDir, "Routes");
        Directory.CreateDirectory(folderPath);

        var content = savePopup.transform.Find("Window/Scroll View/Viewport/Content");

        var nameField = content?.Find("RouteName/Input")?
            .GetComponent<TMP_InputField>();

        var authorField = content?.Find("RouteAuthor/Input")?
            .GetComponent<TMP_InputField>();

        var descriptionField = content?.Find("RouteDescription/Input")?
            .GetComponent<TMP_InputField>();

        var levelField = content?.Find("RouteTargetLevel/Input")?
            .GetComponent<TMP_InputField>();

        authorField?.text = SteamClient.Name ?? "unknownAuthor";
        levelField?.text = CL_EventManager.currentLevel?.levelName ?? "unknownLevel";

        _saveRecordPopup.bgButton.interactable = false;
        _saveRecordPopup.applyButton.onClick.AddListener(() =>
        {
            var routeInfo = new RouteInfo
            {
                uid = Files.GenerateUid(),
                name = string.IsNullOrWhiteSpace(nameField?.text) ? "unnamed" : nameField.text,
                author = authorField?.text,
                description = descriptionField?.text,
                targetLevel = levelField?.text
            };

            var routeDto = RouteMapper.ToDto(_points, _jumpIndices, _notes, routeInfo, GetMinPointDistance());
            var json = JsonConvert.SerializeObject(new[] { routeDto }, Formatting.Indented);

            try
            {
                var filePath = Files.GetNextFilePath(folderPath, $"{routeInfo.targetLevel}_{routeInfo.author}", "json");
                File.WriteAllText(filePath, json);
                Utils.Logger.Info($"RecordPath: JSON saved to {filePath}");
            }
            catch (Exception ex)
            {
                Utils.Logger.Error($"RecordPath: Failed to write JSON: {ex.Message}");
            }

            _saveRecordPopup.Hide();
            Cleanup();
        });

        _saveRecordPopup.cancelButton.onClick.AddListener(Cleanup); 

        var testPrefab = PrefabDatabase.Instance.GetObject("histools/UI_RouteRecorderHistory", true);
        if (!testPrefab) return;

        _recorderHistory = Object.Instantiate(testPrefab);

        _historyContent = _recorderHistory.transform.Find("Scroll View/Viewport/Content");
        _historyContent.gameObject.AddComponent<AutoCenterHorizontalScroll>();

        _historyPoint = _historyContent.Find("Point").gameObject;
        _historyPoint.AddComponent<PointAppear>();
        _historyPoint.AddComponent<PointDisappear>();

        EventBus.Subscribe<PlayerLateUpdateEvent>(OnPlayerLateUpdate);
    }

    public override void OnDisable()
    {
        EventBus.Unsubscribe<PlayerLateUpdateEvent>(OnPlayerLateUpdate);
        if (_points.Count < MinPoints)
        {
            Utils.Logger.Info($"RecordPath: Not enough recorded points to save ({_points.Count} <= {MinPoints})");
            Cleanup();
            return;
        }

        _saveRecordPopup.Show();
    }

    private void Cleanup()
    {
        _recording = false;
        _stopRequested = false;
        if (_addNotePopup) Object.Destroy(_addNotePopup.gameObject);
        if (_previewRoot) Object.Destroy(_previewRoot);
        if (_recorderHistory) Object.Destroy(_recorderHistory);

        _points.Clear();
        _notes.Clear();
        _jumpIndices.Clear();
        _historyStack.Clear();

        if (_recorderMenu) Object.Destroy(_recorderMenu);
    }

    // Disable feature after a short delay to safely exit the current update callback
    private IEnumerator DelayedStop()
    {
        yield return new WaitForSeconds(0.1f);
        EventBus.Publish(new FeatureToggleEvent(this, false));
    }

    private void OnPlayerLateUpdate(PlayerLateUpdateEvent e)
    {
        var level = CL_EventManager.currentLevel;
        if (!_player.transform || !level) return;
        var playerPos = level.transform.InverseTransformPoint(_player.transform.position);
        var distanceToStop = GetSetting<FloatSliderSetting>("Auto stop distance").Value;
        var jumped = InputManager.GetButton(JumpButton).Down && !CL_GameManager.gMan.lockPlayerInput;
        var minDistance = GetMinPointDistance();

        if (_recording && (_points.Count == 0 || Vector3.Distance(_points.Last(), playerPos) >= minDistance || jumped))
        {
            _stepCounter++;

            AddPointLocal(playerPos, jumped);
        }

        if (Input.GetMouseButtonDown(2)) _addNotePopup?.Show();

        if (GetSetting<BoolSetting>("Auto stop").Value)
        {
            if (!_stopRequested &&
                _player.transform.position.DistanceTo(level.GetLevelExit().position) < distanceToStop)
            {
                _stopRequested = true;

                CoroutineRunner.Instance.StartCoroutine(DelayedStop());
            }
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            if (_historyStack.Count == 0) return;

            var chunk = _historyStack.Pop();

            Object.Destroy(chunk.Go);

            Vector3? teleportLocalPos = null;

            if (chunk.From > 0)
            {
                teleportLocalPos = _points[chunk.From - 1];
            }

            _points.RemoveRange(chunk.From, _points.Count - chunk.From);

            _jumpIndices = _jumpIndices
                .Where(i => i < chunk.From)
                .ToHashSet();

            _stepCounter = 0;
            UpdatePreview();

            if (teleportLocalPos.HasValue)
            {
                _player.transform.position =
                    level.transform.TransformPoint(teleportLocalPos.Value);
            }
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            _recording = !_recording;
            if (_recorderMenuPlayButton) _recorderMenuPlayButton.SetActive(!_recording);
            if (_recorderMenuPauseButton) _recorderMenuPauseButton.SetActive(_recording);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            _stopRequested = true;

            CoroutineRunner.Instance.StartCoroutine(DelayedStop());
        }
    }

    private float GetMinPointDistance()
    {
        var q = GetSetting<FloatSliderSetting>("Record quality");
        return (float)Math.Round(math.remap(q.Min, q.Max, q.Max, q.Min, q.Value), 2);
    }

    private void AddNote(Vector3 localPos, string text)
    {
        _notes.Add(new Note(localPos, text));
        SpawnPreviewNoteWorld(localPos, text);
    }

    private void SpawnPreviewNoteWorld(Vector3 localPos, string text)
    {
        TryGetWorldPoint(localPos, out var worldPos);
        var noteLabel = Object.Instantiate(_notePrefab, worldPos, Quaternion.identity, _previewRoot.transform);
        noteLabel.text = text;

        noteLabel.gameObject.SetActive(true);
    }

    private void AddPointLocal(Vector3 localPos, bool isJumped)
    {
        var index = _points.Count;
        _points.Add(localPos);

        if (isJumped)
        {
            _jumpIndices.Add(index);
        }

        if (_stepCounter > MinHistoryStep && _player.IsGrounded())
        {
            var from = _historyStack.Count == 0
                ? 0
                : _historyStack.Peek().To;

            var to = index + 1;

            var go = Object.Instantiate(_historyPoint, _historyContent);
            go.SetActive(true);
            go.GetComponentInChildren<TextMeshProUGUI>().text = from.ToString();

            _historyStack.Push(new HistoryChunk
            {
                From = from,
                To = to,
                Go = go
            });

            _stepCounter = 0;
        }

        UpdatePreview();
    }

    private static void TryGetWorldPoint(Vector3 localPos, out Vector3 worldPos)
    {
        var levelTransformOpt = CL_EventManager.currentLevel?.transform;
        worldPos = levelTransformOpt?.TransformPoint(localPos) ?? localPos;
    }

    private void UpdatePreview()
    {
        if (_previewLine == null)
            return;

        if (_points.Count < 2)
        {
            _previewLine.positionCount = 0;
            return;
        }

        _previewLine.positionCount = 0;
        _previewLine.SetPositions(Array.Empty<Vector3>());

        var pointsWorld = _points
            .Select(p => CL_EventManager.currentLevel.transform.TransformPoint(p))
            .ToArray();

        _previewLine.positionCount = pointsWorld.Length;
        _previewLine.SetPositions(pointsWorld);

        if (_recorderMenuPointsTmp) _recorderMenuPointsTmp.text = $"Points: {_points.Count}";
    }
}