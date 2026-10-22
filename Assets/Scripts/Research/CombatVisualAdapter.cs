// VISUAL-INTEGRATION-001: pure C# read-only bridge from authoritative HitEvent
// to original sprite atlas frame keys and per-target VFX requests.
// No UnityEngine dependency, HP mutation, physics, hitstop or time authority.
using System;
using System.Collections.Generic;

namespace DnfUnityMobile.Research
{
    public readonly struct VisualHitCue
    {
        public int TargetIndex { get; }
        public int TargetId { get; }
        public string EffectKey { get; }
        public bool IsHeavy { get; }
        public bool IsOtg { get; }
        public float DurationSeconds { get; }
        public float ContactX { get; }
        public float ContactLane { get; }
        public float ContactHeight { get; }
        public int PushDirection { get; }

        public VisualHitCue(int targetIndex, CombatHitEvent hit,
            string effectKey, bool heavy, float durationSeconds)
        {
            TargetIndex = targetIndex;
            TargetId = hit.TargetId;
            EffectKey = effectKey;
            IsHeavy = heavy;
            IsOtg = hit.IsOtg;
            DurationSeconds = durationSeconds;
            ContactX = hit.ContactX;
            ContactLane = hit.ContactLane;
            ContactHeight = hit.ContactHeight;
            PushDirection = hit.PushDirection;
        }
    }

    public static class CombatSpriteKeys
    {
        // The three moves keep the ORIGINAL mage atlas/ReferenceAttack frame keys.
        public static string PlayerAttackFrame(int stage, string frameKey)
        {
            return stage >= 1 && stage <= 3 ? frameKey : "idle0";
        }
    }

    public sealed class CombatVisualAdapter
    {
        private readonly HashSet<string> _consumed = new HashSet<string>();
        private long _currentTick = -1;

        public void Reset()
        {
            _currentTick = -1;
            _consumed.Clear();
        }

        // Input must come from one successful Combat004Core.DrainHitEvents.
        // Unknown target IDs are discarded rather than borrowing an arbitrary VFX.
        public bool TryRoute(CombatHitEvent hit, Combat004Enemy[] targets,
            out VisualHitCue cue)
        {
            cue = default(VisualHitCue);
            if (targets == null || hit.Tick < _currentTick ||
                hit.Stage < 1 || hit.Stage > 3 || hit.Damage <= 0 ||
                hit.AttackInstance < 1) return false;
            int index = -1;
            for (int i = 0; i < targets.Length; ++i)
                if (targets[i] != null && targets[i].Id == hit.TargetId)
                {
                    if (index >= 0) return false; // duplicate logical ID is ambiguous
                    index = i;
                }
            if (index < 0) return false;

            if (hit.Tick != _currentTick)
            {
                _consumed.Clear(); // bound memory to a single Tick of events
                _currentTick = hit.Tick;
            }
            string key = hit.AttackInstance + ":" + hit.Stage + ":" + hit.TargetId;
            if (!_consumed.Add(key)) return false;

            bool heavy = hit.Stage == 3 || hit.IsOtg;
            string effectKey = hit.IsOtg ? "sweep" :
                hit.Stage == 3 ? "upper" :
                hit.Stage == 2 ? "sweep" : "thrust";
            cue = new VisualHitCue(index, hit, effectKey, heavy,
                heavy ? .24f : .15f);
            return true;
        }

        // Pure atlas selector. Freeze does not override the visible phase;
        // the Presenter separately holds poseTime while victim-local Freeze>0.
        public static string ChooseEnemyFrame(Combat004Enemy enemy, float poseTime)
        {
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));
            if (enemy.Hp <= 0) return "ko0";
            switch (enemy.Reaction)
            {
                case CombatReactionPhase.KO: return "ko0";
                case CombatReactionPhase.AIR_RISE: return "air0";
                case CombatReactionPhase.AIR_FALL: return "air1";
                case CombatReactionPhase.DOWN:
                    return ((int)(Math.Max(0f, poseTime) / .20f) % 2) == 0
                        ? "down0" : "down1";
                case CombatReactionPhase.GETUP:
                    // poseTime is presenter-local time since this phase began.
                    // The authoritative GETUP lifetime remains unchanged (.16s).
                    return poseTime < CombatReactionContract.GetupSeconds * .5f
                        ? "getup0" : "getup1";
                case CombatReactionPhase.INVUL:
                    return "invul0"; // distinct raised-shield pose AND blue tint
                case CombatReactionPhase.HIT:
                    return "hurt" + ((int)(Math.Max(0f, poseTime) / .18f) % 2);
                default:
                    return "idle" + ((int)(Math.Max(0f, poseTime) / .32f) % 3);
            }
        }
    }
}
