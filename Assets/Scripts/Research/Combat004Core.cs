// Original single-player combat slice; fixed 60Hz 2.5D frame overlap, NOT Unity Physics2D.
// HitEvent is a view-only fact emitted AFTER the one authoritative HP transaction.
using System;
using System.Collections.Generic;

namespace DnfUnityMobile.Research
{
    public sealed class Combat004Enemy
    {
        public readonly int Id;
        public float X;
        public readonly float Lane;
        public int Hp = 100;
        public int Hits;
        public float Freeze;
        public float Stun;
        public float Push;
        public float Height;
        public float VerticalSpeed;

        private float _reactionTime;
        public CombatReactionPhase Reaction { get; private set; } = CombatReactionPhase.READY;
        public bool OtgConsumed { get; private set; }
        public float GroundFootX => X;
        public float GroundFootLane => Lane;
        public bool IsProtected => Reaction == CombatReactionPhase.GETUP ||
            Reaction == CombatReactionPhase.INVUL ||
            (OtgConsumed && (Reaction == CombatReactionPhase.AIR_RISE ||
                             Reaction == CombatReactionPhase.AIR_FALL ||
                             Reaction == CombatReactionPhase.DOWN));

        public Combat004Enemy(int id, float x, float lane)
        {
            Id = id;
            X = x;
            Lane = lane;
        }

        // Compatibility string used by the existing debug-only Unity scene.
        public string State
        {
            get
            {
                if (Hp <= 0) return "KO";
                if (Freeze > 0f) return "HITSTOP";
                if (Reaction == CombatReactionPhase.AIR_RISE ||
                    Reaction == CombatReactionPhase.AIR_FALL) return "AIR";
                if (Reaction == CombatReactionPhase.HIT) return "HIT";
                return Reaction.ToString();
            }
        }

        internal bool CanReceive(int stage)
        {
            if (Hp <= 0 || IsProtected) return false;
            // Only stage 1/2 low attack has a valid grounded OTG matrix.
            if (Reaction == CombatReactionPhase.DOWN)
                return !OtgConsumed && Height == 0f && stage <= 2;
            return true;
        }

        // Damage is applied exactly once here. Event creation is downstream.
        internal bool ReceiveHit(int stage, int damage, int direction, float freeze,
            bool isOtg)
        {
            if (!CanReceive(stage)) return false;
            Hp = Math.Max(0, Hp - damage);
            Hits++;
            Stun = Math.Max(Stun, stage == 3 ? 0.42f : 0.22f);
            Push = direction * (stage == 3 ? 2.3f : (isOtg ? 0.65f : 0.4f));
            if (Hp <= 0)
            {
                Reaction = CombatReactionPhase.KO;
                Height = 0f;
                VerticalSpeed = 0f;
                Push = 0f;
                _reactionTime = 0f;
            }
            else if (isOtg)
            {
                OtgConsumed = true;
                Height = CombatReactionContract.OtgInitialHeight;
                VerticalSpeed = CombatReactionContract.OtgBounceSpeed;
                Reaction = CombatReactionPhase.AIR_RISE;
                _reactionTime = 0f;
            }
            else if (stage == 3)
            {
                // A new independent launch creates a NEW knockdown opportunity.
                OtgConsumed = false;
                VerticalSpeed = CombatReactionContract.LaunchSpeed;
                Reaction = CombatReactionPhase.AIR_RISE;
                _reactionTime = 0f;
            }
            else if (Reaction == CombatReactionPhase.READY ||
                     Reaction == CombatReactionPhase.HIT)
            {
                Reaction = CombatReactionPhase.HIT;
            }
            Freeze = Math.Max(Freeze, freeze);
            return true;
        }

