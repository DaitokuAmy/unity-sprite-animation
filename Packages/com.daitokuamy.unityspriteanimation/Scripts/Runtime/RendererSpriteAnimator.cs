using UnityEngine;

namespace UnitySpriteAnimation {
    /// <summary>
    /// SpriteRenderer に SpriteAnimation を適用するプレイヤー
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class RendererSpriteAnimator : SpriteAnimator {
        [SerializeField, Tooltip("通常表示に使用する SpriteRenderer")]
        private SpriteRenderer _spriteRenderer;

        [SerializeField, Tooltip("FlipBookBlend の遷移元表示に使用する SpriteRenderer")]
        private SpriteRenderer _flipBookBlendSpriteRenderer;

        private Color _spriteRendererBaseColor = Color.white;

        /// <inheritdoc />
        protected override bool CanUseFlipBookBlend {
            get {
                EnsureComponents();
                return _spriteRenderer != null &&
                    _flipBookBlendSpriteRenderer != null &&
                    _spriteRenderer != _flipBookBlendSpriteRenderer;
            }
        }

        /// <inheritdoc />
        protected override void ApplySingleSprite(Sprite sprite) {
            EnsureComponents();
            SyncFlipBookBlendSpriteRendererSettings();
            ApplySpriteRendererState(_spriteRenderer, sprite, 1.0f, _spriteRendererBaseColor);
            ApplySpriteRendererState(_flipBookBlendSpriteRenderer, null, 0.0f, _spriteRendererBaseColor);
        }

        /// <inheritdoc />
        protected override void ApplyFlipBookBlend(Sprite fromSprite, Sprite toSprite, float fadeProgress) {
            EnsureComponents();
            if (!CanUseFlipBookBlend) {
                ApplySingleSprite(toSprite);
                return;
            }

            SyncFlipBookBlendSpriteRendererSettings();
            ApplySpriteRendererState(_spriteRenderer, toSprite, fadeProgress, _spriteRendererBaseColor);
            ApplySpriteRendererState(_flipBookBlendSpriteRenderer, fromSprite, 1.0f - fadeProgress, _spriteRendererBaseColor);
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
        /// FlipBookBlend 用 SpriteRenderer の見た目設定を同期する
        /// </summary>
        private void SyncFlipBookBlendSpriteRendererSettings() {
            if (_spriteRenderer == null || _flipBookBlendSpriteRenderer == null) {
                return;
            }

            _flipBookBlendSpriteRenderer.sharedMaterial = _spriteRenderer.sharedMaterial;
            _flipBookBlendSpriteRenderer.sortingLayerID = _spriteRenderer.sortingLayerID;
            _flipBookBlendSpriteRenderer.sortingOrder = _spriteRenderer.sortingOrder + 1;
            _flipBookBlendSpriteRenderer.flipX = _spriteRenderer.flipX;
            _flipBookBlendSpriteRenderer.flipY = _spriteRenderer.flipY;
            _flipBookBlendSpriteRenderer.drawMode = _spriteRenderer.drawMode;
            _flipBookBlendSpriteRenderer.size = _spriteRenderer.size;
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