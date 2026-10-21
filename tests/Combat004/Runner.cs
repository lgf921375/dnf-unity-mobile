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

    public static int Main()
    {
        Check("one stage hits two independent target IDs once", AttackHitsTwoTargets);
        Check("attacker and BOTH victim clocks freeze and resume", BothHitstopFreezeAndResume);
        Check("three attack stages damage both targets without fourth", ThreeStageMultiTargetCombo);
        Check("facing direction filters rear targets", FacingFiltersRearTargets);
        Check("reset restores both enemies and attack clock", ResetRestoresScene);
        Check("NaN infinity and negative delta rejected", InvalidDeltaRejected);
        Console.WriteLine("COMBAT004: " + _pass + " pass, " + _fail + " fail");
        return _fail == 0 ? 0 : 1;
    }
}
