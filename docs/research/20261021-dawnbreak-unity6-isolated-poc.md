# DNF-Unity / Dawnbreak Three-Hit Isolated POC — 2026-10-21

**Status:** Research completed for the three-hit reference; original Unity C# timing + alignment helpers committed **only to this isolated branch**. No scene wired, no C# compilation, no Unity/Android playtest, no third-party sprites imported, and no cloud build triggered. This is **not** an accepted combat replica.

## Scope and baseline

- Target: Unity `6000.3.26f1`; existing main smoke-test baseline `ae65b231f5b100ac46d71b8f3ddd3348ae948d5e`.
- This branch: `research/unity6-dawnbreak-threehit-20261021`, based on the unchanged main checkpoint.
- Source reference: `luzhipeng728/dawnbreak`, public repository, source browser default branch `main`; reference commit observed during review: `84b3b01caab6056c06aa567f0e89a423e7c86043`.
- Do not modify main, original Godot repositories, remote builds or deployment without a separate acceptance step.

## License / material boundary

The reference repository has publicly readable JavaScript and assets and actively invites pull requests, but an explicit grant to reproduce/repackage its art and code commercially was **not confirmed**: `LICENSE`, `LICENSE.md`, `LICENSE.txt`, `COPYING`, `NOTICE` checks failed on main. The author calls this a personal fan-study involving DNF-themed settings. This is not a license. Commercial reuse and distribution of visual assets and any substantially copied code requires separate rights clearance; DNF character/skill/brand rights are an additional independent concern.

**Safe branch contents:** only original C# helper code, factual timing values, and documentation. No asset bytes, no renamed third-party WebP/PNG, no source-code translation, no artwork in APK or public repo.

**References:** 
- https://github.com/luzhipeng728/dawnbreak/blob/main/src/content/classes/sword.js
- https://github.com/luzhipeng728/dawnbreak/blob/main/src/content/sprites.js
- https://github.com/luzhipeng728/dawnbreak/blob/main/art/final/spr/sword/spr.json
- https://github.com/luzhipeng728/dawnbreak/blob/main/src/models/imgmodel.js
- https://github.com/luzhipeng728/dawnbreak/blob/main/docs/ART_PIPELINE.md
- https://github.com/luzhipeng728/dawnbreak/blob/main/README.md

## Static evidence: reference Sword normal attacks

Source `src/content/classes/sword.js` (SWORD_ACTS) and `src/content/sprites.js` (SPR_ANIMS.sword):

| stage | dur | chain window | hit window | on-hit freeze requested | frame keys and entry times |
| --- | --- | --- | --- | --- | --- |
| 1 | 0.32s | [0.12,0.32] | [0.06,0.11] | 0.055s | a1_1@0, a1_2@0.05, a1_3@0.12 |
| 2 | 0.34s | [0.12,0.34] | [0.06,0.11] | 0.055s | a2_1@0, a2_2@0.05, a2_3@0.14 |
| 3 | 0.46s | none | [0.12,0.18] | 0.09s | a3_1@0, a3_2@0.12 |

Extra context: third hit has stronger knockback/impact than first two; reference processes buffered/held attack input and transitions via named next attacks. **These values are specific to the reference project, not official DNF or verified mobile settings.** The research clock's 0.16s experimental input buffer is our own assumption; it must not be advertised as a Dawnbreak parameter.

## Image and anchors

- Reference asset tree: `art/final/spr/sword/<frame>.webp` plus `spr.json`. The sprite index has `res=1.8` and **121** entries; all eight normal-attack frame keys were present in `spr.json` during inspection. This is a **metadata check**, not an image-loading test.
- Frame metadata fields: `w`, `h`, `ax`, `ay`, `wpn.gx/gy/ang/len/front` and optional head/hand/cut fields.
- Example: `a1_1` w=150, h=195, ax=58.7, ay=195.3; `a3_1` w=155, h=203, ax=22.4, ay=202.6. The anchor varies dramatically from frame to frame.
- The reference JS draws each frame with an offset `(-ax,-ay)` after scale `1/res`, preserving the foot origin. Directly swapping centered sprites without compensating for these anchors will create visibly discontinuous character movement.
- Some `ay > h` by fractions of a pixel. **Do not clamp Unity Sprite pivot to [0,1] and lose this offset.** Use centered sprites and apply a child-renderer local offset based on the source anchor:
  `offsetX = (w/2 - ax)/pixelsPerUnit`, `offsetY = (ay - h/2)/pixelsPerUnit`.
- Suggested world-scale convention for a future authorized import: one reference-world pixel = 0.01 Unity unit, hence `pixelsPerUnit=1.8/0.01=180`. This is a **proposed mapping**, not a confirmed artistic match.
- Unity-supported texture import for WebP must be tested rather than assumed. After authorization, convert a working copy to PNG where needed, keep frame dimensions/padding unchanged, use Sprite Single / Full Rect / Center pivot, preserve alpha.

## New isolated code (not yet run)

- `Assets/Scripts/Research/ThreeHitReferenceClock.cs`: standalone C# timing reference with 60 Hz step, attack queue, three stages, frame-name lookup, and once-per-action contact window reporting; no collision, damage, movement, hitstop execution or asset access.
- `Assets/Scripts/Research/ReferenceSpriteAlignment.cs`: generic Unity SpriteRenderer center-pivot alignment using factual anchor values. Does not load or distribute images.
- Neither class modifies existing `MobileSmoke.cs` or `Bootstrap.unity`; simply building the main smoke scene will not demonstrate the new behavior.

## Minimum test plan (next authorized execution stage)

1. **Unity import / compilation:** Check out isolated branch using Unity `6000.3.26f1`; import and compile the new scripts with no errors. No cloud build until separately approved; record editor version and console output.
2. **C# state-machine deterministic tests:** Drive a clock at 60 Hz. Input `attack` → stage1; queue during the early frames → stage2 no earlier than 0.12s; similarly stage2→stage3; no automatic fourth stage. Verify a new run after reset.
3. **Hit-window boundaries:** Check that contact is only accepted once inside 0.06–0.11s for stages1/2, once inside 0.12–0.18s for stage3; no contact outside window. Confirm freeze values are reported, **not implemented**.
4. **Frame mapping:** Assert frame keys switch at the configured timestamps; no last-frame interpolation guess, and attack speed scaling needs its own later test.
5. **Anchor proof:** With **owned/licensed** test sprites of deliberately different trims, place a stationary foot reference and assert all frames keep the same origin after offset. Confirm horizontal flip and no one-frame vertical jump.
6. **Phone input and visuals:** Only after the prior gates, create an independent Unity test scene and Android build on this branch (by explicit approval). Use a clear owned test character—not DNF or unlicensed fan art—plus attack/collision debug overlay. Capture side-by-side 3-stage action, input buffer, hit effects and receiver reactions.
7. **Owner/architect decision:** Compare reproducible evidence with the original browser reference and adjust mechanics. Third-party art can only be introduced in a separately controlled, rights-cleared workflow.

## Stop conditions

- No license or written permission → no original Dawnbreak image/code reuse in GitHub or APK.
- No actual Unity import / editor logs → cannot say 'compiled'.
- No Android screen recording / logs → cannot say 'feels like DNF', passes hitstop or passes combo tests.
- Do not confuse a public GitHub repo with an open-source commercial-use license.
