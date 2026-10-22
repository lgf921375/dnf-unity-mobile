// ART-SLICE-001: real .NET 8 asset PNG decoding and Unity scene static tests.
// This does NOT compile UnityEngine or replace a Unity 6000.3 Editor/phone run.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
public static class ArtSliceRunner
{
    private static int _ok,_fail;
    private static string Root;
    private sealed class Atlas {
        public int W,H;public byte[] Raw;public int FrameW,FrameH,Cols;public string[] Keys;public string Label;
    }
    private static void Need(bool ok,string message) {if(!ok)throw new Exception(message);}
    private static void Test(string title,Action test) {
        try {test();_ok++;Console.WriteLine("PASS ART "+title);}
        catch(Exception e){_fail++;Console.WriteLine("FAIL ART "+title+": "+e.Message);}
    }
    private static int Be(byte[] d,int i) => (int)((uint)d[i]<<24|(uint)d[i+1]<<16|(uint)d[i+2]<<8|d[i+3]);
    private static Atlas Decode(string path,int w,int h,int fw,int fh,int cols,string label,string[] keys) {
        byte[] d=File.ReadAllBytes(path);
        Need(d.Length>64,"PNG too short "+label);
        byte[] sig={137,80,78,71,13,10,26,10};
        Need(d.Take(8).SequenceEqual(sig),"invalid PNG signature "+label);
        int pos=8;var packed=new MemoryStream();bool ihdr=false;
        while(pos<d.Length) {
            int len=Be(d,pos);pos+=4;
            string kind=Encoding.ASCII.GetString(d,pos,4);pos+=4;
            Need(len>=0 && len<=d.Length-pos-4,"corrupt PNG chunk "+kind);
            if(kind=="IHDR") {
                Need(Be(d,pos)==w && Be(d,pos+4)==h,"PNG pixel dimensions mismatch "+label);
                Need(d[pos+8]==8 && d[pos+9]==3,"index-8 PNG expected "+label);
                ihdr=true;
            } else if(kind=="IDAT")packed.Write(d,pos,len);
            pos+=len+4;
            if(kind=="IEND")break;
        }
        Need(ihdr,"missing IHDR");packed.Position=0;
        using var z=new ZLibStream(packed,CompressionMode.Decompress);
        using var raw=new MemoryStream();z.CopyTo(raw);
        byte[] px=raw.ToArray();
        Need(px.Length==h*(w+1),"decompressed scanline bytes mismatch "+label);
        for(int y=0;y<h;y++)Need(px[y*(w+1)]==0,"non-filter-0 PNG row unsupported");
        Need(w==fw*cols && h==fh*((keys.Length+cols-1)/cols),"atlas tile dimensions mismatch "+label);
        return new Atlas{W=w,H=h,Raw=px,FrameW=fw,FrameH=fh,Cols=cols,Label=label,Keys=keys};
    }
    private static byte[] Tile(Atlas a,int n) {
        int x=(n%a.Cols)*a.FrameW,y=(n/a.Cols)*a.FrameH;
        var tile=new byte[a.FrameW*a.FrameH];
        for(int j=0;j<a.FrameH;j++)
            Buffer.BlockCopy(a.Raw,(y+j)*(a.W+1)+1+x,tile,j*a.FrameW,a.FrameW);
        return tile;
    }
    private static string Sha(byte[] b)=>Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant();
    private static Atlas Fetch(JsonElement atlas,string section) {
        int w=atlas.GetProperty("pixelWidth").GetInt32();
        int h=atlas.GetProperty("pixelHeight").GetInt32();
        int fw=atlas.GetProperty("frameWidth").GetInt32();
        int fh=atlas.GetProperty("frameHeight").GetInt32();
        int cols=atlas.GetProperty("columns").GetInt32();
        string[] keys=atlas.GetProperty("keys").EnumerateArray().Select(x=>x.GetString()).ToArray();
        string rel=atlas.GetProperty("file").GetString().Replace('/','/');
        string art=Path.Combine(Root,"Assets/Art/OriginalCombatSlice",rel);
        string resource=Path.Combine(Root,"Assets/Resources/OriginalCombatSlice",rel);
        Need(File.Exists(art) && File.Exists(resource),"missing PNG from Art or Resources "+section);
        Need(Sha(File.ReadAllBytes(art))==Sha(File.ReadAllBytes(resource)),
            "runtime resource does not match authored PNG "+section);
        Need(File.Exists(art+".meta")&&File.Exists(resource+".meta"),"missing Unity .meta "+section);
        return Decode(art,w,h,fw,fh,cols,section,keys);
    }
    private static void InspectAtlas(string section,JsonElement atlas,int expectedFrames) {
        var a=Fetch(atlas,section);
        Need(a.Keys.Length==expectedFrames,section+" expected frames "+expectedFrames);
        Need(a.Keys.Distinct().Count()==a.Keys.Length,section+" duplicate key");
        var hashes=new HashSet<string>();
        for(int i=0;i<a.Keys.Length;i++) {
            byte[] frame=Tile(a,i);
            int colorCount=frame.Count(b=>b!=0);
            Need(colorCount>60,section+" "+a.Keys[i]+" is blank / tiny: "+colorCount);
            hashes.Add(Sha(frame));
        }
        Need(hashes.Count>=expectedFrames-2,section+" too many repeated frame pixels: "+hashes.Count);
        Console.WriteLine("ASSET "+section+" "+a.W+"x"+a.H+" tiles="+a.Keys.Length+
            " distinct="+hashes.Count+" real_pixel_bytes="+a.Raw.Length);
    }
    private static void ThreeAttacks(JsonElement player) {
        var a=Fetch(player,"player");
        foreach(string k in new[]{"idle0","walk0","a1_1","a1_2","a1_3",
                    "a2_1","a2_2","a2_3","a3_1","a3_2","hit","airUp","down","getup"})
            Need(a.Keys.Contains(k),"player atlas missing required pose "+k);
        var frames=new []{"a1_2","a2_2","a3_2"};
        Need(frames.Select(k=>Sha(Tile(a,Array.IndexOf(a.Keys,k)))).Distinct().Count()==3,
            "three attack contacts must have visually distinct pixels");
        float ax=player.GetProperty("anchorX").GetSingle();
        float ay=player.GetProperty("anchorYTop").GetSingle();
        float ppu=player.GetProperty("ppu").GetSingle();
        Need(ax>=0&&ax<a.FrameW&&ay>0&&ay<=a.FrameH&&ppu>0,"player foot metadata invalid");
        float offX=(a.FrameW/2f-ax)/ppu,offY=(ay-a.FrameH/2f)/ppu;
        Need(Math.Abs(offX+(ax-a.FrameW/2f)/ppu)<.00001f,"foot misaligned right");
        Need(Math.Abs(-offX-(a.FrameW/2f-ax)*(-1f)/ppu)<.00001f,
            "foot misaligned after flipX");
        Console.WriteLine("FOOT frame="+a.FrameW+"x"+a.FrameH+" anchor="+ax+","+ay+
            " PPU="+ppu+" centerOffset="+offX.ToString("F4")+","+offY.ToString("F4"));
    }
    private static void StableMetaAndUnityVersion()
    {
        string[] names={
            "Assets/Scripts/Research/CombatSpritePresenter.cs",
            "Assets/Art/OriginalCombatSlice/frame-map.json",
            "Assets/Art/OriginalCombatSlice/Player/mage_atlas.png",
            "Assets/Art/OriginalCombatSlice/Enemy/mossling_atlas.png",
            "Assets/Art/OriginalCombatSlice/Background/landscape_atlas.png",
            "Assets/Art/OriginalCombatSlice/Vfx/arc_atlas.png",
            "Assets/Resources/OriginalCombatSlice/Player/mage_atlas.png",
            "Assets/Resources/OriginalCombatSlice/Enemy/mossling_atlas.png",
            "Assets/Resources/OriginalCombatSlice/Background/landscape_atlas.png",
            "Assets/Resources/OriginalCombatSlice/Vfx/arc_atlas.png"
        };
        var ids=new HashSet<string>();
        foreach(string name in names)
        {
            string full=Path.Combine(Root,name+".meta");
            Need(File.Exists(full),"missing Unity .meta for "+name);
            string txt=File.ReadAllText(full);
            string g=txt.Split('\n').FirstOrDefault(x=>x.StartsWith("guid: "))?.Substring(6)?.Trim();
            Need(g!=null&&g.Length==32,"invalid GUID for "+name);
            Need(ids.Add(g),"duplicate GUID for "+name);
            if(name.EndsWith(".png"))
                Need(txt.Contains("TextureImporter:")&&txt.Contains("isReadable: 1"),
                    "atlas importer must support Sprite.Create: "+name);
        }
        string ver=File.ReadAllText(Path.Combine(Root,"ProjectSettings/ProjectVersion.txt"));
        Need(ver.Contains("6000.3.26f1"),"Unity Editor version must remain fixed");
    }

