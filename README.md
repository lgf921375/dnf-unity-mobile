# DNF-style Unity Mobile (original IP)

Unity 6 / Android proof-of-build for an **original-world** 2D action game. This public repo contains only original test code and procedural placeholder graphics. No DNF/Nexon assets or unlicensed third-party scripts.

**Status:** Unity project scaffold committed; Unity editor import, Unity Build Automation Android export, APK installation, gameplay and multiplayer performance **not yet verified**.

## Quick start

- Unity Editor version: `6000.3.26f1` (see `ProjectSettings/ProjectVersion.txt`). Select this exact version in Unity Build Automation if available.
- Unity Cloud project: `DNF-Unity-Mobile` (cloud project name is not the GitHub repository name).
- Repository: `lgf921375/dnf-unity-mobile`, branch `main`.
- Scene: `Assets/Scenes/Bootstrap.unity`, enabled in `ProjectSettings/EditorBuildSettings.asset`.
- At runtime, `Assets/Scripts/MobileSmoke.cs` creates a camera and uses minimal legacy IMGUI controls: LEFT / RIGHT / ATTACK / RESET.
- No external art, SDKs, plugins, servers, or Unity secrets are required by the committed source.

See [docs/cloud-build-guide.md](docs/cloud-build-guide.md) before using Unity Build Automation.

## Scope

This project is an **Android build smoke test**, not a DNF replica and not a 500-player benchmark. In particular, no authoritative server, multiplayer networking, damage reconciliation, or performance claim exists.

The old Godot repository `lgf921375/action-rpg-proto` is not modified by this work.

## Public repository safety

Do not commit Unity credentials, license files, signing keystores, tokens, proprietary assets or unreleased personal data. Cloud build integrations must handle credentials through their own secure configuration.
