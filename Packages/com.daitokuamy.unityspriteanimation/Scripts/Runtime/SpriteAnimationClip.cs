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

        /// <summary>
        /// 指定時間に対応するフレーム番号を取得する
        /// </summary>
        /// <param name="time">経過時間</param>
        public int GetFrameIndex(float time) {
            if (!CanPlay) {
                return -1;
            }

            if (FrameCount == 1) {
                return 0;
            }

            if (_loop) {
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
            var frameIndex = GetFrameIndex(time);
            return frameIndex >= 0 ? _sprites[frameIndex] : null;
        }
    }
}
