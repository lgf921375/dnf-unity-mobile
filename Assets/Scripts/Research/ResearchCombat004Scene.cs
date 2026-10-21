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
    private Transform _playerRoot;
    private SpriteRenderer _playerRenderer;
    private string _previousFrame = "";
    private float _displayFps = 60f;
    private float _guiAxis;
    private float _keyboardAxis;
    private float _oldFixedStep;
    private int _oldTargetFps;

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
        _core = new Combat004Core();

        var cameraObject = new GameObject("COMBAT-004 local camera");
        cameraObject.transform.SetParent(transform);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 3.8f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.09f, 0.17f);
        cameraObject.tag = "MainCamera";

        MakePanel("Floor", 0f, -2.65f, 18f, 1.0f,
            new Color32(46, 63, 70, 255), -4);
        MakePanel("Battle area", 0f, -2.0f, 18f, 0.22f,
            new Color32(83, 118, 134, 255), -3);
        MakePanel("Horizon", 0f, 1.0f, 18f, 0.06f,
            new Color32(66, 115, 150, 255), -4);

        // Distinct dimensions and trims: all generated locally at runtime.
        CreateFrame("idle", 78, 110, 39f, 109f, 0);
        CreateFrame("a1_1", 82, 115, 36.5f, 114.2f, 1);
        CreateFrame("a1_2", 99, 108, 54f, 108.2f, 2);
        CreateFrame("a1_3", 106, 112, 49f, 111.7f, 3);
        CreateFrame("a2_1", 88, 114, 44f, 113.8f, 4);
        CreateFrame("a2_2", 109, 105, 58.5f, 105.4f, 5);
        CreateFrame("a2_3", 105, 116, 48f, 115.8f, 6);
        CreateFrame("a3_1", 112, 123, 32.5f, 123.2f, 7);
        CreateFrame("a3_2", 116, 99, 70f, 99.2f, 8);

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
            _enemyRoots[i] = new GameObject("ORIGINAL Enemy " + id).transform;
            _enemyRoots[i].SetParent(transform);
            _enemies[i] = _enemyRoots[i].gameObject.AddComponent<SpriteRenderer>();
            _enemies[i].sprite = DrawEnemySprite(i);
            _enemies[i].sortingOrder = 2;
        }
        RenderCombat();
    }

    private void Update()
    {
        _keyboardAxis = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
                        (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
        if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Space))
            _core.PressAttack();
        if (Input.GetKeyDown(KeyCode.R)) _core.Reset();
        _displayFps = Mathf.Lerp(_displayFps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.06f);
    }

    private void FixedUpdate()
    {
        _core.Move(Mathf.Clamp(_guiAxis + _keyboardAxis, -1f, 1f), Time.fixedDeltaTime);
        _core.Tick(Time.fixedDeltaTime);
        RenderCombat();
    }

    private void RenderCombat()
    {
        _playerRoot.position = new Vector3(_core.PlayerX, -1.90f, 0f);
        string name = _core.Clock.FrameKey;
        FrameInfo f = _frames[name];
        _playerRenderer.flipX = !_core.FacingRight;
        ReferenceSpriteAlignment.ApplyCenteredSprite(_playerRenderer, f.Sprite,
            f.Width, f.Height, f.AnchorX, f.AnchorY, Ppu);
        // Apply on EVERY update, including when flipX changes but frame key does not.
        _playerRenderer.color = _core.AttackerFreeze > 0f
            ? new Color(1f, 0.96f, 0.4f) : Color.white;
        _previousFrame = name;

        for (int i = 0; i < _enemies.Length; i++)
        {
            Combat004Enemy e = _core.Enemies[i];
            _enemyRoots[i].position = new Vector3(e.X, -1.40f + e.Lane * 0.32f + e.Height, -0.1f);
            _enemies[i].color = e.Hp == 0 ? Color.gray
                : e.Freeze > 0f ? Color.yellow
                : e.Stun > 0f ? new Color(1f, 0.52f, 0.45f) : Color.white;
        }
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
        DrawRect(pixels, w, h, foot - 13, 18, 11, 37, outline);
        DrawRect(pixels, w, h, foot + 2, 18, 12, 37, outline);
        DrawRect(pixels, w, h, foot - 21, 45, 44, 43, outline);
        DrawRect(pixels, w, h, foot - 18, 48, 38, 37, blue);
        DrawRect(pixels, w, h, foot - 14, 90, 28, 17, outline);
        DrawRect(pixels, w, h, foot - 11, 93, 22, 11, light);
        DrawRect(pixels, w, h, foot + 4, 98, 4, 4, outline);
        int armY = pose == 0 ? 60 : (pose % 3 == 0 ? 85 : 72);
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

    private void OnGUI()
    {
        if (_core == null) return;
        Matrix4x4 old = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f,
            Screen.height / 720f, 1f));
        GUI.Box(new Rect(16, 12, 870, 228), "COMBAT-004 / ORIGINAL Unity 6 local scene");
        GUI.Label(new Rect(36, 48, 830, 28), "STAGE " + _core.Clock.Stage
            + "    FRAME " + _previousFrame + "    elapsed " + _core.Clock.Elapsed.ToString("F3")
            + "    QUEUE " + _core.Clock.QueuedFor.ToString("F3"));
        GUI.Label(new Rect(36, 82, 830, 28), "HIT WINDOW " + _core.Clock.IsHitWindow
            + "    ATTACK INSTANCE " + _core.AttackInstance
            + "    ATTACKER HITSTOP " + _core.AttackerFreeze.ToString("F3"));
        GUI.Label(new Rect(36, 115, 830, 28), "TARGET 101   HP " + _core.Enemies[0].Hp
            + "    " + _core.Enemies[0].State
            + "   hitstop " + _core.Enemies[0].Freeze.ToString("F3")
            + "   hits " + _core.Enemies[0].Hits);
        GUI.Label(new Rect(36, 147, 830, 28), "TARGET 102   HP " + _core.Enemies[1].Hp
            + "    " + _core.Enemies[1].State
            + "   hitstop " + _core.Enemies[1].Freeze.ToString("F3")
            + "   hits " + _core.Enemies[1].Hits);
        GUI.Label(new Rect(36, 180, 830, 28), "CONTACTS " + _core.TotalContacts
            + "     FPS approx " + _displayFps.ToString("F0")
            + "     LAST " + _core.LastEvent);
        GUI.Label(new Rect(40, 245, 920, 28), "A/D or arrows MOVE  |  J/SPACE ATTACK repeatedly  |  R RESET");

        bool left = GUI.RepeatButton(new Rect(26, 580, 170, 102), "LEFT");
        bool right = GUI.RepeatButton(new Rect(210, 580, 170, 102), "RIGHT");
        _guiAxis = (right ? 1f : 0f) - (left ? 1f : 0f);
        if (GUI.Button(new Rect(980, 580, 170, 102), "ATTACK"))
            _core.PressAttack();
        if (GUI.Button(new Rect(1090, 24, 158, 84), "RESET"))
            _core.Reset();
        GUI.matrix = old;
    }

    private void OnDestroy()
    {
        Time.fixedDeltaTime = _oldFixedStep;
        Application.targetFrameRate = _oldTargetFps;
        for (int i = 0; i < _generatedAssets.Count; i++)
            if (_generatedAssets[i] != null) Destroy(_generatedAssets[i]);
    }
}
