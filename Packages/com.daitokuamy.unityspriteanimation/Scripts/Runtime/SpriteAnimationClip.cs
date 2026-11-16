using System.Collections.Generic;
using UnityEngine;

namespace UnitySpriteAnimation {
    /// <summary>
    /// スプライトアニメーション再生用データ
    /// </summary>
    [CreateAssetMenu(fileName = "sprite_animation.asset", menuName = "Unity Sprite Animation/Sprite Animation Clip")]
    public sealed class SpriteAnimationClip : ScriptableObject {
        private const float MinFrameRate = 0.01f;

        [SerializeField, Tooltip("1秒あたりの再生フレーム数")]
        private float _frameRate = 12.0f;

        [SerializeField, Tooltip("再生順に並べる Sprite 一覧")]
        private Sprite[] _sprites;

        [SerializeField, Tooltip("主レイヤーに同期する拡張レイヤー")]
        private SpriteAnimationLayer[] _extensionSpriteLayers;

        [SerializeField, Tooltip("ループ再生するか")]
        private bool _loop = true;

        [SerializeField, Tooltip("フレーム遷移時に FlipBookBlend を行うか")]
        private bool _enableFlipBookBlend;

        [SerializeField, Tooltip("フレーム遷移時の FlipBookBlend 時間(秒)")]
        private float _flipBookBlendDuration = 0.05f;

        /// <summary>1秒あたりの再生フレーム数</summary>
        public float FrameRate => Mathf.Max(MinFrameRate, _frameRate);

        /// <summary>再生する Sprite 一覧</summary>
        public IReadOnlyList<Sprite> Sprites => _sprites;

        /// <summary>主レイヤーに同期する拡張レイヤー一覧</summary>
        public IReadOnlyList<SpriteAnimationLayer> ExtensionSpriteLayers => _extensionSpriteLayers ?? System.Array.Empty<SpriteAnimationLayer>();

        /// <summary>ループ再生するか</summary>
        public bool Loop => _loop;

        /// <summary>フレーム遷移時に FlipBookBlend を行うか</summary>
        public bool EnableFlipBookBlend => _enableFlipBookBlend;

        /// <summary>フレーム遷移時の FlipBookBlend 時間(秒)</summary>
        public float FlipBookBlendDuration => Mathf.Max(0.0f, _flipBookBlendDuration);

        /// <summary>総フレーム数</summary>
        public int FrameCount => _sprites?.Length ?? 0;

        /// <summary>1ループの再生時間</summary>
        public float Duration => FrameCount > 0 ? FrameCount / FrameRate : 0.0f;

        /// <summary>再生可能か</summary>
        public bool CanPlay => FrameCount > 0;

        /// <summary>名前に対応する拡張レイヤーを取得する</summary>
        /// <param name="layerName">大文字小文字を区別するレイヤー名</param>
        public SpriteAnimationLayer GetExtensionSpriteLayer(string layerName) {
            if (string.IsNullOrEmpty(layerName)) {
                return null;
            }
            foreach (var layer in ExtensionSpriteLayers) {
                if (layer != null && layer.LayerName == layerName) {
                    return layer;
                }
            }
            return null;
        }

        /// <summary>
        /// 指定時間に対応するフレーム番号を取得する
        /// </summary>
        /// <param name="time">経過時間</param>
        public int GetFrameIndex(float time) {
            return GetFrameIndex(time, _loop);
        }

        /// <summary>
        /// 指定時間に対応するフレーム番号を取得する
        /// </summary>
        /// <param name="time">経過時間</param>
        /// <param name="loop">ループ再生として評価するか</param>
        public int GetFrameIndex(float time, bool loop) {
            if (!CanPlay) {
                return -1;
            }

            if (FrameCount == 1) {
                return 0;
            }

            if (loop) {
                time = Mathf.Repeat(time, Duration);
            }
            else {
                time = Mathf.Clamp(time, 0.0f, Duration);
                if (time >= Duration) {
                    return FrameCount - 1;
                }
            }

            return Mathf.Clamp(Mathf.FloorToInt(time * FrameRate), 0, FrameCount - 1);
        }

        /// <summary>
        /// 指定時間に対応する Sprite を取得する
        /// </summary>
        /// <param name="time">経過時間</param>
        public Sprite GetSprite(float time) {
            return GetSprite(time, _loop);
        }

        /// <summary>
        /// 指定時間に対応する Sprite を取得する
        /// </summary>
        /// <param name="time">経過時間</param>
        /// <param name="loop">ループ再生として評価するか</param>
        public Sprite GetSprite(float time, bool loop) {
            var frameIndex = GetFrameIndex(time, loop);
            return frameIndex >= 0 ? _sprites[frameIndex] : null;
        }

        /// <summary>拡張レイヤーの名前とフレーム数を検証する</summary>
        private void OnValidate() {
            var usedNames = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var layer in ExtensionSpriteLayers) {
                layer?.Validate(FrameCount, usedNames);
            }
        }
    }
}
