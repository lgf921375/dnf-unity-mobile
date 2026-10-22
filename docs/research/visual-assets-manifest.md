# ART-SLICE-001 — Original Sprite asset ledger (2026-10-22)

**Owner / author:** Original procedurally drawn pixel artwork authored for this task by OpenAI's assistant; the drawing script generates the listed images from authored primitives/polygons, highlights and layered silhouettes. No Nexon/DNF sprites, Dawnbreak assets, music, source code or third-party character designs were imported. **Source classification:** newly generated project art; no external asset license required for the embedded PNG bytes. This is a rights/provenance statement about the resources created **in this task**, not a blanket warranty regarding later commercial game publication, original game's IP, trademark use or future additions.

**Asset format:** real PNG8 (indexed, 8-bit palette, transparency) imported as Texture2D. Each atlas contains genuinely authored frame pixels and is sliced at runtime with Unity `Sprite.Create(tex, Rect, pivot, PPU)` by `CombatSpritePresenter.cs`. The source-of-truth files are under `Assets/Art/OriginalCombatSlice/{Player,Enemy,Background,Vfx}`. For runtime portability, an **exact-byte mirrored copy** is included under `Assets/Resources/OriginalCombatSlice/{...}` with distinct, stable Unity `.meta` GUIDs. Unity reads the Resources copy in the Android scene; no runtime code draws rectangular placeholder characters.

| Actual PNG art source | SHA-256 (actual file bytes, verified on checkout) | Frames / content | Complete? | Status / PPU |
|---|---|---|---|---|
| `Player/mage_atlas.png` | `aa67028dcf720c7fb440da32943cc60f5abc04d0494fb908429f276ff0bb6d21` | **19** frames: 3 idle, 4 walk, 3 thrust (`a1`), 3 cross-sweep (`a2`), 2 uppercut (`a3`), 4 additional hit/air/down/getup | Prototype subset (19 vs. 24–36 pose target) | 640×640 PNG, 5 columns, each 128×160, PPU **80** |
| `Enemy/mossling_atlas.png` | `01c6dce6b4b950d769c8cee4d762a9af50ac65976331fb6528183adcf1408033` | **12** frame slots: 3 idle, 2 walk, 2 hurt, 2 air, 2 down, 1 KO | Prototype subset (12 vs. 12–18 target), **10 unique pixel poses**: down0/down1/KO share art | 448×384 PNG, 4 cols, each 112×128, PPU **80** |
| `Background/landscape_atlas.png` | `89239a61a13b903af535f0bf3bb7a181a908610e265c4c876139d4d085583265` | **3** independent layers: original sky/mountains, ancient-tree ruins, stone-tiled grass floor | Scene backdrop v1 only | 640×1080 atlas, 1 column, each 640×360, PPU **40** |
| `Vfx/arc_atlas.png` | `a381f132395426dfd4125ab820e41959ad2112ee26b70f3d20e9e9510b7dd47c` | **3** stages: thrust beam, sweep crescent, rising staff magic arc | Temporary swing VFX, **not a verified on-hit spark** | 384×160, 3 cols, each 128×160, PPU **80** |
| `docs/research/evidence/original-art-contact-sheet.png` | `8568eae2190595e02d8dc9df2fd063f03c89026c6db16e800ae31f553c04f089` | Static contact preview of actually committed mage/mossling frame pixels | **Not** a Unity screenshot; preview collage has some crowding/overlap of silhouettes | 960×550 PNG |

Path prefix for first four items: `Assets/Art/OriginalCombatSlice/`. Runtime mirrors use `Assets/Resources/OriginalCombatSlice/` with identical SHA-256 for each respective PNG. All above file hashes were obtained from an actual `sha256sum` on an authorized temporary Git checkout. Git object SHAs: mage `6e0a523adab3db2a88a13d24f8ce57a94d6d0c58`, enemy `04c6aad282cafbda864428dd1a1bb125a28bab28`, background `08351fb117295803d5f6e63e7e092a35533c51fe`, VFX `a5aca3fc5a3ed4080e51b582103b6bd2bb959ec8`, preview `2c2fa7e428f1542422db19fe530b6fe973b8fe75`. **SHA-256 ≠ Git SHA-1 object hash**; do not confuse them.

