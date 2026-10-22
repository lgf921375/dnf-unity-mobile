# Active handoff — DNF-UNITY-ART-SLICE-001 (2026-10-22)

## Only active execution branch
- Issue #11: https://github.com/lgf921375/dnf-unity-mobile/issues/11, awaiting 智脑2 independent acceptance.
- Writable branch `work/unity-art-slice-001-20261022`; fixed parent COMBAT-007 `fbeb2df3b150c6358009ded5a16c41d37b911d7f`.
- **Protected** `main` `ae65b231f5b100ac46d71b8f3ddd3348ae948d5e`, sibling `work/unity-hit-reaction-001-20261022`, successful build-only checkpoint `0434533637bc7dacc63b8fbbd9b545019c27fb55`, Godot and Unity Cloud settings.
- No GitHub Actions/Unity Cloud/Android APK, no merge, no third-party unlicensed art or DNF/Nexon original assets.

## What is actually committed (not a report-only milestone)
- Four original, directly decodable PNG8 atlases `Assets/Art/OriginalCombatSlice/{Player,Enemy,Background,Vfx}/`: player staff mage **19 keyframes**, original shielded mossling **12 pose slots / 10 unique poses**, independently layered ruins background **3 layers**, original magic attack arcs **3 frames**. 37 atlas cells across four PNG files, plus **actual PNG contact-sheet preview**.
- Exact-byte `Assets/Resources/OriginalCombatSlice/{...}` mirror for runtime `Resources.Load<Texture2D>`; all PNG textures have committed Unity `.meta` files with stable unique GUIDs.
- `Assets/Art/OriginalCombatSlice/frame-map.json` (+.meta) declares atlas frame order, pixel size, PPU, foot anchors and licenses.
- `Assets/Scripts/Research/CombatSpritePresenter.cs` (+.meta): loads each atlas and slices Sprites at runtime, independently draws parallax 2.5D ruins and shadows, selects real attack poses from `ThreeHitReferenceClock.FrameKey`, selects enemy states from `Combat004Enemy`, per-frame/flipX foot-anchor alignment, retains hitstop pose timing, temporary attack-trail VFX. **No duplicate hit/damage calculations**.
- `Assets/Scripts/Research/ResearchCombat004Scene.cs`: visual-only integration; removes calls to old block artwork and floor primitives, creates art SpriteRenderer children, changes default HUD to minimal bar text with F1/debug toggle; retains COMBAT007 responsive `Combat007Layout` and multi-touch.
- `tests/ArtSlice001/`: real standalone .NET 8 PNG zlib-decompression and per-asset frame/hash import/.meta/pivot/scene/static code checks.
- Source/rights/sha256 ledger: `docs/research/visual-assets-manifest.md`. Test stdout/hash: `docs/research/evidence/ART-SLICE-001-assets-and-dotnet8-20261022.txt`. Actual frame comparison: `docs/research/evidence/original-art-contact-sheet.png` (NOT Unity screenshot).

## Real executed evidence
- Authorized hk-relay-01, Ubuntu 24.04, .NET 8 SDK 8.0.426 Runtime 8.0.32, isolated temporary Git checkout.
- Final source-and-manifest tested SHA **`49651c71da1f1eb51c30b24b2e75be5baa6cbc40`**, later changes only evidence/docs.
- Art PNG/dependency test **7/7**, COMBAT007 **7/7**, COMBAT004/006 **11/11**, COMBAT002/005 **12/12**; **37/37 actual .NET 8 tests pass, all exit 0**. No Python simulation substitute.
- Actual assets decode as PNG indexed 8-bit, both runtime/resource variants same SHA, 19 mage visually distinct frames, enemy 10 unique of 12 slots; 3 independent background frames, 3 spell VFX.
- Art test log SHA-256 `9996ef8bdb99aff7b6ffe40e911d64a9802429afbcc649785f35b16b5c326572`. Full stdout and 3 other hashes in evidence TXT.

