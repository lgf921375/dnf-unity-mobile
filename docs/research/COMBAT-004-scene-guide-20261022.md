# DNF-UNITY-COMBAT-004 — independent original-art Unity scene, 2026-10-22

Issue: https://github.com/lgf921375/dnf-unity-mobile/issues/3
Branch: `work/unity-combat-004-scene-20261022`
Source baseline: `4c94bc157cc3822fd779e08a5d20e59f97559116`
Target: Unity **6000.3.26f1**. Unity Editor availability was checked on the authorized runner but **no executable was found**. This scene **HAS NOT** been imported/compiled/played in Unity.

## Files and isolation

- `Assets/Scenes/ResearchCombat004.unity` and its stable .meta: standalone scene root with `ResearchCombat004Scene` MonoBehaviour. Open explicitly in Unity Editor. Do NOT add it to Android Build Settings or replace Bootstrap.
- `Assets/Scripts/Research/ResearchCombat004Scene.cs` (+ .meta): author-generated cyan/blue fighter and two warm-color enemies; generates Texture2D+Sprite assets in memory only (no third-party files); displays debugging HUD, touch GUI and keyboard input.
- `Assets/Scripts/Research/Combat004Core.cs`: deterministic 60Hz local simulation; explicit directional/range/lane **frame hit matrix** (no Collider2D yet); two target IDs; one hit per `(attackInstanceId,targetId,hitIndex=0)`; damage, hitstun, stage3 upward reaction; dual attacker-victim hitstop.
- `Assets/Scripts/Research/ThreeHitReferenceClock.cs`: read-only `QueuedFor` display added; COMBAT-005 buffer/variable-dt repairs retained on the newly isolated branch.
- `Assets/Scripts/MobileSmoke.cs`: only one minimal change: AfterSceneLoad initializer checks `SceneManager.GetActiveScene().name == "Bootstrap"`. This prevents its global camera+OnGUI from covering ResearchCombat004; the original Bootstrap scene and MobileSmoke behavior remain the same.
- `tests/Combat004/Combat004.Tests.csproj`, `Runner.cs`: .NET 8 executable tests of original C# pure combat core, original clock and alignment arithmetic using **test-only** UnityEngine stand-ins.
- `ProjectSettings/EditorBuildSettings.asset` left unchanged; ONLY Bootstrap remains enabled for build.

## Simulation / input / animation contract

- This is an *original, single-player proof-of-concept*. It is NOT production multiplayer, DNF or Dawnbreak code/art.
- Attack timeline sourced as factual reference timing only: stage1 duration 0.32 s, stage2 0.34 s, stage3 0.46 s; chain windows from 0.12 s; hit windows [0.06,0.11), [0.06,0.11), [0.12,0.18); hitstop requests 0.055, 0.055, 0.09 s. The 0.16 s queue is an **experimental** value, not an official game value.
- Inputs: A/D or ←/→ move and change facing; J/Space attack (repeat in/near each chain window for three stages); R reset. On-screen LEFT, RIGHT, ATTACK, RESET Unity legacy IMGUI buttons for local PlayMode testing.
- `Time.fixedDeltaTime=1/60` during this scene, restored on destroy. HUD shows stage, frame key, elapsed, queue time, hit window, attack instance, attacker hitstop, both victims' HP/state/freeze/hit count, total contacts, approximate FPS.
- Rule: a successful hit assigns freeze to attacker and **each** hit victim. While their combat-local freeze is positive, those actors' action/animation, movement and hurt/reaction time do not advance. Main UI/Update continues. Input received during the freeze is buffered and does not decay until the attack clock resumes. This is specific to the prototype and subject to design review.
- No global `Time.timeScale=0`; no UI/network clock freeze. Victims have independent freeze clocks, stun and stage3 upward velocity; knockback/height resume after freeze.
- A frame contact is eligible if target is in attacker's forward 3.0 world-unit sector and lane difference <=0.65. Each attack-stage instance can contact each unique target at most once. This is a **discrete frame hit matrix**, not swept continuous collision or Collider2D.
- The research clock's global `TryRegisterContact` is NOT used for two targets; per-target deduplication is owned by Combat004Core.
- Sprite art is made **at runtime** from colored rectangles for 9 different trimmed frame dimensions; no disk asset input, no Unity import pipeline. Explicit PPU=100, `Sprite.Create(... Center, FullRect)`; `ReferenceSpriteAlignment` moves child renderer according to varying frame `ax/ay`, preserves z=0.12, and is called every FixedUpdate after `flipX` update. Never combine child flipX with negative parent X scale.
- The icon/sword/frames are programmer art for testing only. The actual Unity play result and visual quality remain UNVERIFIED.

