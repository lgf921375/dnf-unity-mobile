// Original research prototype; no third-party source code or art is included.
// Source facts (timings only): github.com/luzhipeng728/dawnbreak
// src/content/classes/sword.js and src/content/sprites.js, inspected 2026-10-21.
// Requires Unity integration and tests before declaring behavior correct.
using System;

namespace DnfUnityMobile.Research
{
    public readonly struct ReferenceAttack
    {
        public readonly float Duration;
        public readonly float ChainBegin;
        public readonly float HitBegin;
        public readonly float HitEnd;
        public readonly float ContactFreeze;
        public readonly string[] FrameKeys;
        public readonly float[] FrameStarts;

        public ReferenceAttack(float duration, float chainBegin, float hitBegin,
            float hitEnd, float contactFreeze, string[] frameKeys, float[] frameStarts)
        {
            Duration = duration;
            ChainBegin = chainBegin;
            HitBegin = hitBegin;
            HitEnd = hitEnd;
            ContactFreeze = contactFreeze;
            FrameKeys = frameKeys;
            FrameStarts = frameStarts;
        }
    }

    // Independent state machine for a three-step attack sequence, without rendering.
    // The 0.16 s input buffer is a *local experiment*, not a verified source value.
    public sealed class ThreeHitReferenceClock
    {
        public const float StepSeconds = 1f / 60f;
        public const float ExperimentalInputBufferSeconds = 0.16f;

        private static readonly ReferenceAttack[] Sequence =
        {
            new ReferenceAttack(0.32f, 0.12f, 0.06f, 0.11f, 0.055f,
                new[] { "a1_1", "a1_2", "a1_3" }, new[] { 0f, 0.05f, 0.12f }),
            new ReferenceAttack(0.34f, 0.12f, 0.06f, 0.11f, 0.055f,
                new[] { "a2_1", "a2_2", "a2_3" }, new[] { 0f, 0.05f, 0.14f }),
            new ReferenceAttack(0.46f, float.PositiveInfinity, 0.12f, 0.18f, 0.09f,
                new[] { "a3_1", "a3_2" }, new[] { 0f, 0.12f }),
        };

        private float _elapsed;
        private float _queuedFor;
        private bool _contactConsumed;

        public int Stage { get; private set; } // 0 = idle; 1, 2, 3 = attacks.
        public float Elapsed => _elapsed;
        public bool IsHitWindow => Stage != 0 &&
            _elapsed >= Current.HitBegin && _elapsed < Current.HitEnd;
        public ReferenceAttack Current => Stage == 0
            ? default(ReferenceAttack) : Sequence[Stage - 1];

        public string FrameKey
        {
            get
            {
                if (Stage == 0) return "idle";
                var a = Current;
                int frame = 0;
                for (int i = 1; i < a.FrameStarts.Length; i++)
                    if (_elapsed >= a.FrameStarts[i]) frame = i;
                return a.FrameKeys[frame];
            }
        }

        public void Reset()
        {
            Stage = 0;
            _elapsed = 0f;
            _queuedFor = 0f;
            _contactConsumed = false;
        }

        public void PressAttack()
        {
            if (Stage == 0)
            {
                Begin(1);
                return;
            }
            if (Stage >= 3) return;
            if (_elapsed >= Current.ChainBegin && _elapsed < Current.Duration)
            {
                Begin(Stage + 1);
                return;
            }
            _queuedFor = ExperimentalInputBufferSeconds;
        }

        // Call once per fixed 60 Hz simulation tick (or with a tested fixed step).
        // Freeze/unstick on contact belongs to the future hit-resolution layer.
        public void Step(float deltaSeconds)
        {
            if (deltaSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (Stage == 0) return;

            _elapsed += deltaSeconds;
            if (_queuedFor > 0f) _queuedFor = Math.Max(0f, _queuedFor - deltaSeconds);

            if (Stage < 3 && _queuedFor > 0f &&
                _elapsed >= Current.ChainBegin && _elapsed < Current.Duration)
            {
                Begin(Stage + 1);
                return;
            }
            if (_elapsed >= Current.Duration) Reset();
        }

        // Reports a single eligible contact during the attack's configured hit window.
        // No damage, hitbox overlap, collision, or opponent logic is implemented here.
        public bool TryRegisterContact(out float requestedFreezeSeconds)
        {
            requestedFreezeSeconds = 0f;
            if (!IsHitWindow || _contactConsumed) return false;
            _contactConsumed = true;
            requestedFreezeSeconds = Current.ContactFreeze;
            return true;
        }

        private void Begin(int stage)
        {
            Stage = stage;
            _elapsed = 0f;
            _queuedFor = 0f;
            _contactConsumed = false;
        }
    }
}