        public void Tick(float dt)
        {
            if (Hp <= 0)
            {
                Reaction = CombatReactionPhase.KO;
                Height = 0f;
                VerticalSpeed = 0f;
                Push = 0f;
                Freeze = 0f;
                return;
            }
            if (Freeze > 0f)
            {
                Freeze = Math.Max(0f, Freeze - dt);
                return; // independent victim-local time: motion and phases pause.
            }

            Stun = Math.Max(0f, Stun - dt);
            X += Push * dt;
            Push *= Math.Max(0f, 1f - 6f * dt);
            switch (Reaction)
            {
                case CombatReactionPhase.HIT:
                    if (Stun <= 0f) Reaction = CombatReactionPhase.READY;
                    break;
                case CombatReactionPhase.AIR_RISE:
                case CombatReactionPhase.AIR_FALL:
                    Height += VerticalSpeed * dt;
                    VerticalSpeed -= CombatReactionContract.Gravity * dt;
                    if (Height <= 0f && VerticalSpeed <= 0f)
                    {
                        Height = 0f;
                        VerticalSpeed = 0f;
                        if (OtgConsumed) BeginTimed(CombatReactionPhase.GETUP,
                            CombatReactionContract.GetupSeconds);
                        else BeginTimed(CombatReactionPhase.DOWN,
                            CombatReactionContract.DownSeconds);
                    }
                    else
                        Reaction = VerticalSpeed > 0f
                            ? CombatReactionPhase.AIR_RISE : CombatReactionPhase.AIR_FALL;
                    break;
                case CombatReactionPhase.DOWN:
                    _reactionTime -= dt;
                    if (_reactionTime <= 0f)
                        BeginTimed(CombatReactionPhase.GETUP,
                            CombatReactionContract.GetupSeconds);
                    break;
                case CombatReactionPhase.GETUP:
                    _reactionTime -= dt;
                    if (_reactionTime <= 0f)
                        BeginTimed(CombatReactionPhase.INVUL,
                            CombatReactionContract.InvulSeconds);
                    break;
                case CombatReactionPhase.INVUL:
                    _reactionTime -= dt;
                    if (_reactionTime <= 0f)
                    {
                        _reactionTime = 0f;
                        OtgConsumed = false;
                        Reaction = CombatReactionPhase.READY;
                    }
                    break;
            }
        }

        private void BeginTimed(CombatReactionPhase phase, float seconds)
        {
            Reaction = phase;
            _reactionTime = seconds;
        }
    }

    public sealed class Combat004Core
    {
        public const float FixedStep = 1f / 60f;
        public const float Range = 3.0f;
        public const float LaneTolerance = 0.65f;
        public readonly ThreeHitReferenceClock Clock = new ThreeHitReferenceClock();
        public readonly Combat004Enemy[] Enemies =
        {
            new Combat004Enemy(101, -1.95f, -0.12f),
            new Combat004Enemy(102, -1.55f, +0.12f)
        };

        private readonly HashSet<string> _alreadyHit = new HashSet<string>();
        private readonly List<CombatHitEvent> _events = new List<CombatHitEvent>();
        private int _lastStage;
        private int _attackInstance;
        private long _tickIndex;
        public float PlayerX { get; private set; } = -4.4f;
        public float PlayerLane { get; private set; }
        public bool FacingRight { get; private set; } = true;
        public float AttackerFreeze { get; private set; }
        public int TotalContacts { get; private set; }
        public string LastEvent { get; private set; } = "Press J / ATTACK";
        public int AttackInstance => _attackInstance;
        public long TickIndex => _tickIndex;

        // Defensive copy, current Tick only. Read and play effects once after Tick.
        // Neither caller edits nor repeated enumeration can mutate the core's queue.
        public IReadOnlyList<CombatHitEvent> HitEvents => _events.ToArray();
        public IReadOnlyList<CombatHitEvent> DrainHitEvents()
        {
            CombatHitEvent[] result = _events.ToArray();
            _events.Clear();
            return result;
        }

