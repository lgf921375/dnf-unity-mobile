// Experimental original combat values; do NOT claim official DNF timings.
// Units: seconds, game-world units, per-second velocities. Fixed-step 60Hz.
namespace DnfUnityMobile.Research
{
    public enum CombatReactionPhase
    {
        READY, HIT, AIR_RISE, AIR_FALL, DOWN, GETUP, INVUL, KO
    }
    public static class CombatReactionContract
    {
        public const float Gravity = 10f;
        public const float LaunchSpeed = 3.1f;
        public const float DownSeconds = 0.32f;
        public const float GetupSeconds = 0.16f;
        public const float InvulSeconds = 0.24f;
        public const float OtgInitialHeight = 0.08f;
        public const float OtgBounceSpeed = 1.05f;
    }
}
