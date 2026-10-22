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
        foreach(var stage in new[]{"DIAGNOSTICS_INSTALLED","SCENE_LOADED",
                     "SCENE_AWAKE_ENTER","CORE_READY","CAMERA_READY",
                     "PRESENTER_INITIALIZED","FIRST_SPRITES_ASSIGNED"})
            state.Enter(stage);
        Check(state.Count==7 && state.LastStage=="FIRST_SPRITES_ASSIGNED",
            "startup stage sequencing");
        state.SetReady();
        Check(state.Ready && !state.Failed && state.LastStage=="SCENE_READY",
            "successful scene must be marked ready");
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
        Console.WriteLine("ANDROID-BLANK-001 "+_pass+" pass, "+_fail+" fail");
        return _fail==0?0:1;
    }
}
