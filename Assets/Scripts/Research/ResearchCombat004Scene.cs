// COMBAT-004: self-drawn sprite proof of concept. Scene must be opened explicitly.
// NOT Dawnbreak artwork; NOT a production renderer or a Unity-playtested feature.
using System;
using System.Collections.Generic;
using DnfUnityMobile.Research;
using UnityEngine;

public sealed class ResearchCombat004Scene : MonoBehaviour
{
    private const float Ppu = 100f;
    private readonly Dictionary<string, FrameInfo> _frames = new Dictionary<string, FrameInfo>();
    private readonly List<UnityEngine.Object> _generatedAssets = new List<UnityEngine.Object>();
    private readonly SpriteRenderer[] _enemies = new SpriteRenderer[2];
    private readonly Transform[] _enemyRoots = new Transform[2];
    private Combat004Core _core;
    private CombatSpritePresenter _spritePresenter;
    private bool _debugHud; // F1 or compact on-screen toggle; OFF by default.
    private Transform _playerRoot;
    private SpriteRenderer _playerRenderer;
    private string _previousFrame = "";
    private float _displayFps = 60f;
    private float _guiAxis;
    private float _keyboardAxis;
    private float _oldFixedStep;
    private int _oldTargetFps;
    private Combat007Layout _layout;

    // Android native launch orientation is also set by the Editor prebuild
    // callback. Runtime request is a one-shot safety net for older PlayerSettings.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RequestAndroidLandscapeOnce()
    {
        if (Combat007Layout.ShouldLockLandscapeOnAndroid(Application.platform == RuntimePlatform.Android))
            Screen.orientation = ScreenOrientation.LandscapeLeft;
    }

    private void UpdateLayout()
    {
        Rect safe = Screen.safeArea; // device/screen bottom-left pixel coordinates
        _layout = Combat007Layout.Calculate(Screen.width, Screen.height,
            safe.x, safe.y, safe.width, safe.height);
    }

    private sealed class FrameInfo
    {
        public Sprite Sprite;
        public int Width, Height;
        public float AnchorX, AnchorY;
    }

    private void Awake()
    {
        _oldFixedStep = Time.fixedDeltaTime;
        _oldTargetFps = Application.targetFrameRate;
        Time.fixedDeltaTime = Combat004Core.FixedStep;
        Application.targetFrameRate = 60;
        if (Combat007Layout.ShouldLockLandscapeOnAndroid(Application.platform == RuntimePlatform.Android))
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        _core = new Combat004Core();
        UpdateLayout();

        var cameraObject = new GameObject("COMBAT-004 local camera");
        cameraObject.transform.SetParent(transform);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 3.8f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.09f, 0.17f);
        cameraObject.tag = "MainCamera";

        // ART-SLICE-001: all visible character/scene frames now come from
        // original checked-in PNG atlases, NOT CreateFrame/MakePanel placeholders.
        _playerRoot = new GameObject("ORIGINAL Blue Attacker [foot origin]").transform;
        _playerRoot.SetParent(transform);
        GameObject spriteChild = new GameObject("Centered frame + anchor offset");
        spriteChild.transform.SetParent(_playerRoot);
        spriteChild.transform.localPosition = new Vector3(0f, 0f, 0.12f);
        _playerRenderer = spriteChild.AddComponent<SpriteRenderer>();
        _playerRenderer.sortingOrder = 3;

