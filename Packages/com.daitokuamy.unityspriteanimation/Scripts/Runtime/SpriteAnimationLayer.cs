using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnitySpriteAnimation {
    /// <summary>主レイヤーと同じフレーム枠を持つ拡張レイヤー</summary>
    [Serializable]
    public sealed class SpriteAnimationLayer {
        /// <summary>主レイヤーを基準とする Preview の配置先</summary>
        public enum LayerPlacement {
            /// <summary>主レイヤーより奥に描画する</summary>
            Back = 0,
            /// <summary>主レイヤーより手前に描画する</summary>
            Front = 1,
        }

        [SerializeField, Tooltip("表示先との対応に使用する一意の名前")]
        private string _layerName;
        [SerializeField, Tooltip("Preview で主レイヤーの手前または奥に配置する。実際の表示先の描画順は変更しない")]
        private LayerPlacement _placement;
        [SerializeField, Tooltip("主レイヤーと同じフレーム数の Sprite 一覧")]
        private Sprite[] _sprites = Array.Empty<Sprite>();

        /// <summary>表示先との対応に使用する名前</summary>
        public string LayerName => _layerName;
        /// <summary>主レイヤーを基準とする Preview の配置先</summary>
        public LayerPlacement Placement => _placement;
        /// <summary>フレームごとの Sprite 一覧</summary>
        public IReadOnlyList<Sprite> Sprites => _sprites;

        /// <summary>指定フレームの Sprite を取得する</summary>
        /// <param name="frameIndex">フレーム番号</param>
        public Sprite GetSprite(int frameIndex) {
            return _sprites != null && frameIndex >= 0 && frameIndex < _sprites.Length ? _sprites[frameIndex] : null;
        }

        /// <summary>主レイヤーにフレーム数を揃え、名前を正規化する</summary>
        /// <param name="frameCount">主レイヤーのフレーム数</param>
        /// <param name="usedNames">使用済みの名前</param>
        internal void Validate(int frameCount, HashSet<string> usedNames) {
            _layerName = string.IsNullOrWhiteSpace(_layerName) ? "Layer" : _layerName.Trim();
            var baseName = _layerName;
            var suffix = 2;
            while (!usedNames.Add(_layerName)) {
                _layerName = $"{baseName}_{suffix++}";
            }
            if (_sprites == null || _sprites.Length != frameCount) {
                Array.Resize(ref _sprites, frameCount);
            }
        }
    }
}