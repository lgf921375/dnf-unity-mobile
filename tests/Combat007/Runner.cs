// Real .NET 8 layout/landscape policy test. No UnityEngine stand-ins or Android.
using System;
using DnfUnityMobile.Research;

public static class Combat007LayoutTests
{
    private static int _pass, _fail;
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static void Near(float actual, float expected, string label)
    {
        Assert(Math.Abs(actual - expected) <= 0.004f, label + " got " + actual + " expected " + expected);
    }
    private static void Test(string name, Action body)
    {
        try { body(); _pass++; Console.WriteLine("PASS COMBAT007 " + name); }
        catch (Exception e) { _fail++; Console.WriteLine("FAIL COMBAT007 " + name + ": " + e); }
    }
    private static void Bounds(Combat007Rect r, Combat007Layout layout, string name)
    {
        Assert(r.Width >= 80f && r.Height >= 25f, name + " collapsed");
        Assert(r.X >= -0.001f && r.Y >= -0.001f, name + " negative coordinate");
        Assert(r.X+r.Width <= layout.VirtualWidth + .01f &&
               r.Y+r.Height <= layout.VirtualHeight + .01f,
               name + " crosses virtual safe view");
    }
    private static void Scenario(string title, int w, int h, float x, float y, float sw, float sh,
        bool portrait, float expectedScale)
    {
        var a=Combat007Layout.Calculate(w,h,x,y,sw,sh);
        var b=Combat007Layout.Calculate(w,h,x,y,sw,sh);
        Near(a.Scale, expectedScale, title + " uniform scale");
        Near(a.Scale, b.Scale, title + " deterministic layout");
        Assert(a.VirtualWidth >= 1280f-.01f && a.VirtualHeight >= 720f-.01f, title + " virtual minima");
        Assert(Combat007Layout.NeedsLandscapeRotation(w,h)==portrait, title + " portrait fallback policy");
        foreach (var entry in new[] {
          Tuple.Create("HUD",a.Hud), Tuple.Create("RESET",a.Reset),
          Tuple.Create("LEFT",a.Left), Tuple.Create("RIGHT",a.Right),
          Tuple.Create("ATTACK",a.Attack), Tuple.Create("HINT",a.Hint)})
            Bounds(entry.Item2,a,title+" "+entry.Item1);
        Assert(!a.Left.Overlaps(a.Right) && !a.Left.Overlaps(a.Attack) &&
               !a.Right.Overlaps(a.Attack), title + " gameplay buttons overlap");
        Assert(!a.Reset.Overlaps(a.Attack) && !a.Reset.Overlaps(a.Hud),
            title + " accidental reset overlap");
        Assert(!a.Hud.Overlaps(a.Left) && !a.Hud.Overlaps(a.Attack),
            title + " HUD covers movement/attack");
        foreach (var entry in new[] {
            Tuple.Create("LEFT",a.Left), Tuple.Create("RIGHT",a.Right),
            Tuple.Create("ATTACK",a.Attack), Tuple.Create("RESET",a.Reset) })
        {
            float cx=entry.Item2.X+entry.Item2.Width/2f,cy=entry.Item2.Y+entry.Item2.Height/2f;
            float sx,sy,rx,ry;
            a.VirtualToBottomLeftScreen(cx,cy,out sx,out sy);
            Assert(sx>=a.SafePixelsTopLeft.X && sx<=a.SafePixelsTopLeft.X+a.SafePixelsTopLeft.Width &&
                   h-sy>=a.SafePixelsTopLeft.Y && h-sy<=a.SafePixelsTopLeft.Y+a.SafePixelsTopLeft.Height,
                   title+" "+entry.Item1+" center outside device safe pixels");
            a.BottomLeftScreenToVirtual(sx,sy,out rx,out ry);
            Near(rx,cx,title+" "+entry.Item1+" mapping X");
            Near(ry,cy,title+" "+entry.Item1+" mapping Y");
            Assert(entry.Item2.Contains(rx,ry),title+" "+entry.Item1+" touch misses");
        }
        Console.WriteLine("MATRIX "+title+" virtual="+a.VirtualWidth.ToString("F2")+
            "x"+a.VirtualHeight.ToString("F2")+" scale="+a.Scale.ToString("F4")+
            " safeTop="+a.OriginTopY+" controls=4");
    }
    private static void SafeRectClampAndFallback()
    {
        var a=Combat007Layout.Calculate(1920,1080,-20f,-10f,3000f,2000f);
        Near(a.OriginX,0f,"clamped X");
        Near(a.OriginTopY,0f,"clamped Y");
        Near(a.VirtualWidth,1280f,"clamped width");
        var b=Combat007Layout.Calculate(1920,1080,float.NaN,0f,0f,0f);
        Near(b.Scale,1.5f,"transient safeArea fallback");
        bool threw=false;
        try { Combat007Layout.Calculate(0,0,0,0,0,0); }
        catch (ArgumentOutOfRangeException) { threw=true; }
        Assert(threw,"invalid zero size accepted");
    }
    private static void OrientationDecision()
    {
        Assert(Combat007Layout.ShouldLockLandscapeOnAndroid(true), "Android lock missing");
        Assert(!Combat007Layout.ShouldLockLandscapeOnAndroid(false), "Editor/non-Android should not auto-rotate");
        Assert(Combat007Layout.NeedsLandscapeRotation(1080,2400), "portrait fallback missing");
        Assert(!Combat007Layout.NeedsLandscapeRotation(2400,1080), "landscape should not show portrait hint");
        Assert(!Combat007Layout.NeedsLandscapeRotation(1280,720), "16:9 landscape misclassified");
    }
    private static void SimultaneousTouchAndNoRepeat()
    {
        var l=Combat007Layout.Calculate(2400,1080,120,0,2160,1080);
        float leftX,leftY,attackX,attackY,rightX,rightY,resetX,resetY;
        l.VirtualToBottomLeftScreen(l.Left.X+50,l.Left.Y+55,out leftX,out leftY);
        l.VirtualToBottomLeftScreen(l.Attack.X+90,l.Attack.Y+56,out attackX,out attackY);
        l.VirtualToBottomLeftScreen(l.Right.X+50,l.Right.Y+55,out rightX,out rightY);
        l.VirtualToBottomLeftScreen(l.Reset.X+60,l.Reset.Y+35,out resetX,out resetY);
        var frame=new Combat007PointerFrame();
        l.AddPointer(ref frame,leftX,leftY,true,true);
        l.AddPointer(ref frame,attackX,attackY,true,true);
        Assert(frame.MoveAxis==-1f && frame.AttackDown && !frame.ResetDown,
            "left hold plus independent attack finger");
        frame=new Combat007PointerFrame();
        l.AddPointer(ref frame,leftX,leftY,true,false);
        l.AddPointer(ref frame,attackX,attackY,true,false);
        Assert(frame.MoveAxis==-1f && !frame.AttackDown,
            "held ATTACK must not repeat action");
        frame=new Combat007PointerFrame();
        l.AddPointer(ref frame,rightX,rightY,true,true);
        l.AddPointer(ref frame,resetX,resetY,true,true);
        Assert(frame.MoveAxis==1f && frame.ResetDown && !frame.AttackDown,
            "right hold and separately tapped reset");
        frame=new Combat007PointerFrame();
        l.AddPointer(ref frame,leftX,leftY,false,true);
        Assert(frame.MoveAxis==0f && !frame.AttackDown, "ended touch must not trigger");
    }

    public static int Main()
    {
        Test("1280x720 16:9 full safe touch matrix",()=>Scenario("1280x720",1280,720,0,0,1280,720,false,1f));
        Test("2400x1080 ultrawide with left/right notch safe matrix",
            ()=>Scenario("2400x1080",2400,1080,120,0,2160,1080,false,1.5f));
        Test("1920x1080 16:9 full safe matrix",()=>Scenario("1920x1080",1920,1080,0,0,1920,1080,false,1.5f));
        Test("1080x2400 portrait orientation-transition safe matrix",
            ()=>Scenario("1080x2400",1080,2400,0,80,1080,2240,true,0.84375f));
        Test("safeArea clamp/transient fallback and invalid resolution",SafeRectClampAndFallback);
        Test("Android-only orientation policy and portrait fallback",OrientationDecision);
        Test("two-finger held move and once-only ATTACK/RESET",SimultaneousTouchAndNoRepeat);
        Console.WriteLine("COMBAT007: "+_pass+" pass, "+_fail+" fail");
        return _fail==0?0:1;
    }
}
