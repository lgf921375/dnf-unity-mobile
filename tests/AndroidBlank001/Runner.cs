// ANDROID-BLANK-001: pure .NET8 simulation + actual committed Unity source/PNG checks.
// Does NOT execute MonoBehaviour, Unity Player or Android. No fake logcat.
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using DnfUnityMobile.Research;
public static class AndroidBlank001Runner
{
    private static int _pass, _fail;
    private static string _root;
    private static void Check(bool b,string why)
    {
        if(!b)throw new Exception(why);
    }
    private static void Test(string name,Action test)
    {
        try{test();_pass++;Console.WriteLine("PASS BLANK001 "+name);}
        catch(Exception ex){_fail++;Console.WriteLine("FAIL BLANK001 "+name+": "+ex.Message);}
    }
    private static string Source(string path)=>
        File.ReadAllText(Path.Combine(_root,path));
    private static void FindRoot()
    {
        string cursor=AppContext.BaseDirectory;
        for(int i=0;i<12;i++)
        {
            if(File.Exists(Path.Combine(cursor,"ProjectSettings/EditorBuildSettings.asset")))
            { _root=cursor;return;}
            cursor=Directory.GetParent(cursor)?.FullName;
            if(cursor==null)break;
        }
        throw new InvalidOperationException("Run tests within the repository checkout");
    }
    private static void SuccessfulBootstrap()
    {
        var state=new CombatStartupTrace();
        // Awake() runs before sceneLoaded on scene objects; this is a pure
        // state simulation, NOT proof of actual Unity callback scheduling.
        foreach(var stage in new[]{"DIAGNOSTICS_INSTALLED",
                     "SCENE_AWAKE_ENTER","CORE_READY","CAMERA_READY",
                     "PRESENTER_INITIALIZED","FIRST_SPRITES_ASSIGNED"})
            state.Enter(stage);
        Check(state.Count==6 && state.LastStage=="FIRST_SPRITES_ASSIGNED",
            "startup stage sequencing");
        state.SetReady();
        Check(state.Ready && !state.Failed && state.LastStage=="SCENE_READY",
            "successful scene must be marked ready");
        state.Enter("SCENE_LOADED");
        state.Enter("SCENE_GUI_REPAINT");
        Check(state.Ready && state.LastStage=="SCENE_GUI_REPAINT",
            "GUI renderer opportunity recorded after ready");
    }
    private static void MissingAtlasFaultInjection()
    {
        var state=new CombatStartupTrace();
        state.Enter("SCENE_AWAKE_ENTER");
        state.Enter("CAMERA_READY");
        state.Enter("RESOURCES_BEGIN_Player/mage_atlas");
        state.Fail("SCENE_AWAKE","InvalidOperationException");
        Check(state.Failed&&!state.Ready,"missing atlas failure swallowed");
        Check(state.FaultStage=="SCENE_AWAKE" &&
              state.FaultType=="InvalidOperationException","no failure provenance");
        Check(state.Snapshot().Any(x=>x=="RESOURCES_BEGIN_Player/mage_atlas"),
            "diagnostic does not retain the resource loading stage");
        state.SetReady();
        Check(!state.Ready,"cannot label startup READY after exception");
    }
    private static void FirstFaultWins()
    {
        var state=new CombatStartupTrace();
        state.Fail("SCENE_AWAKE","InvalidOperationException");
        state.Fail("SCENE_FIXED","NullReferenceException");
        Check(state.FaultStage=="SCENE_AWAKE" &&
            state.FaultType=="InvalidOperationException" && state.Count==1,
            "later secondary error should not overwrite first root cause");
        state.Enter("SCENE_READY");
        Check(state.Count==1,"fatal trace must be stable");
    }
    private static void UnsafeLogPayloadRejected()
    {
        foreach(string payload in new[]{"", "path:/home/user/secret",
                "some-password value",new string('A',90)})
        {
            bool threw=false;
            try{CombatStartupTrace.Validate(payload);}
            catch(ArgumentException){threw=true;}
            Check(threw,"unsafe diagnostic label accepted");
        }
        CombatStartupTrace.Validate("RESOURCES_BEGIN_Enemy/mossling_atlas");
    }
    private static int Be(byte[] data,int p)=>
        (int)((uint)data[p]<<24|(uint)data[p+1]<<16|(uint)data[p+2]<<8|data[p+3]);
    private static void CheckPng(string name,int width,int height)
    {
        string a=Path.Combine(_root,"Assets/Art/OriginalCombatSlice",name+".png");
        string b=Path.Combine(_root,"Assets/Resources/OriginalCombatSlice",name+".png");
        byte[] art=File.ReadAllBytes(a), runtime=File.ReadAllBytes(b);
        Check(art.SequenceEqual(runtime),"authoring vs runtime PNG mismatch "+name);
        Check(art.Length>64&&art.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),
            "invalid PNG "+name);
        Check(Be(art,16)==width && Be(art,20)==height,
            "wrong PNG atlas dimensions "+name);
        Check(File.Exists(a+".meta") && File.Exists(b+".meta"),
            "missing stable PNG import .meta");
    }
    private static void FourAtlasesAndKeys()
    {
        CheckPng("Player/mage_atlas",640,640);
        CheckPng("Enemy/mossling_atlas",448,512);
        CheckPng("Background/landscape_atlas",640,1080);
        CheckPng("Vfx/arc_atlas",384,160);
        using(var json=JsonDocument.Parse(Source("Assets/Art/OriginalCombatSlice/frame-map.json")))
        {
            var root=json.RootElement;
            foreach(var item in new[]{("player",19),("enemy",15),("background",3),("vfx",3)})
                Check(root.GetProperty(item.Item1).GetProperty("keys").GetArrayLength()==item.Item2,
                    "frame-map mismatch "+item.Item1);
        }
    }
    private static void SceneFirstAndGuids()
    {
        string build=Source("ProjectSettings/EditorBuildSettings.asset");
        int battle=build.IndexOf("Assets/Scenes/ResearchCombat004.unity",StringComparison.Ordinal);
        int bootstrap=build.IndexOf("Assets/Scenes/Bootstrap.unity",StringComparison.Ordinal);
        Check(battle>=0 && bootstrap>battle,"BuildSettings must boot ResearchCombat004 first");
        Check(Source("Assets/Scenes/ResearchCombat004.unity").Contains(
            "m_Script: {fileID: 11500000, guid: 9db231ab58d2468599f4b5eb2c99fe03"),
            "scene root is not wired to battle MonoBehaviour");
        Check(Source("Assets/Scripts/Research/ResearchCombat004Scene.cs.meta").Contains(
            "guid: 9db231ab58d2468599f4b5eb2c99fe03"),
            "battle MonoBehaviour .meta GUID mismatch");
        var known=new[]{
            "Assets/Scripts/Research/CombatStartupTrace.cs.meta",
            "Assets/Scripts/Research/CombatStartupDiagnostics.cs.meta",
            "Assets/Scripts/Research/ResearchCombat004Scene.cs.meta",
            "Assets/Scripts/Research/CombatSpritePresenter.cs.meta"
        };
        var guids=new HashSet<string>();
        foreach(var path in known)
        {
            var entry=Source(path).Split('\n').First(x=>x.StartsWith("guid: "));
            Check(guids.Add(entry),"duplicate startup GUID: "+path);
        }
        Check(Source("ProjectSettings/ProjectVersion.txt").Contains("6000.3.26f1"),
            "Unity version changed");
    }
    private static void DiagnosticIndependentOfGame()
    {
        string diag=Source("Assets/Scripts/Research/CombatStartupDiagnostics.cs");
        Check(diag.Contains("RuntimeInitializeLoadType.BeforeSceneLoad") &&
            diag.Contains("DontDestroyOnLoad") &&
            diag.Contains("private void OnGUI()") &&
            diag.Contains("Application.persistentDataPath") &&
            diag.Contains("File.AppendAllText") &&
            diag.Contains("TintFailureCamera") &&
            diag.Contains("GetType().Name"),"pre-scene independent diagnostic missing");
        Check(!diag.Contains("Combat004Core") && !diag.Contains("Resources.Load<") &&
            !diag.Contains("CombatSpritePresenter"),"diagnostic depends on failing battle core");
        string scene=Source("Assets/Scripts/Research/ResearchCombat004Scene.cs");
        Check(scene.Contains("CombatStartupDiagnostics.Fail(\"SCENE_AWAKE\", ex)") &&
            scene.Contains("CombatStartupDiagnostics.Fail(\"SCENE_UPDATE\", ex)") &&
            scene.Contains("CombatStartupDiagnostics.Fail(\"SCENE_FIXED\", ex)") &&
            scene.Contains("CombatStartupDiagnostics.Fail(\"SCENE_ONGUI\", ex)") &&
            scene.Contains("CombatStartupDiagnostics.Ready()"),"scene fault path not covered");
    }
    private static void StageGranularityAndNoCoreDiff()
    {
        string presenter=Source("Assets/Scripts/Research/CombatSpritePresenter.cs");
        string scene=Source("Assets/Scripts/Research/ResearchCombat004Scene.cs");
        Check(presenter.Contains("RESOURCES_BEGIN_") &&
              presenter.Contains("ATLAS_SLICE_BEGIN_") &&
              presenter.Contains("ATLAS_SLICE_OK_"),
              "no distinction between Resources/atlas import stages");
        Check(scene.Contains("FIRST_SPRITES_ASSIGNED") &&
              scene.Contains("CameraClearFlags.SolidColor"),
              "camera / last-success stage missing");
        Check(scene.Contains("_core.Tick(Time.fixedDeltaTime)") &&
              scene.Contains("Input.GetTouch") &&
              scene.Contains("Combat007Layout.Calculate"),
              "gameplay timing, touch or layout path lost");
        Check(Source("Assets/Scripts/Research/Combat004Core.cs").Contains(
              "Combat004Core requires <= 1/60s"),
              "Core unchanged contract expected");
    }
    // Static source-contract checks only. These DO NOT replace a Unity/Android run.
    private static void FailedAwakeKeepsOwnErrorGui()
    {
        string scene=Source("Assets/Scripts/Research/ResearchCombat004Scene.cs");
        int awake=scene.IndexOf("private void Awake()",StringComparison.Ordinal);
        int update=scene.IndexOf("private void Update()",StringComparison.Ordinal);
        int gui=scene.IndexOf("private void OnGUI()",StringComparison.Ordinal);
        Check(awake>=0 && update>awake && gui>update,"scene lifecycle methods missing");
        string awakeBody=scene.Substring(awake,update-awake);
        string guiBody=scene.Substring(gui);
        Check(awakeBody.Contains("_startupFailureType = ex.GetType().Name") &&
              awakeBody.Contains("CombatStartupDiagnostics.Fail(\"SCENE_AWAKE\", ex)") &&
              !awakeBody.Contains("enabled = false"),
              "Awake catch must preserve the game's fallback error GUI");
        Check(guiBody.Contains("if (!_startupComplete)") &&
              guiBody.Contains("if (!string.IsNullOrEmpty(_startupFailureType))") &&
              guiBody.Contains("DNF STARTUP ERROR"),
              "incomplete startup must expose an independent GUI error panel");
    }

    private static void LegacyInputFailureDoesNotKillRendering()
    {
        string scene=Source("Assets/Scripts/Research/ResearchCombat004Scene.cs");
        int update=scene.IndexOf("private void Update()",StringComparison.Ordinal);
        int fixedUpdate=scene.IndexOf("private void FixedUpdate()",StringComparison.Ordinal);
        Check(update>=0 && fixedUpdate>update,"Update/FixedUpdate missing");
        string body=scene.Substring(update,fixedUpdate-update);
        int guarded=body.IndexOf("if (!_legacyInputUnavailable)",StringComparison.Ordinal);
        int caught=body.IndexOf("catch (InvalidOperationException)",StringComparison.Ordinal);
        int core=body.IndexOf("if (attackDown) _core.PressAttack()",StringComparison.Ordinal);
        Check(guarded>=0 && caught>guarded && core>caught,
              "Input-only exception handler must not mask core exceptions");
        Check(body.Contains("#if ENABLE_LEGACY_INPUT_MANAGER") &&
              body.Contains("#else") && body.Contains("#endif"),
              "legacy input must be gated by actual Unity backend define");
        Check(body.Contains("_legacyInputUnavailable = true;") &&
              body.Contains("_keyboardAxis = 0f;") && body.Contains("_guiAxis = 0f;") &&
              body.Contains("Input.GetTouch") && body.Contains("pointers.AttackDown") &&
              !body.Substring(caught,core-caught).Contains("enabled = false"),
              "Input failure should release controls and preserve scene rendering");
        Check(scene.Contains("INPUT UNAVAILABLE: check Android Active Input Handling"),
              "Input setup failure must have a visible explanatory cue");
    }

    private static void GenericExceptionTypeDoesNotHideFirstFault()
    {
        var state = new CombatStartupTrace();
        state.Fail("SCENE_AWAKE","GenericFailure`1");
        Check(state.Failed && state.FaultType=="GenericFailure_1",
            "generic exception type should normalize safely");
        state.Fail("SCENE_FIXED","SecondaryException");
        Check(state.FaultStage=="SCENE_AWAKE" && state.FaultType=="GenericFailure_1",
            "first failure should remain authoritative");
        var unsafeState = new CombatStartupTrace();
        unsafeState.Fail("SCENE_AWAKE","secret: /private/path");
        Check(unsafeState.FaultType=="UnknownException",
            "unsafe exception names must not be persisted");
        var longState = new CombatStartupTrace();
        longState.Fail("SCENE_AWAKE",new string('Q',90));
        Check(longState.FaultType=="UnknownException",
            "overlong type names must fall back to fixed token");
    }

    private static void DiagnosticFailureProtectionSourceContract()
    {
        string diag=Source("Assets/Scripts/Research/CombatStartupDiagnostics.cs");
        string scene=Source("Assets/Scripts/Research/ResearchCombat004Scene.cs");
        Check(diag.Contains("_writePolicy.TryBegin(code)") &&
              diag.Contains("_writePolicy.Reset()") &&
              Source("Assets/Scripts/Research/CombatDiagnosticWritePolicy.cs").Contains(
                  "public const int MaxCodes = 96"),
              "portable diagnostic write rate policy is missing");
        Check(diag.Contains("finally") &&
              diag.Contains("GUI.color = previousGuiColor") &&
              diag.Contains("_diagnosticGuiFaultReported"),
              "diagnostic UI restoration/second-fault protection missing");
        Check(scene.Contains("_startupFailureGuiFaultReported") &&
              scene.Contains("CombatStartupDiagnostics.Fail(\"SCENE_FAILURE_GUI\", ex)"),
              "scene failure GUI can throw without guarded diagnostics");
    }

    private static void BoundedDiagnosticWritePolicyBehavior()
    {
        var policy = new CombatDiagnosticWritePolicy();
        Check(policy.TryBegin("DIAG_FIRST"), "first category should be accepted");
        Check(!policy.TryBegin("DIAG_FIRST"), "same category must be deduplicated");
        for (int i = 1; i < CombatDiagnosticWritePolicy.MaxCodes; ++i)
            Check(policy.TryBegin("DIAG_STAGE_" + i), "unique category rejected early: " + i);
        Check(policy.Count == CombatDiagnosticWritePolicy.MaxCodes,
            "policy failed to stop at the hard session cap");
        Check(!policy.TryBegin("DIAG_OVERFLOW"),
            "event beyond per-session limit should not reach filesystem");
        Check(!policy.TryBegin("DIAG_FIRST"),
            "deduplication should still hold at capacity");

        policy.Reset();
        Check(policy.Count == 0 && policy.TryBegin("DIAG_FIRST"),
            "fresh session must reset deduplication and capacity");

        bool rejected = false;
        try { policy.TryBegin("SECRET: /a/private/path"); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected && policy.Count == 1,
            "invalid payload must be rejected without consuming event quota");
    }

    public static int Main()
    {
        FindRoot();
        Test("startup state successful multi-stage then GUI ready",SuccessfulBootstrap);
        Test("injected missing atlas preserves failure stage",MissingAtlasFaultInjection);
        Test("first root exception preserved against secondary exceptions",FirstFaultWins);
        Test("safe diagnostic labels reject sensitive free-form messages",UnsafeLogPayloadRejected);
        Test("four real PNG resource imports and current atlas dimension keys",FourAtlasesAndKeys);
        Test("scene#0 Bootstrap#1 script GUID and stable editor version",SceneFirstAndGuids);
        Test("pre-scene independent error overlay and guarded event loop",DiagnosticIndependentOfGame);
        Test("Resources versus Sprite stage and combat/touch invariants",StageGranularityAndNoCoreDiff);
        Test("failed Awake preserves source-level error GUI",FailedAwakeKeepsOwnErrorGui);
        Test("unsupported legacy Input does not shut down rendering",LegacyInputFailureDoesNotKillRendering);
        Test("generic exception type preserves first failure",GenericExceptionTypeDoesNotHideFirstFault);
        Test("diagnostic second-fault and disk-write guards",DiagnosticFailureProtectionSourceContract);
        Test("bounded diagnostic write admission policy (pure C#)",BoundedDiagnosticWritePolicyBehavior);
        Console.WriteLine("ANDROID-BLANK-001 "+_pass+" pass, "+_fail+" fail");
        return _fail==0?0:1;
    }
}