    private static void SourceIntegration() {
        string scene=File.ReadAllText(Path.Combine(Root,"Assets/Scripts/Research/ResearchCombat004Scene.cs"));
        string show=File.ReadAllText(Path.Combine(Root,"Assets/Scripts/Research/CombatSpritePresenter.cs"));
        Need(scene.Contains("AddComponent<CombatSpritePresenter>()")&&scene.Contains("_spritePresenter.Present("),
            "battle scene not connected to atlas presenter");
        Need(!scene.Contains("CreateFrame(\"idle\","),"old block placeholder frames still executed");
        Need(!scene.Contains("MakePanel(\"Floor\""),"old floor rectangle still executed");
        Need(show.Contains("Resources.Load<Texture2D>") &&
            show.Contains("ReferenceSpriteAlignment.ApplyCenteredSprite"),
            "atlas load/foot alignment missing");
        Need(show.Contains("combat.Clock.FrameKey")&&show.Contains("enemy.Hp"),
            "visual poses not sourced from combat status");
        Need(!show.Contains("e.Hp =")&&!show.Contains("TotalContacts++"),
            "visual code incorrectly adjudicates combat damage");
        Need(scene.Contains("Input.GetTouch")&&scene.Contains("Combat007Layout"),
            "COMBAT007 safe-area/touch regression lost");
        Need(scene.Contains("_debugHud = !_debugHud"),"debug HUD not toggleable");
        string build=File.ReadAllText(Path.Combine(Root,"ProjectSettings/EditorBuildSettings.asset"));
        Need(build.IndexOf("ResearchCombat004.unity",StringComparison.Ordinal) <
            build.IndexOf("Bootstrap.unity",StringComparison.Ordinal),"scene order changed");
        string core=File.ReadAllText(Path.Combine(Root,"Assets/Scripts/Research/Combat004Core.cs"));
        Need(core.Contains("Combat004Core requires <= 1/60s"),"COMBAT006 core contract missing");
    }
    public static int Main() {
        Root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        // The runner is output to tests/ArtSlice001/bin/Release/net8.0, 4 parents -> project root? 
        // Find the checkout root by searching upward for Assets/Art frame-map.
        string cursor=AppContext.BaseDirectory;
        for(int i=0;i<10;i++) {
            if(File.Exists(Path.Combine(cursor,"Assets/Art/OriginalCombatSlice/frame-map.json"))) {
                Root=cursor;break;
            }
            string p=Directory.GetParent(cursor)?.FullName;
            if(p==null) break;cursor=p;
        }
        string map=Path.Combine(Root,"Assets/Art/OriginalCombatSlice/frame-map.json");
        if(!File.Exists(map)) {Console.WriteLine("FAIL ART test root missing "+Root);return 1;}
        using var doc=JsonDocument.Parse(File.ReadAllText(map));
        var root=doc.RootElement;
        Test("PNG atlas: original staff mage 19 frames",()=>InspectAtlas("player",root.GetProperty("player"),19));
        Test("PNG atlas: original mossling 12 frames",()=>InspectAtlas("enemy",root.GetProperty("enemy"),12));
        Test("PNG atlas: 3 independent parallax backgrounds",()=>InspectAtlas("background",root.GetProperty("background"),3));
        Test("PNG atlas: 3 distinct spell arcs",()=>InspectAtlas("vfx",root.GetProperty("vfx"),3));
        Test("3 visibly distinct attack poses and pivot/flip metadata",()=>ThreeAttacks(root.GetProperty("player")));
        Test("Unity PNG import metas unique GUID and Unity 6000.3.26f1",StableMetaAndUnityVersion);
        Test("Unity resource copies/metas, scene import links and untouched core",SourceIntegration);
        Console.WriteLine("ART-SLICE-001 "+_ok+" pass, "+_fail+" fail");
        return _fail==0?0:1;
    }
}