## Reproducible local Editor steps (NEVER trigger Unity Cloud / GitHub Actions)

1. Checkout **exact branch** `work/unity-combat-004-scene-20261022`, record HEAD, verify `main` remains protected. Do not cherry-pick or merge to main.
2. In local Unity Hub with Editor **6000.3.26f1**, open project root.
3. Inspect Editor Console for all imported scripts. Open `Assets/Scenes/ResearchCombat004.unity` and press Play. Verify only one camera named "COMBAT-004 local camera"; no Smoke Camera/Controller.
4. Use J (three timed presses) while both enemies are in forward range. Check stage1→2→3, correct changing silhouettes, enemy HP 100→90→70→40 each, hit counts 1→2→3 per enemy, separate freeze counters and stage3 upward reaction. The numbers here are prototype-specific.
5. During attacker freeze, press J and verify queue is held. Move during freeze and verify attacker displacement pauses; after freeze attack resumes.
6. Turn left while enemies remain right: no hits. Turn right again. Ensure no foot-origin pop through nine frames at 100 PPU, and `flipX` reapply handles continuous facing changes. Test actual SpriteRenderer localPosition.z, graphical feet positions, runtime FPS. Explicitly note real cropped/asset-imported sprites/other PPU still need Editor tests.
7. Stop Play and reopen `Assets/Scenes/Bootstrap.unity`: original MobileSmoke GUI and keyboard/touch behavior should still work unchanged.
8. Save Console output, screenshots/video, exact Editor version, import errors, frame captures and SHA. Mark PASS only when they actually run.
9. Do not build APK, trigger cloud builds, import copyrighted art, or deploy.

## Real non-Unity evidence and gaps

- Real `.NET 8` tests ran on authorized hk-relay-01 Ubuntu 24.04, SDK 8.0.426. On final source candidate `a097d9647df0f526d1841c7e8e01d944cd570dcf`: original COMBAT002 runner **12/12, exit 0**; new COMBAT004 runner **7/7, exit 0**. Full stdout and SHA256 are in `docs/research/evidence/COMBAT-004-dotnet8-20261022.txt`.
- The new 7th .NET test validates frame **anchor mathematics** for all 9 generated metadata frames × 3 PPU values × dynamic flipX changes, *with UnityEngine stubs only*. Does not prove rendered foot position or real Sprite import settings.
- Code statically checked scene YAML root/MonoBehaviour .meta GUID, and the Bootstrap-only guard was verified via grep on the actual checkout. **Unity file importer may still reveal errors!**
- NOT RUN/BLOCKED: Unity 6000.3.26f1 Editor compile/import, PlayMode, real renderer screenshot, mobile input, Android APK, stress testing.
- NOT IMPLEMENTED: backend networking, client reconciliation, actual swept Collider2D, full animation state blending, animation hitboxes with depth/occlusion, stage3 landing recovery.
- No Dawnbreak/DNF assets, audio or proprietary scripts included.

## Rollback / scope

If local Unity import fails: abandon/restore the isolated work branch or remove only added COMBAT004 scene/core/tests and revert the branch-specific `MobileSmoke` guard and clock queue getter. No changes to main or Godot are necessary. DO NOT merge until 智脑2 signs off after an actual Editor session.
