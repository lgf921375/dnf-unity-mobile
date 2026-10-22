// ANDROID-BLANK-001: independent startup trace / fault-injection contract.
// No UnityEngine API: testable on .NET 8 without an Android/Editor build.
using System;
using System.Collections.Generic;

namespace DnfUnityMobile.Research
{
    public sealed class CombatStartupTrace
    {
        private readonly List<string> _stages = new List<string>();
        public bool Ready { get; private set; }
        public bool Failed { get; private set; }
        public string LastStage { get; private set; } = "PRE_SCENE";
        public string FaultStage { get; private set; } = "";
        public string FaultType { get; private set; } = "";
        public int Count => _stages.Count;

        // Stages contain only our literal identifiers and known asset keys.
        // Never store passwords, user directories, device IDs, arbitrary log lines,
        // or exception messages in the portable UI trace.
        public void Enter(string stage)
        {
            if (Failed) return;
            Validate(stage);
            LastStage = stage;
            if (_stages.Count >= 64) _stages.RemoveAt(0);
            _stages.Add(stage);
        }

        public void SetReady()
        {
            if (Failed) return;
            Ready = true;
            Enter("SCENE_READY");
        }

        public void Fail(string stage, string exceptionType)
        {
            if (Failed) return; // preserve the FIRST root cause
            Validate(stage);
            Validate(exceptionType);
            FaultStage = stage;
            FaultType = exceptionType;
            LastStage = "FAILED:" + stage;
            Failed = true;
            Ready = false;
            _stages.Add("FAILED:" + stage + ":" + exceptionType);
        }

        public string[] Snapshot() => _stages.ToArray();

        public static void Validate(string label)
        {
            if (string.IsNullOrEmpty(label) || label.Length > 78)
                throw new ArgumentException("Invalid diagnostic stage identifier");
            foreach (char c in label)
                if (!((c >= 'A' && c <= 'Z') ||
                      (c >= 'a' && c <= 'z') ||
                      (c >= '0' && c <= '9') ||
                       c == '_' || c == '-' || c == '.' || c == '/'))
                    throw new ArgumentException("Unsafe diagnostic stage identifier");
        }
    }
}