## User-facing improvements, strict limits and next request
1. New visually recognizable original long-haired blue-robed **staff mage** instead of old square sword avatar; Stage1 forward strike, Stage2 sweeping spell, Stage3 rising uppercut have different pixel silhouettes.
2. New shielded, horned forest **mossling** with independent hit/air/KO art instead of flat brown training blocks.
3. Original distant skyline/tree ruins/stone floor, ground shadows and three blue spell arcs; full debug overlay off by default. Prior COMBAT007 auto-landscape/safeArea/two-finger source preserved.
- **Unity Editor 6000.3.26f1 import/compile/PlayMode and new Android APK were not run**. Source candidate only. No GPU render screenshots, no actual mobile FPS or hit feedback certification. Android real phone results remain those of the OLD 043453... block-build.
- Creature has 12 mapped keys but only 10 unique pixel frames; visual quality is original technical slice, **not DNF-grade production art**.
- This branch intentionally does NOT add HitEvent APIs, apply extra HP damage, do a new OTG/getup/dedup implementation, or claim landed-hit sparks/audio. Await explicit integration contract from sibling `work/unity-hit-reaction-001-20261022` when 智脑2 authorizes an integration branch.
- If Unity Editor import is possible, validate actual TextureImporter settings for indexed PNGs, `Sprite.Create` sub-rect Y handling, all frame origins/flipX/shadows, mobile safeArea, core freeze pose hold, and initial display; then compare with old phone screenshot.
- No next build permitted without an independently approved manual-only Cloud job pinned to exact reviewed branch/SHA, Editor 6000.3.26f1, Android target/price limit. Never enable auto build.
- Rollback: select untouched COMBAT007 commit `fbeb2df3...` or prior successful build-only `043453...`; do not merge to main.

---

## Historical prior-stage handoff (not current branch execution)

# Active handoff — DNF-UNITY-COMBAT-007 (2026-10-22)

## Scope / user feedback / protection
- GitHub Issue: https://github.com/lgf921375/dnf-unity-mobile/issues/9; awaiting 智脑2 acceptance.
- Only writable branch `work/unity-combat-007-landscape-ui-20261022`; baseline first successful manual Android battle APK commit `0434533637bc7dacc63b8fbbd9b545019c27fb55`.
- Original Android build #3 (user-supplied evidence) installed and started battle scene, but **opened portrait**, distorting the IMGUI buttons/HUD. This is an Android phone defect report, not repeated in this task.
- Never touch `main` (`ae65b231f5b100ac46d71b8f3ddd3348ae948d5e`), Godot, Unity Cloud manual trigger mode, existing build-only baseline, or unauthorized assets.
- **No new Unity Cloud, GitHub Actions, Editor/Android build, APK** performed. Future single manual build requires separate explicit user approval.

## Delivered fix candidate
- `Assets/Editor/Combat007AndroidLandscapePrebuild.cs` (+ meta): Editor-only Android prebuild callback sets documented `PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft`; never starts a build. This is needed because `ProjectSettings/ProjectSettings.asset` is absent; do NOT fabricate that entire PlayerSettings file.
- `Assets/Scripts/Research/ResearchCombat004Scene.cs`: Android-only `Screen.orientation = LandscapeLeft` called once before scene load with Awake fallback, NOT each Update; safe-area IMGUI single uniform scale, full viewport responsive layout; touch controls moved from `GUI.Button/RepeatButton` to Unity `Input.GetTouch` polling in Update, independent fingers and no hold autorepeat.
- `Assets/Scripts/Research/Combat007Layout.cs` (+ meta): Unity-independent safeArea pixel layout, bottom-left touch pixel to top-left IMGUI map and pointer-frame reducer supporting two simultaneous fingers.
- `tests/Combat007`: real .NET 8 layout and gesture regression, including 1280×720, 2400×1080, 1920×1080, 1080×2400 portrait fallback; touch area bounds, safe notches and pinch-free left+attack, right+reset.

