// COMBAT-007: Unity-independent, deterministic screen-safe 1280x720 layout math.
// Screen safeArea coordinates are BOTTOM-LEFT-origin pixels; IMGUI uses TOP-LEFT.
// Supports portrait fallback without stretching game/UI non-uniformly.
using System;

namespace DnfUnityMobile.Research
{
    public readonly struct Combat007Rect
    {
        public readonly float X, Y, Width, Height;
        public Combat007Rect(float x, float y, float width, float height)
        {
            X = x; Y = y; Width = width; Height = height;
        }
        public bool Contains(float x, float y) =>
            x >= X && x <= X + Width && y >= Y && y <= Y + Height;
        public bool Overlaps(Combat007Rect b) =>
            X < b.X + b.Width && b.X < X + Width &&
            Y < b.Y + b.Height && b.Y < Y + Height;
    }

    // One input-frame snapshot built from multiple simultaneous touch pointers.
    // "Down" means a new finger contact, so ATTACK/RESET cannot repeat on hold.
    public struct Combat007PointerFrame
    {
        public bool LeftHeld, RightHeld, AttackDown, ResetDown;
        public float MoveAxis => (RightHeld ? 1f : 0f) - (LeftHeld ? 1f : 0f);
    }

    // No mutable globals. Always recompute after rotation/resolution/safeArea change.
    public readonly struct Combat007Layout
    {
        public const float ReferenceWidth = 1280f;
        public const float ReferenceHeight = 720f;
        public readonly int PixelWidth, PixelHeight;
        public readonly float Scale, OriginX, OriginTopY, VirtualWidth, VirtualHeight;
        public readonly Combat007Rect SafePixelsTopLeft;
        public readonly Combat007Rect Hud, Reset, Left, Right, Attack, Hint;

        private Combat007Layout(int pxW, int pxH, float scale, float originX,
            float originTopY, float virtualW, float virtualH, Combat007Rect safe)
        {
            PixelWidth = pxW; PixelHeight = pxH; Scale = scale;
            OriginX = originX; OriginTopY = originTopY;
            VirtualWidth = virtualW; VirtualHeight = virtualH;
            SafePixelsTopLeft = safe;
            // Controls are anchored within the *safe* virtual viewport, not
            // a 1280-wide design panel stretched across an ultrawide phone.
            Hud = new Combat007Rect(24f, 16f, Math.Min(840f, virtualW - 220f), 218f);
            Reset = new Combat007Rect(virtualW - 180f, 26f, 148f, 70f);
            Left = new Combat007Rect(28f, virtualH - 142f, 166f, 110f);
            Right = new Combat007Rect(214f, virtualH - 142f, 166f, 110f);
            Attack = new Combat007Rect(virtualW - 210f, virtualH - 147f, 180f, 114f);
            Hint = new Combat007Rect(28f, 243f, Math.Min(960f, virtualW - 56f), 32f);
        }

        public static Combat007Layout Calculate(int screenWidth, int screenHeight,
            float safeX, float safeY, float safeWidth, float safeHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(screenWidth));
            // Invalid or transient safeArea while the platform rotates => full viewport.
            if (!IsFinite(safeX) || !IsFinite(safeY) ||
                !IsFinite(safeWidth) || !IsFinite(safeHeight) ||
                safeWidth <= 0f || safeHeight <= 0f)
            {
                safeX = 0f; safeY = 0f;
                safeWidth = screenWidth; safeHeight = screenHeight;
            }

            float left = Math.Max(0f, Math.Min(screenWidth, safeX));
            float bottom = Math.Max(0f, Math.Min(screenHeight, safeY));
            float right = Math.Max(left, Math.Min(screenWidth, safeX + safeWidth));
            float top = Math.Max(bottom, Math.Min(screenHeight, safeY + safeHeight));
            if (right - left < 1f || top - bottom < 1f)
            {
                left = 0f; bottom = 0f; right = screenWidth; top = screenHeight;
            }
            float width = right - left, height = top - bottom;
            float scale = Math.Min(width / ReferenceWidth, height / ReferenceHeight);
            float virtualWidth = width / scale;
            float virtualHeight = height / scale;
            return new Combat007Layout(screenWidth, screenHeight, scale, left,
                screenHeight - top, virtualWidth, virtualHeight,
                new Combat007Rect(left, screenHeight - top, width, height));
        }

        public static bool ShouldLockLandscapeOnAndroid(bool isAndroid) => isAndroid;

        public static bool NeedsLandscapeRotation(int screenWidth, int screenHeight)
            => screenHeight > screenWidth;

        // Input screen pixels are in Unity's BOTTOM-LEFT origin.
        // Draw commands are in virtual IMGUI TOP-LEFT coordinates.
        public void BottomLeftScreenToVirtual(float screenX, float screenY,
            out float uiX, out float uiY)
        {
            uiX = (screenX - OriginX) / Scale;
            uiY = (PixelHeight - screenY - OriginTopY) / Scale;
        }

        // Called for each active pointer independently. Pointer phases/positions
        // are captured by the platform adapter (Unity Update) but resolved here.
        public void AddPointer(ref Combat007PointerFrame frame,
            float screenX, float screenY, bool isDown, bool justBegan)
        {
            if (!isDown) return;
            float x, y;
            BottomLeftScreenToVirtual(screenX, screenY, out x, out y);
            if (Left.Contains(x, y)) frame.LeftHeld = true;
            if (Right.Contains(x, y)) frame.RightHeld = true;
            if (justBegan)
            {
                if (Attack.Contains(x, y)) frame.AttackDown = true;
                else if (Reset.Contains(x, y)) frame.ResetDown = true;
            }
        }

        public void VirtualToBottomLeftScreen(float uiX, float uiY,
            out float screenX, out float screenY)
        {
            screenX = OriginX + uiX * Scale;
            screenY = PixelHeight - (OriginTopY + uiY * Scale);
        }

        private static bool IsFinite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
    }
}