        public void Reset()
        {
            Clock.Reset();
            PlayerX = -4.4f;
            PlayerLane = 0f;
            FacingRight = true;
            AttackerFreeze = 0f;
            _lastStage = 0;
            _attackInstance = 0;
            _tickIndex = 0;
            _alreadyHit.Clear();
            _events.Clear();
            TotalContacts = 0;
            LastEvent = "RESET";
            Enemies[0] = new Combat004Enemy(101, -1.95f, -0.12f);
            Enemies[1] = new Combat004Enemy(102, -1.55f, +0.12f);
        }

        public void Move(float axis, float dt)
        {
            if (AttackerFreeze > 0f) return;
            if (axis > 0.01f) FacingRight = true;
            else if (axis < -0.01f) FacingRight = false;
            PlayerX = Math.Max(-6.4f, Math.Min(4.4f, PlayerX + axis * dt * 3.2f));
        }

        public void PressAttack()
        {
            int previous = Clock.Stage;
            Clock.PressAttack();
            if (Clock.Stage != previous) _events.Clear(); // stage boundary: no stale VFX
            ObserveStage();
        }

        private void ObserveStage()
        {
            if (Clock.Stage == _lastStage) return;
            _lastStage = Clock.Stage;
            if (_lastStage != 0)
            {
                ++_attackInstance;
                _alreadyHit.Clear();
                LastEvent = "ATTACK " + _lastStage + " #" + _attackInstance;
            }
        }

        // COMBAT-006 invariant: damage sampling only at <=1/60s finite steps.
        // Hitstop rounds UP to whole ticks; input-buffer time does NOT expire
        // on an attacker frozen tick. This is an experimental game contract.
        public void Tick(float dt)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt < 0f ||
                dt > FixedStep + 0.000001f)
                throw new ArgumentOutOfRangeException(nameof(dt),
                    "Combat004Core requires <= 1/60s finite simulation increments.");
            _tickIndex++;
            _events.Clear(); // per-frame snapshot only; no old-frame replays.
            for (int i = 0; i < Enemies.Length; i++) Enemies[i].Tick(dt);
            if (AttackerFreeze > 0f)
            {
                AttackerFreeze = Math.Max(0f, AttackerFreeze - dt);
                return;
            }

            Clock.Step(dt);
            ObserveStage();
            if (Clock.Stage == 0 || !Clock.IsHitWindow) return;

            for (int i = 0; i < Enemies.Length; i++)
            {
                Combat004Enemy e = Enemies[i];
                float dx = (e.X - PlayerX) * (FacingRight ? 1f : -1f);
                if (dx < -0.10f || dx > Range ||
                    Math.Abs(e.Lane - PlayerLane) > LaneTolerance ||
                    !e.CanReceive(Clock.Stage)) continue;

                string key = _attackInstance + ":" + e.Id + ":0";
                if (_alreadyHit.Contains(key)) continue;

                bool isOtg = e.Reaction == CombatReactionPhase.DOWN;
                int damage = 10 * Clock.Stage;
                int direction = FacingRight ? 1 : -1;
                float freeze = Clock.Current.ContactFreeze;
                // Snapshot contact from the actual successful damage path.
                float contactX = e.X;
                float contactLane = e.Lane;
                float contactHeight = e.Height;
                if (!e.ReceiveHit(Clock.Stage, damage, direction, freeze, isOtg))
                    continue;
                _alreadyHit.Add(key);
                AttackerFreeze = Math.Max(AttackerFreeze, freeze);
                ++TotalContacts;
                LastEvent = "HIT target " + e.Id + " stage " + Clock.Stage;
                _events.Add(new CombatHitEvent(_tickIndex, _attackInstance, Clock.Stage,
                    e.Id, damage, contactX, contactLane, contactHeight, freeze,
                    direction, isOtg ? CombatReactionContract.OtgBounceSpeed :
                        (Clock.Stage == 3 ? CombatReactionContract.LaunchSpeed : 0f),
                    isOtg));
            }
        }
    }
}
