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


    // HIT-REACTION-001: executable RED tests; reflection allows compilation
    // against the pre-feature core and makes missing contracts fail at runtime.
    private static object Get(object obj, string property)
    {
        var p = obj.GetType().GetProperty(property);
        Require(p != null, "missing contract " + obj.GetType().Name + "." + property);
        return p.GetValue(obj);
    }
    private static int Number(object obj, string property) => Convert.ToInt32(Get(obj, property));
    private static string Reaction(Combat004Enemy e) => Get(e, "Reaction").ToString();
    private static System.Collections.Generic.List<object> Events(Combat004Core c)
    {
        var result = new System.Collections.Generic.List<object>();
        foreach (object e in (System.Collections.IEnumerable)Get(c, "HitEvents"))
            result.Add(e);
        return result;
    }
    private static void LaunchBoth(Combat004Core c)
    {
        c.PressAttack();
        Until(c, () => c.TotalContacts == 2, 25);
        c.PressAttack();
        Until(c, () => c.Clock.Stage == 2, 50);
        c.PressAttack();
        Until(c, () => c.Clock.Stage == 3, 60);
        Until(c, () => c.TotalContacts == 6, 100);
    }
    private static void PerTargetHitEvents()
    {
        var c = new Combat004Core();
        c.PressAttack();
        Until(c, () => c.TotalContacts == 2, 20);
        var events = Events(c);
        Require(events.Count == 2, "same-frame two event snapshot");
        Require(Number(events[0],"TargetId") != Number(events[1],"TargetId"), "unique target ids");
        foreach (var evt in events)
        {
            Require(Number(evt,"AttackInstance") == 1 && Number(evt,"Stage") == 1, "event attack key");
            Require(Number(evt,"Damage") == 10, "event damage must match HP transaction");
            Require((float)Get(evt, "RequestedFreeze") > 0f, "freeze event payload");
        }
        Require(c.Enemies[0].Hp == 90 && c.Enemies[1].Hp == 90, "HP exactly once per target");
        c.Tick(Combat004Core.FixedStep);
        Require(Events(c).Count == 0, "no stale events on next frozen tick");
    }
    private static void FullReactionLifecycle()
    {
        var c = new Combat004Core();
        LaunchBoth(c);
        var states = new System.Collections.Generic.List<string>();
        string last = "";
        for(int i=0;i<180;i++)
        {
            string state = Reaction(c.Enemies[0]);
            if(state != last) { states.Add(state); last=state; }
            if(state == "READY") break;
            c.Tick(Combat004Core.FixedStep);
        }
        string joined = string.Join(",",states);
        foreach(string state in new[]{"AIR_RISE","AIR_FALL","DOWN","GETUP","INVUL","READY"})
            Require(states.Contains(state), "missing lifecycle "+state+" in "+joined);
        Require(c.Enemies[0].Height == 0f, "foot height on ground");
    }
    private static void OnceOnlyOtgWithProtection()
    {
        var c = new Combat004Core();
        LaunchBoth(c);
        c.Enemies[1].Hp = 0; // isolate target 101 from the second victim
        Until(c, () => c.Clock.Stage == 0, 90);
        Until(c, () => Reaction(c.Enemies[0]) == "DOWN", 100);
        c.PressAttack();
        Until(c, () => c.Enemies[0].Hits == 4, 18);
        Require(c.Enemies[0].Hp == 30, "OTG low hit must damage exactly once");
        var events=Events(c);
        Require(events.Count == 1 && (bool)Get(events[0],"IsOtg"), "OTG event flag");
        Require((bool)Get(c.Enemies[0],"OtgConsumed"), "OTG usage tracked");
        c.PressAttack(); // chain to stage 2 while victim is bounced/protected
        for(int i=0;i<35;i++) c.Tick(Combat004Core.FixedStep);
        Require(c.Enemies[0].Hits == 4, "protected victim accepted extra grounded hit");
    }
    private static void FreezePausesReactionClock()
    {
        var c = new Combat004Core();
        LaunchBoth(c);
        string state=Reaction(c.Enemies[0]);
        float height=c.Enemies[0].Height, speed=c.Enemies[0].VerticalSpeed;
        Require(state == "AIR_RISE" && speed > 0f, "launch immediate reaction");
        for(int i=0;i<6;i++) c.Tick(Combat004Core.FixedStep);
        Close(c.Enemies[0].Height,height,"victim motion frozen during hitstop");
        Require(Reaction(c.Enemies[0]) == state, "victim phase must freeze");
        c.Tick(Combat004Core.FixedStep);
        Require(c.Enemies[0].Height > height, "launch resumes after freeze");
    }
    private static void ResetAndKOEvents()
    {
        var c=new Combat004Core();
        c.Enemies[0].Hp=10;
        c.Enemies[1].Hp=0;
        c.PressAttack();
        Until(c,()=>c.TotalContacts==1,20);
        Require(Reaction(c.Enemies[0])=="KO","KO must stop reaction");
        Require(Events(c).Count==1,"KO hit event retained");
        for(int i=0;i<10;i++) c.Tick(Combat004Core.FixedStep);
        Require(Reaction(c.Enemies[0])=="KO","KO remains terminal");
        c.Reset();
        Require(Events(c).Count==0,"reset must clear event snapshot");
        Require(Reaction(c.Enemies[0])=="READY","reset to READY");
    }
    private static void GroundedRangeRequiresOverlap()
    {
        var c=new Combat004Core();
        LaunchBoth(c);
        c.Enemies[1].Hp=0;
        Until(c,()=>c.Clock.Stage==0,90);
        Until(c,()=>Reaction(c.Enemies[0])=="DOWN",100);
        c.Enemies[0].X = c.PlayerX+Combat004Core.Range+0.2f;
        c.PressAttack();
        for(int i=0;i<15;i++) c.Tick(Combat004Core.FixedStep);
        Require(c.Enemies[0].Hits==3,"off-range low hit illegally hit down victim");
    }
    private static void HundredSeedDeterminism()
    {
        for(int seed=0;seed<100;seed++)
        {
            var random=new Random(seed);
            float dx=0.2f+(float)random.NextDouble()*2.7f;
            var a=new Combat004Core(); var b=new Combat004Core();
            foreach(var c in new[]{a,b})
            {
                c.Enemies[0].X=c.PlayerX+dx;
                c.Enemies[1].X=c.PlayerX+dx+.05f;
                c.PressAttack();
                for(int i=0;i<25;i++) c.Tick(Combat004Core.FixedStep);
            }
            Require(a.Enemies[0].Hp==b.Enemies[0].Hp &&
                    a.Enemies[1].Hp==b.Enemies[1].Hp &&
                    a.TotalContacts==b.TotalContacts,
                    "fixed seed diverged seed "+seed);
        }
    }

    public static int Main()
    {
        Check("HIT REACTION per-target immutable event snapshot", PerTargetHitEvents);
        Check("HIT REACTION full launch/landing/getup/protection lifecycle", FullReactionLifecycle);
        Check("HIT REACTION grounded OTG once and subsequent protection", OnceOnlyOtgWithProtection);
        Check("HIT REACTION victim state clock freezes separately", FreezePausesReactionClock);
        Check("HIT REACTION KO terminal and Reset clears events", ResetAndKOEvents);
        Check("HIT REACTION down overlap still obeys forward range", GroundedRangeRequiresOverlap);
        Check("HIT REACTION deterministic fixed seeds 0..99", HundredSeedDeterminism);
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