## Real execution evidence
- Authorized hk-relay-01, Ubuntu 24.04 linux-x64, .NET SDK 8.0.426 / runtime 8.0.32, private temporary checkout; tested **code SHA `1e460156cfee3513344993975d357e6a8f2ca678`**.
- COMBAT007 **7 PASS / 0 FAIL, exit 0**, SHA256 `a7c36a08909f822294b7390003e7f75d9fb10536dc715d617e27762b7b5e3b61`.
- Unmodified COMBAT004+006 **11 PASS / 0 FAIL**, SHA256 `51ac182ef7689233e025ca408ee840a63675141b2b3c480bcc6e2e13ba0b832f`.
- Unmodified COMBAT002+005 **12 PASS / 0 FAIL**, SHA256 `7425c820db63362dc9dca9e7a3dd5e5d558f9138d9b43e80a2d495f973f46f17`.
- **TOTAL 30 real .NET 8 PASS**. Test source and compilation are real; the Android/Unity-specific script is NOT compiled by this console harness. Full output, device, commands, results and limitations: [COMBAT-007-landscape-ui-dotnet8-20261022.txt](research/evidence/COMBAT-007-landscape-ui-dotnet8-20261022.txt).
- `EditorBuildSettings.asset` unchanged from successful build: index0 ResearchCombat004; index1 Bootstrap. No battle-core or timing edit. No DNF/Dawnbreak resources.

## NOT YET ACCEPTED
- `ResearchCombat004Scene.cs`, Android `Screen.orientation` runtime call, Editor prebuild callback and GUI drawing: **Unity Editor compile/PlayMode NOT RUN**, no available Editor.
- Real native orientation from Android cold boot with system auto-rotate OFF, Android cutout/safeArea, IMGUI view, simultaneous device touch and Bootstrap scene regression: **ANDROID NOT RUN** on newly fixed source.
- `ProjectSettings/ProjectSettings.asset` still missing. Unity Cloud PlayerSettings and Active Input Handling need evidence from a future, separately authorized manual build.
- After 智脑2 code review, future authorization must pin new exact branch/SHA, Editor 6000.3.26f1, Android target, manual-only build policy, available minutes/cost, and required startup/rotation/two-finger phone acceptance. DO NOT build before approval.
- Rollback is simple: reselect unchanged prior successful build-only SHA `0434533637bc7dacc63b8fbbd9b545019c27fb55`; do not merge or rewrite it.

---

## Historical parent (COMBAT-006; not current acceptance status)

# Active handoff — DNF-UNITY-COMBAT-006 (2026-10-22)

## Task and protected context
- Issue: https://github.com/lgf921375/dnf-unity-mobile/issues/7 — 智脑2 review outstanding.
- **Only writable branch**: `work/unity-combat-006-preflight-20261022`, branched from fixed `6e45289ade3ceb5d3c4eae3f459bd6c32f42b9fb`.
- `main` protected at `ae65b231f5b100ac46d71b8f3ddd3348ae948d5e`; no Godot modifications.
- **Unity Cloud Build Automation stays manually triggered. No automatic triggers, no build, no APK, no workflow, no cloud configuration change performed.**

## Verified execution
- Authorized device: hk-relay-01, Ubuntu 24.04, official isolated .NET 8 SDK 8.0.426 / Runtime 8.0.32. Actual C# run (not Python or Unity Editor).
- RED regression-only commit `2f657b42180cee1dcf3f2c229f51015ccb9bc8aa`: COMBAT004 tests **10 PASS, 1 FAIL, exit 1**. Failure: `Tick(0.12f)` was accepted, creating risk of skipped Stage1 hit.
- Minimal C# repair `0d14a205b835bc0e4ce3cce7e87652a6771bc02a`: reject oversized dt before any mutation in `Combat004Core.Tick(dt)`. Contract is fixed 60Hz core steps, **not** interval event queue.
- GREEN at code SHA `0d14a205b835bc0e4ce3cce7e87652a6771bc02a`: COMBAT004 **11 PASS, 0 FAIL, exit 0**, COMBAT002 **12 PASS, 0 FAIL, exit 0**, total **23 actual .NET cases**.
- RED log sha256 `65f4e8297c3cb72b164cb9ade812a8d549ffe45965f4b53e2b6e163e5d45ef48`.
- GREEN log sha256 `51ac182ef7689233e025ca408ee840a63675141b2b3c480bcc6e2e13ba0b832f`. Legacy log sha256 `7425c820db63362dc9dca9e7a3dd5e5d558f9138d9b43e80a2d495f973f46f17`.
- Complete real SDK info/stdout/stderr: [COMBAT-006-dotnet8-red-green-20261022.txt](research/evidence/COMBAT-006-dotnet8-red-green-20261022.txt).

