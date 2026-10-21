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
        // Clone on input AND output to prevent mutation of shared static action data.
        private readonly string[] _frameKeys;
        private readonly float[] _frameStarts;
        public string[] FrameKeys => _frameKeys == null ? Array.Empty<string>() : (string[])_frameKeys.Clone();
        public float[] FrameStarts => _frameStarts == null ? Array.Empty<float>() : (float[])_frameStarts.Clone();
        public int FrameCount => _frameKeys == null ? 0 : _frameKeys.Length;
        public string FrameAt(int index) => _frameKeys[index];
        public float FrameStartAt(int index) => _frameStarts[index];

        public ReferenceAttack(float duration, float chainBegin, float hitBegin,
            float hitEnd, float contactFreeze, string[] frameKeys, float[] frameStarts)
        {
            Duration = duration;
            ChainBegin = chainBegin;
            HitBegin = hitBegin;
            HitEnd = hitEnd;
            ContactFreeze = contactFreeze;
            if (frameKeys == null || frameStarts == null || frameKeys.Length != frameStarts.Length)
                throw new ArgumentException("Frame keys and times must have equal non-null length.");
            _frameKeys = (string[])frameKeys.Clone();
            _frameStarts = (float[])frameStarts.Clone();
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
        // Only reports traversal during the most recent Step. This is temporal
        // eligibility, NOT collision detection nor actual hit resolution.
        private bool _crossedHitWindow;

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
                for (int i = 1; i < a.FrameCount; i++)
                    if (_elapsed >= a.FrameStartAt(i)) frame = i;
                return a.FrameAt(frame);
            }
        }

        public void Reset()
        {
            Stage = 0;
            _elapsed = 0f;
            _queuedFor = 0f;
            _contactConsumed = false;
            _crossedHitWindow = false;
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

        // Variable-dt event contract (also supports 60 Hz):
        // A queued attack is valid until the input-buffer expiry instant.
        // If its valid interval reaches ChainBegin, transition exactly at
        // ChainBegin, carrying remaining dt into the next stage.
        // The last Step exposes whether the *current* stage's hit window was
        // traversed, so a long dt cannot hide its whole temporal window.
        // A future combat/physics layer must do swept overlap and per-target dedup:
        // this clock does not infer that a target was actually hit.
        public void Step(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (Stage == 0) return;

            _crossedHitWindow = false;
            float remaining = deltaSeconds;
            int transitions = 0;
            while (Stage != 0 && remaining > 0f)
            {
                // At most two chained transitions before the third attack;
                // guard against future zero-duration configuration errors.
                if (++transitions > 4)
                    throw new InvalidOperationException("Unexpected attack stage loop.");

                ReferenceAttack attack = Current;
                float part = Math.Min(remaining, Math.Max(0f, attack.Duration - _elapsed));
                bool chainNow = false;
                if (Stage < 3 && _queuedFor > 0f)
                {
                    float untilChain = Math.Max(0f, attack.ChainBegin - _elapsed);
                    // Evaluate eligibility at the crossing instant, BEFORE buffer
                    // lifetime is decremented for that part of the interval.
                    if (untilChain <= part && untilChain <= _queuedFor)
                    {
                        part = untilChain;
                        chainNow = true;
                    }
                }

                if (part > 0f)
                {
                    if (_elapsed < attack.HitEnd && _elapsed + part > attack.HitBegin)
                        _crossedHitWindow = true;
                    _elapsed += part;
                    _queuedFor = Math.Max(0f, _queuedFor - part);
                    remaining -= part;
                }

                if (chainNow)
                {
                    Begin(Stage + 1);
                    continue;
                }

                if (_elapsed >= attack.Duration)
                {
                    Reset();
                    break;
                }
                if (part <= 0f) break;
            }
        }

        // Reports a single eligible contact during the attack's configured hit window.
        // No damage, hitbox overlap, collision, or opponent logic is implemented here.
        public bool TryRegisterContact(out float requestedFreezeSeconds)
        {
            requestedFreezeSeconds = 0f;
            if (!(IsHitWindow || _crossedHitWindow) || _contactConsumed) return false;
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
            _crossedHitWindow = false;
        }
    }
}
