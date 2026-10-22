// ART-SLICE-001: original hand-authored PNG atlases, no third-party art.
// Display layer ONLY. HP, hit detection, attack stages, hitstop and damage
// continue to be produced solely by Combat004Core / ThreeHitReferenceClock.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DnfUnityMobile.Research
{
    public sealed class CombatSpritePresenter : MonoBehaviour
    {
        public const float PixelsPerUnit = 80f;
        private static readonly string[] PlayerKeys = {
            "idle0","idle1","idle2","walk0","walk1","walk2","walk3",
            "a1_1","a1_2","a1_3","a2_1","a2_2","a2_3","a3_1","a3_2",
            "hit","airUp","down","getup"
        };
        private static readonly string[] EnemyKeys = {
            "idle0","idle1","idle2","walk0","walk1","hurt0","hurt1",
            "air0","air1","down0","down1","ko0"
        };
        private static readonly string[] BackgroundKeys = {"sky","ruins","floor"};
        private static readonly string[] FxKeys = {"thrust","sweep","upper"};
        private readonly Dictionary<string, Sprite> _player =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> _enemy =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> _background =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> _vfx =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly List<Sprite> _slicedSprites = new List<Sprite>();
        private readonly List<UnityEngine.Object> _ownedGeometry = new List<UnityEngine.Object>();
        private readonly SpriteRenderer[] _enemyShadows = new SpriteRenderer[2];
        private readonly int[] _previousHits = new int[2];
        private readonly float[] _enemyPoseTime = new float[2];
        private readonly float[] _hitFlash = new float[2];
        private SpriteRenderer _playerShadow;
        private SpriteRenderer _attackTrail;
        private SpriteRenderer[] _enemyRenderers;
        private Transform[] _enemyRoots;
        private Transform _playerRoot;
        private SpriteRenderer _playerRenderer;
        private Transform _worldRoot;
        private float _walkTime;
        private float _idleTime;
        private bool _lastWasMoving;
        private bool _initialized;

        // Atlases are in both Assets/Art (authoring) and Assets/Resources
        // (runtime copies, git-identical PNG blobs). Sprite.Create references
        // a full atlas texture with RECT selection: no runtime colored boxes.
        private static Texture2D RequireAtlas(string path)
        {
            Texture2D texture = Resources.Load<Texture2D>("OriginalCombatSlice/" + path);
            if (texture == null)
                throw new InvalidOperationException("Missing original art Resources atlas: " + path);
            return texture;
        }

        private void LoadAtlas(string name, int frameW, int frameH, int columns,
            string[] keys, float ppu, Dictionary<string, Sprite> output)
        {
            Texture2D tex = RequireAtlas(name);
            int rows = (keys.Length + columns - 1) / columns;
            if (tex.width != frameW * columns || tex.height != frameH * rows)
                throw new InvalidOperationException(name + " has unexpected pixel dimensions: " +
                    tex.width + "x" + tex.height);
            for (int i = 0; i < keys.Length; i++)
            {
                // Unity's texture/Sprite rect Y is bottom-left, atlas metadata
                // keys and rows are enumerated from the top-left.
                float x = (i % columns) * frameW;
                float y = tex.height - ((i / columns) + 1) * frameH;
                Sprite sprite = Sprite.Create(tex, new Rect(x, y, frameW, frameH),
                    new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
                sprite.name = name + "/" + keys[i];
                output.Add(keys[i], sprite);
                _slicedSprites.Add(sprite);
            }
        }

        public void Initialize(Transform worldRoot, Transform playerRoot,
            SpriteRenderer playerRenderer, Transform[] enemyRoots,
            SpriteRenderer[] enemyRenderers)
        {
            if (_initialized) return;
            if (enemyRoots == null || enemyRenderers == null ||
                enemyRoots.Length != 2 || enemyRenderers.Length != 2)
                throw new ArgumentException("ART-SLICE-001 expects two existing enemy renderers.");
            _worldRoot = worldRoot;
            _playerRoot = playerRoot;
            _playerRenderer = playerRenderer;
            _enemyRoots = enemyRoots;
            _enemyRenderers = enemyRenderers;
            LoadAtlas("Player/mage_atlas",128,160,5,PlayerKeys,PixelsPerUnit,_player);
            LoadAtlas("Enemy/mossling_atlas",112,128,4,EnemyKeys,PixelsPerUnit,_enemy);
            LoadAtlas("Background/landscape_atlas",640,360,1,BackgroundKeys,40f,_background);
            LoadAtlas("Vfx/arc_atlas",128,160,3,FxKeys,PixelsPerUnit,_vfx);

            MakeBackdrop("sky", -92, new Color(1f,1f,1f,1f), 1.5f);
            MakeBackdrop("ruins", -85, new Color(.82f,.93f,1f,1f), 1.5f);
            MakeBackdrop("floor", -75, Color.white, 1.5f);

            Sprite shadow = MakeShadow();
            _playerShadow = MakeChildShadow(playerRoot,"Ground shadow: player",shadow,0);
            for (int i = 0; i < 2; i++)
                _enemyShadows[i] = MakeChildShadow(enemyRoots[i],"Ground shadow: enemy",shadow,0);
            GameObject swing = new GameObject("Weapon arc (not damage adjudication)");
            swing.transform.SetParent(playerRoot,false);
            swing.transform.localPosition = new Vector3(0f,0.7f,0.03f);
            _attackTrail = swing.AddComponent<SpriteRenderer>();
            _attackTrail.sortingOrder = 7;
            _attackTrail.enabled = false;

            _playerRenderer.sortingOrder = 5;
            _initialized = true;
        }

        private void MakeBackdrop(string key, int order, Color tint, float scale)
        {
            GameObject go = new GameObject("Original 2.5D " + key);
            go.transform.SetParent(_worldRoot,false);
            go.transform.localPosition = new Vector3(0f,0f,0f);
            go.transform.localScale = new Vector3(scale,scale,1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _background[key]; sr.sortingOrder = order; sr.color = tint;
        }

        private Sprite MakeShadow()
        {
            const int w=70,h=20;
            Texture2D tex = new Texture2D(w,h,TextureFormat.RGBA32,false);
            var pixels = new Color32[w*h];
            for(int y=0;y<h;y++) for(int x=0;x<w;x++)
            {
                float u=(x-w*.5f)/(w*.48f),v=(y-h*.5f)/(h*.45f);
                float d=u*u+v*v;
                pixels[y*w+x] = d>1f? new Color32(0,0,0,0) :
                    new Color32(14,20,34,(byte)Mathf.RoundToInt(110f*(1f-d)));
            }
            tex.SetPixels32(pixels); tex.Apply(false,false);
            Sprite sprite=Sprite.Create(tex,new Rect(0f,0f,w,h),
                new Vector2(.5f,.5f),PixelsPerUnit);
            _ownedGeometry.Add(tex);_ownedGeometry.Add(sprite);
            return sprite;
        }

        private SpriteRenderer MakeChildShadow(Transform actor,string name,Sprite sprite,int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(actor,false);
            go.transform.localPosition = new Vector3(0f,-.03f,.01f);
            SpriteRenderer sr=go.AddComponent<SpriteRenderer>();
            sr.sprite=sprite;sr.sortingOrder=order;return sr;
        }

        // The old game loop owns all states. We observe it AFTER core.Tick().
        // Frame selection NEVER changes combat hits, HP, hitstop, knockback or timing.
        public void Present(Combat004Core combat, bool isMoving, float deltaSeconds)
        {
            if (!_initialized) return;
            if (combat.AttackerFreeze <= 0f)
            {
                _idleTime += deltaSeconds;
                if (isMoving) _walkTime += deltaSeconds;
                else if (_lastWasMoving) _walkTime = 0f;
            }
            _lastWasMoving = isMoving;
            string frame=ChoosePlayerFrame(combat,isMoving,true);
            _playerRenderer.flipX = !combat.FacingRight;
            // Position origin is the actor's *foot*; ReferenceSpriteAlignment
            // recalculates X on EVERY facing/frame change and keeps Z unchanged.
            ReferenceSpriteAlignment.ApplyCenteredSprite(_playerRenderer,_player[frame],
                128,160,48f,146f,PixelsPerUnit);
            _playerRenderer.color=combat.AttackerFreeze>0f
                ? new Color(.9f,.98f,1f,1f):Color.white;
            _playerShadow.sortingOrder=2;
            _playerShadow.transform.localPosition=new Vector3(0f,-.03f,.01f);
            _attackTrail.enabled=combat.Clock.Stage>0 && combat.Clock.IsHitWindow;
            if(_attackTrail.enabled)
            {
                string fx=combat.Clock.Stage==1?"thrust":combat.Clock.Stage==2?"sweep":"upper";
                _attackTrail.sprite=_vfx[fx];
                _attackTrail.flipX=!combat.FacingRight;
            }

            for(int i=0;i<2;i++)
            {
                Combat004Enemy e=combat.Enemies[i];
                if (e.Freeze<=0f) _enemyPoseTime[i]+=deltaSeconds;
                if (e.Hits!=_previousHits[i]) { _hitFlash[i]=.16f;_previousHits[i]=e.Hits; }
                if (_hitFlash[i]>0f) _hitFlash[i]=Math.Max(0f,_hitFlash[i]-deltaSeconds);
                string enemyKey=ChooseEnemyFrame(e,_enemyPoseTime[i]);
                _enemyRenderers[i].flipX=true; // enemies face attacker at left
                ReferenceSpriteAlignment.ApplyCenteredSprite(_enemyRenderers[i],
                    _enemy[enemyKey],112,128,55f,118f,PixelsPerUnit);
                _enemyRenderers[i].sortingOrder=4+Mathf.RoundToInt(-e.Lane*4f);
                _enemyRenderers[i].color= _hitFlash[i]>0f ?
                    new Color(1f,.75f,.66f,1f):Color.white;
                // Height only affects body: ground shadow is decoupled from jump.
                _enemyShadows[i].transform.localPosition=
                    new Vector3(0f,-e.Height-.02f,.02f);
                _enemyShadows[i].sortingOrder=2;
            }
        }

        public static string ChoosePlayerFrame(Combat004Core combat,bool moving)
        {
            if(combat.Clock.Stage>0) return combat.Clock.FrameKey;
            // Time-dependent idle/walk is resolved by instance below.
            return moving ? "walk0" : "idle0";
        }

        private string ChoosePlayerFrame(Combat004Core combat, bool moving, bool animated)
        {
            if (combat.Clock.Stage>0) return combat.Clock.FrameKey;
            if(moving) return "walk" + ((int)(_walkTime/.11f)%4);
            return "idle" + ((int)(_idleTime/.34f)%3);
        }

        public static string ChooseEnemyFrame(Combat004Enemy enemy,float poseTime)
        {
            if(enemy.Hp<=0) return "ko0";
            if(enemy.Height>0f || enemy.VerticalSpeed>0f)
                return "air"+((int)(poseTime/.16f)%2);
            if(enemy.Freeze>0f || enemy.Stun>0f)
                return "hurt"+((int)(poseTime/.18f)%2);
            return "idle"+((int)(poseTime/.32f)%3);
        }

        public void ResetVisualOnly()
        {
            _walkTime=0f;_idleTime=0f;_lastWasMoving=false;
            for(int i=0;i<2;i++){_previousHits[i]=0;_enemyPoseTime[i]=0f;_hitFlash[i]=0f;}
        }

        private void OnDestroy()
        {
            for(int i=0;i<_slicedSprites.Count;i++)
                if(_slicedSprites[i]!=null) Destroy(_slicedSprites[i]);
            for(int i=0;i<_ownedGeometry.Count;i++)
                if(_ownedGeometry[i]!=null) Destroy(_ownedGeometry[i]);
        }
    }
}
