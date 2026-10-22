# HIT-REACTION-001 | deterministic victim-state + HitEvent contract

Scope: original Unity-independent C# battle core, NOT Unity Editor, PlayMode, Android or measured DNF gameplay.

## Single HP authority, readonly events
- Combat004Core.Tick(dt) only accepts finite 0 <= dt <= 1/60+1e-6. Current 2.5D frame overlap is not Physics2D or swept Collider2D.
- A successful Combat004Enemy.ReceiveHit applies HP/Hits exactly once. Dedup identity is (AttackInstance, TargetId, HitIndex=0). Only AFTER that successful transaction does the core emit readonly struct CombatHitEvent.
- Event fields: Tick, AttackInstance, Stage, TargetId, Damage, ContactX, ContactLane, ContactHeight, RequestedFreeze, PushDirection, LaunchImpulse, IsOtg. HitEvents is a defensive read-only array snapshot of the CURRENT simulation tick; DrainHitEvents returns and clears. Tick, Reset, and a stage transition caused by PressAttack clear pending events; Presenter must consume on the same tick, before stepping or switching stage. This is not a persistent event journal.
- Distinct target IDs in the same hit tick receive independent records and HP changes. Future VFX and sound should route by TargetId. Target IDs must be unique; duplicate IDs are an unsupported input.

## Deterministic states, foot, OTG
- Reaction phase READY/HIT/AIR_RISE/AIR_FALL/DOWN/GETUP/INVUL/KO is exposed separately from legacy State strings (the latter still shows HITSTOP and AIR).
- GroundFootX/GroundFootLane describe floor placement; Height is visual vertical displacement, not an offset to logical foot X/lane.
- Third hit launch speed=3.1, gravity=10.0; height is clamped to zero at landing. DOWN lasts 0.32 seconds, then GETUP 0.16, INVUL 0.24, READY.
- Only a low stage 1 or 2, actual sampled forward overlap dx [-0.10,3.0], lane delta <=0.65 and fully grounded DOWN victim can land one OTG; it sets OtgConsumed, bounces from initial height 0.08 with vertical speed 1.05. After OTG bounce, enter GETUP then INVUL (no second DOWN opportunity); while bounced or protected, additional low attacks cannot hit.
- New stage3 launch resets OTG per-knockdown eligibility. HP==0 is KO terminal, cancelling movement and float. Reset reinstates clean targets, clears snapshot and tick.
- This is an ORIGINAL experimental tuning set, NOT official DNF movement/attack figures.

## Clock and build separation
- Keep ThreeHitReferenceClock three durations 0.32/0.34/0.46 seconds and stage limit 3. Preserve input buffer during attacker-local hitstop, and independent victim Freeze. 0.055s victim/attacker freeze => 4 ticks; 0.09s => 6 ticks, not exact unquantized time.
- Future #11 display layer: on fixed tick, read HitEvents once, use TargetId and Phase/Height/GroundFoot for original sprites. No scene, asset, ProjectSettings or Android build change in #12.
- Not validated: Unity presentation, animations, Android, real hitboxes/physics, original summoner attack speed, replay after missed Presenter ticks, target ID collisions, networking, high concurrency.

Evidence: docs/research/evidence/HIT-REACTION-001-dotnet8-red-green-20261022.txt
RED commit: 3afdc5620d103776aa8c6267799b9b1ad6331064
GREEN code commit: 7c77aad6e7aff8ca3b6120e4b642699de2c47a5a
