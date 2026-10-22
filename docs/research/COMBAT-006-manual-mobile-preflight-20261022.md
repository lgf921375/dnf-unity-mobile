# DNF-UNITY-COMBAT-006 — Manual Unity phone-build preflight (2026-10-22)

Issue: https://github.com/lgf921375/dnf-unity-mobile/issues/7
Only write branch: `work/unity-combat-006-preflight-20261022`
Base: `6e45289ade3ceb5d3c4eae3f459bd6c32f42b9fb` from COMBAT-004
Unity Editor target: **6000.3.26f1**, NOT installed in the authorized HK execution host as previously established in EDITOR-001.
**No permission to invoke Unity Cloud, GitHub Actions or Android builds in this task. Cloud Build Automation must remain MANUAL; no commit, PR or branch creation should enable build triggers.**

## Verified C# decisions — experimental game rules, not official DNF
- `Combat004Core.Tick(dt)` is **fixed-60Hz-step-only** at the core API: finite nonnegative delta, **at most `FixedStep + 1e-6` seconds**. It rejects `Tick(.12f)` and `Tick(.20f)` before modifying any state. Callers must advance with repeated legal steps. This is different from `ThreeHitReferenceClock.Step`, which is a variable-dt, time-interval clock. This is deliberate: the core only resolves attack hit candidates for the final stage/window of a single Tick and has no event queue for multiple skipped stages.
- The test scene's `FixedUpdate` uses `Time.fixedDeltaTime = 1/60`; it naturally satisfies this core rule. A future render-frame Update caller MUST NOT call `Tick(Time.deltaTime)` directly or discard accumulated time. Use a bounded accumulator and repeat `FixedStep`, subject to freeze and maximum catch-up budget decided separately.
- When a hit requests **0.055 seconds**, current whole-tick quantization freezes 4 full 60Hz ticks, approximately **66.667ms**. **0.09 seconds** freezes 6 ticks, **100ms**. There is no fractional-delta compensation. The input buffer expires only with combat-local clock time, therefore DOES NOT decay during attacker hitstop; the victim's stun/push/lift also freeze for its local ticks. This is a documented prototype approximation, NOT exact 55ms or 90ms implementation.
- While unfrozen, `Move(axis,dt)` is allowed even during a normal-attack windup/active/recovery window. The attack-facing follows the last axis input; no facing lock. This choice is experimental; in `ResearchCombat004Scene.FixedUpdate`, movement/turn precedes hit sampling in the same step. Attack button is consumed in `Update/OnGUI` before the following fixed update. Later design can change the rule only with a separately approved scenario and tests.
- Rear-hit leniency intentionally accepts `dx >= -0.10` (forward range ends at `dx <= 3.0`), lateral lane `abs(lane diff) <= 0.65`; all in prototype world units. Boundaries are inclusive. This is a directional/range frame **matrix, not Collider2D**.
- Per-target dedup key is `(attackInstanceId,targetId,hitIndex=0)`; IDs **must be distinct and stable for one active attack**. Test passes target order swapped. Duplicate IDs, replacement mid-attack and actual swept physics contacts are **not supported contracts**.

## Actual .NET 8 regression evidence
See `docs/research/evidence/COMBAT-006-dotnet8-red-green-20261022.txt`.
Red (new regression-only SHA `2f657b42180cee1dcf3f2c229f51015ccb9bc8aa`): 10 pass / 1 fail / process exit 1, oversized .12 Tick accepted.
Green (minimal core SHA `0d14a205b835bc0e4ce3cce7e87652a6771bc02a`): COMBAT004 11 pass / 0 fail and COMBAT002 12 pass / 0 fail; both exit 0. Full .NET CLI info, complete stdout/stderr and SHA256 included in TXT.
Tests compile original combat C# and test-only UnityEngine math substitutes; **`ResearchCombat004Scene.cs` is not compiled by either console csproj.** No Unity Editor or actual phone rendering has been tested.

