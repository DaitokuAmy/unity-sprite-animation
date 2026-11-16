using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnitySpriteAnimation {
    /// <summary>拡張レイヤー名と Image または SpriteRenderer の対応</summary>
    [Serializable]
    public sealed class SpriteAnimationLayerBinding {
        [SerializeField, Tooltip("Clip の拡張レイヤーと一致する名前")]
        private string _layerName;
        [SerializeField, Tooltip("拡張レイヤーを表示する Image または SpriteRenderer")]
        private Component _target;

        private Material _sourceMaterial;
        private Material _runtimeMaterial;
        private Component _materialTarget;

        /// <summary>対応するレイヤー名</summary>
        public string LayerName => _layerName;
        /// <summary>表示先の Image または SpriteRenderer</summary>
        public Component Target => _target;

        /// <summary>対応 Material が設定されているか</summary>
        internal bool CanBlend => MaterialUtility.SupportsMaterial(GetSourceMaterial());

        /// <summary>表示先の Sprite と補間状態を更新する</summary>
        /// <param name="current">現在の Sprite</param>
        /// <param name="previous">遷移元 Sprite</param>
        /// <param name="progress">補間率</param>
        internal void Apply(Sprite current, Sprite previous, float progress) {
            if (_target is Image image) {
                image.sprite = current;
                image.enabled = current != null;
            }
            else if (_target is SpriteRenderer renderer) {
                renderer.sprite = current;
                renderer.enabled = current != null;
            }
            else {
                return;
            }
            var source = GetSourceMaterial();
            if (!MaterialUtility.SupportsMaterial(source)) {
                Release();
                return;
            }
            if (_runtimeMaterial == null || _sourceMaterial != source || _materialTarget != _target) {
                Release();
                _sourceMaterial = source;
                _materialTarget = _target;
                _runtimeMaterial = new Material(source) {
                    hideFlags = HideFlags.HideAndDontSave,
                    name = $"{source.name} (Layer Clone)",
                };
                if (_target is Image targetImage) {
                    targetImage.material = _runtimeMaterial;
                }
                else if (_target is SpriteRenderer targetRenderer) {
                    targetRenderer.sharedMaterial = _runtimeMaterial;
                }
            }
            if (previous != null && current != null && (!previous.packed || previous.packingMode != SpritePackingMode.Tight) && (!current.packed || current.packingMode != SpritePackingMode.Tight)) {
                MaterialUtility.ApplyProperties(_runtimeMaterial, current, previous, progress);
            }
            else {
                MaterialUtility.ResetProperties(_runtimeMaterial, current);
            }
            if (_target is Image dirtyImage) {
                dirtyImage.SetMaterialDirty();
            }
        }

        /// <summary>表示先の Material を復元して複製を解放する</summary>
        internal void Release() {
            if (_runtimeMaterial == null) {
                return;
            }
            if (_materialTarget is Image image && image.material == _runtimeMaterial) {
                image.material = _sourceMaterial;
            }
            else if (_materialTarget is SpriteRenderer renderer && renderer.sharedMaterial == _runtimeMaterial) {
                renderer.sharedMaterial = _sourceMaterial;
            }
            if (Application.isPlaying) {
                UnityEngine.Object.Destroy(_runtimeMaterial);
            }
            else {
                UnityEngine.Object.DestroyImmediate(_runtimeMaterial);
            }
            _runtimeMaterial = null;
            _sourceMaterial = null;
            _materialTarget = null;
        }

        /// <summary>現在の表示先の元 Material を取得する</summary>
        private Material GetSourceMaterial() {
            var material = _target is Image image ? image.material : (_target as SpriteRenderer)?.sharedMaterial;
            return _target == _materialTarget && _runtimeMaterial != null && material == _runtimeMaterial ? _sourceMaterial : material;
        }
    }
}