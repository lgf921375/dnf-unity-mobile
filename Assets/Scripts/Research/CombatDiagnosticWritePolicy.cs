// Android startup diagnostics: bounded, no-Unity side-effect admission policy.
// This pure C# class can be exercised by .NET tests without a Unity Editor.
// Caller owns the actual file write. No exception messages or paths are accepted.
using System;
using System.Collections.Generic;

namespace DnfUnityMobile.Research
{
    public sealed class CombatDiagnosticWritePolicy
    {
        public const int MaxCodes = 96;

        private readonly HashSet<string> _seen =
            new HashSet<string>(StringComparer.Ordinal);

        public int Count => _seen.Count;

        public bool TryBegin(string code)
        {
            CombatStartupTrace.Validate(code);
            if (_seen.Count >= MaxCodes) return false;
            return _seen.Add(code);
        }

        public void Reset()
        {
            _seen.Clear();
        }
    }
}