## Confirmed prototype contracts / next phase
- 0.055s requested hitstop quantizes to 4×60Hz ticks ≈66.67ms; 0.09s quantizes to 6×60Hz =100ms. **Not exact 55/90ms freezes**. Frozen actors' combat-local action / buffer timers stop. UI keeps running.
- Facing and position freely steerable during unfrozen normal attacks; current facing at hit sampling decides hit. This is **an experimental rule**, not an official game attribute.
- Per-target hit ID must be stable and unique; back leniency `dx >= -0.10`, forward `dx <= 3.0`, lane width `abs(delta)<=0.65` are prototype rules. Collision remains a discrete directional hit matrix, NOT Unity Collider2D.
- Reversible, NOT APPLIED build-scene plan: [COMBAT-006-manual-mobile-preflight-20261022.md](research/COMBAT-006-manual-mobile-preflight-20261022.md). For later separately authorized manual build, use new build-only branch and put ResearchCombat004 at scene index 0. Do not auto-build.
- **STATIC ONLY** scene YAML and critical GUIDs appear matched; only Bootstrap remains enabled in BuildSettings; `ProjectSettings/ProjectSettings.asset` is currently absent; `ResearchCombat004Scene.cs` was never compiled in the standalone console project.
- **BLOCKED/NOT RUN** Unity 6000.3.26f1 Editor import/compile/PlayMode, `Sprite.Create` after `Texture2D.Apply(false,true)`, GUI/input/camera, Bootstrap PlayMode, real phone performance/touch, any APK.
- Next: 智脑2 review #7 and stage later Editor / explicitly authorized *manual* cloud build. Do not close #7 or change trigger mode without permission.

---

## Parent COMBAT-004 handoff (historical; separate branch and verification scope)

# Active handoff — COMBAT-004 visual scene candidate (2026-10-22)

## Fixed task/branch
- Task Issue: https://github.com/lgf921375/dnf-unity-mobile/issues/3 (leave OPEN for 智脑2).
- **Only approved writable branch:** `work/unity-combat-004-scene-20261022`, created from research checkpoint `4c94bc157cc3822fd779e08a5d20e59f97559116`.
- Protected main checked unchanged: `ae65b231f5b100ac46d71b8f3ddd3348ae948d5e`.
- No Unity Cloud, GitHub Actions, APK or Godot work was carried out.

## Delivered code
- Independent scene: `Assets/Scenes/ResearchCombat004.unity` (+ .meta), with one `ResearchCombat004Scene` behaviour; only open it explicitly in local Unity 6000.3.26f1 Editor.
- `Assets/Scripts/Research/ResearchCombat004Scene.cs` (+ .meta): programmer-authored in-memory sprites (nine variable-trim frames), keyboard/legacy IMGUI controls, combat HUD, SpriteRenderer flipX and foot anchor reapply, two unique enemy displays.
- `Assets/Scripts/Research/Combat004Core.cs`: 60Hz single-player frame-hit matrix, two targets with independent hit dedup, attacker+victim hitstop, local-time input buffering, stun, knockback and third-hit lift.
- `Assets/Scripts/MobileSmoke.cs`: branch-only, minimum Bootstrap-only guard to keep old smoke scene unchanged but prevent its global auto GUI/camera on ResearchCombat004.
- `Assets/Scripts/Research/ThreeHitReferenceClock.cs`: added readonly `QueuedFor` debug value.
- `tests/Combat004/Combat004.Tests.csproj` and `Runner.cs`: real .NET 8 test runner + UnityEngine stand-ins for math only.
- Editor testing guide: `docs/research/COMBAT-004-scene-guide-20261022.md`.
- Actual console test evidence TXT: `docs/research/evidence/COMBAT-004-dotnet8-20261022.txt`.

