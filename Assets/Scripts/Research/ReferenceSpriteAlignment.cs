// Original Unity helper for independently licensed/created sprites.
// No Dawnbreak images, JSON, or other third-party assets are bundled.
using System;
using UnityEngine;

namespace DnfUnityMobile.Research
{
    public static class ReferenceSpriteAlignment
    {
        // Source reference spr.json coordinates start at top-left: ax, ay can be
        // fractionally outside the image bounds. Avoid an out-of-range Sprite pivot
        // by creating/importing Sprite with centered pivot and offsetting its renderer.
        //
        // Set TextureImporter sprite pivot to Center and Mesh Type to Full Rect.
        // Keep one child SpriteRenderer per character, with its foot origin at (0, 0).
        public static Vector2 CenterPivotOffset(
            float widthPixels, float heightPixels, float anchorXFromLeft,
            float anchorYFromTop, float pixelsPerUnityUnit)
        {
            if (widthPixels <= 0f || heightPixels <= 0f || pixelsPerUnityUnit <= 0f)
                throw new ArgumentOutOfRangeException(nameof(pixelsPerUnityUnit));
            return new Vector2(
                (widthPixels * 0.5f - anchorXFromLeft) / pixelsPerUnityUnit,
                (anchorYFromTop - heightPixels * 0.5f) / pixelsPerUnityUnit);
        }

        public static void ApplyCenteredSprite(
            SpriteRenderer childRenderer, Sprite authorizedSprite,
            float widthPixels, float heightPixels, float ax, float ay,
            float pixelsPerUnityUnit)
        {
            if (childRenderer == null) throw new ArgumentNullException(nameof(childRenderer));
            if (authorizedSprite == null) throw new ArgumentNullException(nameof(authorizedSprite));
            childRenderer.sprite = authorizedSprite;
            Vector2 offset = CenterPivotOffset(widthPixels, heightPixels, ax, ay, pixelsPerUnityUnit);
            // SpriteRenderer.flipX mirrors pixels around their centered pivot, but does
            // not mirror childRenderer.transform.localPosition. Compensate the anchor.
            // Set flipX before calling; re-apply when facing changes. If instead the
            // character root has negative localScale.x, do not also set flipX.
            if (childRenderer.flipX)
                offset.x = -offset.x;
            childRenderer.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
        }
    }
}
