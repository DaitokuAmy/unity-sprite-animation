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
        private MaterialPropertyBlock _materialPropertyBlock;
        private Material _sourceMaterial;
        private Material _runtimeMaterial;

        /// <inheritdoc />
        protected override bool CanUseFlipBookBlend => SupportsFlipBookBlendMaterial(GetSourceMaterial());

        /// <inheritdoc />
        protected override void ApplySingleSprite(Sprite sprite) {
            EnsureComponents();
            ApplySpriteRendererState(_spriteRenderer, sprite, 1.0f, _spriteRendererBaseColor);

            if (Application.isPlaying) {
                var runtimeMaterial = GetRuntimeMaterial();
                if (SupportsFlipBookBlendMaterial(runtimeMaterial)) {
                    ResetFlipBookBlendMaterialProperties(runtimeMaterial, sprite);
                }
                return;
            }

            var sourceMaterial = GetSourceMaterial();
            if (SupportsFlipBookBlendMaterial(sourceMaterial)) {
                ResetFlipBookBlendPropertyBlock(sprite);
            }
            else {
                ClearPropertyBlock();
            }
        }

        /// <inheritdoc />
        protected override void ApplyFlipBookBlend(Sprite fromSprite, Sprite toSprite, float fadeProgress) {
            EnsureComponents();
            if (Application.isPlaying) {
                var runtimeMaterial = GetRuntimeMaterial();
                if (!SupportsFlipBookBlendMaterial(runtimeMaterial)) {
                    ApplySingleSprite(toSprite);
                    return;
                }

                ApplySpriteRendererState(_spriteRenderer, toSprite, 1.0f, _spriteRendererBaseColor);
                ApplyFlipBookBlendMaterialProperties(runtimeMaterial, toSprite, fromSprite, fadeProgress);
                return;
            }

            var sourceMaterial = GetSourceMaterial();
            if (!SupportsFlipBookBlendMaterial(sourceMaterial)) {
                ApplySingleSprite(toSprite);
                return;
            }

            ApplySpriteRendererState(_spriteRenderer, toSprite, 1.0f, _spriteRendererBaseColor);
            ApplyFlipBookBlendPropertyBlock(toSprite, fromSprite, fadeProgress);
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
            ReleaseRuntimeMaterial();
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
            if (_spriteRenderer == null) {
                return null;
            }

            if (_runtimeMaterial != null && _spriteRenderer.sharedMaterial == _runtimeMaterial) {
                return _sourceMaterial;
            }

            return _spriteRenderer.sharedMaterial;
        }

        /// <summary>
        /// FlipBookBlend に使う複製済み Material を取得する
        /// </summary>
        /// <returns>Renderer 専用の Material</returns>
        private Material GetRuntimeMaterial() {
            EnsureComponents();
            if (_spriteRenderer == null) {
                return null;
            }

            var sourceMaterial = GetSourceMaterial();
            if (!SupportsFlipBookBlendMaterial(sourceMaterial)) {
                ReleaseRuntimeMaterial();
                return null;
            }

            if (_runtimeMaterial != null && _sourceMaterial == sourceMaterial) {
                return _runtimeMaterial;
            }

            ReleaseRuntimeMaterial();

            _sourceMaterial = sourceMaterial;
            _runtimeMaterial = new Material(sourceMaterial) {
                hideFlags = HideFlags.HideAndDontSave,
                name = $"{sourceMaterial.name} (Clone)"
            };
            _spriteRenderer.sharedMaterial = _runtimeMaterial;
            return _runtimeMaterial;
        }

        /// <summary>
        /// MaterialPropertyBlock の初期参照を補完する
        /// </summary>
        private void EnsurePropertyBlock() {
            if (_materialPropertyBlock == null) {
                _materialPropertyBlock = new MaterialPropertyBlock();
            }
        }

        /// <summary>
        /// 複製済み Material を解放する
        /// </summary>
        private void ReleaseRuntimeMaterial() {
            if (_runtimeMaterial == null) {
                return;
            }

            if (_spriteRenderer != null && _spriteRenderer.sharedMaterial == _runtimeMaterial) {
                _spriteRenderer.sharedMaterial = _sourceMaterial;
            }

            if (Application.isPlaying) {
                Destroy(_runtimeMaterial);
            }
            else {
                DestroyImmediate(_runtimeMaterial);
            }

            _runtimeMaterial = null;
            _sourceMaterial = null;
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

        /// <summary>
        /// FlipBookBlend 用の property block を更新する
        /// </summary>
        /// <param name="currentSprite">現在表示する Sprite</param>
        /// <param name="previousSprite">遷移元 Sprite</param>
        /// <param name="fadeProgress">0.0-1.0 の補間率</param>
        private void ApplyFlipBookBlendPropertyBlock(Sprite currentSprite, Sprite previousSprite, float fadeProgress) {
            if (_spriteRenderer == null) {
                return;
            }

            EnsurePropertyBlock();
            _spriteRenderer.GetPropertyBlock(_materialPropertyBlock);
            MaterialUtility.ApplyProperties(_materialPropertyBlock, currentSprite, previousSprite, fadeProgress);
            _spriteRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        /// <summary>
        /// FlipBookBlend 用の property block を通常表示状態へ戻す
        /// </summary>
        /// <param name="currentSprite">現在表示する Sprite</param>
        private void ResetFlipBookBlendPropertyBlock(Sprite currentSprite) {
            if (_spriteRenderer == null) {
                return;
            }

            EnsurePropertyBlock();
            _spriteRenderer.GetPropertyBlock(_materialPropertyBlock);
            MaterialUtility.ResetProperties(_materialPropertyBlock, currentSprite);
            _spriteRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        /// <summary>
        /// MaterialPropertyBlock をクリアする
        /// </summary>
        private void ClearPropertyBlock() {
            if (_spriteRenderer == null) {
                return;
            }

            EnsurePropertyBlock();
            _materialPropertyBlock.Clear();
            _spriteRenderer.SetPropertyBlock(_materialPropertyBlock);
        }
    }
}
