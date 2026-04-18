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

        /// <inheritdoc />
        protected override bool CanUseFlipBookBlend => false;

        /// <inheritdoc />
        protected override void ApplySingleSprite(Sprite sprite) {
            EnsureComponents();
            ApplyImageState(_image, sprite, 1.0f, _imageBaseColor);
        }

        /// <inheritdoc />
        protected override void ApplyFlipBookBlend(Sprite fromSprite, Sprite toSprite, float fadeProgress) {
            ApplySingleSprite(toSprite);
        }

        /// <inheritdoc />
        protected override void AwakeInternal() {
            EnsureComponents();
            _imageBaseColor = _image != null ? _image.color : Color.white;
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
        /// Image の表示状態を設定する
        /// </summary>
        /// <param name="image">対象 Image</param>
        /// <param name="sprite">表示する Sprite</param>
        /// <param name="alpha">適用する Alpha</param>
        /// <param name="baseColor">基準となる Color</param>
        private static void ApplyImageState(Image image, Sprite sprite, float alpha, Color baseColor) {
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
