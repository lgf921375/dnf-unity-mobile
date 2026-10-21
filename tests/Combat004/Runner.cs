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

    public static int Main()
    {
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
