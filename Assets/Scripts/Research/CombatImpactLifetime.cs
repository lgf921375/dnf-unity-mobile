// VISUAL-FIX-001 red test baseline: previously duplicated immediate-consumption
// lifetime logic, moved into a pure C# class for deterministic repro.
using System;
namespace DnfUnityMobile.Research
{
    public sealed class CombatImpactLifetime
    {
        private readonly float[] _remaining;
        private readonly float[] _duration;
        private readonly bool[] _firstRenderOpportunityPending;
        public int Capacity => _remaining.Length;
        public CombatImpactLifetime(int targets)
        {
            if (targets <= 0) throw new ArgumentOutOfRangeException(nameof(targets));
            _remaining = new float[targets];
            _duration = new float[targets];
            _firstRenderOpportunityPending = new bool[targets];
        }
        public void Begin(int index, float duration)
        {
            if (index < 0 || index >= Capacity || float.IsNaN(duration) ||
                float.IsInfinity(duration) || duration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(duration));
            _remaining[index] = duration;
            _duration[index] = duration;
            _firstRenderOpportunityPending[index] = true;
        }
        public void AdvanceRenderFrame(float dt)
        {
            if (dt < 0 || float.IsNaN(dt) || float.IsInfinity(dt))
                throw new ArgumentOutOfRangeException(nameof(dt));
            for (int i = 0; i < Capacity; ++i)
            {
                if (_remaining[i] <= 0f) continue;
                // A new impact can be activated during ANY number of FixedUpdate
                // calls before this Update/render. The entire first render-frame
                // update must not consume its lifetime, even if dt=0.25s.
                // The next frame and later consume the real elapsed dt.
                if (_firstRenderOpportunityPending[i])
                {
                    _firstRenderOpportunityPending[i] = false;
                    continue;
                }
                _remaining[i] = Math.Max(0f, _remaining[i] - dt);
            }
        }
        public bool Visible(int index) => _remaining[index] > 0f;
        public float Opacity(int index) =>
            _duration[index] > 0 ? _remaining[index] / _duration[index] : 0f;
        public void Reset()
        {
            Array.Clear(_remaining, 0, Capacity);
            Array.Clear(_duration, 0, Capacity);
            Array.Clear(_firstRenderOpportunityPending, 0, Capacity);
        }
    }
}
