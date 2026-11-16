using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySpriteAnimation.Editor {
    /// <summary>拡張レイヤーの選択と共通フレーム編集</summary>
    public sealed partial class SpriteAnimationClipEditorWindow {
        /// <summary>全レイヤーの Sprite 配列を取得する</summary>
        private List<SerializedProperty> GetLayerSpriteProperties() {
            var properties = new List<SerializedProperty> { _serializedClip.FindProperty("_sprites") };
            var layers = _serializedClip.FindProperty("_extensionSpriteLayers");
            for (var i = 0; i < layers.arraySize; i++) {
                properties.Add(layers.GetArrayElementAtIndex(i).FindPropertyRelative("_sprites"));
            }
            return properties;
        }

        /// <summary>全レイヤーの枠数を変更し、追加枠を None にする</summary>
        /// <param name="count">変更後の枠数</param>
        private void ResizeLayerFrames(int count) {
            foreach (var sprites in GetLayerSpriteProperties()) {
                var previousCount = sprites.arraySize;
                sprites.arraySize = count;
                for (var i = previousCount; i < count; i++) {
                    sprites.GetArrayElementAtIndex(i).objectReferenceValue = null;
                }
            }
        }

        /// <summary>奥から手前への描画順を取得する。主レイヤーは -1 で表す</summary>
        private List<int> GetLayerDrawOrder() {
            var order = new List<int>();
            if (_clip == null) {
                return order;
            }
            for (var i = 0; i < _clip.ExtensionSpriteLayers.Count; i++) {
                var layer = _clip.ExtensionSpriteLayers[i];
                if (layer != null && layer.Placement != SpriteAnimationLayer.LayerPlacement.Front) {
                    order.Add(i);
                }
            }
            order.Add(-1);
            for (var i = 0; i < _clip.ExtensionSpriteLayers.Count; i++) {
                var layer = _clip.ExtensionSpriteLayers[i];
                if (layer != null && layer.Placement == SpriteAnimationLayer.LayerPlacement.Front) {
                    order.Add(i);
                }
            }
            return order;
        }

        /// <summary>選択レイヤーを同じ配置先の隣へ移動する</summary>
        /// <param name="direction">手前へ移動する場合は 1、奥へ移動する場合は -1</param>
        private void MoveSelectedLayer(int direction) {
            if (_serializedClip == null || _selectedLayerIndex < 0 || (direction != 1 && direction != -1)) {
                return;
            }
            _serializedClip.Update();
            var layers = _serializedClip.FindProperty("_extensionSpriteLayers");
            if (_selectedLayerIndex >= layers.arraySize) {
                return;
            }
            var placement = layers.GetArrayElementAtIndex(_selectedLayerIndex).FindPropertyRelative("_placement").intValue;
            for (var i = _selectedLayerIndex + direction; i >= 0 && i < layers.arraySize; i += direction) {
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("_placement").intValue != placement) {
                    continue;
                }
                BeginClipEdit("Reorder Sprite Animation Layer", true);
                layers.MoveArrayElement(_selectedLayerIndex, i);
                _selectedLayerIndex = i;
                ApplyClipEdit();
                RefreshClipViews();
                return;
            }
        }

        /// <summary>選択レイヤーの配置先を変更する</summary>
        /// <param name="placement">変更後の配置先</param>
        private void SetSelectedLayerPlacement(SpriteAnimationLayer.LayerPlacement placement) {
            if (_serializedClip == null || _selectedLayerIndex < 0) {
                return;
            }
            BeginClipEdit("Change Sprite Animation Layer Placement", true);
            var layers = _serializedClip.FindProperty("_extensionSpriteLayers");
            if (_selectedLayerIndex >= layers.arraySize) {
                return;
            }
            layers.GetArrayElementAtIndex(_selectedLayerIndex).FindPropertyRelative("_placement").intValue = (int)placement;
            ApplyClipEdit();
            RefreshClipViews();
        }

        /// <summary>レイヤー選択と名前編集を更新する</summary>
        private void RefreshLayerToolbar() {
            if (_layerToolbar == null) {
                return;
            }
            _layerToolbar.Clear();
            _layerToolbar.style.flexDirection = FlexDirection.Row;
            _layerToolbar.style.alignItems = Align.Center;
            if (_serializedClip == null) {
                return;
            }
            _serializedClip.Update();
            var layers = _serializedClip.FindProperty("_extensionSpriteLayers");
            _selectedLayerIndex = Mathf.Clamp(_selectedLayerIndex, -1, layers.arraySize - 1);
            var names = new HashSet<string>();
            for (var i = 0; i < layers.arraySize; i++) {
                names.Add(layers.GetArrayElementAtIndex(i).FindPropertyRelative("_layerName").stringValue);
            }
            var order = GetLayerDrawOrder();
            var layerField = new PopupField<int>("Layer", order, order.IndexOf(_selectedLayerIndex), GetLayerSelectionLabel, GetLayerSelectionLabel);
            layerField.style.width = 210.0f;
            layerField.style.flexShrink = 0.0f;
            layerField.labelElement.style.minWidth = 36.0f;
            layerField.RegisterValueChangedCallback(evt => {
                _selectedLayerIndex = evt.newValue;
                ResetTimelineDragState();
                RefreshClipViews();
            });
            _layerToolbar.Add(layerField);
            _layerToolbar.Add(new Button(ShowAddLayerMenu) { text = "Add Layer" });

            if (_selectedLayerIndex < 0) {
                return;
            }
            var selected = layers.GetArrayElementAtIndex(_selectedLayerIndex);
            var nameField = new TextField("Name") {
                value = selected.FindPropertyRelative("_layerName").stringValue,
                isDelayed = true,
                tooltip = "Renaming requires updating the Animator binding. Names must be nonempty and unique.",
            };
            nameField.RegisterValueChangedCallback(evt => {
                var name = evt.newValue.Trim();
                if (string.IsNullOrEmpty(name) || names.Contains(name)) {
                    RefreshClipViews();
                    return;
                }
                BeginClipEdit("Rename Sprite Animation Layer", false);
                layers.GetArrayElementAtIndex(_selectedLayerIndex).FindPropertyRelative("_layerName").stringValue = name;
                ApplyClipEdit();
                RefreshClipViews();
            });
            nameField.style.width = 180.0f;
            nameField.style.minWidth = 100.0f;
            nameField.style.flexShrink = 1.0f;
            nameField.style.marginLeft = 8.0f;
            nameField.labelElement.style.minWidth = 40.0f;
            _layerToolbar.Add(nameField);
            var placement = (SpriteAnimationLayer.LayerPlacement)selected.FindPropertyRelative("_placement").intValue;
            var placementField = new EnumField(placement) { tooltip = "Preview Placement" };
            placementField.RegisterValueChangedCallback(evt => SetSelectedLayerPlacement((SpriteAnimationLayer.LayerPlacement)evt.newValue));
            placementField.style.width = 76.0f;
            placementField.style.flexShrink = 0.0f;
            _layerToolbar.Add(placementField);
            var forward = new Button(() => MoveSelectedLayer(1)) { text = "Forward", tooltip = "Bring Forward" };
            var backward = new Button(() => MoveSelectedLayer(-1)) { text = "Backward", tooltip = "Send Backward" };
            var groupOrder = new List<int>();
            foreach (var index in order) {
                if (index >= 0 && _clip.ExtensionSpriteLayers[index].Placement == placement) {
                    groupOrder.Add(index);
                }
            }
            forward.SetEnabled(groupOrder.IndexOf(_selectedLayerIndex) < groupOrder.Count - 1);
            backward.SetEnabled(groupOrder.IndexOf(_selectedLayerIndex) > 0);
            _layerToolbar.Add(forward);
            _layerToolbar.Add(backward);
            _layerToolbar.Add(new Button(() => {
                BeginClipEdit("Remove Sprite Animation Layer", true);
                layers.DeleteArrayElementAtIndex(_selectedLayerIndex);
                _selectedLayerIndex = -1;
                ApplyClipEdit();
                RefreshClipViews();
            }) { text = "Remove Layer" });
        }

        /// <summary>配置先付きのレイヤー選択名を取得する</summary>
        /// <param name="index">主レイヤーは -1、それ以外は拡張レイヤーの位置</param>
        private string GetLayerSelectionLabel(int index) {
            if (index < 0) {
                return "Main";
            }
            var layer = _clip.ExtensionSpriteLayers[index];
            return $"{layer.Placement}: {layer.LayerName}";
        }

        /// <summary>レイヤーの追加先を選択するメニューを表示する</summary>
        private void ShowAddLayerMenu() {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Back"), false, () => AddLayer(SpriteAnimationLayer.LayerPlacement.Back));
            menu.AddItem(new GUIContent("Front"), false, () => AddLayer(SpriteAnimationLayer.LayerPlacement.Front));
            menu.ShowAsContext();
        }

        /// <summary>指定した配置先へ空のレイヤーを追加する</summary>
        /// <param name="placement">追加先の配置</param>
        private void AddLayer(SpriteAnimationLayer.LayerPlacement placement) {
            if (_serializedClip == null || _clip == null) {
                return;
            }
            BeginClipEdit("Add Sprite Animation Layer", true);
            var layers = _serializedClip.FindProperty("_extensionSpriteLayers");
            var names = new HashSet<string>();
            for (var i = 0; i < layers.arraySize; i++) {
                names.Add(layers.GetArrayElementAtIndex(i).FindPropertyRelative("_layerName").stringValue);
            }
            var index = layers.arraySize;
            layers.arraySize++;
            var layer = layers.GetArrayElementAtIndex(index);
            var name = "Layer";
            var suffix = 2;
            while (names.Contains(name)) {
                name = $"Layer_{suffix++}";
            }
            layer.FindPropertyRelative("_layerName").stringValue = name;
            layer.FindPropertyRelative("_placement").intValue = (int)placement;
            var sprites = layer.FindPropertyRelative("_sprites");
            sprites.arraySize = _clip.FrameCount;
            for (var i = 0; i < sprites.arraySize; i++) {
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = null;
            }
            ApplyClipEdit();
            _selectedLayerIndex = index;
            RefreshClipViews();
        }

    }
}