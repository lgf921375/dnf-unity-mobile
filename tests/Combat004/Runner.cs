// Original .NET 8 test suite. Compiles actual combat core and clock; Unity scene is separate.
using System;
using DnfUnityMobile.Research;

public static class Combat004Runner
{
    private static int _pass, _fail;
    private static void Require(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
    private static void Close(float a, float b, string message)
    {
        Require(Math.Abs(a - b) <= .0005f, message + ": " + a + " != " + b);
    }
    private static void Check(string title, Action body)
    {
        try { body(); _pass++; Console.WriteLine("PASS COMBAT004 " + title); }
        catch (Exception e) { _fail++; Console.WriteLine("FAIL COMBAT004 " + title + ": " + e); }
    }
    private static void Until(Combat004Core c, Func<bool> predicate, int max = 240)
    {
        for (int i = 0; i < max && !predicate(); i++) c.Tick(Combat004Core.FixedStep);
        Require(predicate(), "condition not reached by tick " + max);
    }

    private static void AttackHitsTwoTargets()
    {
        var c = new Combat004Core();
        c.PressAttack();
        Until(c, () => c.TotalContacts == 2, 20);
        Require(c.Enemies[0].Hits == 1 && c.Enemies[1].Hits == 1,
            "one attack must hit BOTH targets once");
        Require(c.Enemies[0].Hp == 90 && c.Enemies[1].Hp == 90, "damage");
        for (int i = 0; i < 24; i++) c.Tick(Combat004Core.FixedStep);
        Require(c.TotalContacts == 2, "same attack may not hit same target again");
    }

    private static void BothHitstopFreezeAndResume()
    {
        var c = new Combat004Core();
        c.PressAttack();
        Until(c, () => c.TotalContacts == 2, 20);
        float t = c.Clock.Elapsed, x = c.Enemies[0].X;
        float stun = c.Enemies[0].Stun;
        Require(c.AttackerFreeze > 0f && c.Enemies[0].Freeze > 0f &&
                c.Enemies[1].Freeze > 0f, "dual freeze not scheduled");
        c.PressAttack(); // queue during freeze; expiry is LOCAL combat time
        float q = c.Clock.QueuedFor;
        c.Move(1f, Combat004Core.FixedStep); // no movement during freeze
        c.Tick(Combat004Core.FixedStep);
        Close(c.Clock.Elapsed, t, "attacker elapsed must freeze");
        Close(c.Enemies[0].X, x, "victim translation must freeze");
        Close(c.Enemies[0].Stun, stun, "victim hurt time must freeze");
        Close(c.Clock.QueuedFor, q, "input expiry must freeze");
        Until(c, () => c.AttackerFreeze == 0f, 20);
        Until(c, () => c.Clock.Stage == 2, 40);
        Require(c.Enemies[0].Hits >= 1, "hit state lost during resume");
    }

    private static void ThreeStageMultiTargetCombo()
    {
        var c = new Combat004Core();
        c.PressAttack();
        Until(c, () => c.TotalContacts == 2, 25);
        c.PressAttack();
        Until(c, () => c.Clock.Stage == 2, 50);
        c.PressAttack();
        Until(c, () => c.Clock.Stage == 3, 60);
        c.PressAttack(); // stage 3 must never chain stage 4
        Require(c.Clock.Stage == 3, "fourth attack erroneously started");
        Until(c, () => c.TotalContacts == 6, 80);
        Require(c.Enemies[0].Hits == 3 && c.Enemies[1].Hits == 3,
                "expected three hits per target");
        Require(c.Enemies[0].Hp == 40 && c.Enemies[1].Hp == 40,
                "stages must deal 10+20+30 damage per target");
        Require(c.Enemies[0].VerticalSpeed > 0f && c.Enemies[1].VerticalSpeed > 0f,
                "final hit launch missing");
        Until(c, () => c.Clock.Stage == 0, 100);
    }

    private static void FacingFiltersRearTargets()
    {
        var c = new Combat004Core();
        c.Move(-1f, Combat004Core.FixedStep);
        Require(!c.FacingRight, "left facing");
        c.PressAttack();
        for (int i = 0; i < 30; i++) c.Tick(Combat004Core.FixedStep);
        Require(c.TotalContacts == 0, "rear targets should not hit");
    }

    private static void ResetRestoresScene()
    {
        var c = new Combat004Core();
        c.PressAttack();
        Until(c, () => c.TotalContacts == 2, 20);
        c.Reset();
        Require(c.Clock.Stage == 0 && c.TotalContacts == 0 && c.AttackerFreeze == 0f,
            "clock and hitstop not reset");
        Require(c.Enemies[0].Hp == 100 && c.Enemies[1].Hp == 100 &&
                c.Enemies[0].Hits == 0 && c.Enemies[1].Hits == 0, "target reset");
    }

    private static void InvalidDeltaRejected()
    {
        var c = new Combat004Core();
        foreach (float dt in new [] { -0.001f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            bool caught = false;
            try { c.Tick(dt); } catch (ArgumentOutOfRangeException) { caught = true; }
            Require(caught, "invalid delta accepted");
        }
        Require(c.Clock.Stage == 0, "failed validation changed state");
    }


    // Probes the 9 GENERATED sprite-frame metadata entries in ResearchCombat004Scene.
    // Tests transform math using stand-in UnityEngine types; NOT actual Unity rendering.
    private static void ProceduralSpriteAnchorMatrix()
    {
        float[,] frames = {
            {78,110,39f,109f}, {82,115,36.5f,114.2f},
            {99,108,54f,108.2f}, {106,112,49f,111.7f},
            {88,114,44f,113.8f}, {109,105,58.5f,105.4f},
            {105,116,48f,115.8f}, {112,123,32.5f,123.2f},
            {116,99,70f,99.2f}
        };
        foreach (float ppu in new [] { 100f, 180f, 256f })
        {
            for (int i = 0; i < frames.GetLength(0); i++)
            {
                float w = frames[i,0], h = frames[i,1],
                    ax = frames[i,2], ay = frames[i,3];
                var sr = new UnityEngine.SpriteRenderer();
                sr.transform.localPosition = new UnityEngine.Vector3(0.6f,-2.1f,0.12f);
                var spr = new UnityEngine.Sprite();
                foreach (bool facingLeft in new [] { false, true, false })
                {
                    // Dynamic flipX toggling must call ApplyCenteredSprite again.
                    sr.flipX = facingLeft;
                    ReferenceSpriteAlignment.ApplyCenteredSprite(sr, spr, w, h, ax, ay, ppu);
                    float footX = sr.transform.localPosition.x +
                        (facingLeft ? (w*.5f-ax) : (ax-w*.5f)) / ppu;
                    float footY = sr.transform.localPosition.y + (h*.5f-ay) / ppu;
                    Close(footX, 0f, "foot X stable");
                    Close(footY, 0f, "foot Y stable");
                    Close(sr.transform.localPosition.z, .12f, "depth retained");
                }
            }
        }
    }


    // COMBAT-006 RED gate: this prototype accepts only 60 Hz (or smaller)
    // ticks; a larger dt would silently discard first-stage hit events.
    private static void RejectOversizeTicksWithoutMutation()
    {
        foreach (float delta in new[] { .12f, .20f })
        {
            var c = new Combat004Core();
            c.PressAttack(); c.PressAttack();
            float before = c.Clock.Elapsed;
            bool rejected = false;
            try { c.Tick(delta); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Require(rejected, "oversized Tick(" + delta + ") was not rejected");
            Close(c.Clock.Elapsed, before, "failed tick cannot advance state");
            Require(c.TotalContacts == 0 && c.Clock.Stage == 1,
                "oversized step caused ghost collision or transition");
        }
        // Supported discrete path should still hit stage1 prior to chain.
        var normal = new Combat004Core();
        normal.PressAttack(); normal.PressAttack();
        for (int i = 0; i < 12; ++i)
            normal.Tick(Combat004Core.FixedStep);
        Require(normal.TotalContacts == 2, "12 legal ticks must resolve first stage");
        Require(normal.Clock.Stage == 2, "12 legal ticks must chain at .12");
    }

    private static void FreezeQuantizationContract()
    {
        // Design decision: requested .055 s freezes four full 60Hz ticks;
        // requested .09 s freezes six full 60Hz ticks, with NO partial-tick carry.
        var c = new Combat004Core();
        c.PressAttack();
        Until(c, () => c.TotalContacts == 2, 20);
        Close(c.AttackerFreeze, .055f, "first freeze requested");
        float actionTime = c.Clock.Elapsed;
        c.PressAttack();
        float queued = c.Clock.QueuedFor;
        for (int i = 1; i <= 4; ++i)
        {
            c.Tick(Combat004Core.FixedStep);
            Close(c.Clock.Elapsed, actionTime, "frozen attacker tick " + i);
            Close(c.Clock.QueuedFor, queued, "input buffer frozen tick " + i);
            if (i < 4) Require(c.AttackerFreeze > 0f, "freeze ended too early");
        }
        Require(c.AttackerFreeze == 0f, "four ticks must consume .055 seconds");
        c.Tick(Combat004Core.FixedStep);
        Require(c.Clock.Elapsed > actionTime, "action must resume next tick");
        Until(c, () => c.Clock.Stage == 2, 40);
        c.PressAttack();
        Until(c, () => c.Clock.Stage == 3, 60);
        Until(c, () => c.TotalContacts == 6, 100);
        Close(c.AttackerFreeze, .09f, "final freeze requested");
        actionTime = c.Clock.Elapsed;
        for (int i = 1; i <= 6; ++i)
        {
            c.Tick(Combat004Core.FixedStep);
            Close(c.Clock.Elapsed, actionTime, "final stage freeze tick " + i);
            if (i < 6) Require(c.AttackerFreeze > 0f, "final freeze ended early");
        }
        Require(c.AttackerFreeze == 0f, "six ticks must consume .09 seconds");
    }

    private static void ImmediateFacingContract()
    {
        // Experimental rule: attack facing remains freely steerable until hit
        // sampling (not official DNF behavior); movement allowed outside hitstop.
        var c = new Combat004Core();
        c.PressAttack();
        c.Move(-1f, Combat004Core.FixedStep);
        Require(!c.FacingRight, "direction did not flip inside windup");
        c.Tick(Combat004Core.FixedStep);
        c.Tick(Combat004Core.FixedStep);
        c.Tick(Combat004Core.FixedStep);
        Require(c.TotalContacts == 0, "backwards windup should not hit targets");
        c.Move(+1f, Combat004Core.FixedStep);
        Require(c.FacingRight, "facing should follow latest input");
        Until(c, () => c.TotalContacts == 2, 10);
        Require(c.Enemies[0].Hits == 1 && c.Enemies[1].Hits == 1,
            "current facing should select forward targets");
    }

    private static void TargetOrderAndBounds()
    {
        // Unique IDs are mandatory for per-target dedup; swapping array order
        // must not change the number of successful contacts.
        var swapped = new Combat004Core();
        Combat004Enemy hold = swapped.Enemies[0];
        swapped.Enemies[0] = swapped.Enemies[1];
        swapped.Enemies[1] = hold;
        swapped.PressAttack();
        Until(swapped, () => swapped.TotalContacts == 2, 20);
        Require(swapped.Enemies[0].Hits == 1 && swapped.Enemies[1].Hits == 1,
            "target order changed unique-ID hit registration");
        foreach (float dx in new[] { -.11f, -.10f, -.05f, 0f, 3f, 3.01f })
        {
            var c = new Combat004Core();
            c.Enemies[0].X = c.PlayerX + dx;
            c.Enemies[1].Hp = 0;
            c.PressAttack();
            for (int i = 0; i < 6; ++i) c.Tick(Combat004Core.FixedStep);
            bool expected = dx >= -.10f && dx <= Combat004Core.Range;
            Require((c.Enemies[0].Hits == 1) == expected,
                "rear tolerance/range contract violated at dx=" + dx);
        }
        foreach (float lane in new[] { -.66f, -.65f, 0f, +.65f, +.66f })
        {
            var c = new Combat004Core();
            c.Enemies[0].X = c.PlayerX + 1f;
            c.Enemies[0] = new Combat004Enemy(101, c.PlayerX + 1f, lane);
            c.Enemies[1].Hp = 0;
            c.PressAttack();
            for (int i=0; i<6; ++i) c.Tick(Combat004Core.FixedStep);
            bool expected = Math.Abs(lane) <= Combat004Core.LaneTolerance;
            Require((c.Enemies[0].Hits == 1) == expected,
                "lane threshold violated at lane=" + lane);
        }
    }

    public static int Main()
    {
        Check("COMBAT006 dt 0.12/0.20 rejected vs legal 60Hz path", RejectOversizeTicksWithoutMutation);
        Check("COMBAT006 55/90ms whole-tick hitstop quantization", FreezeQuantizationContract);
        Check("COMBAT006 freely steered attack-facing contract", ImmediateFacingContract);
        Check("COMBAT006 target order, rear tolerance, range/lane thresholds", TargetOrderAndBounds);
        Check("one stage hits two independent target IDs once", AttackHitsTwoTargets);
        Check("attacker and BOTH victim clocks freeze and resume", BothHitstopFreezeAndResume);
        Check("three attack stages damage both targets without fourth", ThreeStageMultiTargetCombo);
        Check("facing direction filters rear targets", FacingFiltersRearTargets);
        Check("reset restores both enemies and attack clock", ResetRestoresScene);
        Check("NaN infinity and negative delta rejected", InvalidDeltaRejected);
        Check("9 self-drawn frame anchors, 3 PPU, dynamic mirror", ProceduralSpriteAnchorMatrix);
        Console.WriteLine("COMBAT004: " + _pass + " pass, " + _fail + " fail");
        return _fail == 0 ? 0 : 1;
    }
}

// Test-only minimal UnityEngine mathematical stand-ins. NO Unity Editor validation.
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
