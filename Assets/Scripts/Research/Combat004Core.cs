// Original standalone single-player combat slice for COMBAT-004.
// No third-party game logic or artwork. Physics is a declared 2.5D frame hit matrix;
// a future Collider2D integration requires separate Unity PlayMode coverage.
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

        public Combat004Enemy(int id, float x, float lane)
        {
            Id = id;
            X = x;
            Lane = lane;
        }

        public string State
        {
            get
            {
                if (Hp <= 0) return "KO";
                if (Freeze > 0f) return "HITSTOP";
                if (Height > 0f) return "AIR";
                if (Stun > 0f) return "HIT";
                return "READY";
            }
        }

        public void Tick(float dt)
        {
            if (Freeze > 0f)
            {
                Freeze = Math.Max(0f, Freeze - dt);
                return; // Stop victim animation, displacement and hurt-state timer.
            }
            Stun = Math.Max(0f, Stun - dt);
            X += Push * dt;
            Push *= Math.Max(0f, 1f - 6f * dt);
            if (Height > 0f || VerticalSpeed > 0f)
            {
                Height += VerticalSpeed * dt;
                VerticalSpeed -= 10f * dt;
                if (Height <= 0f) { Height = 0f; VerticalSpeed = 0f; }
            }
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
        private int _lastStage;
        private int _attackInstance;
        public float PlayerX { get; private set; } = -4.4f;
        public float PlayerLane { get; private set; }
        public bool FacingRight { get; private set; } = true;
        public float AttackerFreeze { get; private set; }
        public int TotalContacts { get; private set; }
        public string LastEvent { get; private set; } = "Press J / ATTACK";
        public int AttackInstance => _attackInstance;

        public void Reset()
        {
            Clock.Reset();
            PlayerX = -4.4f;
            PlayerLane = 0f;
            FacingRight = true;
            AttackerFreeze = 0f;
            _lastStage = 0;
            _attackInstance = 0;
            _alreadyHit.Clear();
            TotalContacts = 0;
            LastEvent = "RESET";
            Enemies[0] = new Combat004Enemy(101, -1.95f, -0.12f);
            Enemies[1] = new Combat004Enemy(102, -1.55f, +0.12f);
        }

        public void Move(float axis, float dt)
        {
            if (AttackerFreeze > 0f) return; // Freeze attacker displacement.
            if (axis > 0.01f) FacingRight = true;
            else if (axis < -0.01f) FacingRight = false;
            PlayerX = Math.Max(-6.4f, Math.Min(4.4f, PlayerX + axis * dt * 3.2f));
        }

        public void PressAttack()
        {
            Clock.PressAttack(); // Buffer lifetime uses combat-local time (frozen during hitstop).
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

        // COMBAT-006 invariant: this *combat core* accepts one 60 Hz step (or less).
        // Callers must issue repeated fixed steps, never forward a large render-frame
        // delta. The clock itself supports larger dt, but this core samples damage
        // only once per Tick and cannot resolve intermediate attack-stage hit events.
        // Do not claim equivalent single-step 0.12/0.20 s damage behavior.
        // Deterministic hitstop intentionally rounds UP to whole 60 Hz ticks:
        // .055 s freezes 4 ticks, .09 s freezes 6 ticks; no fractional carry.
        public void Tick(float dt)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt < 0f ||
                dt > FixedStep + 0.000001f)
                throw new ArgumentOutOfRangeException(nameof(dt),
                    "Combat004Core requires <= 1/60s finite simulation increments.");
            // Victims tick independently; each freeze counter is real and observable.
            for (int i = 0; i < Enemies.Length; i++) Enemies[i].Tick(dt);
            if (AttackerFreeze > 0f)
            {
                AttackerFreeze = Math.Max(0f, AttackerFreeze - dt);
                return; // Do NOT advance attack, animation frame or input expiry.
            }

            Clock.Step(dt);
            ObserveStage();
            if (Clock.Stage == 0 || !Clock.IsHitWindow) return;

            // Our explicit frame hit matrix: forward 3.0 units, 0.65 lane width,
            // 1 hit per (attackInstance,targetId,hitIndex), here hitIndex=0.
            for (int i = 0; i < Enemies.Length; i++)
            {
                Combat004Enemy e = Enemies[i];
                float dx = (e.X - PlayerX) * (FacingRight ? 1f : -1f);
                if (e.Hp <= 0 || dx < -0.10f || dx > Range ||
                    Math.Abs(e.Lane - PlayerLane) > LaneTolerance) continue;

                string key = _attackInstance + ":" + e.Id + ":0";
                if (!_alreadyHit.Add(key)) continue;

                e.Hits++;
                e.Hp = Math.Max(0, e.Hp - 10 * Clock.Stage);
                e.Stun = Math.Max(e.Stun, Clock.Stage == 3 ? 0.42f : 0.22f);
                e.Push = (FacingRight ? 1f : -1f) * (Clock.Stage == 3 ? 2.3f : 0.4f);
                if (Clock.Stage == 3) e.VerticalSpeed = 3.1f;
                float freeze = Clock.Current.ContactFreeze;
                e.Freeze = Math.Max(e.Freeze, freeze);
                AttackerFreeze = Math.Max(AttackerFreeze, freeze);
                ++TotalContacts;
                LastEvent = "HIT target " + e.Id + " stage " + Clock.Stage;
            }
        }
    }
}
