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

            _clearFramesButton = new Button(ClearFrames) {
                text = "Clear Frames",
            };
            _clearFramesButton.SetEnabled(_clip != null && _clip.FrameCount > 0);
            toolbar.Add(_clearFramesButton);

            root.Add(toolbar);
        }

        /// <summary>
        /// ツールバー表示を更新する
        /// </summary>
        private void RefreshToolbar() {
            if (_clearFramesButton != null) {
                _clearFramesButton.SetEnabled(_clip != null && _clip.FrameCount > 0);
            }
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

            _previewScaleSlider = new Slider(PreviewScaleMin, PreviewScaleMax) {
                value = _previewScale,
            };
            _previewScaleSlider.style.width = 140.0f;
            _previewScaleSlider.style.marginRight = 6.0f;
            _previewScaleSlider.RegisterValueChangedCallback(evt => {
                SetPreviewScale(evt.newValue);
            });
            previewScaleArea.Add(_previewScaleSlider);

            _previewScaleLabel = new Label($"{_previewScale:0.00}x");
            _previewScaleLabel.style.minWidth = 42.0f;
            _previewScaleLabel.style.color = new Color(0.90f, 0.90f, 0.90f);
            previewScaleArea.Add(_previewScaleLabel);

            var previewSeekTitle = new Label("Seek");
            previewSeekTitle.style.marginLeft = 16.0f;
            previewSeekTitle.style.marginRight = 6.0f;
            previewSeekTitle.style.color = new Color(0.85f, 0.85f, 0.85f);
            previewScaleArea.Add(previewSeekTitle);

            _previewSeekSlider = new Slider(0.0f, 1.0f) {
                value = GetPreviewSeekValue(),
            };
            _previewSeekSlider.style.width = 180.0f;
            _previewSeekSlider.style.marginRight = 6.0f;
            _previewSeekSlider.RegisterValueChangedCallback(evt => {
                SetPreviewSeek(evt.newValue);
            });
            previewScaleArea.Add(_previewSeekSlider);

            _previewSeekLabel = new Label("0%");
            _previewSeekLabel.style.minWidth = 36.0f;
            _previewSeekLabel.style.color = new Color(0.90f, 0.90f, 0.90f);
            previewScaleArea.Add(_previewSeekLabel);

            var previewBackgroundTitle = new Label("BG");
            previewBackgroundTitle.style.marginLeft = 16.0f;
            previewBackgroundTitle.style.marginRight = 6.0f;
            previewBackgroundTitle.style.color = new Color(0.85f, 0.85f, 0.85f);
            previewScaleArea.Add(previewBackgroundTitle);

            _previewBackgroundColorField = new ColorField("Background");
            _previewBackgroundColorField.label = string.Empty;
            _previewBackgroundColorField.showAlpha = false;
            _previewBackgroundColorField.value = _previewBackgroundColor;
            _previewBackgroundColorField.style.width = 76.0f;
            _previewBackgroundColorField.RegisterValueChangedCallback(evt => {
                var nextColor = evt.newValue;
                nextColor.a = 1.0f;
                _previewBackgroundColor = nextColor;
                RefreshPreview();
            });
            previewScaleArea.Add(_previewBackgroundColorField);

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
            _timelineFlipBookBlendDurationField.style.marginRight = 4.0f;
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
            _timelineFlipBookBlendDurationAutoButton = new Button(SetAutoFlipBookBlendDuration) {
                text = "Auto",
            };
            _timelineFlipBookBlendDurationAutoButton.style.marginRight = 12.0f;
            flipBookBlendDurationGroup.Add(flipBookBlendDurationLabel);
            flipBookBlendDurationGroup.Add(_timelineFlipBookBlendDurationField);
            flipBookBlendDurationGroup.Add(_timelineFlipBookBlendDurationAutoButton);
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
            root.style.height = FrameCellHeight + TimelineHoldIndicatorTopMargin + TimelineHoldIndicatorHeight;
            root.style.marginRight = FrameCellGap;
            root.style.position = Position.Relative;
            root.style.overflow = Overflow.Visible;
            root.style.opacity = (_isDraggingFrame && frameIndex == _dragFrameIndex) ? 0.55f : 1.0f;

            var body = new VisualElement();
            body.style.width = FrameCellWidth;
            body.style.height = FrameCellHeight;
            body.style.paddingLeft = FrameCellHorizontalPadding;
            body.style.paddingRight = FrameCellHorizontalPadding;
            body.style.paddingTop = 4.0f;
            body.style.paddingBottom = 4.0f;
            body.style.position = Position.Relative;
            body.style.overflow = Overflow.Hidden;
            body.style.backgroundColor = frameIndex == _selectedFrameIndex ? new Color(0.20f, 0.32f, 0.48f) : new Color(0.19f, 0.19f, 0.19f);
            body.style.borderBottomWidth = 1.0f;
            body.style.borderTopWidth = 1.0f;
            body.style.borderLeftWidth = 1.0f;
            body.style.borderRightWidth = 1.0f;
            body.style.borderBottomColor = new Color(0.30f, 0.30f, 0.30f);
            body.style.borderTopColor = new Color(0.30f, 0.30f, 0.30f);
            body.style.borderLeftColor = new Color(0.30f, 0.30f, 0.30f);
            body.style.borderRightColor = new Color(0.30f, 0.30f, 0.30f);
            ApplyFrameInsertionMarker(body, frameIndex);
            root.Add(body);

            var header = new Label(frameIndex.ToString());
            header.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            body.Add(header);

            var sprite = GetFrameSprite(frameIndex);
            var hasPreviousSameSprite = IsSameFrameSprite(frameIndex - 1, sprite);
            var hasNextSameSprite = IsSameFrameSprite(frameIndex + 1, sprite);
            var previewAlpha = hasPreviousSameSprite ? TimelineRepeatFramePreviewAlpha : 1.0f;

            var previewArea = new VisualElement();
            previewArea.style.height = TimelinePreviewHeight;
            previewArea.style.flexShrink = 0.0f;
            previewArea.style.marginTop = 4.0f;
            previewArea.style.marginBottom = 4.0f;
            previewArea.style.position = Position.Relative;
            previewArea.style.overflow = Overflow.Hidden;
            body.Add(previewArea);

            var preview = new IMGUIContainer(() => DrawTimelineFramePreview(frameIndex, previewAlpha));
            preview.style.height = TimelinePreviewHeight;
            preview.style.flexShrink = 0.0f;
            previewArea.Add(preview);

            var nameLabel = new Label(GetTimelineFrameLabelText(sprite));
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            nameLabel.style.whiteSpace = WhiteSpace.Normal;
            nameLabel.style.height = TimelineLabelHeight;
            nameLabel.style.maxHeight = TimelineLabelHeight;
            nameLabel.style.flexShrink = 0.0f;
            nameLabel.style.overflow = Overflow.Hidden;
            body.Add(nameLabel);

            AddTimelineHoldIndicator(root, hasPreviousSameSprite, hasNextSameSprite);

            root.RegisterCallback<MouseDownEvent>(evt => {
                if (evt.button != 0) {
                    return;
                }

                BeginTimelineFramePointerDown(frameIndex, evt.mousePosition);
                evt.StopPropagation();
            });

            RegisterFrameContextMenu(root, frameIndex);
            RegisterFrameContextMenu(body, frameIndex);
            RegisterFrameContextMenu(preview, frameIndex);
            RegisterFrameContextMenu(header, frameIndex);
            RegisterFrameContextMenu(nameLabel, frameIndex);
            RegisterTimelineDropHandlers(root, frameIndex);
            RegisterTimelineDropHandlers(body, frameIndex);

            return root;
        }

        /// <summary>
        /// タイムラインに連続フレーム表示を追加する
        /// </summary>
        /// <param name="root">追加先</param>
        /// <param name="hasPreviousSameSprite">前フレームが同じ Sprite の場合 true</param>
        /// <param name="hasNextSameSprite">次フレームが同じ Sprite の場合 true</param>
        private static void AddTimelineHoldIndicator(VisualElement root, bool hasPreviousSameSprite, bool hasNextSameSprite) {
            if (!hasPreviousSameSprite && !hasNextSameSprite) {
                return;
            }

            var indicatorTop = FrameCellHeight + TimelineHoldIndicatorTopMargin;
            var indicator = CreateTimelineHoldIndicatorPart();
            indicator.style.left = 0.0f;
            indicator.style.top = indicatorTop;
            indicator.style.width = FrameCellWidth;
            ApplyTimelineHoldIndicatorRadius(indicator, !hasPreviousSameSprite, !hasNextSameSprite);
            root.Add(indicator);

            if (!hasNextSameSprite) {
                return;
            }

            var bridge = CreateTimelineHoldIndicatorPart();
            bridge.style.left = FrameCellWidth;
            bridge.style.top = indicatorTop;
            bridge.style.width = FrameCellGap;
            root.Add(bridge);
        }

        /// <summary>
        /// 指定フレームが同じ Sprite か判定する
        /// </summary>
        /// <param name="frameIndex">対象フレーム</param>
        /// <param name="sprite">比較 Sprite</param>
        private bool IsSameFrameSprite(int frameIndex, Sprite sprite) {
            return sprite != null && GetFrameSprite(frameIndex) == sprite;
        }

        /// <summary>
        /// タイムライン連続フレーム表示の要素を生成する
        /// </summary>
        private static VisualElement CreateTimelineHoldIndicatorPart() {
            var element = new VisualElement();
            element.pickingMode = PickingMode.Ignore;
            element.style.position = Position.Absolute;
            element.style.height = TimelineHoldIndicatorHeight;
            element.style.backgroundColor = new Color(0.25f, 0.74f, 0.95f, TimelineHoldIndicatorAlpha);
            return element;
        }

        /// <summary>
        /// タイムライン連続フレーム表示の角丸を適用する
        /// </summary>
        /// <param name="element">対象要素</param>
        /// <param name="roundStart">先頭を丸める場合 true</param>
        /// <param name="roundEnd">末尾を丸める場合 true</param>
        private static void ApplyTimelineHoldIndicatorRadius(VisualElement element, bool roundStart, bool roundEnd) {
            var startRadius = roundStart ? TimelineHoldIndicatorRadius : 0.0f;
            var endRadius = roundEnd ? TimelineHoldIndicatorRadius : 0.0f;
            element.style.borderTopLeftRadius = startRadius;
            element.style.borderBottomLeftRadius = startRadius;
            element.style.borderTopRightRadius = endRadius;
            element.style.borderBottomRightRadius = endRadius;
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

            if (_timelineFlipBookBlendDurationAutoButton != null) {
                _timelineFlipBookBlendDurationAutoButton.SetEnabled(_clip != null && _clip.EnableFlipBookBlend);
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
        /// 現在の FrameRate から FlipBookBlend 秒数を自動設定する
        /// </summary>
        private void SetAutoFlipBookBlendDuration() {
            if (_clip == null || _serializedClip == null || _flipBookBlendDurationProperty == null) {
                return;
            }

            var maxDuration = GetMaxFlipBookBlendDuration(_clip.FrameRate);
            if (Mathf.Approximately(maxDuration, _flipBookBlendDurationProperty.floatValue)) {
                return;
            }

            BeginClipEdit("Auto Set Sprite Animation FlipBookBlend Duration", false);
            _flipBookBlendDurationProperty.floatValue = maxDuration;
            ApplyClipEdit();
            RefreshClipViews();
        }

        /// <summary>
        /// FrameRate から最大 FlipBookBlend 秒数を取得する
        /// </summary>
        /// <param name="frameRate">対象 FrameRate</param>
        private static float GetMaxFlipBookBlendDuration(float frameRate) {
            var frameDuration = 1.0f / Mathf.Max(0.01f, frameRate);
            return Mathf.Max(0.0f, frameDuration - FlipBookBlendDurationEpsilon);
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
