// ANDROID-BLANK-001. Independent runtime proof of scene startup progress.
// Installs BEFORE scene load. Does not require original art, combat classes,
// Resources.Load, or a successful ResearchCombat004Scene.Awake().
// Diagnostic is intentionally visible on screen until first successful GUI draw.
using System;
using System.IO;
using DnfUnityMobile.Research;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public sealed class CombatStartupDiagnostics : MonoBehaviour
{
    private static CombatStartupDiagnostics _instance;
    private readonly CombatStartupTrace _trace = new CombatStartupTrace();
    private bool _sceneGuiPainted;
    private bool _sceneLoaded;
    private float _born;
    private string _logPath;
    private bool _hasEngineError;
    private string _lastEngineError = "";
    private bool _tintedFailure;
    private int _repaintCount;
    private bool _diagnosticGuiFaultReported;
    // Pure C# policy enforces per-session deduplication and a hard entry cap.
    private readonly CombatDiagnosticWritePolicy _writePolicy =
        new CombatDiagnosticWritePolicy();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallBeforeSceneLoad()
    {
        if (_instance != null) return;
        GameObject host = new GameObject("ANDROID-BLANK-001 Startup Diagnostics");
        DontDestroyOnLoad(host);
        _instance = host.AddComponent<CombatStartupDiagnostics>();
        Mark("DIAGNOSTICS_INSTALLED");
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        _born = Time.realtimeSinceStartup;
        try
        {
            _logPath = Path.Combine(Application.persistentDataPath, "combat-startup-diagnostic.txt");
            // Fresh cold-start snapshot; see Persist() for per-session hard cap.
            _writePolicy.Reset();
            File.WriteAllText(_logPath,
                "ANDROID-BLANK-001 startup diagnostics; no user credentials or raw stack traces\n");
        }
        catch (Exception) { _logPath = null; }
        _trace.Enter("DIAGNOSTICS_AWAKE");
        Persist("DIAGNOSTICS_AWAKE");
        Application.logMessageReceived += OnEngineLog;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _sceneLoaded = true;
        Mark("SCENE_LOADED");
        // Scene name is available in adb logcat, not recorded with any user data.
        Debug.Log("ANDROID-BLANK-001 scene-loaded / " + scene.name);
    }

    public static void Mark(string literalStage)
    {
        if (_instance == null || _instance._trace.Failed) return;
        try
        {
            _instance._trace.Enter(literalStage);
            _instance.Persist(literalStage);
        }
        catch (Exception) { /* diagnostics must never crash the battle scene */ }
    }

    public static void Ready()
    {
        if (_instance == null) return;
        _instance._trace.SetReady();
        _instance.Persist("SCENE_READY");
    }

    // Do not convert an initialization exception into a misleading success.
    // Preserve its first type, stage, and full stack separately in adb logcat.
    public static void Fail(string literalStage, Exception ex)
    {
        if (_instance != null)
        {
            try
            {
                _instance._trace.Fail(literalStage,
                    ex == null ? "UnknownException" : ex.GetType().Name);
            }
            catch (Exception) { /* never mask the scene's original exception */ }
            try { _instance.Persist("FAIL_" + literalStage); }
            catch (Exception) { }
            try { _instance.TintFailureCamera(); }
            catch (Exception) { }
        }
        if (ex != null) Debug.LogException(ex);
    }

    private void OnEngineLog(string condition, string stackTrace, LogType kind)
    {
        if (kind != LogType.Error && kind != LogType.Exception &&
            kind != LogType.Assert) return;
        // Type-only durable signal. NEVER persist raw message/stack potentially
        // containing local filesystem paths or other sensitive fields.
        _hasEngineError = true;
        _lastEngineError = kind.ToString();
        Persist("ENGINE_" + _lastEngineError);
    }

    private void Persist(string code)
    {
        if (_logPath == null) return;
        try
        {
            // Known own stage identifiers only; no arbitrary Unity log body.
            if (!_writePolicy.TryBegin(code)) return;
            File.AppendAllText(_logPath, DateTime.UtcNow.ToString("HH:mm:ss.fff") +
                " " + code + "\n");
        }
        catch (Exception) { }
    }

    public static void SceneGuiPainted()
    {
        if (_instance == null || _instance._sceneGuiPainted) return;
        _instance._sceneGuiPainted = true;
        Mark("SCENE_GUI_REPAINT");
    }

    private void TintFailureCamera()
    {
        if (_tintedFailure) return;
        _tintedFailure = true;
        Camera cam = Camera.main;
        if (cam == null && Camera.allCamerasCount > 0)
            cam = Camera.allCameras[0];
        if (cam == null)
        {
            // No scene camera at all: make startup FAILURE visually distinct
            // without loading any texture, font, sprite or combat object.
            GameObject emergency = new GameObject("ANDROID-BLANK-001 Failure Camera");
            emergency.transform.SetParent(transform, false);
            emergency.transform.position = new Vector3(0f, 0f, -20f);
            cam = emergency.AddComponent<Camera>();
            cam.cullingMask = 0;
            cam.depth = -100f;
        }
        cam.backgroundColor = new Color(0.33f, 0.065f, 0.075f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    private void Update()
    {
        // This independent MonoBehaviour continues if the research script failed.
        if (!_trace.Ready && !_trace.Failed &&
            Time.realtimeSinceStartup - _born > 12f)
        {
            _trace.Fail(_sceneLoaded ? "SCENE_NOT_READY" : "SCENE_NOT_LOADED",
                "StartupTimeout");
            Persist("FAIL_STARTUP_TIMEOUT");
            TintFailureCamera();
        }
    }

    private void OnGUI()
    {
        Color previousGuiColor = Color.white;
        bool restoreGuiColor = false;
        try
        {
            // GUI coordinates are top-left-origin physical pixels.
            Rect safe = Screen.safeArea;
            if (safe.width < 1f || safe.height < 1f)
                safe = new Rect(0f, 0f, Screen.width, Screen.height);
            float x = Mathf.Clamp(safe.xMin + 10f, 6f, Screen.width - 30f);
            float top = Mathf.Clamp(Screen.height - safe.yMax + 8f, 5f,
                Mathf.Max(5f, Screen.height - 30f));
            float width = Mathf.Min(610f, Screen.width - x - 10f);
            if (width < 120f) return;
            bool failed = _trace.Failed;
            bool showDetails = failed || !_trace.Ready || !_sceneGuiPainted;
            float height = showDetails ? 180f : 76f;
            previousGuiColor = GUI.color;
            restoreGuiColor = true;
            GUI.color = failed ? new Color(1f, .55f, .57f, 1f)
                : (_trace.Ready ? new Color(.65f, 1f, .78f, 1f)
                    : new Color(1f, .91f, .62f, 1f));
            GUI.Box(new Rect(x, top, width, height),
                failed ? "ANDROID STARTUP FAILED — DIAGNOSTIC" :
                    (_trace.Ready ? "ANDROID STARTUP READY — VERIFY ART + CONTROLS"
                         : "ANDROID STARTUP IN PROGRESS"));
            GUIStyle label = new GUIStyle(GUI.skin.label);
            label.fontSize = Mathf.Clamp(Screen.height / 37, 14, 23);
            label.normal.textColor = Color.white;
            GUI.Label(new Rect(x + 14f, top + 30f, width - 25f, 34f),
                "Stage: " + _trace.LastStage, label);
            if (showDetails)
            {
                GUI.Label(new Rect(x + 14f, top + 62f, width - 25f, 32f),
                    failed ? "Cause: " + _trace.FaultStage + " / " + _trace.FaultType :
                        "Waiting: scene=" + _sceneLoaded + " GUI=" + _sceneGuiPainted,
                    label);
                GUI.Label(new Rect(x + 14f, top + 94f, width - 25f, 32f),
                    "Unity errors seen: " + _hasEngineError + " " + _lastEngineError,
                    label);
                GUI.Label(new Rect(x + 14f, top + 126f, width - 25f, 38f),
                    "Log: combat-startup-diagnostic.txt (app files); adb logcat",
                    label);
            }
            if (Event.current != null && Event.current.type == EventType.Repaint)
                _repaintCount++;
        }
        catch (Exception ex)
        {
            // Report ONCE: even a broken independent diagnostic GUI must not
            // degrade back into an unreported dark-blue screen.
            if (!_diagnosticGuiFaultReported)
            {
                _diagnosticGuiFaultReported = true;
                // Error reporting itself must not throw or overwrite first fault.
                try { _trace.Fail("DIAG_ONGUI", ex.GetType().Name); }
                catch (Exception) { }
                try { Persist("FAIL_DIAG_ONGUI"); }
                catch (Exception) { }
                try { TintFailureCamera(); }
                catch (Exception) { }
                try { Debug.LogException(ex); }
                catch (Exception) { }
            }
        }
        finally
        {
            if (restoreGuiColor)
            {
                try { GUI.color = previousGuiColor; }
                catch (Exception) { }
            }
        }
    }

    private void OnDestroy()
    {
        if (_instance != this) return;
        Application.logMessageReceived -= OnEngineLog;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _instance = null;
    }
}
