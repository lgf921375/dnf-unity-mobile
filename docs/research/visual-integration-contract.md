# VISUAL-INTEGRATION-001: implemented read-only visual contract (CANDIDATE)

Source branches: ART-SLICE #11 d918f01a61f7f060711c9330fedaead6a6a1636e; HIT-REACTION #12 verified code 7c77aad6e7aff8ca3b6120e4b642699de2c47a5a. Only the new work/unity-visual-integration-001-20261022 branch was written.

## Concrete Unity code path
ResearchCombat004Scene.FixedUpdate performs Move, Core.Tick(Time.fixedDeltaTime), and IMMEDIATELY passes Core.DrainHitEvents to CombatSpritePresenter.ConsumeHitEvents, then renders actors. This guarantees one consumption before the next simulation Tick or PressAttack clears event data. The sole HP mutation remains Combat004Enemy.ReceiveHit. Presenter does not compute extra HP or re-run collision.

CombatVisualAdapter is real Unity-independent C# compiled by VisualIntegration001 tests; it routes per-target events by unique target ID, blocks same-Tick replay, stale events, unknown/duplicate IDs, and maps stage 1 to thrust light, stage 2 to sweep light, stage 3 to upper heavy, and actual once-only low OTG to heavy sweep. It does NOT authorize OTG; the core checks grounded state, range and lane.

CombatSpritePresenter generates a separate SpriteRenderer for each of two existing enemy target IDs from original Vfx/arc_atlas PNG; uses HitEvent contact X/lane/height and push direction to place and mirror magic sparks, not e.Hits counter polling. Independent unscaled visual time decays the impact alpha while combat 60Hz local hitstop freezes attackers/victims; subtle original scenery tint pulse provides a light whole-screen contact feedback. No original SFX exists, so NO AUDIO was added or claimed.

## Original art, combat phase and foot semantics
Original unmodified source art and byte-identical Resources copies, per-art .meta and rights are documented in docs/research/visual-assets-manifest.md inherited from #11: mage_atlas (19 frames), mossling_atlas (12 slots / 10 unique), landscape_atlas (3 layers), arc_atlas (3 effects). No copyrighted DNF or Dawnbreak assets copied. Mage attacks 1/2/3 still use original Clock.FrameKey (a1/a2/a3).

CombatVisualAdapter.ChooseEnemyFrame maps HIT to hurt0/1, AIR_RISE to air0, AIR_FALL to air1, DOWN to down0/1, GETUP to down1 FALLBACK (enemy has no dedicated GETUP pose), INVUL to idle0 FALLBACK with blue tint, KO to ko0, READY to idle0/1/2. Victim Freeze pauses only its pose clock; reaction state remains visible. Enemy root rises by e.Height; child shadow receives -e.Height so the ground shadow stays on lane; ReferenceSpriteAlignment preserves pivot and flipX. This foot/graphics behavior is source-level, NOT actual Unity frame capture.

Unity .meta: new CombatHitEvent, CombatReactionState and CombatVisualAdapter have stable distinct GUIDs. Existing ART PNG/Resource metadata and BuildSettings (ResearchCombat004 index 0, Bootstrap index 1) unchanged. COMBAT007 landscape, safeArea and two-finger input code unchanged.

## Evidence and limitations
The exact GitHub tested SHA 6259b892c703fa7d0ad4c3e214d80ec99239ad63 passed 53/53 real .NET 8 tests: Combat002 12, Combat004 18, Combat007 7, ArtSlice001 7, VisualIntegration001 9. RED CS2001 exit1 and intermediate legacy ART assertion failure were saved and explained in evidence/VISUAL-INTEGRATION-001-dotnet8-20261022.txt.
The ART source assertion was updated only because source-of-truth phase mapping moved from Presenter to adapter; no original asset/anchor/touch checks removed.

Remaining P0 and NOT VERIFIED: Unity 6000.3.26f1 real import/compile/PlayMode; Sprite.Create atlas import and alpha/filtering; visual poses, HitEvent spark presentation on-screen and actual ground shadow; Android 16:9/20:9 cold start portrait lock, touch, device FPS and 15-30s screen recording; subjective action feel; no original audio yet. Do not treat a .NET test as actual mobile acceptance. Next manual cloud build requires explicit separate user approval for exact SHA/branch/Android target/cost; automation stays disabled.
