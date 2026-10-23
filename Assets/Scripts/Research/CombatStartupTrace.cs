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
            // Exception.GetType().Name can contain a backtick for generic types.
            // Never record messages or paths; use a fixed fallback for unsafe names.
            string safeType = SafeExceptionTypeName(exceptionType);
            FaultStage = stage;
            FaultType = safeType;
            LastStage = "FAILED:" + stage;
            Failed = true;
            Ready = false;
            if (_stages.Count >= 64) _stages.RemoveAt(0);
            _stages.Add("FAILED:" + stage + ":" + safeType);
        }

        // The caller must pass only an exception TYPE name, never an exception
        // message. Unknown names are intentionally reduced to a fixed token.
        public static string SafeExceptionTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName) || typeName.Length > 78)
                return "UnknownException";
            string safe = typeName.Replace('`', '_');
            try { Validate(safe); return safe; }
            catch (ArgumentException) { return "UnknownException"; }
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
