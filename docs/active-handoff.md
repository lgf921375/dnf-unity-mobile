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
