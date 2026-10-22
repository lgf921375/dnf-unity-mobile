using System;
using System.Collections.Generic;
using DnfUnityMobile.Research;
internal static class VisualFix001Runner
{
    static int good,bad;
    static void Require(bool v,string m){if(!v)throw new Exception(m);}
    static void Close(float a,float b,string m){Require(Math.Abs(a-b)<.0002f,m+" got "+a+" expected "+b);}
    static void Test(string name,Action run){try{run();good++;Console.WriteLine("PASS VISUALFIX "+name);}catch(Exception e){bad++;Console.WriteLine("FAIL VISUALFIX "+name+": "+e.Message);}}
    static void LowFpsFirstFrame()
    {
        var f=new CombatImpactLifetime(2);
        f.Begin(0,.15f);
        f.AdvanceRenderFrame(.25f); // simulated 0.25s render stall
        Require(f.Visible(0),"first render opportunity lost after .25s stall");
        Close(f.Opacity(0),1f,"first frame must not decay");
        f.AdvanceRenderFrame(.25f);
        Require(!f.Visible(0),"FX must expire after second render opportunity");
    }
    static void Fps30And60()
    {
        foreach(float step in new[]{1f/30f,1f/60f})
        {
            var f=new CombatImpactLifetime(2);
            f.Begin(0,.15f);
            f.AdvanceRenderFrame(step);
            Close(f.Opacity(0),1f,"newly spawned effect must get full first frame at fps "+step);
            for(int i=0;i<20;i++) f.AdvanceRenderFrame(step);
            Require(!f.Visible(0),"effect never expired at fps "+step);
        }
    }
    static void SameTickTwoTargets()
    {
        var c=new Combat004Core();
        c.PressAttack();
        for(int i=0;i<30 && c.TotalContacts<2;i++)c.Tick(Combat004Core.FixedStep);
        var events=c.DrainHitEvents();
        Require(events.Count==2,"need two authoritative same-tick hits");
        var router=new CombatVisualAdapter();
        var fx=new CombatImpactLifetime(2);
        foreach(var hit in events)
        {
            VisualHitCue cue;
            Require(router.TryRoute(hit,c.Enemies,out cue),"valid target cue");
            fx.Begin(cue.TargetIndex,cue.DurationSeconds);
        }
        fx.AdvanceRenderFrame(.25f);
        Require(fx.Visible(0)&&fx.Visible(1),"same tick impact 101/102 lost before first render");
        fx.AdvanceRenderFrame(.25f);
        Require(!fx.Visible(0)&&!fx.Visible(1),"effects persist forever");
        Require(c.Enemies[0].Hp==90&&c.Enemies[1].Hp==90,"visual lifetime caused damage");
    }
    static void RepeatedFixedTicksBeforeDraw()
    {
        var f=new CombatImpactLifetime(2);
        f.Begin(0,.15f);
        // 12+ FixedUpdate logic steps can happen before a single Update/Render frame.
        // No AdvanceRenderFrame call -> effects must remain armed independently.
        for(int i=0;i<12;i++) Require(f.Visible(0),"FixedUpdate consumed render lifetime");
        f.Begin(1,.24f);
        f.AdvanceRenderFrame(.35f);
        Require(f.Visible(0)&&f.Visible(1),"stalled render must still see both");
        f.AdvanceRenderFrame(.05f);
        Require(f.Visible(0)&&f.Visible(1),"incorrect second-frame lifetime");
        for(int i=0;i<10;i++)f.AdvanceRenderFrame(.05f);
        Require(!f.Visible(0)&&!f.Visible(1),"stale persistent effects");
    }
    static void RefreshSameTargetAndReset()
    {
        var f=new CombatImpactLifetime(2);
        f.Begin(0,.15f);
        f.AdvanceRenderFrame(1f/60f);
        f.AdvanceRenderFrame(.1f);
        f.Begin(0,.24f); // legitimate second event overwrites first renderer
        f.AdvanceRenderFrame(.25f);
        Require(f.Visible(0),"refreshed FX must also have a first render chance");
        Close(f.Opacity(0),1f,"refreshed target opacity");
        f.Reset();
        Require(!f.Visible(0)&&!f.Visible(1),"reset must remove all effects immediately");
        f.AdvanceRenderFrame(.25f);
        Require(!f.Visible(0),"Reset unexpectedly rearms FX");
    }
    static void Validation()
    {
        var f=new CombatImpactLifetime(2); bool badDelta=false,badSlot=false;
        try{f.AdvanceRenderFrame(float.NaN);}catch(ArgumentOutOfRangeException){badDelta=true;}
        try{f.Begin(5,.15f);}catch(ArgumentOutOfRangeException){badSlot=true;}
        Require(badDelta&&badSlot,"invalid input not rejected");
    }
    static int Main()
    {
        Test("0.25s stall cannot hide impact before first rendering chance",LowFpsFirstFrame);
        Test("30/60 FPS first frame and bounded expiry",Fps30And60);
        Test("same-tick target101 and target102 with real HitEvents",SameTickTwoTargets);
        Test("twelve FixedUpdates then one render frame",RepeatedFixedTicksBeforeDraw);
        Test("same target refresh and Reset removes FX",RefreshSameTargetAndReset);
        Test("invalid dt and target index guarded",Validation);
        Console.WriteLine("VISUAL-FIX-001 "+good+" pass, "+bad+" fail");
        return bad==0?0:1;
    }
}