## Current actual acceptance status
- **PASS, ACTUAL .NET 8:** Ubuntu 24.04, SDK 8.0.426, real source compilation and tests: COMBAT002/005 **12/12**, COMBAT004 **7/7**, both exit 0, tested code SHA `a097d9647df0f526d1841c7e8e01d944cd570dcf`. Full stdout/hashes in evidence TXT.
- **PASS, STATIC:** new scene YAML references the generated script .meta GUID, old smoke's AfterSceneLoad guard matches `Bootstrap`, original Bootstrap scene and EditorBuildSettings remain unchanged.
- **NOT RUN (BLOCKED, no Editor on authorized runner):** actual Unity 6000.3.26f1 import/compilation/PlayMode, scene screenshot/video, real SpriteRenderer foot alignment, GUI, FPS and phone touch.
- **NOT RUN:** APK, cloud/CI/build, deployment.
- **NOT IMPLEMENTED:** network authoritative simulation, true swept Collider2D, official game/third-party art, per-attack PvP protection rules or production-grade effects.

## Required follow-on / independent acceptance
1. In a *local, user-authorized Unity 6000.3.26f1 installation* open the scene, save import/Console output; run PlayMode and capture display, attacker and two targets.
2. Verify timing, buffered combo, two independent targets, both freeze and resume, target lift/knock, nine frames, continuously switching flipX, pivot and local Z.
3. Open original `Bootstrap.unity` in PlayMode and confirm original smoke input/render still works.
4. Report measured Editor version, exact SHA, logs/screenshots and PASS/FAIL; any errors only repair in this work branch, then re-run real tests.
5. 智脑2 decides acceptance; no APK/cloud without separate user authorization.

---

## Historical parent research handoff (not the active COMBAT-004 task)

# Active handoff — DNF-UNITY-COMBAT-005 (2026-10-22, awaiting 智脑2)

**Issue:** https://github.com/lgf921375/dnf-unity-mobile/issues/4

## Real .NET 8 result (executed; do not confuse with Unity Editor)
- Environment: owner-authorized hk-relay-01, Ubuntu 24.04 linux-x64, isolated temporary private .NET SDK 8.0.426, runtime 8.0.32.
- Source baseline before new tests: `8a460cd7fce17cee93583d58cb142174b2049ba4`.
- RED regression test-only commit: `8ae40782e37d79f9e040c229bb05839bd716efa5`; actual `dotnet run --project tests/Combat002/Combat002.Tests.csproj --configuration Release`: **5 PASS / 7 FAIL / exit 1**.
- Failure categories: queued input crossing expiry, variable-dt time partition, skipped hit-window candidate, NaN/Infinity, shared mutable frame arrays, localPosition.z being erased; old frame test also failed because frame arrays were mutated by earlier test.
- Minimal repair commits: `01b636cbae3cbe4165ba5a70cf0a1293ed9a00bb` (clock) and `c7217e3f95b812186099c779f47232695c1601e8` (alignment). The test runner remains with six added regression groups.
- GREEN on **source `c7217e3f95b812186099c779f47232695c1601e8`**: real `.NET 8` compilation + tests **12 PASS / 0 FAIL / exit 0**. RED log SHA256 `0d8716b44fe7a527b82c83c7a2110d298b95d47177b29d8e6377a93e6cf4f828`; GREEN log SHA256 `7425c820db63362dc9dca9e7a3dd5e5d558f9138d9b43e80a2d495f973f46f17`.
- Evidence: [COMBAT-005-dotnet8-red-green-20261022.txt](research/evidence/COMBAT-005-dotnet8-red-green-20261022.txt). Contains exact environment, full stdout/stderr, exit codes, hashes, limitations and future contracts.
- Changed code files limited to `ThreeHitReferenceClock.cs`, `ReferenceSpriteAlignment.cs` and `tests/Combat002/Runner.cs` on research branch.
- **Clock contract:** finite variable dt supported; queue consumed at crossing instant, overshoot carried. Hit-window traversal is temporal eligibility, **NOT** swept collision, target-specific damage or full physical replay.
- **Main protected**: `ae65b231f5b100ac46d71b8f3ddd3348ae948d5e`, independently rechecked. No Godot changes, no cloud build/CI, no unauthorized assets.
- **Still NOT RUN / NOT IMPLEMENTED**: Unity 6000.3.26f1 Editor compile/import, actual flipX/cropped Sprite/PPU/parent scaling, Android APK, multi-target contact dedup, real hitstop and victim feedback, continuous physical collision. Tests use fake UnityEngine stand-ins.
- **Next:** 智脑2 review the red/green evidence in Issue #4; then independently decide whether to authorize COMBAT-004 real Unity scene tests. Do NOT close Issue here.

