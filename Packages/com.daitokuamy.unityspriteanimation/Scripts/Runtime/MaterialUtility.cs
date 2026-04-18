using UnityEngine;

namespace UnitySpriteAnimation {
    /// <summary>
    /// FlipBookBlend 用の Material property 操作を補助する Utility
    /// </summary>
    public static class MaterialUtility {
        private static readonly int PrevTexPropertyId = Shader.PropertyToID("_PrevTex");
        private static readonly int CurrentTexUVRectPropertyId = Shader.PropertyToID("_CurrentTexUVRect");
        private static readonly int PrevTexUVRectPropertyId = Shader.PropertyToID("_PrevTexUVRect");
        private static readonly int FlipBookBlendParamsPropertyId = Shader.PropertyToID("_FlipBookBlendParams");
        private static readonly Vector4 DefaultSpriteUVRect = new(0.0f, 0.0f, 1.0f, 1.0f);
        private static readonly Vector4 DisabledFlipBookBlendParams = new(0.0f, 1.0f, 0.0f, 0.0f);

        /// <summary>
        /// FlipBookBlend 用 property を持つ Material か判定する
        /// </summary>
        /// <param name="material">判定対象 Material</param>
        /// <returns>対応している場合 true</returns>
        public static bool SupportsMaterial(Material material) {
            if (material == null) {
                return false;
            }

            return material.HasProperty(PrevTexPropertyId) &&
                   material.HasProperty(CurrentTexUVRectPropertyId) &&
                   material.HasProperty(PrevTexUVRectPropertyId) &&
                   material.HasProperty(FlipBookBlendParamsPropertyId);
        }

        /// <summary>
        /// FlipBookBlend 用の Material property を更新する
        /// </summary>
        /// <param name="material">更新対象 Material</param>
        /// <param name="currentSprite">現在表示する Sprite</param>
        /// <param name="previousSprite">遷移元 Sprite</param>
        /// <param name="fadeProgress">0.0-1.0 の補間率</param>
        public static void ApplyProperties(Material material, Sprite currentSprite, Sprite previousSprite, float fadeProgress) {
            if (material == null) {
                return;
            }

            var previousTexture = previousSprite != null && previousSprite.texture != null
                ? previousSprite.texture
                : Texture2D.blackTexture;

            material.SetTexture(PrevTexPropertyId, previousTexture);
            material.SetVector(CurrentTexUVRectPropertyId, GetSpriteUVRect(currentSprite));
            material.SetVector(PrevTexUVRectPropertyId, GetSpriteUVRect(previousSprite));
            material.SetVector(
                FlipBookBlendParamsPropertyId,
                new Vector4(previousSprite != null && currentSprite != null ? 1.0f : 0.0f, Mathf.Clamp01(fadeProgress), 0.0f, 0.0f));
        }

        /// <summary>
        /// FlipBookBlend 用の Material property を通常表示状態へ戻す
        /// </summary>
        /// <param name="material">更新対象 Material</param>
        /// <param name="currentSprite">現在表示する Sprite</param>
        public static void ResetProperties(Material material, Sprite currentSprite) {
            if (material == null) {
                return;
            }

            material.SetTexture(PrevTexPropertyId, Texture2D.blackTexture);
            material.SetVector(CurrentTexUVRectPropertyId, GetSpriteUVRect(currentSprite));
            material.SetVector(PrevTexUVRectPropertyId, DefaultSpriteUVRect);
            material.SetVector(FlipBookBlendParamsPropertyId, DisabledFlipBookBlendParams);
        }

        /// <summary>
        /// Sprite の UV rect を取得する
        /// </summary>
        /// <param name="sprite">対象 Sprite</param>
        /// <returns>UV rect</returns>
        public static Vector4 GetSpriteUVRect(Sprite sprite) {
            if (sprite == null) {
                return DefaultSpriteUVRect;
            }

            var uv = sprite.uv;
            if (uv == null || uv.Length == 0) {
                return DefaultSpriteUVRect;
            }

            var min = uv[0];
            var max = uv[0];
            for (var i = 1; i < uv.Length; i++) {
                min = Vector2.Min(min, uv[i]);
                max = Vector2.Max(max, uv[i]);
            }

            return new Vector4(
                min.x,
                min.y,
                Mathf.Max(0.0f, max.x - min.x),
                Mathf.Max(0.0f, max.y - min.y));
        }
    }
}
