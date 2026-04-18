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
        private Material _runtimeMaterial;

        /// <inheritdoc />
        protected override bool CanUseFlipBookBlend => SupportsFlipBookBlendMaterial(GetSourceMaterial());

        /// <inheritdoc />
        protected override void ApplySingleSprite(Sprite sprite) {
            EnsureComponents();
            ApplySpriteRendererState(_spriteRenderer, sprite, 1.0f, _spriteRendererBaseColor);

            var material = GetRuntimeMaterial();
            if (SupportsFlipBookBlendMaterial(material)) {
                ResetFlipBookBlendMaterialProperties(material, sprite);
            }
        }

        /// <inheritdoc />
        protected override void ApplyFlipBookBlend(Sprite fromSprite, Sprite toSprite, float fadeProgress) {
            EnsureComponents();
            var material = GetRuntimeMaterial();
            if (!SupportsFlipBookBlendMaterial(material)) {
                ApplySingleSprite(toSprite);
                return;
            }

            ApplySpriteRendererState(_spriteRenderer, toSprite, 1.0f, _spriteRendererBaseColor);
            ApplyFlipBookBlendMaterialProperties(material, toSprite, fromSprite, fadeProgress);
        }

        /// <inheritdoc />
        protected override void AwakeInternal() {
            EnsureComponents();
            _spriteRendererBaseColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
        }

        /// <summary>
        /// 破棄時に生成済み Material を解放する
        /// </summary>
        private void OnDestroy() {
            if (_runtimeMaterial == null) {
                return;
            }

            if (Application.isPlaying) {
                Destroy(_runtimeMaterial);
                return;
            }

            DestroyImmediate(_runtimeMaterial);
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
        /// 元となる Material を取得する
        /// </summary>
        /// <returns>現在共有されている Material</returns>
        private Material GetSourceMaterial() {
            EnsureComponents();
            return _spriteRenderer != null ? _spriteRenderer.sharedMaterial : null;
        }

        /// <summary>
        /// FlipBookBlend に使う実体化済み Material を取得する
        /// </summary>
        /// <returns>Renderer 専用の Material</returns>
        private Material GetRuntimeMaterial() {
            EnsureComponents();
            if (_spriteRenderer == null) {
                return null;
            }

            if (_runtimeMaterial == null && SupportsFlipBookBlendMaterial(GetSourceMaterial())) {
                _runtimeMaterial = _spriteRenderer.material;
            }

            return _runtimeMaterial;
        }

        /// <summary>
        /// SpriteRenderer の表示状態を設定する
        /// </summary>
        /// <param name="spriteRenderer">対象 SpriteRenderer</param>
        /// <param name="sprite">表示する Sprite</param>
        /// <param name="alpha">適用する Alpha</param>
        /// <param name="baseColor">基準となる Color</param>
        private void ApplySpriteRendererState(SpriteRenderer spriteRenderer, Sprite sprite, float alpha, Color baseColor) {
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