## Frame metadata and validation contract

Machine-readable index: `Assets/Art/OriginalCombatSlice/frame-map.json` (+ .meta).

- **Player** atlas 640×640, individual 128×160, 5 columns; key order left-to-right then top-to-bottom:
  `idle0 idle1 idle2 walk0 walk1 walk2 walk3 a1_1 a1_2 a1_3 a2_1 a2_2 a2_3 a3_1 a3_2 hit airUp down getup`. Centered import pivot is compensated with **(ax=48, ay=146)** in *top-left* pixel coordinates. **PPU=80**.
- **Mossling** atlas 448×384, individual 112×128, 4 columns:
  `idle0 idle1 idle2 walk0 walk1 hurt0 hurt1 air0 air1 down0 down1 ko0`. Anchor (ax=55, ay=118) from top-left, PPU=80.
- **Background** atlas top-to-bottom `sky ruins floor`, cell 640×360, PPU=40; independent SpriteRenderer sortingOrder behind all characters.
- **VFX** atlas `thrust sweep upper`, each 128×160, PPU=80; an attack trail driven by `Clock.Stage` and `Clock.IsHitWindow`. **The present frame layer must not treat a swing arc as a landed-hit event.** Per-target hit-spark and physical attack event integration await the independently managed hit-reaction branch.
- Unity atlas slicing from top-left row `r` maps to Unity's bottom-left Rectangle Y `atlasHeight - (r+1)*frameHeight`.
- `ReferenceSpriteAlignment.ApplyCenteredSprite` realigns the centered SpriteRenderer each render, including `flipX`; preserves child Z. Sprite/texture importer `.meta` files committed, no runtime File.ReadAllBytes or folder glob required.
- `Assets/Scripts/Research/CombatSpritePresenter.cs` only **reads** `Combat004Core.Clock.Stage/FrameKey`, `Combat004Enemy.Hp/Freeze/Stun/Height/Hits`; no damage or hitstop values are written in the presenter. Combo/damage still owned by original `Combat004Core` from COMBAT-007 branch.
- The 2.5D effect is parallax **layering plus floor shadow and enemy height offset**, not a complete Y-lane locomotion / Z sorting / swept Collider implementation. Each victim owns its own pose time and shadow; no multiplayer claim.
- If first Unity import rejects a PNG texture or `Sprite.Create`, this remains an Editor-specific open gate, **not proven PASS** by independent .NET tests.

## Exact evidence boundaries

- **Real PNG/asset verification:** system `file` detects the four PNG8 images; .NET 8 test decoder parses, zlib-decompresses and counts nontransparent cells, computes distinct SHA-256 per frame, checks art/runtime same SHA and corresponding `.meta` GUID uniqueness. **19 distinct character poses; 10 distinct of 12 monster slots; 3 distinct backgrounds; 3 distinct VFX**.
- **Existing gameplay and COMBAT-007 tests:** run as separate real .NET 8 console tests. These do not compile `CombatSpritePresenter.cs` or `ResearchCombat004Scene.cs`, so not Unity Editor acceptance.
- **No actual Unity 6000.3.26f1 Editor import/PlayMode or Android new APK** performed. No claims of actual GPU rendering, FPS, double-hit effects or finished DNF-grade art.
- Preview link: https://github.com/lgf921375/dnf-unity-mobile/blob/work/unity-art-slice-001-20261022/docs/research/evidence/original-art-contact-sheet.png
- **No audio asset** delivered: distinct hit/swing audio and grounded OTG/getup depend on the separate approved hit-event implementation. Do not claim complete sound design.
