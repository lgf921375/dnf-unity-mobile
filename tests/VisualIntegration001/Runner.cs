// Integration regression of actual Unity-free adapter + actual core; not Unity Editor rendering.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DnfUnityMobile.Research;

public static class VisualIntegrationRunner
{
    static int ok, fail;
    static void Need(bool value, string message) { if (!value) throw new Exception(message); }
    static void Test(string title, Action test) {
        try { test(); ok++; Console.WriteLine("PASS VISUAL " + title); }
        catch (Exception ex) { fail++; Console.WriteLine("FAIL VISUAL " + title + ": " + ex); }
    }
    static void Until(Combat004Core c, Func<bool> predicate, int ticks=240) {
        for (int i=0;i<ticks && !predicate();i++) c.Tick(Combat004Core.FixedStep);
        Need(predicate(), "condition not met in "+ticks+" ticks");
    }
    static Combat004Core FirstHit() {
        var c=new Combat004Core(); c.PressAttack(); Until(c,()=>c.TotalContacts==2,25); return c;
    }
    static void Launch(Combat004Core c) {
        c.PressAttack(); Until(c,()=>c.Clock.Stage==2,65);
        c.PressAttack(); Until(c,()=>c.Clock.Stage==3,65);
        Until(c,()=>c.TotalContacts==6,100);
    }
    static void MultiTargetRouteExactOnce() {
        var c=FirstHit(); var events=c.DrainHitEvents(); var router=new CombatVisualAdapter();
        Need(events.Count==2,"two actual independent HitEvents");
        Need(c.DrainHitEvents().Count==0,"drain must clear actual core snapshots");
        var mapped=new HashSet<int>();
        for(int i=0;i<events.Count;i++) {
            VisualHitCue cue;
            Need(router.TryRoute(events[i],c.Enemies,out cue),"valid event should route");
            Need(mapped.Add(cue.TargetId),"one per target");
            Need(c.Enemies[cue.TargetIndex].Id==cue.TargetId,"routing index ID mismatch");
            Need(cue.EffectKey=="thrust"&&!cue.IsHeavy,"stage1 light VFX");
            Need(Math.Abs(cue.ContactX-events[i].ContactX)<0.0001f,"contact position copied");
            Need(!router.TryRoute(events[i],c.Enemies,out cue),"duplicate event replayed");
        }
        Need(c.Enemies[0].Hp==90 && c.Enemies[1].Hp==90,"router must not deal damage");
    }
    static void HeavyAndLaunchCue() {
        var c=FirstHit(); var router=new CombatVisualAdapter();
        Launch(c);
        var events=c.DrainHitEvents();
        Need(events.Count==2,"two stage3 impacts");
        for(int i=0;i<events.Count;i++) {
            VisualHitCue cue; Need(router.TryRoute(events[i],c.Enemies,out cue),"stage3 routed");
            Need(cue.IsHeavy && cue.EffectKey=="upper" && cue.DurationSeconds>0.14f,"heavy upper VFX");
            Need(c.Enemies[cue.TargetIndex].Reaction==CombatReactionPhase.AIR_RISE,"airborne after event");
        }
        Need(c.TotalContacts==6,"no duplicate authoritative contact");
    }
    static void PoseStateMatrix() {
        var c=FirstHit();var e=c.Enemies[0];
        Need(CombatVisualAdapter.ChooseEnemyFrame(e,0f)=="hurt0","HIT sprite");
        Launch(c);
        Need(CombatVisualAdapter.ChooseEnemyFrame(e,0f)=="air0","AIR_RISE sprite");
        Until(c,()=>e.Reaction==CombatReactionPhase.AIR_FALL,75);
        Need(CombatVisualAdapter.ChooseEnemyFrame(e,0f)=="air1","AIR_FALL sprite");
        Until(c,()=>e.Reaction==CombatReactionPhase.DOWN,90);
        Need(CombatVisualAdapter.ChooseEnemyFrame(e,0f)=="down0","DOWN sprite");
        Need(CombatVisualAdapter.ChooseEnemyFrame(e,0.25f)=="down1","DOWN alternate");
        Until(c,()=>e.Reaction==CombatReactionPhase.GETUP,90);
        Need(CombatVisualAdapter.ChooseEnemyFrame(e,0f)=="down1","GETUP declared fallback");
        Until(c,()=>e.Reaction==CombatReactionPhase.INVUL,70);
        Need(CombatVisualAdapter.ChooseEnemyFrame(e,0f)=="idle0","INVUL safe idle fallback");
        Until(c,()=>e.Reaction==CombatReactionPhase.READY,70);
        Need(CombatVisualAdapter.ChooseEnemyFrame(e,0f)=="idle0","READY");
        e.Hp=0; Need(CombatVisualAdapter.ChooseEnemyFrame(e,0f)=="ko0","KO sprite");
    }
    static void OnlyRealOtgRoutes() {
        var c=FirstHit(); Launch(c); var router=new CombatVisualAdapter();
        Until(c,()=>c.Clock.Stage==0,100);
        Until(c,()=>c.Enemies[0].Reaction==CombatReactionPhase.DOWN,100);
        c.Enemies[1].Hp=0;
        c.PressAttack();
        Until(c,()=>c.Enemies[0].Hits==4,25);
        var events=c.DrainHitEvents();
        Need(events.Count==1 && events[0].IsOtg,"only real low overlap OTG event");
        VisualHitCue cue; Need(router.TryRoute(events[0],c.Enemies,out cue) &&
             cue.IsOtg && cue.IsHeavy && cue.EffectKey=="sweep","OTG distinct rebound cue");
        int hp=c.Enemies[0].Hp;
        c.PressAttack();
        for(int i=0;i<35;i++) c.Tick(Combat004Core.FixedStep);
        Need(c.Enemies[0].Hp==hp,"protected target HP unchanged");
    }
    static void KOAndInvalidTargetRouting() {
        var c=FirstHit(); var router=new CombatVisualAdapter();
        var events=c.DrainHitEvents();
        VisualHitCue cue;
        Need(!router.TryRoute(new CombatHitEvent(events[0].Tick,events[0].AttackInstance,
              events[0].Stage,999,10,0f,0f,0f,.055f,1,0f,false),c.Enemies,out cue),
              "unknown target must not be routed");
        c.Enemies[0].Hp=0;
        Need(CombatVisualAdapter.ChooseEnemyFrame(c.Enemies[0],0f)=="ko0","KO remains identifiable");
        router.Reset();
        Need(router.TryRoute(events[0],c.Enemies,out cue),"reset allows new sequence routing");
    }
    static void StageKeysAndImmutableDamage() {
        var c=new Combat004Core();
        var router=new CombatVisualAdapter();
        c.PressAttack();
        Need(CombatSpriteKeys.PlayerAttackFrame(c.Clock.Stage,c.Clock.FrameKey)=="a1_1",
             "stage1 original mage frame selection");
        Until(c,()=>c.TotalContacts==2,25);
        var e=c.DrainHitEvents()[0];
        VisualHitCue cue; Need(router.TryRoute(e,c.Enemies,out cue),"valid event");
        int oldHp=c.Enemies[0].Hp;
        Need(CombatVisualAdapter.ChooseEnemyFrame(c.Enemies[0],0f)=="hurt0","frame mapping");
        Need(c.Enemies[0].Hp==oldHp,"reading visual pose cannot alter HP");
        Need(CombatSpriteKeys.PlayerAttackFrame(2,"a2_2")=="a2_2", "stage2 frame");
        Need(CombatSpriteKeys.PlayerAttackFrame(3,"a3_2")=="a3_2", "stage3 frame");
    }
    static void SceneStaticWiringContract() {
        string root=Directory.GetCurrentDirectory();
        string scene=File.ReadAllText(Path.Combine(root,"Assets/Scripts/Research/ResearchCombat004Scene.cs"));
        string presenter=File.ReadAllText(Path.Combine(root,"Assets/Scripts/Research/CombatSpritePresenter.cs"));
        int tick=scene.IndexOf("_core.Tick(Time.fixedDeltaTime)",StringComparison.Ordinal);
        int drain=scene.IndexOf("_core.DrainHitEvents()",StringComparison.Ordinal);
        int route=scene.IndexOf("_spritePresenter.ConsumeHitEvents",StringComparison.Ordinal);
        Need(tick>=0 && route>tick && drain>route,"Tick then Consume(Drain()) before next Tick");
        Need(presenter.Contains("Resources.Load<Texture2D>"),"must use shipped mage/mossling atlases");
        Need(!presenter.Contains("e.Hits!=_previousHits"),"no fake VFX from Hits counter");
        Need(presenter.Contains("CombatVisualAdapter.ChooseEnemyFrame"),"core phases mapped by adapter");
        Need(scene.Contains("Combat007Layout.Calculate")&&scene.Contains("Input.GetTouch"),
            "COMBAT007 landscape-safe two finger path unchanged");
    }
    static void NoStaleOrDuplicateRouting()
    {
        var c=FirstHit(); var events=c.DrainHitEvents();
        var adapter=new CombatVisualAdapter();
        VisualHitCue cue;
        Need(adapter.TryRoute(events[0],c.Enemies,out cue),"first event");
        Need(!adapter.TryRoute(events[0],c.Enemies,out cue),"same event cannot route twice");
        var stale=new CombatHitEvent(events[0].Tick-1,1,1,events[1].TargetId,
            events[1].Damage,events[1].ContactX,events[1].ContactLane,0f,.055f,1,0f,false);
        Need(!adapter.TryRoute(stale,c.Enemies,out cue),"older Tick rejected");
        Need(adapter.TryRoute(events[1],c.Enemies,out cue),"second target same Tick");
        var duplicated=new [] {c.Enemies[0],c.Enemies[0]};
        adapter.Reset();
        Need(!adapter.TryRoute(events[0],duplicated,out cue),"duplicate logical ID is ambiguous");
    }
    static void PerTargetIndependentPresenterSource()
    {
        string root=Directory.GetCurrentDirectory();
        string presenter=File.ReadAllText(Path.Combine(root,
            "Assets/Scripts/Research/CombatSpritePresenter.cs"));
        string scene=File.ReadAllText(Path.Combine(root,
            "Assets/Scripts/Research/ResearchCombat004Scene.cs"));
        Need(presenter.Contains("new SpriteRenderer[2]") &&
            presenter.Contains("_targetImpact[i]") && presenter.Contains("cue.TargetIndex"),
            "each target requires its own SpriteRenderer");
        Need(presenter.Contains("_vfx[cue.EffectKey]") &&
            presenter.Contains("cue.ContactX") && presenter.Contains("cue.ContactLane"),
            "real arc_atlas stage / impact contact must drive sparks");
        Need(presenter.Contains("Time.unscaledDeltaTime") &&
            presenter.Contains("_impactRemaining[i]"),
            "VFX must advance independently from frozen simulation");
        Need(presenter.Contains("_enemyShadows[i].transform.localPosition") &&
            presenter.Contains("-e.Height"),
            "ground shadow must compensate airborne body root");
        Need(scene.Contains("Time.fixedDeltaTime = Combat004Core.FixedStep"),
            "scene fixed step contract");
    }
    static int Main() {
        Test("same-tick 2 targets routed once by ID from real events",MultiTargetRouteExactOnce);
        Test("stage 3 launches and produces heavy upper effects",HeavyAndLaunchCue);
        Test("AIR DOWN GETUP INVUL KO atlas selection",PoseStateMatrix);
        Test("only verified stage1/2 OTG gets FX, no damage replay",OnlyRealOtgRoutes);
        Test("wrong target and Reset route protection",KOAndInvalidTargetRouting);
        Test("three original mage attack keys and readonly damage",StageKeysAndImmutableDamage);
        Test("scene consumes one-time event and retains COMBAT007",SceneStaticWiringContract);
        Test("reject duplicate target IDs and replayed/stale ticks",NoStaleOrDuplicateRouting);
        Test("actual Sprite Presenter source has independent arc-hit effects",PerTargetIndependentPresenterSource);
        Console.WriteLine("VISUAL-INTEGRATION-001 "+ok+" pass, "+fail+" fail");
        return fail==0?0:1;
    }
}
