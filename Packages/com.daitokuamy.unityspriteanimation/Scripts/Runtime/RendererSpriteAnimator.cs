using UnityEngine;

namespace UnitySpriteAnimation {
    /// <summary>
    /// SpriteRenderer に SpriteAnimation を適用するプレイヤー
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class RendererSpriteAnimator : SpriteAnimator {
        [SerializeField, Tooltip("通常表示に使用する SpriteRenderer")]
        private SpriteRenderer _spriteRenderer;

        private Color _spriteRendererBaseColor = Color.white;

        /// <inheritdoc />
        protected override bool CanUseFlipBookBlend => false;

        /// <inheritdoc />
        protected override void ApplySingleSprite(Sprite sprite) {
            EnsureComponents();
            ApplySpriteRendererState(_spriteRenderer, sprite, 1.0f, _spriteRendererBaseColor);
        }

        /// <inheritdoc />
        protected override void ApplyFlipBookBlend(Sprite fromSprite, Sprite toSprite, float fadeProgress) {
            ApplySingleSprite(toSprite);
        }

        /// <inheritdoc />
        protected override void AwakeInternal() {
            EnsureComponents();
            _spriteRendererBaseColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
        }

        /// <summary>
        /// Component の初期参照を補完する
        /// </summary>
        private void Reset() {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// 必要な Component 参照を補完する
        /// </summary>
        private void EnsureComponents() {
            if (_spriteRenderer == null) {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// SpriteRenderer の表示状態を設定する
        /// </summary>
        /// <param name="spriteRenderer">対象 SpriteRenderer</param>
        /// <param name="sprite">表示する Sprite</param>
        /// <param name="alpha">適用する Alpha</param>
        /// <param name="baseColor">基準となる Color</param>
        private static void ApplySpriteRendererState(SpriteRenderer spriteRenderer, Sprite sprite, float alpha, Color baseColor) {
            if (spriteRenderer == null) {
                return;
            }

            spriteRenderer.sprite = sprite;
            var color = baseColor;
            color.a = baseColor.a * Mathf.Clamp01(alpha);
            spriteRenderer.color = color;
            spriteRenderer.enabled = sprite != null && color.a > 0.0f;
        }
    }
}
