# Unity Build Automation: first Android smoke build

## Prerequisites

1. Unity Cloud project `DNF-Unity-Mobile` already exists in organization `lgf921375`.
2. GitHub repository `lgf921375/dnf-unity-mobile` is public by the owner's choice.
3. The source repository contains a minimal Unity project, not a verified Android build.
4. Confirm available free build minutes, platform support and any payment prompt in Unity Cloud before triggering a build.

## Configure through Unity Cloud (not GameCI)

1. Open **Build Automation** for the Unity Cloud project.
2. Choose **Connect repository**, then GitHub. Authorize *only the intended repository* if the authorization UI supports selecting individual repositories.
3. Select `lgf921375/dnf-unity-mobile` and branch `main`. The Unity project directory is the repository **root** (`/`).
4. Create a build target for **Android**, development/debug APK if supported by the service.
5. Choose Unity Editor **6000.3.26f1**, matching `ProjectSettings/ProjectVersion.txt`. If not available, **stop and report** the versions offered; do not guess or silently upgrade.
6. Leave paid features and billing disabled until the owner reviews the cost. Do not initiate the first build without explicit approval.
7. On the first authorized run, save build ID, editor version, source commit SHA, error logs and the APK artifact link (if the build succeeds).
8. Download the artifact on Android; confirm package installs, launches in landscape, LEFT/RIGHT buttons move the cyan actor, ATTACK reduces red target HP when in range, RESET restores its initial state.

## Validation states

- **Scaffold available in GitHub**: verified by commit and file list.
- **Editor import / scene / C# compilation**: not verified until a real Unity editor build runs.
- **Unity Build Automation Android APK**: not verified until a real build reports success and an APK artifact exists.
- **Phone install and touch controls**: not verified until the owner tests on a real Android device.
- **500 real players on screen**: not implemented; a separate architecture and performance test is required.

## Security

Never commit Unity ID passwords, cloud tokens, secret build variables, signing keys, Play Store credentials, or proprietary art to this public repository.

## Reuse policy

All included sample graphics are generated procedurally (solid rectangles). No third-party or Nexon/DNF source code, audiovisual material or trademarks are imported.
