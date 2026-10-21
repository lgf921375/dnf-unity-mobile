# Active handoff — isolated Unity research branch
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
