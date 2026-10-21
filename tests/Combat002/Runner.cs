// COMBAT-002 stand-alone .NET test harness (not a Unity Editor/Android test).
// Fake UnityEngine types are test-only and this file lives OUTSIDE Unity's Assets folder.
using System;
using DnfUnityMobile.Research;
using UnityEngine;

public static class Runner
{
    private static int passes, failures;
    static void Require(bool ok, string what) { if (!ok) throw new Exception(what); }
    static void Close(float actual, float expected)
    { Require(Math.Abs(actual - expected) < .0001f, "expected " + expected + " got " + actual); }
    static void Test(string title, Action a)
    {
        try { a(); ++passes; Console.WriteLine("PASS " + title); }
        catch (Exception e) { ++failures; Console.WriteLine("FAIL " + title + ": " + e); }
    }
    static void Reset()
    {
        var c = new ThreeHitReferenceClock();
        Require(c.Stage == 0 && c.FrameKey == "idle", "idle");
        c.PressAttack(); Require(c.Stage == 1 && c.FrameKey == "a1_1", "start");
        c.Step(.33f); Require(c.Stage == 0 && c.FrameKey == "idle", "timeout");
        c.PressAttack(); Require(c.Stage == 1, "restart");
        c.Reset(); Require(c.Stage == 0, "explicit reset");
    }
    static void Frames()
    {
        var c = new ThreeHitReferenceClock();
        c.PressAttack();
        c.Step(.05f); Require(c.FrameKey == "a1_2", "a1_2");
        c.Step(.07f); Require(c.FrameKey == "a1_3", "a1_3");
        c.PressAttack(); Require(c.Stage == 2 && c.FrameKey == "a2_1", "a2_1");
        c.Step(.05f); Require(c.FrameKey == "a2_2", "a2_2");
        c.Step(.09f); Require(c.FrameKey == "a2_3", "a2_3");
        c.PressAttack(); Require(c.Stage == 3 && c.FrameKey == "a3_1", "a3_1");
        c.Step(.12f); Require(c.FrameKey == "a3_2", "a3_2");
    }
    static void BufferAt60Hz()
    {
        var c = new ThreeHitReferenceClock();
        c.PressAttack(); c.PressAttack();
        for (int i = 0; i < 7; i++) c.Step(ThreeHitReferenceClock.StepSeconds);
        Require(c.Stage == 1, "chain must wait until 0.12s");
        c.Step(ThreeHitReferenceClock.StepSeconds);
        Require(c.Stage == 2, "buffered 1->2");
        c.PressAttack();
        for (int i = 0; i < 8; i++) c.Step(ThreeHitReferenceClock.StepSeconds);
        Require(c.Stage == 3, "buffered 2->3");
        c.PressAttack(); Require(c.Stage == 3, "no fourth attack");
        c.Step(.47f); Require(c.Stage == 0, "third attack ends");
    }
    static void Contacts()
    {
        for (int stage = 1; stage <= 3; stage++)
        {
            var c = new ThreeHitReferenceClock();
            c.PressAttack();
            for (int i = 1; i < stage; i++) { c.Step(.13f); c.PressAttack(); }
            Require(c.Stage == stage, "stage " + stage);
            float freeze;
            Require(!c.TryRegisterContact(out freeze), "early contact");
            var a = c.Current;
            c.Step(a.HitBegin + .001f);
            Require(c.IsHitWindow && c.TryRegisterContact(out freeze), "valid contact");
            Close(freeze, a.ContactFreeze);
            Require(!c.TryRegisterContact(out freeze), "same stage double hit");
            c.Step(a.HitEnd - a.HitBegin + .001f);
            Require(!c.IsHitWindow && !c.TryRegisterContact(out freeze), "end exclusive");
        }
    }
    static void NegativeStep()
    {
        var c = new ThreeHitReferenceClock(); c.PressAttack();
        bool caught = false;
        try { c.Step(-.01f); } catch (ArgumentOutOfRangeException) { caught = true; }
        Require(caught, "negative delta accepted");
    }
    static void Alignment()
    {
        // Only factual dimensions and anchor coordinates; no copyrighted image data.
        float[,] samples = {
            {150,195,58.7f,195.3f}, {159,185,119.8f,185.2f},
            {163,186,114.2f,186.2f}, {162,185,119.8f,185.2f},
            {169,184,132.1f,184.5f}, {169,195,130.3f,195.3f},
            {155,203,22.4f,202.6f}, {160,168,116f,168.4f}
        };
        const float pixelsPerUnit = 180f;
        for (int i = 0; i < samples.GetLength(0); i++)
        {
            float w = samples[i,0], h = samples[i,1], ax = samples[i,2], ay = samples[i,3];
            foreach (bool flip in new bool[] { false, true })
            {
                var renderer = new SpriteRenderer { flipX = flip };
                var sprite = new Sprite();
                ReferenceSpriteAlignment.ApplyCenteredSprite(renderer, sprite, w, h, ax, ay, pixelsPerUnit);
                Require(object.ReferenceEquals(renderer.sprite, sprite), "sprite assigned");
                float anchorX = (flip ? w * .5f - ax : ax - w * .5f) / pixelsPerUnit;
                float anchorY = (h * .5f - ay) / pixelsPerUnit;
                Close(renderer.transform.localPosition.x + anchorX, 0f);
                Close(renderer.transform.localPosition.y + anchorY, 0f);
            }
        }
    }

