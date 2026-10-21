using UnityEngine;

/// <summary>
/// Original, code-only Android touch smoke test. Not a networking or performance benchmark.
/// Spawned automatically when the Bootstrap scene loads; no prefab or paid assets required.
/// </summary>
public sealed class MobileSmoke : MonoBehaviour
{
    private const float ReferenceWidth = 1280f;
    private const float ReferenceHeight = 720f;
    private const float TargetX = 820f;

    private float _playerX = 340f;
    private float _inputAxis;
    private float _attackUntil;
    private float _smoothedFps = 60f;
    private int _targetHp = 50;
    private int _attackCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateSmokeScene()
    {
        if (FindAnyObjectByType<MobileSmoke>() != null)
            return;

        var cameraObject = new GameObject("Smoke Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.10f, 0.16f);
        cameraObject.tag = "MainCamera";

        new GameObject("Mobile Smoke Controller").AddComponent<MobileSmoke>();
    }

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Application.targetFrameRate = 60;
    }

    private void Update()
    {
        _playerX = Mathf.Clamp(_playerX + _inputAxis * 300f * Time.deltaTime, 80f, 1180f);
        _smoothedFps = Mathf.Lerp(_smoothedFps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.08f);
    }

    private void Attack()
    {
        _attackCount++;
        _attackUntil = Time.time + 0.18f;

        if (Mathf.Abs(_playerX - TargetX) <= 190f && _targetHp > 0)
            _targetHp = Mathf.Max(0, _targetHp - 10);
    }

    private static void Paint(Rect rectangle, Color color)
    {
        var oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rectangle, Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    private void OnGUI()
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(
            Screen.width / ReferenceWidth,
            Screen.height / ReferenceHeight,
            1f));

        Paint(new Rect(0, 0, ReferenceWidth, ReferenceHeight), new Color(0.09f, 0.13f, 0.20f));
        Paint(new Rect(0, 500, ReferenceWidth, 160), new Color(0.19f, 0.26f, 0.30f));

        bool attacking = Time.time < _attackUntil;
        Paint(new Rect(_playerX - 28, 410, 56, 90), attacking ? Color.yellow : new Color(0.2f, 0.8f, 0.9f));
        if (_targetHp > 0)
            Paint(new Rect(TargetX - 30, 410, 60, 90), new Color(0.94f, 0.35f, 0.28f));

        GUI.Label(new Rect(24, 22, 1120, 34), "UNITY ANDROID SMOKE TEST - ORIGINAL PLACEHOLDER GAME");
        GUI.Label(new Rect(24, 65, 500, 30), "FPS (approx): " + _smoothedFps.ToString("F1"));
        GUI.Label(new Rect(24, 100, 630, 30), "Target HP: " + _targetHp + "    Attacks: " + _attackCount);
        GUI.Label(new Rect(24, 135, 1050, 30), "Move close to the red target. Tap ATTACK to test touch input.");
        GUI.Label(new Rect(_playerX - 36, 379, 120, 30), "PLAYER");
        GUI.Label(new Rect(TargetX - 25, 379, 120, 30), "TARGET");

        bool left = GUI.RepeatButton(new Rect(60, 555, 150, 110), "LEFT");
        bool right = GUI.RepeatButton(new Rect(230, 555, 150, 110), "RIGHT");
        _inputAxis = (right ? 1f : 0f) - (left ? 1f : 0f);

        if (GUI.Button(new Rect(950, 555, 155, 110), "ATTACK"))
            Attack();
        if (GUI.Button(new Rect(1110, 30, 130, 72), "RESET"))
        {
            _playerX = 340f;
            _targetHp = 50;
            _attackCount = 0;
        }

        GUI.matrix = previousMatrix;
    }
}
