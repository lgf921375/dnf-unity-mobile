// COMBAT-007: Editor-only mobile orientation preflight. This callback does NOT
// trigger a build; it runs only IF a user separately starts an authorized build.
// We cannot trust missing ProjectSettings/ProjectSettings.asset to persist defaults.
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class Combat007AndroidLandscapePrebuild : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        // Set the native Android launch orientation BEFORE Player generation.
        // Exact property API is in Unity 6000.3 PlayerSettings documentation.
        // Only the Android build path is authorized to apply this setting.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        Debug.Log("COMBAT-007 Android prebuild: default orientation = LandscapeLeft. " +
                  "Manual-only build authorization is still required.");
    }
}