        for (int i = 0; i < 2; i++)
        {
            int id = 101 + i;
            _enemyRoots[i] = new GameObject("Original Mossling " + id).transform;
            _enemyRoots[i].SetParent(transform);
            GameObject frame = new GameObject("Enemy atlas frame / centered-pivot foot offset");
            frame.transform.SetParent(_enemyRoots[i], false);
            frame.transform.localPosition = new Vector3(0f,0f,.12f);
            _enemies[i] = frame.AddComponent<SpriteRenderer>();
        }
        _spritePresenter = gameObject.AddComponent<CombatSpritePresenter>();
        _spritePresenter.Initialize(transform, _playerRoot, _playerRenderer,
            _enemyRoots, _enemies);
        RenderCombat();
    }

    private void Update()
    {
        UpdateLayout(); // Recompute after actual Android orientation/safe-area changes.

        _keyboardAxis = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
                        (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
        if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Space))
            _core.PressAttack();
        if (Input.GetKeyDown(KeyCode.R)) ResetScene();
        if (Input.GetKeyDown(KeyCode.F1)) _debugHud = !_debugHud;

        // Read all active fingers: left/right movement is held independently
        // while a different finger taps ATTACK. Attack/Reset only on TouchPhase.Began
        // (no IMGUI Button event repeats). Unity touch positions are bottom-left.
        Combat007PointerFrame pointers = new Combat007PointerFrame();
        int n = Input.touchCount;
        if (n > 0)
        {
            for (int i = 0; i < n; i++)
            {
                Touch touch = Input.GetTouch(i);
                bool active = touch.phase != TouchPhase.Ended &&
                              touch.phase != TouchPhase.Canceled;
                _layout.AddPointer(ref pointers, touch.position.x, touch.position.y,
                    active, touch.phase == TouchPhase.Began);
            }
        }
        else
        {
            // Ignore synthesized mouse while any fingers are present.
            Vector3 mouse = Input.mousePosition;
            _layout.AddPointer(ref pointers, mouse.x, mouse.y,
                Input.GetMouseButton(0), Input.GetMouseButtonDown(0));
        }
        if (pointers.AttackDown) _core.PressAttack();
        else if (pointers.ResetDown) ResetScene();
        _guiAxis = pointers.MoveAxis;
        _displayFps = Mathf.Lerp(_displayFps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.06f);
    }

    private void FixedUpdate()
    {
        _core.Move(Mathf.Clamp(_guiAxis + _keyboardAxis, -1f, 1f), Time.fixedDeltaTime);
        _core.Tick(Time.fixedDeltaTime);
        // HIT-REACTION: this is the ONLY consumption point for this 60 Hz tick.
        // Consume before next core.Tick or PressAttack can clear the snapshot.
        // Presenter never applies HP damage, nor generates fake contacts.
        _spritePresenter.ConsumeHitEvents(_core.DrainHitEvents(), _core);
        RenderCombat();
    }

    private void ResetScene()
    {
        _core.Reset();
        if (_spritePresenter != null) _spritePresenter.ResetVisualOnly();
    }

    private void RenderCombat()
    {
        _playerRoot.position = new Vector3(_core.PlayerX, -1.90f, 0f);
        for (int i = 0; i < _enemies.Length; i++)
        {
            Combat004Enemy e = _core.Enemies[i];
            // Foot origin and shadow share the lane; air height shifts body up.
            _enemyRoots[i].position = new Vector3(
                e.X, -1.90f + e.Lane * 0.32f + e.Height, -0.1f);
        }
        bool isMoving = Mathf.Abs(_guiAxis + _keyboardAxis) > 0.01f;
        _spritePresenter.Present(_core, isMoving, Time.fixedDeltaTime);
        _previousFrame = _core.Clock.FrameKey;
    }

    private void CreateFrame(string key, int w, int h, float ax, float ay, int pose)
    {
        var pixels = new Color32[w * h];
        Color32 outline = new Color32(24, 35, 55, 255);
        Color32 blue = new Color32(73, 192, 225, 255);
        Color32 light = new Color32(139, 230, 245, 255);
        Color32 steel = new Color32(223, 236, 247, 255);
        int foot = Mathf.RoundToInt(ax);
        // Deliberately different trims and arm poses. Bottom remains at each supplied foot anchor.
        // Feet terminate at texture y=0, at the declared ground anchor.
        DrawRect(pixels, w, h, foot - 13, 0, 11, 40, outline);
        DrawRect(pixels, w, h, foot + 2, 0, 12, 40, outline);
        DrawRect(pixels, w, h, foot - 21, 35, 44, 43, outline);
        DrawRect(pixels, w, h, foot - 18, 38, 38, 37, blue);
        int headY = h - 27;
        DrawRect(pixels, w, h, foot - 14, headY, 28, 20, outline);
        DrawRect(pixels, w, h, foot - 11, headY + 3, 22, 14, light);
        DrawRect(pixels, w, h, foot + 4, headY + 9, 4, 4, outline);
        int armY = pose == 0 ? 49 : (pose % 3 == 0 ? 62 : 55);
        DrawRect(pixels, w, h, foot + 17, armY, 18, 9, outline);
        DrawRect(pixels, w, h, foot + 22, armY + 2, 16, 5, light);
        // White diagonal/vertical sword is original colored rectangle artwork.
        int bladeX = pose == 0 ? foot + 34 : foot + 22 + (pose % 3) * 9;
        DrawRect(pixels, w, h, bladeX, armY - 8 + (pose % 2) * 6,
            pose % 2 == 0 ? 8 : 25, pose % 2 == 0 ? 28 : 6, steel);
        Sprite sprite = MakeSprite(key, w, h, pixels);
        _frames.Add(key, new FrameInfo { Sprite = sprite, Width = w, Height = h,
            AnchorX = ax, AnchorY = ay });
    }

    private Sprite DrawEnemySprite(int index)
    {
        int w = 70, h = 90;
        var pixels = new Color32[w * h];
        DrawRect(pixels, w, h, 6, 7, 58, 70, new Color32(77, 33, 43, 255));
        DrawRect(pixels, w, h, 10, 11, 50, 62,
            index == 0 ? new Color32(246, 128, 107, 255) : new Color32(223, 153, 91, 255));
        DrawRect(pixels, w, h, 17, 54, 10, 9, new Color32(15, 27, 35, 255));
        DrawRect(pixels, w, h, 44, 54, 10, 9, new Color32(15, 27, 35, 255));
        DrawRect(pixels, w, h, 18, 27, 34, 7, new Color32(83, 45, 40, 255));
        return MakeSprite("enemy" + index, w, h, pixels);
    }

    private static void DrawRect(Color32[] pixels, int w, int h,
        int x, int y, int rw, int rh, Color32 color)
    {
        for (int iy = Mathf.Max(y, 0); iy < Mathf.Min(y + rh, h); iy++)
            for (int ix = Mathf.Max(x, 0); ix < Mathf.Min(x + rw, w); ix++)
                pixels[iy * w + ix] = color;
    }

    private Sprite MakeSprite(string key, int w, int h, Color32[] pixels)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.name = "COMBAT004 original " + key;
        tex.filterMode = FilterMode.Point;
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, w, h),
            new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
        sprite.name = key;
        _generatedAssets.Add(tex);
        _generatedAssets.Add(sprite);
        return sprite;
    }

    private void MakePanel(string name, float x, float y, float w, float h,
        Color32 color, int order)
    {
        var px = new Color32[] { color };
        Sprite sprite = MakeSprite(name, 1, 1, px);
        // 1 pixel at PPU=100 equals 0.01 world units.
        GameObject g = new GameObject(name);
        g.transform.SetParent(transform);
        g.transform.position = new Vector3(x, y, 0f);
        g.transform.localScale = new Vector3(w * Ppu, h * Ppu, 1f);
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
    }

    private static Rect ToGuiRect(Combat007Rect r)
    {
        return new Rect(r.X, r.Y, r.Width, r.Height);
    }

    private static void ControlVisual(Combat007Rect box, string title, bool pressed)
    {
        Color previous = GUI.color;
        GUI.color = pressed ? new Color(0.75f, 1f, 0.72f, 1f) :
            new Color(0.92f, 0.95f, 1f, 1f);
        GUI.Box(ToGuiRect(box), title);
        GUI.color = previous;
    }

    private void OnGUI()
    {
        if (_core == null) return;
        UpdateLayout();
        Matrix4x4 previous = GUI.matrix;
        // One scale for X and Y. Translate to Screen.safeArea (incl. notch).
        GUI.matrix = Matrix4x4.TRS(
            new Vector3(_layout.OriginX, _layout.OriginTopY, 0f),
            Quaternion.identity, new Vector3(_layout.Scale, _layout.Scale, 1f));
        // An unobtrusive HUD keeps the action readable on a phone.
        GUI.Box(new Rect(20f,14f,380f,76f), "AETHER WARD • STAFF MAGE");
        GUI.Label(new Rect(36f,42f,340f,26f), "MOSS 101  " + _core.Enemies[0].Hp +
            "/100    MOSS 102  " + _core.Enemies[1].Hp + "/100");
        if (GUI.Button(new Rect(_layout.VirtualWidth - 102f, 116f, 80f, 42f),
            _debugHud ? "HIDE" : "DEBUG"))
            _debugHud = !_debugHud;
        if (_debugHud)
        {
            GUI.Box(ToGuiRect(_layout.Hud), "COMBAT ART SLICE / DEBUG");
            float left = _layout.Hud.X + 14f;
            float width = _layout.Hud.Width - 24f;
            GUI.Label(new Rect(left, 48, width, 25), "STAGE " + _core.Clock.Stage
                + "   FRAME " + _previousFrame + "   elapsed " + _core.Clock.Elapsed.ToString("F3")
                + "   BUFFER " + _core.Clock.QueuedFor.ToString("F3"));
            GUI.Label(new Rect(left, 80, width, 25), "HIT WINDOW " + _core.Clock.IsHitWindow
                + "   INSTANCE " + _core.AttackInstance
                + "   ATTACKER FREEZE " + _core.AttackerFreeze.ToString("F3"));
            GUI.Label(new Rect(left, 112, width, 25), "ENEMY 101   HP " + _core.Enemies[0].Hp
                + "   " + _core.Enemies[0].State
                + "   FREEZE " + _core.Enemies[0].Freeze.ToString("F3"));
            GUI.Label(new Rect(left, 144, width, 25), "ENEMY 102   HP " + _core.Enemies[1].Hp
                + "   " + _core.Enemies[1].State
                + "   FREEZE " + _core.Enemies[1].Freeze.ToString("F3"));
            GUI.Label(new Rect(left, 177, width, 25), "CONTACTS " + _core.TotalContacts
                + "   FPS approx " + _displayFps.ToString("F0")
                + "   " + _core.LastEvent);
        }
        GUI.Label(ToGuiRect(_layout.Hint), Combat007Layout.NeedsLandscapeRotation(
            Screen.width, Screen.height)
            ? "Waiting for Android landscape orientation..."
            : "Move / Strike 1 - 2 - 3   •   F1 DEBUG");
        // Drawing only: touch handling is done in Update, supporting two fingers.
        ControlVisual(_layout.Left, "LEFT", _guiAxis < 0f);
        ControlVisual(_layout.Right, "RIGHT", _guiAxis > 0f);
        ControlVisual(_layout.Attack, "ATTACK", false);
        ControlVisual(_layout.Reset, "RESET", false);
        GUI.matrix = previous;
    }

    private void OnDestroy()
    {
        Time.fixedDeltaTime = _oldFixedStep;
        Application.targetFrameRate = _oldTargetFps;
        for (int i = 0; i < _generatedAssets.Count; i++)
            if (_generatedAssets[i] != null) Destroy(_generatedAssets[i]);
    }
}