---

## Earlier handoff (historical; .NET BLOCKED status superseded by COMBAT-003 and COMBAT-005)

# Active handoff — DNF-UNITY-COMBAT-003 (2026-10-22)

## Current verified outcome — real .NET 8 test
- Issue: https://github.com/lgf921375/dnf-unity-mobile/issues/1 (leave open until 智脑2 review).
- Approved branch: `research/unity6-dawnbreak-threehit-20261021`.
- **Real test source commit:** `c5fcee2bfd8de11b29469ede55bc85f238b84eff`.
- **Verified**: .NET SDK 8.0.426 / Runtime 8.0.32, Ubuntu 24.04 linux-x64, owner-authorized HK temporary isolated checkout; real .NET 8 build/run completed with **6 PASS / 0 FAIL, exit 0**.
- Exact command: `dotnet run --project tests/Combat002/Combat002.Tests.csproj --configuration Release` (using privately installed SDK executable).
- Combined complete test stdout/stderr and SDK info: [COMBAT-003-dotnet8-run-20261022.txt](research/evidence/COMBAT-003-dotnet8-run-20261022.txt). Log SHA256: `066d382dd5ccb3865af9ba26d4b7ebaa109dbdebe95ae5f5fa8200d9c5324932`.
- Tests: idle/reset; frame change points; early queued 60Hz chain and no 4th hit; one registered contact per stage and sampled hit-window limits; negative Step rejected; 8 frame anchor records × both flipX directions (test-only `UnityEngine` stubs).
- Build created untracked `tests/Combat002/bin/` in the temporary checkout; **no tracked source file changed**. No actual code fix required.
- **Still not verified:** Unity 6000.3.26f1 Editor compile/import; real SpriteRenderer/animation and flip; gameplay hitstop/collision/reaction; Android. Test-only UnityEngine stand-ins cannot verify Unity.
- No modifications to main (verified `ae65b231f5b100ac46d71b8f3ddd3348ae948d5e`), Godot, Unity Cloud, CI workflows or third-party art. No APK built.
- Next action: 智脑2 independently assess issue/evidence, then authorize local Unity Editor sprite/PlayMode tests if appropriate; do **not** close Issue before review.

---

## Historical handoff (COMBAT-002; statuses below are superseded by the verified COMBAT-003 .NET outcome)

# Active handoff — isolated Unity research branch (historical)
Date: 2026-10-21
Task: DNF-UNITY-COMBAT-002

## Authoritative current state
- Main untouched at previously verified ae65b231f5b100ac46d71b8f3ddd3348ae948d5e.
- Isolated branch: research/unity6-dawnbreak-threehit-20261021.
- Prior isolated checkpoint: 945185fcdab86d953a18d9485084906c210fb32e.
- New work: flipX anchor correction; a standalone .NET 8 console test project that references real C# helper files; verification report.
- Report path: docs/research/COMBAT-002-verification-20261021.md.
- Standalone test entry: tests/Combat002/Combat002.Tests.csproj.
- No main merge, no Unity Cloud build, no Android APK, no original Godot changes, no third-party art.

## Acceptance status
- GitHub branch evidence and patch diff: VERIFIED.
- Local Python equivalent model/algebra: 8/8 PASS **not a real C# test**.
- Unity C# compile: NOT RUN (Unity/SDK absent).
- Standalone .NET console executable tests: NOT RUN (SDK absent).
- Frame rendering/foot anchor/flipX in real Unity scene: NOT RUN.
- Hitstop, victim reaction, physical collisions, target deduplication: NOT IMPLEMENTED.
- Android: NOT RUN.

## Next authorized small gate
Run `dotnet run --project tests/Combat002/Combat002.Tests.csproj -c Release` with SDK 8 in an authorized isolated checkout, record stdout+exit code. Next use Unity 6000.3.26f1 Editor to import and test original sprites; do not run cloud builds without new permission. Fix only branch-specific defects and append evidence. Never mark true runtime success based on test stubs or Python.
