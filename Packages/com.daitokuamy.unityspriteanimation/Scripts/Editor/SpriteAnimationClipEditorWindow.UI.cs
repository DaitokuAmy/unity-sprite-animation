using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySpriteAnimation.Editor {
    /// <summary>
    /// SpriteAnimationClipEditorWindow の UI 構築処理
    /// </summary>
    public sealed partial class SpriteAnimationClipEditorWindow {
        /// <summary>
        /// ツールバーを生成する
        /// </summary>
        /// <param name="root">追加先</param>
        private void BuildToolbar(VisualElement root) {
            var toolbar = new Toolbar();

            _clipField = new ObjectField {
                objectType = typeof(SpriteAnimationClip),
                allowSceneObjects = false,
                value = _clip,
            };
            _clipField.style.minWidth = 320.0f;
            _clipField.RegisterValueChangedCallback(evt => SetClip(evt.newValue as SpriteAnimationClip));
            toolbar.Add(_clipField);

            root.Add(toolbar);
        }

        /// <summary>
        /// 中央領域を生成する
        /// </summary>
        /// <param name="root">追加先</param>
        private void BuildMainArea(VisualElement root) {
            var mainArea = new VisualElement();
            mainArea.style.flexDirection = FlexDirection.Row;
            mainArea.style.flexGrow = 1.0f;

            var previewPanel = new VisualElement();
            previewPanel.style.flexGrow = 1.0f;
            previewPanel.style.marginRight = 8.0f;
            previewPanel.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            previewPanel.style.borderBottomWidth = 1.0f;
            previewPanel.style.borderTopWidth = 1.0f;
            previewPanel.style.borderLeftWidth = 1.0f;
            previewPanel.style.borderRightWidth = 1.0f;
            previewPanel.style.borderBottomColor = new Color(0.22f, 0.22f, 0.22f);
            previewPanel.style.borderTopColor = new Color(0.22f, 0.22f, 0.22f);
            previewPanel.style.borderLeftColor = new Color(0.22f, 0.22f, 0.22f);
            previewPanel.style.borderRightColor = new Color(0.22f, 0.22f, 0.22f);

            var previewHeader = new VisualElement();
            previewHeader.style.flexDirection = FlexDirection.Column;
            previewHeader.style.alignItems = Align.Stretch;
            previewHeader.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);

            var previewTitle = new Label("Preview");
            previewTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            previewTitle.style.paddingLeft = 8.0f;
            previewTitle.style.paddingRight = 8.0f;
            previewTitle.style.paddingTop = 6.0f;
            previewTitle.style.paddingBottom = 6.0f;
            previewHeader.Add(previewTitle);

            var previewScaleArea = new VisualElement();
            previewScaleArea.style.flexDirection = FlexDirection.Row;
            previewScaleArea.style.alignItems = Align.Center;
            previewScaleArea.style.justifyContent = Justify.FlexStart;
            previewScaleArea.style.paddingLeft = 8.0f;
            previewScaleArea.style.paddingRight = 8.0f;
            previewScaleArea.style.paddingTop = 6.0f;
            previewScaleArea.style.paddingBottom = 6.0f;
            previewScaleArea.style.backgroundColor = new Color(0.20f, 0.20f, 0.20f);
            previewScaleArea.style.borderTopWidth = 1.0f;
            previewScaleArea.style.borderTopColor = new Color(0.10f, 0.10f, 0.10f);

            var previewScaleTitle = new Label("Scale");
            previewScaleTitle.style.marginRight = 6.0f;
            previewScaleTitle.style.color = new Color(0.85f, 0.85f, 0.85f);
            previewScaleArea.Add(previewScaleTitle);

            _previewScaleSlider = new Slider(0.1f, 1.0f) {
                value = _previewScale,
            };
            _previewScaleSlider.style.width = 140.0f;
            _previewScaleSlider.style.marginRight = 6.0f;
            _previewScaleSlider.RegisterValueChangedCallback(evt => {
                _previewScale = evt.newValue;
                RefreshPreview();
            });
            previewScaleArea.Add(_previewScaleSlider);

            _previewScaleLabel = new Label($"{_previewScale:0.00}x");
            _previewScaleLabel.style.minWidth = 42.0f;
            _previewScaleLabel.style.color = new Color(0.90f, 0.90f, 0.90f);
            previewScaleArea.Add(_previewScaleLabel);

            previewHeader.Add(previewScaleArea);
            previewPanel.Add(previewHeader);

            _previewContainer = new IMGUIContainer(DrawPreviewGui);
            _previewContainer.style.flexGrow = 1.0f;
            previewPanel.Add(_previewContainer);

            _inspectorContainer = new VisualElement();
            _inspectorContainer.style.width = InspectorWidth;
            _inspectorContainer.style.flexShrink = 0.0f;
            _inspectorContainer.style.paddingLeft = 8.0f;
            _inspectorContainer.style.paddingRight = 8.0f;
            _inspectorContainer.style.paddingTop = 8.0f;
            _inspectorContainer.style.paddingBottom = 8.0f;
            _inspectorContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            _inspectorContainer.style.borderBottomWidth = 1.0f;
            _inspectorContainer.style.borderTopWidth = 1.0f;
            _inspectorContainer.style.borderLeftWidth = 1.0f;
            _inspectorContainer.style.borderRightWidth = 1.0f;
            _inspectorContainer.style.borderBottomColor = new Color(0.22f, 0.22f, 0.22f);
            _inspectorContainer.style.borderTopColor = new Color(0.22f, 0.22f, 0.22f);
            _inspectorContainer.style.borderLeftColor = new Color(0.22f, 0.22f, 0.22f);
            _inspectorContainer.style.borderRightColor = new Color(0.22f, 0.22f, 0.22f);
            BuildInspectorArea();

            mainArea.Add(previewPanel);
            mainArea.Add(_inspectorContainer);
            root.Add(mainArea);
        }

        /// <summary>
        /// インスペクタ領域を構築する
        /// </summary>
        private void BuildInspectorArea() {
            _inspectorContainer.Clear();

            _inspectorTitleLabel = new Label();
            _inspectorTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _inspectorTitleLabel.style.marginBottom = 8.0f;
            _inspectorContainer.Add(_inspectorTitleLabel);

            _inspectorRemoveButton = new Button(() => RemoveFrameAt(_selectedFrameIndex)) {
                text = "Remove",
            };
            _inspectorRemoveButton.style.marginBottom = 8.0f;
            _inspectorRemoveButton.style.alignSelf = Align.FlexStart;
            _inspectorContainer.Add(_inspectorRemoveButton);

            _inspectorHelpBox = new HelpBox("編集対象の SpriteAnimationClip を設定してください。", HelpBoxMessageType.Info);
            _inspectorContainer.Add(_inspectorHelpBox);

            _inspectorSpriteField = new ObjectField {
                objectType = typeof(Sprite),
                allowSceneObjects = false,
            };
            _inspectorSpriteField.style.marginTop = 4.0f;
            _inspectorSpriteField.style.marginBottom = 4.0f;
            _inspectorSpriteField.RegisterValueChangedCallback(evt => {
                if (_selectedFrameIndex < 0) {
                    return;
                }

                SetFrameSprites(_selectedFrameIndex, new[] { evt.newValue as Sprite });
            });
            _inspectorContainer.Add(_inspectorSpriteField);

            _inspectorEmptyLabel = new Label("No Sprite Assigned");
            _inspectorEmptyLabel.style.marginTop = 8.0f;
            _inspectorContainer.Add(_inspectorEmptyLabel);

            _inspectorNameLabel = new Label();
            _inspectorNameLabel.style.marginTop = 12.0f;
            _inspectorNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _inspectorContainer.Add(_inspectorNameLabel);

            _inspectorTextureSizeLabel = new Label();
            _inspectorTextureSizeLabel.style.marginTop = 4.0f;
            _inspectorContainer.Add(_inspectorTextureSizeLabel);

            _inspectorRectLabel = new Label();
            _inspectorRectLabel.style.marginTop = 2.0f;
            _inspectorContainer.Add(_inspectorRectLabel);

            _inspectorPivotLabel = new Label();
            _inspectorPivotLabel.style.marginTop = 2.0f;
            _inspectorContainer.Add(_inspectorPivotLabel);
        }

        /// <summary>
        /// タイムライン領域を生成する
        /// </summary>
        /// <param name="root">追加先</param>
        private void BuildTimelineArea(VisualElement root) {
            var timelinePanel = new VisualElement();
            timelinePanel.style.height = 220.0f;
            timelinePanel.style.flexShrink = 0.0f;
            timelinePanel.style.backgroundColor = new Color(0.13f, 0.13f, 0.13f);
            timelinePanel.style.borderBottomWidth = 1.0f;
            timelinePanel.style.borderTopWidth = 1.0f;
            timelinePanel.style.borderLeftWidth = 1.0f;
            timelinePanel.style.borderRightWidth = 1.0f;
            timelinePanel.style.borderBottomColor = new Color(0.22f, 0.22f, 0.22f);
            timelinePanel.style.borderTopColor = new Color(0.22f, 0.22f, 0.22f);
            timelinePanel.style.borderLeftColor = new Color(0.22f, 0.22f, 0.22f);
            timelinePanel.style.borderRightColor = new Color(0.22f, 0.22f, 0.22f);

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Column;
            header.style.alignItems = Align.Stretch;
            header.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);

            var title = new Label("Timeline");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.paddingLeft = 8.0f;
            title.style.paddingRight = 8.0f;
            title.style.paddingTop = 6.0f;
            title.style.paddingBottom = 6.0f;
            header.Add(title);

            var controls = new VisualElement();
            controls.style.flexDirection = FlexDirection.Row;
            controls.style.alignItems = Align.Center;
            controls.style.flexWrap = Wrap.Wrap;
            controls.style.justifyContent = Justify.FlexStart;
            controls.style.paddingLeft = 8.0f;
            controls.style.paddingRight = 8.0f;
            controls.style.paddingTop = 6.0f;
            controls.style.paddingBottom = 6.0f;
            controls.style.backgroundColor = new Color(0.20f, 0.20f, 0.20f);
            controls.style.borderTopWidth = 1.0f;
            controls.style.borderTopColor = new Color(0.10f, 0.10f, 0.10f);

            var totalFramesGroup = CreateHeaderFieldGroup("Total Frames", out var totalFramesLabel);
            _timelineTotalFramesField = new IntegerField("Total Frames");
            _timelineTotalFramesField.label = string.Empty;
            _timelineTotalFramesField.style.width = 56.0f;
            _timelineTotalFramesField.style.marginRight = 12.0f;
            _timelineTotalFramesField.RegisterValueChangedCallback(evt => {
                if (_clip == null || _serializedClip == null || _spritesProperty == null) {
                    return;
                }

                var nextCount = Mathf.Max(0, evt.newValue);
                if (nextCount == _spritesProperty.arraySize) {
                    return;
                }

                SetTotalFrameCount(nextCount);
            });
            totalFramesGroup.Add(totalFramesLabel);
            totalFramesGroup.Add(_timelineTotalFramesField);
            controls.Add(totalFramesGroup);

            var frameRateGroup = CreateHeaderFieldGroup("Frame Rate", out var frameRateLabel);
            _timelineFrameRateField = new FloatField("Frame Rate");
            _timelineFrameRateField.label = string.Empty;
            _timelineFrameRateField.style.width = 56.0f;
            _timelineFrameRateField.style.marginRight = 12.0f;
            _timelineFrameRateField.RegisterValueChangedCallback(evt => {
                if (_clip == null || _serializedClip == null || _frameRateProperty == null) {
                    return;
                }

                var nextFrameRate = Mathf.Max(0.01f, evt.newValue);
                if (Mathf.Approximately(nextFrameRate, _frameRateProperty.floatValue)) {
                    return;
                }

                BeginClipEdit("Change Sprite Animation Frame Rate", false);
                _frameRateProperty.floatValue = nextFrameRate;
                ApplyClipEdit();
                RefreshClipViews();
            });
            frameRateGroup.Add(frameRateLabel);
            frameRateGroup.Add(_timelineFrameRateField);
            controls.Add(frameRateGroup);

            var loopGroup = CreateHeaderFieldGroup("Loop", out var loopLabel);
            _timelineLoopToggle = new Toggle("Loop");
            _timelineLoopToggle.label = string.Empty;
            _timelineLoopToggle.style.marginRight = 12.0f;
            _timelineLoopToggle.RegisterValueChangedCallback(evt => {
                if (_clip == null || _serializedClip == null || _loopProperty == null) {
                    return;
                }

                BeginClipEdit("Toggle Sprite Animation Loop", false);
                _loopProperty.boolValue = evt.newValue;
                ApplyClipEdit();
                RefreshClipViews();
            });
            loopGroup.Add(loopLabel);
            loopGroup.Add(_timelineLoopToggle);
            controls.Add(loopGroup);

            var flipBookBlendGroup = CreateHeaderFieldGroup("Flip Book Blend", out var flipBookBlendLabel);
            _timelineFlipBookBlendToggle = new Toggle("Flip Book Blend");
            _timelineFlipBookBlendToggle.label = string.Empty;
            _timelineFlipBookBlendToggle.style.marginRight = 12.0f;
            _timelineFlipBookBlendToggle.RegisterValueChangedCallback(evt => {
                if (_clip == null || _serializedClip == null || _enableFlipBookBlendProperty == null) {
                    return;
                }

                BeginClipEdit("Toggle Sprite Animation FlipBookBlend", false);
                _enableFlipBookBlendProperty.boolValue = evt.newValue;
                ApplyClipEdit();
                RefreshClipViews();
            });
            flipBookBlendGroup.Add(flipBookBlendLabel);
            flipBookBlendGroup.Add(_timelineFlipBookBlendToggle);
            controls.Add(flipBookBlendGroup);

            var flipBookBlendDurationGroup = CreateHeaderFieldGroup("Blend Seconds", out var flipBookBlendDurationLabel);
            _timelineFlipBookBlendDurationField = new FloatField("Blend Seconds");
            _timelineFlipBookBlendDurationField.label = string.Empty;
            _timelineFlipBookBlendDurationField.style.width = 72.0f;
            _timelineFlipBookBlendDurationField.style.marginRight = 12.0f;
            _timelineFlipBookBlendDurationField.RegisterValueChangedCallback(evt => {
                if (_clip == null || _serializedClip == null || _flipBookBlendDurationProperty == null) {
                    return;
                }

                var nextDuration = Mathf.Max(0.0f, evt.newValue);
                if (Mathf.Approximately(nextDuration, _flipBookBlendDurationProperty.floatValue)) {
                    return;
                }

                BeginClipEdit("Change Sprite Animation FlipBookBlend Duration", false);
                _flipBookBlendDurationProperty.floatValue = nextDuration;
                ApplyClipEdit();
                RefreshClipViews();
            });
            flipBookBlendDurationGroup.Add(flipBookBlendDurationLabel);
            flipBookBlendDurationGroup.Add(_timelineFlipBookBlendDurationField);
            controls.Add(flipBookBlendDurationGroup);

            var durationGroup = CreateHeaderFieldGroup("Total Seconds", out var durationLabel);
            _timelineDurationLabel = new Label("0.000s");
            _timelineDurationLabel.style.color = new Color(0.90f, 0.90f, 0.90f);
            durationGroup.Add(durationLabel);
            durationGroup.Add(_timelineDurationLabel);
            controls.Add(durationGroup);

            header.Add(controls);
            _timelineFlipBookBlendWarningHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            _timelineFlipBookBlendWarningHelpBox.style.display = DisplayStyle.None;
            header.Add(_timelineFlipBookBlendWarningHelpBox);
            timelinePanel.Add(header);

            _timelineScrollView = new ScrollView(ScrollViewMode.Horizontal);
            _timelineScrollView.style.flexGrow = 1.0f;
            _timelineScrollView.style.paddingLeft = 8.0f;
            _timelineScrollView.style.paddingRight = 8.0f;
            _timelineScrollView.style.paddingBottom = 8.0f;

            _timelineContent = new VisualElement();
            _timelineContent.style.flexDirection = FlexDirection.Row;
            _timelineContent.style.alignItems = Align.FlexStart;
            _timelineContent.style.paddingTop = 4.0f;
            _timelineContent.style.paddingBottom = 4.0f;

            RegisterTimelineDropHandlers(_timelineContent, -1);
            RegisterTimelineDropHandlers(_timelineScrollView, -2);

            _timelineScrollView.Add(_timelineContent);
            timelinePanel.Add(_timelineScrollView);
            root.Add(timelinePanel);
        }

        /// <summary>
        /// インスペクタ表示を更新する
        /// </summary>
        private void RefreshInspector() {
            if (_inspectorContainer == null || _inspectorTitleLabel == null) {
                return;
            }

            _inspectorTitleLabel.text = _selectedFrameIndex >= 0 ? $"Current Frame [{_selectedFrameIndex}]" : "Current Frame";
            _inspectorRemoveButton.SetEnabled(_clip != null && _selectedFrameIndex >= 0 && _clip.FrameCount > 0);

            if (_clip == null || _serializedClip == null) {
                _inspectorHelpBox.style.display = DisplayStyle.Flex;
                _inspectorSpriteField.style.display = DisplayStyle.None;
                _inspectorEmptyLabel.style.display = DisplayStyle.None;
                _inspectorNameLabel.style.display = DisplayStyle.None;
                _inspectorTextureSizeLabel.style.display = DisplayStyle.None;
                _inspectorRectLabel.style.display = DisplayStyle.None;
                _inspectorPivotLabel.style.display = DisplayStyle.None;
                return;
            }

            _serializedClip.Update();
            _inspectorHelpBox.style.display = DisplayStyle.None;

            var selectedSprite = GetFrameSprite(_selectedFrameIndex);
            _inspectorSpriteField.style.display = DisplayStyle.Flex;
            _inspectorSpriteField.SetEnabled(_selectedFrameIndex >= 0);
            _inspectorSpriteField.SetValueWithoutNotify(selectedSprite);

            if (selectedSprite == null) {
                _inspectorEmptyLabel.style.display = DisplayStyle.Flex;
                _inspectorNameLabel.style.display = DisplayStyle.None;
                _inspectorTextureSizeLabel.style.display = DisplayStyle.None;
                _inspectorRectLabel.style.display = DisplayStyle.None;
                _inspectorPivotLabel.style.display = DisplayStyle.None;
                return;
            }

            _inspectorEmptyLabel.style.display = DisplayStyle.None;
            _inspectorNameLabel.style.display = DisplayStyle.Flex;
            _inspectorTextureSizeLabel.style.display = DisplayStyle.Flex;
            _inspectorRectLabel.style.display = DisplayStyle.Flex;
            _inspectorPivotLabel.style.display = DisplayStyle.Flex;
            _inspectorNameLabel.text = $"Name: {selectedSprite.name}";
            _inspectorTextureSizeLabel.text = $"Texture Size: {selectedSprite.texture.width} x {selectedSprite.texture.height}";
            _inspectorRectLabel.text = $"Rect: {selectedSprite.rect.width:0} x {selectedSprite.rect.height:0}";
            _inspectorPivotLabel.text = $"Pivot: {selectedSprite.pivot.x:0.##}, {selectedSprite.pivot.y:0.##}";
        }

        /// <summary>
        /// タイムラインを再構築する
        /// </summary>
        private void RebuildTimeline() {
            if (_timelineContent == null) {
                return;
            }

            _timelineContent.Clear();
            _frameElements.Clear();

            if (_clip == null) {
                return;
            }

            for (var i = 0; i < Mathf.Max(0, _clip.FrameCount); i++) {
                var frameElement = CreateFrameElement(i);
                _frameElements.Add(frameElement);
                _timelineContent.Add(frameElement);
            }
        }

        /// <summary>
        /// フレーム要素を生成する
        /// </summary>
        /// <param name="frameIndex">対象フレーム</param>
        private VisualElement CreateFrameElement(int frameIndex) {
            var root = new VisualElement();
            root.style.width = FrameCellWidth;
            root.style.height = FrameCellHeight;
            root.style.marginRight = 6.0f;
            root.style.paddingLeft = 4.0f;
            root.style.paddingRight = 4.0f;
            root.style.paddingTop = 4.0f;
            root.style.paddingBottom = 4.0f;
            root.style.opacity = (_isDraggingFrame && frameIndex == _dragFrameIndex) ? 0.55f : 1.0f;
            root.style.backgroundColor = frameIndex == _selectedFrameIndex ? new Color(0.20f, 0.32f, 0.48f) : new Color(0.19f, 0.19f, 0.19f);
            root.style.borderBottomWidth = 1.0f;
            root.style.borderTopWidth = 1.0f;
            root.style.borderLeftWidth = 1.0f;
            root.style.borderRightWidth = 1.0f;
            root.style.borderBottomColor = new Color(0.30f, 0.30f, 0.30f);
            root.style.borderTopColor = new Color(0.30f, 0.30f, 0.30f);
            root.style.borderLeftColor = new Color(0.30f, 0.30f, 0.30f);
            root.style.borderRightColor = new Color(0.30f, 0.30f, 0.30f);
            ApplyFrameInsertionMarker(root, frameIndex);

            var header = new Label(frameIndex.ToString());
            header.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(header);

            var preview = new IMGUIContainer(() => DrawTimelineFramePreview(frameIndex));
            preview.style.height = TimelinePreviewHeight;
            preview.style.flexShrink = 0.0f;
            preview.style.marginTop = 4.0f;
            preview.style.marginBottom = 4.0f;
            root.Add(preview);

            var sprite = GetFrameSprite(frameIndex);
            var nameLabel = new Label(GetTimelineFrameLabelText(sprite));
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            nameLabel.style.whiteSpace = WhiteSpace.Normal;
            nameLabel.style.height = TimelineLabelHeight;
            nameLabel.style.maxHeight = TimelineLabelHeight;
            nameLabel.style.flexShrink = 0.0f;
            nameLabel.style.overflow = Overflow.Hidden;
            root.Add(nameLabel);

            root.RegisterCallback<MouseDownEvent>(evt => {
                if (evt.button != 0) {
                    return;
                }

                _selectedFrameIndex = frameIndex;
                _dragFrameIndex = frameIndex;
                _dragInsertIndex = frameIndex;
                _isDraggingFrame = true;
                rootVisualElement.Focus();
                SyncPreviewToSelectedFrame();
                RefreshInspector();
                RebuildTimeline();
                evt.StopPropagation();
            });

            RegisterFrameContextMenu(root, frameIndex);
            RegisterFrameContextMenu(preview, frameIndex);
            RegisterFrameContextMenu(header, frameIndex);
            RegisterFrameContextMenu(nameLabel, frameIndex);
            RegisterTimelineDropHandlers(root, frameIndex);

            return root;
        }

        /// <summary>
        /// タイムラインヘッダー表示を更新する
        /// </summary>
        private void RefreshTimelineHeader() {
            if (_timelineTotalFramesField != null) {
                _timelineTotalFramesField.SetValueWithoutNotify(_clip != null ? Mathf.Max(0, _clip.FrameCount) : 0);
                _timelineTotalFramesField.SetEnabled(_clip != null);
            }

            if (_timelineFrameRateField != null) {
                _timelineFrameRateField.SetValueWithoutNotify(_clip != null ? _clip.FrameRate : 0.0f);
                _timelineFrameRateField.SetEnabled(_clip != null);
            }

            if (_timelineLoopToggle != null) {
                _timelineLoopToggle.SetValueWithoutNotify(_clip != null && _clip.Loop);
                _timelineLoopToggle.SetEnabled(_clip != null);
            }

            if (_timelineFlipBookBlendToggle != null) {
                var enableFlipBookBlend = _clip != null && _clip.EnableFlipBookBlend;
                _timelineFlipBookBlendToggle.SetValueWithoutNotify(enableFlipBookBlend);
                _timelineFlipBookBlendToggle.SetEnabled(_clip != null);
            }

            if (_timelineFlipBookBlendDurationField != null) {
                var flipBookBlendDuration = _clip != null ? _clip.FlipBookBlendDuration : 0.0f;
                var canEditFlipBookBlendDuration = _clip != null && _clip.EnableFlipBookBlend;
                _timelineFlipBookBlendDurationField.SetValueWithoutNotify(flipBookBlendDuration);
                _timelineFlipBookBlendDurationField.SetEnabled(canEditFlipBookBlendDuration);
            }

            if (_timelineDurationLabel != null) {
                _timelineDurationLabel.text = _clip != null ? $"{_clip.Duration:0.000}s" : "0.000s";
            }

            RefreshTimelineFlipBookBlendWarning();
        }

        /// <summary>
        /// FlipBookBlend 警告表示を更新する
        /// </summary>
        private void RefreshTimelineFlipBookBlendWarning() {
            if (_timelineFlipBookBlendWarningHelpBox == null) {
                return;
            }

            if (_clip == null || !_clip.EnableFlipBookBlend) {
                _timelineFlipBookBlendWarningHelpBox.style.display = DisplayStyle.None;
                return;
            }

            var frameDuration = 1.0f / Mathf.Max(0.01f, _clip.FrameRate);
            if (_clip.FlipBookBlendDuration <= frameDuration) {
                _timelineFlipBookBlendWarningHelpBox.style.display = DisplayStyle.None;
                return;
            }

            _timelineFlipBookBlendWarningHelpBox.text = $"FlipBookBlendDuration is longer than one frame ({frameDuration:0.###} sec). Runtime playback clamps it based on the effective frame time.";
            _timelineFlipBookBlendWarningHelpBox.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// ヘッダー内の項目グループを生成する
        /// </summary>
        /// <param name="labelText">見出し文字列</param>
        /// <param name="label">生成した見出しLabel</param>
        private static VisualElement CreateHeaderFieldGroup(string labelText, out Label label) {
            var group = new VisualElement();
            group.style.flexDirection = FlexDirection.Row;
            group.style.alignItems = Align.Center;
            group.style.marginRight = 10.0f;

            label = new Label(labelText);
            label.style.marginRight = 6.0f;
            label.style.color = new Color(0.85f, 0.85f, 0.85f);

            return group;
        }
    }
}