    // COMBAT-005: RED/GREEN tests added before code fixes. Tests use actual .NET C#.
    static void CrossStepBufferedChain()
    {
        foreach (float dt in new[] { 0.12f, 0.16f, 0.20f })
        {
            var c = new ThreeHitReferenceClock();
            c.PressAttack(); c.PressAttack();
            c.Step(dt);
            Require(c.Stage == 2, "buffered input missed chain at dt=" + dt);
            Close(c.Elapsed, dt - 0.12f); // time beyond chain event must carry into attack 2
        }
    }

    static void PartitionEquivalence()
    {
        var single = new ThreeHitReferenceClock();
        var ticks = new ThreeHitReferenceClock();
        single.PressAttack(); single.PressAttack();
        ticks.PressAttack(); ticks.PressAttack();
        single.Step(0.20f);
        for (int i = 0; i < 12; i++) ticks.Step(ThreeHitReferenceClock.StepSeconds);
        Require(single.Stage == 2 && ticks.Stage == 2, "single/ticks stage divergence");
        Require(Math.Abs(single.Elapsed - ticks.Elapsed) < .001f,
            "time lost at chain transition: one=" + single.Elapsed + " 12ticks=" + ticks.Elapsed);
    }

    static void IntervalHitWindow()
    {
        var c = new ThreeHitReferenceClock();
        c.PressAttack();
        c.Step(0.12f); // [0,.12] completely crosses [0.06,.11)
        Require(!c.IsHitWindow, "instantaneous hit window should be closed");
        float freeze;
        Require(c.TryRegisterContact(out freeze), "skipped hit window not reported in Step interval");
        Close(freeze, 0.055f);
        Require(!c.TryRegisterContact(out freeze), "duplicate contact after interval traversal");

        var exp = new ThreeHitReferenceClock();
        exp.PressAttack(); exp.Step(0.12f); exp.Step(0.02f);
        Require(!exp.TryRegisterContact(out freeze),
            "old skipped interval must not remain a contact candidate indefinitely");
    }

    static void RejectNonFiniteTime()
    {
        foreach (float dt in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            var c = new ThreeHitReferenceClock(); c.PressAttack();
            bool caught = false;
            try { c.Step(dt); } catch (ArgumentOutOfRangeException) { caught = true; }
            Require(caught, "non-finite time accepted " + dt);
            Require(c.Stage == 1 && !float.IsNaN(c.Elapsed) && !float.IsInfinity(c.Elapsed)
                    && c.Elapsed == 0f, "invalid delta corrupted clock");
        }
    }

    static void ImmutableReferenceArrays()
    {
        var c1 = new ThreeHitReferenceClock();
        var c2 = new ThreeHitReferenceClock();
        c1.PressAttack(); c2.PressAttack();
        c1.Current.FrameStarts[1] = 99f;
        c1.Current.FrameKeys[1] = "polluted";
        Require(c1.Current.FrameStarts[1] == .05f && c2.Current.FrameStarts[1] == .05f,
            "shared FrameStarts mutated");
        Require(c1.Current.FrameKeys[1] == "a1_2" && c2.Current.FrameKeys[1] == "a1_2",
            "shared FrameKeys mutated");
        c1.Step(.05f); c2.Step(.05f);
        Require(c1.FrameKey == "a1_2" && c2.FrameKey == "a1_2", "frame changed externally");

        var keys = new[] { "x", "y" }; var times = new[] { 0f, .1f };
        var spec = new ReferenceAttack(1f, .2f, .3f, .4f, .01f, keys, times);
        keys[1] = "bad"; times[1] = 99f;
        Require(spec.FrameKeys[1] == "y" && spec.FrameStarts[1] == .1f,
            "constructor kept caller-owned arrays");
    }

    static void PreserveRendererDepth()
    {
        var sr = new SpriteRenderer();
        sr.transform.localPosition = new Vector3(1f, 2f, 7.25f);
        ReferenceSpriteAlignment.ApplyCenteredSprite(sr, new Sprite(), 150f, 195f, 58.7f, 195.3f, 180f);
        Close(sr.transform.localPosition.z, 7.25f);
        sr.flipX = true;
        ReferenceSpriteAlignment.ApplyCenteredSprite(sr, new Sprite(), 150f, 195f, 58.7f, 195.3f, 180f);
        Close(sr.transform.localPosition.z, 7.25f);
    }

    public static int Main()
    {
        Test("COMBAT-005 crossed-buffer 0.12/0.16/0.20", CrossStepBufferedChain);
        Test("COMBAT-005 dt partition equivalence", PartitionEquivalence);
        Test("COMBAT-005 interval hit-window traversal", IntervalHitWindow);
        Test("COMBAT-005 nonfinite dt guarded", RejectNonFiniteTime);
        Test("COMBAT-005 public frame arrays isolated", ImmutableReferenceArrays);
        Test("COMBAT-005 SpriteRenderer Z preserved", PreserveRendererDepth);
        Test("idle/reset", Reset);
        Test("frame change points", Frames);
        Test("input buffer at 60Hz", BufferAt60Hz);
        Test("one eligible contact per stage", Contacts);
        Test("negative step rejected", NegativeStep);
        Test("eight anchor records x both facings", Alignment);
        Console.WriteLine("COMBAT-002: " + passes + " pass, " + failures + " fail");
        return failures == 0 ? 0 : 1;
    }
}

// Minimal UnityEngine stand-ins. Real Unity compilation must be tested separately.
namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float a, float b) { x = a; y = b; }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float a, float b, float c) { x = a; y = b; z = c; }
    }
    public sealed class Transform { public Vector3 localPosition; }
    public sealed class Sprite { }
    public sealed class SpriteRenderer
    {
        public bool flipX;
        public Sprite sprite;
        public readonly Transform transform = new Transform();
    }
}