## Static Unity scene import check (no Editor, NOT a runtime failure claim)
- Existing scene `Assets/Scenes/ResearchCombat004.unity` GUID from `Assets/Scenes/ResearchCombat004.unity.meta`: `ce4286d4cab543c0b7c0ef56bbbd67e5`. Scene MonoBehaviour references script GUID `9db231ab58d2468599f4b5eb2c99fe03` from `ResearchCombat004Scene.cs.meta`. These two key serialized references were present on the base branch.
- Other plain C# research scripts lack checked-in `.cs.meta`; Unity Editor normally generates missing metas at local import. This is a reproducibility consideration (commit newly generated metas on an authorized branch if needed), not proven compile failure.
- `ProjectSettings/EditorBuildSettings.asset` currently contains ONLY `Bootstrap.unity` (GUID `4a4f795e2fcb4b2d8d1a6e5cf85d6a40`). It remains **unchanged in COMBAT-006**.
- `ProjectSettings/ProjectSettings.asset` was not present in branch when queried, so old Input Handling, Android orientation, scripting backend, graphics, package-resolution and install settings are NOT established. Inspect/repair in an approved Unity Editor session. Do not make unverified claims of phone compatibility.
- `ResearchCombat004Scene.cs` uses `Input.GetKey`, `GUI.Button/RepeatButton`, `Camera`, procedural `Texture2D` and `Sprite.Create`. Static sequence: `tex.SetPixels32(pixels)` → `tex.Apply(false,true)` (CPU buffer becomes non-readable) → `Sprite.Create(...,FullRect)`. Without running Editor, cannot claim this fails or passes; on actual Editor first inspect Sprite.Create and frame display. If a failure is observed, then test `tex.Apply(false,false)` as a **separately logged minimal change**; do not guess now.
- `MobileSmoke.cs` uses a Bootstrap-only `AfterSceneLoad` guard and is not modified here; direct Play of each scene is the known intended use, not dynamic multi-scene switching.
- `Assets/Scenes/ResearchCombat004.unity` remains independent of `Bootstrap.unity`. Nothing in this task changes camera, GUI, scene defaults or triggers build.

## Exact REVERSIBLE BuildSettings proposal — NOT APPLIED
Only **after** 智脑2 signs off AND the user independently authorizes the **specific manual cloud build** (branch, scene, target Editor 6000.3.26f1, Android target, budget/minutes and cost cap):
1. Confirm Unity Cloud Build Automation trigger mode is STILL **manual only**, with all automatic GitHub commit, PR, main, branch and scheduled triggers disabled. Do not change trigger settings except to preserve manual mode.
2. Prefer make a **further isolated build-only branch** from the independently reviewed COMBAT-006 SHA; never change or merge `main`. Its BuildSettings change would place `ResearchCombat004.unity` as **first enabled scene (build index 0)**, and leave Bootstrap as a second entry for regression if desired. This step is NOT taken now.
3. Proposed `ProjectSettings/EditorBuildSettings.asset` `m_Scenes` only (copy exact GUID values from tracked metas; do not hand-edit other Unity project settings without Editor acceptance):

```yaml
  m_Scenes:
  - enabled: 1
    path: Assets/Scenes/ResearchCombat004.unity
    guid: ce4286d4cab543c0b7c0ef56bbbd67e5
  - enabled: 1
    path: Assets/Scenes/Bootstrap.unity
    guid: 4a4f795e2fcb4b2d8d1a6e5cf85d6a40
```

   Or disable Bootstrap in the **build-only branch** if only one enabled scene is desirable, while preserving its file unchanged. Always verify the import and scene order in a REAL Unity Editor before authorizing a phone artifact.
4. In manually operated Unity Cloud UI, target ONLY that new isolated build-only branch, select Editor **6000.3.26f1** and Android development configuration; verify pricing/remaining minutes/credentials and no account charges. Do NOT press the manual Build button without the user's distinct consent for that specific run. No auto-trigger subscriptions or workflow files should be enabled.
5. On a future **separately authorized** build, archive exact source SHA, BuildSettings SHA, Editor log/compiler errors, build ID, time, cost and APK artifact URL. A compiled APK does not establish touchscreen/gameplay correctness: user must test on phone, record stage/hitstop/two enemies/facing/foot anchor/Bootstrap regression and include video/Console if available.

## Rollback
- Preferred: select previous COMBAT-004 branch `work/unity-combat-004-scene-20261022` without merging, or revert only the COMBAT-006 core guard and its new regression test on this branch.
- If build-only branch created later: delete/revert the BuildSettings-only commit or simply point the manual build target back to the original isolated branch. Never touch `main`, `Bootstrap.unity`, or Cloud Automation trigger mode.
- Stop and report if Unity Editor is unavailable or compiler errors are seen; this task deliberately does NOT trigger a build.
