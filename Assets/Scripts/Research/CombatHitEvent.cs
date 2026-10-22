// A readonly event emitted only after ONE successful HP mutation.
// Presenter/VFX/Audio consumption may NOT apply damage again.
namespace DnfUnityMobile.Research
{
    public readonly struct CombatHitEvent
    {
        public long Tick { get; }
        public int AttackInstance { get; }
        public int Stage { get; }
        public int TargetId { get; }
        public int Damage { get; }
        public float ContactX { get; }
        public float ContactLane { get; }
        public float ContactHeight { get; }
        public float RequestedFreeze { get; }
        public int PushDirection { get; }
        public float LaunchImpulse { get; }
        public bool IsOtg { get; }

        public CombatHitEvent(long tick, int attackInstance, int stage,
            int targetId, int damage, float contactX, float contactLane,
            float contactHeight, float requestedFreeze, int pushDirection,
            float launchImpulse, bool isOtg)
        {
            Tick = tick;
            AttackInstance = attackInstance;
            Stage = stage;
            TargetId = targetId;
            Damage = damage;
            ContactX = contactX;
            ContactLane = contactLane;
            ContactHeight = contactHeight;
            RequestedFreeze = requestedFreeze;
            PushDirection = pushDirection;
            LaunchImpulse = launchImpulse;
            IsOtg = isOtg;
        }
    }
}
