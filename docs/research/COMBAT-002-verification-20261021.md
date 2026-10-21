# DNF-UNITY-COMBAT-002 | isolated verification report | 2026-10-21

## Scope and immutable baseline
- Repo: lgf921375/dnf-unity-mobile
- Writable **research branch only**: research/unity6-dawnbreak-threehit-20261021
- Initial research checkpoint: 945185fcdab86d953a18d9485084906c210fb32e
- Main checkpoint at both preflight and previous review: ae65b231f5b100ac46d71b8f3ddd3348ae948d5e
- Unity target: 6000.3.26f1.
- Main, original Godot repository, Unity Cloud, and any remote server were NOT changed. No APK was built.

## Evidence and tool limits
- Read GitHub branch, contents, compare, prior research report, and both production helper sources.
- Container environment checked: `dotnet`, `csc`, `mcs`, `mono`, `msbuild`, and Unity Editor were not available.
- GitHub DNS was unavailable from the test container, preventing online toolchain installation or git clone. GitHub connector was available and used only against the research branch for the writes documented below.
- Consequently **there was no real C# compile, .NET executable test, Unity import, Unity playmode, Unity batchmode, Unity Test Framework run, or Android test** this round.
- An independently written Python behavioral translation plus algebraic checks ran locally. Result: 8/8 PASS for idle, frame order, 60Hz buffered chain, single consumed contact per stage, duration reset, rejected negative step, anchor invariance for both facings, and mathematically reproducing the original flipX error. **These are model checks, not C# or Unity execution.** Do not promote them to compiled/run acceptance.

## Static review
1. `ThreeHitReferenceClock.cs` stores a three-stage clock; Stage 0 = idle; durations 0.32/0.34/0.46s, first two chain windows from 0.12s; fixed-step intention 1/60s. Source code also implements a local *experimental* 0.16s input buffer; it is NOT an official Dawnbreak parameter.
2. Frame keys and active hit windows are configured. `TryRegisterContact` returns an eligible contact **at most once per attack stage** and requested freeze seconds. It does NOT apply collision, actual hitstop, damage, target-specific dedupe, victim feedback, or hitbox overlap.
3. Previous `ReferenceSpriteAlignment.ApplyCenteredSprite` always used the positive X offset. This is incorrect for `SpriteRenderer.flipX` because flipping pixels does not flip a child transform's localPosition. With frame a1_1 (width=150, ax=58.7, PPU=180), unflipped center offset is +0.0905556 Unity units while `flipX=true` requires -0.0905556. Without reversal the flipped anchor is displaced by ~0.181111 units. This is **geometric proof**, not a Unity render pass.
4. Isolated research branch now conditionally reverses offset.x when `childRenderer.flipX` is true. The helper must be invoked after changing flipX; root negative scale and SpriteRenderer.flipX should not be combined without deliberate testing.

## Files changed (research branch only)
- Modified: `Assets/Scripts/Research/ReferenceSpriteAlignment.cs` (flipX sign compensation and usage note).
- Added: `tests/Combat002/Combat002.Tests.csproj` (standalone .NET 8 console test project, not auto-imported into Unity).
- Added: `tests/Combat002/Runner.cs` (real production source files included at build time; minimal test-only UnityEngine substitute types for verifying pure alignment math).
- Added this report and research-branch active handoff.

## Acceptance matrix
| Test | Current status | Evidence/limit |
| --- | --- | --- |
| T1 Unity 6000.3.26f1 C# import/compile | NOT RUN, blocked by toolchain | No Unity Editor available |
| T2 three-hit state/queue/frame timing | STATIC CHECK + Python model 8/8 overall; executable test NOT RUN | `ThreeHitReferenceClock.cs`, `tests/Combat002/Runner.cs` |
| T3 single eligible contact per attack | STATIC CHECK + Python model; executable test NOT RUN | No physics/hit target integration |
| T4 Sprite anchor (8 metadata frames, both facings) | Algebraic model PASS; .NET stub and actual Unity SpriteRenderer NOT RUN | flipX correction remains Unity-unverified |
| T5 hitstop / victim reaction | NOT IMPLEMENTED | No damage/target/physics feedback layer |
| T6 Android gameplay/APK | NOT RUN; requires separate authorization | No Unity Cloud build |

## Reproducible next steps (no cloud build)
1. In an authorized isolated checkout of this **research branch**, first verify its HEAD and a clean working tree.
2. With .NET SDK 8 installed, run `dotnet run --project tests/Combat002/Combat002.Tests.csproj --configuration Release`. Save stdout, exit code, SDK version and commit SHA. The console project deliberately does **not** claim Unity import verification.
3. Using Unity Editor **6000.3.26f1** on an isolated checkout, open the project, capture full compile/import log and errors; then test an original/owned temporary 8-frame character with variable trim and both facings, including exact pixels-per-unit, frame changes, and mirrored alignment.
4. Stop and report on any compile/import errors. Fix only on isolated research branch. Unity Cloud or Android build needs NEW user authorization.
5. Evaluate duration overshoots, frame exact boundaries and opponent-specific contact once real environment exists; no success claim without logs and captures.

## Evidence links
- Source review: `Assets/Scripts/Research/ThreeHitReferenceClock.cs`, `Assets/Scripts/Research/ReferenceSpriteAlignment.cs`.
- Previous research: `docs/research/20261021-dawnbreak-unity6-isolated-poc.md`.
- Reference input data: https://github.com/luzhipeng728/dawnbreak/blob/84b3b01caab6056c06aa567f0e89a423e7c86043/src/content/classes/sword.js and https://github.com/luzhipeng728/dawnbreak/blob/84b3b01caab6056c06aa567f0e89a423e7c86043/art/final/spr/sword/spr.json
- No license for Dawnbreak art or original code was verified; no asset bytes were imported.
