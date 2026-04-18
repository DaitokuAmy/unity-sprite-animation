using UnityEngine;
using UnityEngine.UI;

namespace UnitySpriteAnimation {
    /// <summary>
    /// Image に SpriteAnimation を適用するプレイヤー
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class ImageSpriteAnimator : SpriteAnimator {
        [SerializeField, Tooltip("通常表示に使用する Image")]
        private Image _image;

        private Color _imageBaseColor = Color.white;
        private Material _sourceMaterial;
        private Material _runtimeMaterial;

        /// <inheritdoc />
        protected override bool CanUseFlipBookBlend => SupportsFlipBookBlendMaterial(GetSourceMaterial());

        /// <inheritdoc />
        protected override void ApplySingleSprite(Sprite sprite) {
            EnsureComponents();
            ApplyImageState(_image, sprite, 1.0f, _imageBaseColor);

            var material = GetRuntimeMaterial();
            if (SupportsFlipBookBlendMaterial(material)) {
                ResetFlipBookBlendMaterialProperties(material, sprite);
                _image.SetMaterialDirty();
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

            ApplyImageState(_image, toSprite, 1.0f, _imageBaseColor);
            ApplyFlipBookBlendMaterialProperties(material, toSprite, fromSprite, fadeProgress);
            _image.SetMaterialDirty();
        }

        /// <inheritdoc />
        protected override void AwakeInternal() {
            EnsureComponents();
            _imageBaseColor = _image != null ? _image.color : Color.white;
        }

        /// <summary>
        /// 破棄時に生成済み Material を解放する
        /// </summary>
        private void OnDestroy() {
            if (_image != null && _runtimeMaterial != null && _image.material == _runtimeMaterial) {
                _image.material = _sourceMaterial;
                _image.SetMaterialDirty();
            }

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
            _image = GetComponent<Image>();
        }

        /// <summary>
        /// 必要な Component 参照を補完する
        /// </summary>
        private void EnsureComponents() {
            if (_image == null) {
                _image = GetComponent<Image>();
            }
        }

        /// <summary>
        /// 元となる Material を取得する
        /// </summary>
        /// <returns>現在明示的に設定されている Material</returns>
        private Material GetSourceMaterial() {
            EnsureComponents();
            if (_image == null) {
                return null;
            }

            if (_runtimeMaterial != null && _image.material == _runtimeMaterial) {
                return _sourceMaterial;
            }

            var material = _image.material;
            if (material == null || material == Graphic.defaultGraphicMaterial) {
                return null;
            }

            return material;
        }

        /// <summary>
        /// FlipBookBlend に使う複製済み Material を取得する
        /// </summary>
        /// <returns>Image 専用の Material</returns>
        private Material GetRuntimeMaterial() {
            EnsureComponents();
            if (_image == null) {
                return null;
            }

            var sourceMaterial = GetSourceMaterial();
            if (!SupportsFlipBookBlendMaterial(sourceMaterial)) {
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

            _image.material = _runtimeMaterial;
            _image.SetMaterialDirty();
            return _runtimeMaterial;
        }

        /// <summary>
        /// 複製済み Material を解放する
        /// </summary>
        private void ReleaseRuntimeMaterial() {
            if (_runtimeMaterial == null) {
                return;
            }

            if (_image != null && _image.material == _runtimeMaterial) {
                _image.material = _sourceMaterial;
                _image.SetMaterialDirty();
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
        /// Image の表示状態を設定する
        /// </summary>
        /// <param name="image">対象 Image</param>
        /// <param name="sprite">表示する Sprite</param>
        /// <param name="alpha">適用する Alpha</param>
        /// <param name="baseColor">基準となる Color</param>
        private void ApplyImageState(Image image, Sprite sprite, float alpha, Color baseColor) {
            if (image == null) {
                return;
            }

            image.sprite = sprite;
            var color = baseColor;
            color.a = baseColor.a * Mathf.Clamp01(alpha);
            image.color = color;
            image.enabled = sprite != null && color.a > 0.0f;
        }
    }
}
