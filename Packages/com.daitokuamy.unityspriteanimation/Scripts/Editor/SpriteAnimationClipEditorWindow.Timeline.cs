using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySpriteAnimation.Editor {
    /// <summary>
    /// SpriteAnimationClipEditorWindow のタイムライン編集処理
    /// </summary>
    public sealed partial class SpriteAnimationClipEditorWindow {
        /// <summary>
        /// タイムライン D&D 登録
        /// </summary>
        private void RegisterTimelineDropHandlers(VisualElement element, int frameIndex) {
            element.RegisterCallback<DragUpdatedEvent>(evt => {
                if (!CanAcceptDraggedSprites()) {
                    return;
                }

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            });

            element.RegisterCallback<DragPerformEvent>(evt => {
                if (_clip == null) {
                    return;
                }

                var sprites = GetDraggedFrameSprites();
                if (sprites.Count <= 0) {
                    return;
                }

                DragAndDrop.AcceptDrag();
                var targetFrameIndex = GetDropTargetFrameIndex(frameIndex, evt.localMousePosition.x);
                SetFrameSprites(targetFrameIndex, sprites);
                evt.StopPropagation();
            });
        }

        /// <summary>
        /// ドロップ先フレーム番号を取得する
        /// </summary>
        private int GetDropTargetFrameIndex(int frameIndex, float localMousePositionX) {
            if (_clip == null) {
                return 0;
            }

            if (frameIndex >= 0) {
                return frameIndex;
            }

            if (frameIndex == -2) {
                return _clip.FrameCount;
            }

            return GetFrameIndexFromTimelinePosition(localMousePositionX);
        }

        /// <summary>
        /// フレーム Sprite を取得する
        /// </summary>
        private Sprite GetFrameSprite(int frameIndex) {
            if (_clip == null || _spritesProperty == null || frameIndex < 0 || frameIndex >= _clip.FrameCount) {
                return null;
            }

            _serializedClip.Update();
            return _spritesProperty.GetArrayElementAtIndex(frameIndex).objectReferenceValue as Sprite;
        }

        /// <summary>
        /// フレームへ Sprite 群を設定する
        /// </summary>
        private void SetFrameSprites(int startFrameIndex, IReadOnlyList<Sprite> sprites) {
            if (!CanEditClipFrames() || sprites == null || sprites.Count <= 0) {
                return;
            }

            startFrameIndex = Mathf.Max(0, startFrameIndex);

            BeginClipEdit("Edit Sprite Animation Clip", true);

            var requiredSize = startFrameIndex + sprites.Count;
            if (_spritesProperty.arraySize < requiredSize) {
                _spritesProperty.arraySize = requiredSize;
            }

            for (var i = 0; i < sprites.Count; i++) {
                _spritesProperty.GetArrayElementAtIndex(startFrameIndex + i).objectReferenceValue = sprites[i];
            }

            ApplyClipEdit();
            _selectedFrameIndex = startFrameIndex;
            RefreshClipViews();
        }

        /// <summary>
        /// 選択フレームの Sprite 参照をクリアする
        /// </summary>
        private void ClearSelectedFrameSprite() {
            ClearFrameSpriteAt(_selectedFrameIndex);
        }

        /// <summary>
        /// 選択フレームを削除操作として処理する
        /// </summary>
        private void DeleteSelectedFrameContent() {
            if (!CanEditClipFrames() ||
                _selectedFrameIndex < 0 ||
                _selectedFrameIndex >= _spritesProperty.arraySize) {
                return;
            }

            _serializedClip.Update();
            if (_spritesProperty.GetArrayElementAtIndex(_selectedFrameIndex).objectReferenceValue == null) {
                RemoveFrameAt(_selectedFrameIndex);
                return;
            }

            ClearSelectedFrameSprite();
        }

        /// <summary>
        /// 指定フレームの Sprite 参照をクリアする
        /// </summary>
        private void ClearFrameSpriteAt(int frameIndex) {
            if (!CanEditClipFrames() || frameIndex < 0 || frameIndex >= _spritesProperty.arraySize) {
                return;
            }

            BeginClipEdit("Clear Sprite Animation Frame", false);
            _spritesProperty.GetArrayElementAtIndex(frameIndex).objectReferenceValue = null;
            ApplyClipEdit();
            _selectedFrameIndex = frameIndex;
            RefreshClipViews();
        }

        /// <summary>
        /// フレームを全削除する
        /// </summary>
        private void ClearFrames() {
            if (!CanEditClipFrames() || _spritesProperty.arraySize <= 0) {
                return;
            }

            BeginClipEdit("Clear Sprite Animation Frames", true);
            _spritesProperty.arraySize = 0;
            ApplyClipEdit();

            _selectedFrameIndex = -1;
            ResetTimelineDragState();
            RefreshClipViews();
        }

        /// <summary>
        /// 総フレーム数を変更する
        /// </summary>
        private void SetTotalFrameCount(int frameCount) {
            if (!CanEditClipFrames()) {
                return;
            }

            BeginClipEdit("Resize Sprite Animation Frames", true);
            _spritesProperty.arraySize = Mathf.Max(0, frameCount);
            ApplyClipEdit();

            _selectedFrameIndex = _spritesProperty.arraySize > 0 ? Mathf.Clamp(_selectedFrameIndex, 0, _spritesProperty.arraySize - 1) : -1;
            ResetTimelineDragState();
            RefreshClipViews();
        }

        /// <summary>
        /// フレーム順を変更する
        /// </summary>
        /// <returns>移動した場合 true</returns>
        private bool MoveFrame(int fromFrameIndex, int insertIndex) {
            if (!CanEditClipFrames() || fromFrameIndex < 0 || fromFrameIndex >= _spritesProperty.arraySize) {
                return false;
            }

            insertIndex = Mathf.Clamp(insertIndex, 0, _spritesProperty.arraySize);
            var destinationIndex = fromFrameIndex < insertIndex ? insertIndex - 1 : insertIndex;
            if (destinationIndex == fromFrameIndex) {
                return false;
            }

            BeginClipEdit("Reorder Sprite Animation Frame", true);

            var sprites = new Sprite[_spritesProperty.arraySize];
            for (var i = 0; i < _spritesProperty.arraySize; i++) {
                sprites[i] = _spritesProperty.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
            }

            var reordered = new List<Sprite>(sprites);
            var movingSprite = reordered[fromFrameIndex];
            reordered.RemoveAt(fromFrameIndex);
            reordered.Insert(destinationIndex, movingSprite);

            for (var i = 0; i < reordered.Count; i++) {
                _spritesProperty.GetArrayElementAtIndex(i).objectReferenceValue = reordered[i];
            }

            ApplyClipEdit();
            _selectedFrameIndex = destinationIndex;
            ResetTimelineDragState();
            RefreshClipViews();
            return true;
        }

        /// <summary>
        /// 指定位置の次へフレームを挿入する
        /// </summary>
        private void InsertFrameAfter(int frameIndex) {
            if (!CanEditClipFrames()) {
                return;
            }

            var insertIndex = Mathf.Clamp(frameIndex + 1, 0, _spritesProperty.arraySize);

            BeginClipEdit("Insert Sprite Animation Frame", true);

            var previousSprites = new Sprite[_spritesProperty.arraySize];
            for (var i = 0; i < _spritesProperty.arraySize; i++) {
                previousSprites[i] = _spritesProperty.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
            }

            _spritesProperty.arraySize++;
            for (var i = _spritesProperty.arraySize - 1; i > insertIndex; i--) {
                _spritesProperty.GetArrayElementAtIndex(i).objectReferenceValue = previousSprites[i - 1];
            }

            _spritesProperty.GetArrayElementAtIndex(insertIndex).objectReferenceValue = null;

            ApplyClipEdit();
            _selectedFrameIndex = insertIndex;
            ResetTimelineDragState();
            RefreshClipViews();
        }

        /// <summary>
        /// 指定位置の次へフレームを挿入し、Sprite を設定する
        /// </summary>
        /// <param name="frameIndex">基準フレーム位置</param>
        /// <param name="sprite">設定するSprite</param>
        private void InsertFrameAfter(int frameIndex, Sprite sprite) {
            InsertFrameAfter(frameIndex);

            if (!CanEditClipFrames() || _selectedFrameIndex < 0 || _selectedFrameIndex >= _spritesProperty.arraySize) {
                return;
            }

            _serializedClip.Update();
            _spritesProperty.GetArrayElementAtIndex(_selectedFrameIndex).objectReferenceValue = sprite;
            ApplyClipEdit();
            RefreshClipViews();
        }

        /// <summary>
        /// 指定フレームを複製して右隣へ追加する
        /// </summary>
        private void DuplicateFrameAt(int frameIndex) {
            if (!CanEditClipFrames() || frameIndex < 0 || frameIndex >= _spritesProperty.arraySize) {
                return;
            }

            InsertFrameAfter(frameIndex, GetFrameSprite(frameIndex));
        }

        /// <summary>
        /// 選択フレームをコピーする
        /// </summary>
        private void CopySelectedFrame() {
            CopyFrameAt(_selectedFrameIndex);
        }

        /// <summary>
        /// 指定フレームをコピーする
        /// </summary>
        /// <param name="frameIndex">コピー元フレーム</param>
        private void CopyFrameAt(int frameIndex) {
            if (_clip == null || frameIndex < 0 || frameIndex >= _clip.FrameCount) {
                return;
            }

            _copiedFrameSprite = GetFrameSprite(frameIndex);
            _hasCopiedFrame = true;
        }

        /// <summary>
        /// コピー済みフレームを選択位置の右へ貼り付ける
        /// </summary>
        private void PasteCopiedFrameAfterSelected() {
            if (!_hasCopiedFrame || _selectedFrameIndex < 0) {
                return;
            }

            InsertFrameAfter(_selectedFrameIndex, _copiedFrameSprite);
        }

        /// <summary>
        /// 指定フレームを削除する
        /// </summary>
        private void RemoveFrameAt(int frameIndex) {
            if (!CanEditClipFrames() || frameIndex < 0 || frameIndex >= _spritesProperty.arraySize) {
                return;
            }

            BeginClipEdit("Remove Sprite Animation Frame", true);
            _spritesProperty.DeleteArrayElementAtIndex(frameIndex);
            if (frameIndex < _spritesProperty.arraySize &&
                _spritesProperty.GetArrayElementAtIndex(frameIndex).objectReferenceValue == null) {
                _spritesProperty.DeleteArrayElementAtIndex(frameIndex);
            }

            ApplyClipEdit();
            _selectedFrameIndex = _spritesProperty.arraySize > 0 ? Mathf.Clamp(frameIndex, 0, _spritesProperty.arraySize - 1) : -1;
            ResetTimelineDragState();
            RefreshClipViews();
        }

        /// <summary>
        /// D&D 受付可能か
        /// </summary>
        private static bool CanAcceptDraggedSprites() {
            return GetDraggedFrameSprites().Count > 0;
        }

        /// <summary>
        /// D&D 中のオブジェクトからフレーム設定可能な Sprite を取得する
        /// </summary>
        private static IReadOnlyList<Sprite> GetDraggedFrameSprites() {
            var sprites = new List<Sprite>();
            foreach (var objectReference in DragAndDrop.objectReferences) {
                if (objectReference is Sprite sprite) {
                    sprites.Add(sprite);
                    continue;
                }

                if (objectReference is Texture2D texture && TryGetSingleSpriteFromTexture(texture, out var textureSprite)) {
                    sprites.Add(textureSprite);
                }
            }

            return sprites;
        }

        /// <summary>
        /// Texture から Single Sprite を取得する
        /// </summary>
        /// <param name="texture">対象 Texture</param>
        /// <param name="sprite">取得した Sprite</param>
        /// <returns>取得できた場合 true</returns>
        private static bool TryGetSingleSpriteFromTexture(Texture2D texture, out Sprite sprite) {
            sprite = null;
            var assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(assetPath)) {
                return false;
            }

            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter textureImporter ||
                textureImporter.textureType != TextureImporterType.Sprite ||
                textureImporter.spriteImportMode != SpriteImportMode.Single) {
                return false;
            }

            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            return sprite != null;
        }

        /// <summary>
        /// タイムライン上の位置からフレーム番号を取得
        /// </summary>
        private int GetFrameIndexFromTimelinePosition(float localPositionX) {
            var framePitch = FrameCellWidth + FrameCellGap;
            return Mathf.Max(0, Mathf.FloorToInt(Mathf.Max(0.0f, localPositionX) / framePitch));
        }

        /// <summary>
        /// フレーム用コンテキストメニューを登録する
        /// </summary>
        private void RegisterFrameContextMenu(VisualElement element, int frameIndex) {
            element.RegisterCallback<MouseUpEvent>(evt => {
                if (evt.button != 1) {
                    return;
                }

                ShowFrameContextMenu(frameIndex);
                evt.StopPropagation();
            });
        }

        /// <summary>
        /// Timeline フレームの左クリック開始時処理
        /// </summary>
        /// <param name="frameIndex">対象フレーム</param>
        /// <param name="mousePosition">マウス位置</param>
        private void BeginTimelineFramePointerDown(int frameIndex, Vector2 mousePosition) {
            _selectedFrameIndex = frameIndex;
            _pendingDragFrameIndex = frameIndex;
            _pendingDragMousePosition = mousePosition;
            _dragFrameIndex = -1;
            _dragInsertIndex = -1;
            _isFrameDragPending = true;
            _isDraggingFrame = false;

            rootVisualElement.CaptureMouse();
            rootVisualElement.Focus();
            SyncPreviewToSelectedFrame();
            RefreshInspector();
            RebuildTimeline();
        }

        /// <summary>
        /// 保留中の Timeline フレームドラッグを開始する
        /// </summary>
        /// <param name="mousePosition">マウス位置</param>
        private void StartPendingTimelineFrameDrag(Vector2 mousePosition) {
            if (!_isFrameDragPending || _pendingDragFrameIndex < 0 || _timelineContent == null || _clip == null) {
                return;
            }

            var localPosition = _timelineContent.WorldToLocal(mousePosition);
            _dragFrameIndex = _pendingDragFrameIndex;
            _dragInsertIndex = GetFrameInsertIndexFromTimelinePosition(localPosition.x);
            _pendingDragFrameIndex = -1;
            _isFrameDragPending = false;
            _isDraggingFrame = true;
            RebuildTimeline();
        }

        /// <summary>
        /// ルートのマウス移動時処理
        /// </summary>
        private void OnRootMouseMove(MouseMoveEvent evt) {
            if (_isFrameDragPending) {
                if (Vector2.Distance(_pendingDragMousePosition, evt.mousePosition) < TimelineDragStartDistance) {
                    return;
                }

                StartPendingTimelineFrameDrag(evt.mousePosition);
                evt.StopPropagation();
                return;
            }

            if (!_isDraggingFrame || _timelineContent == null || _clip == null) {
                return;
            }

            var localPosition = _timelineContent.WorldToLocal(evt.mousePosition);
            _dragInsertIndex = GetFrameInsertIndexFromTimelinePosition(localPosition.x);
            RebuildTimeline();
        }

        /// <summary>
        /// ルートのマウス解放時処理
        /// </summary>
        private void OnRootMouseUp(MouseUpEvent evt) {
            if (evt.button != 0) {
                return;
            }

            if (_isFrameDragPending) {
                ResetTimelineDragState();
                evt.StopPropagation();
                return;
            }

            if (!_isDraggingFrame) {
                return;
            }

            var fromFrameIndex = _dragFrameIndex;
            var insertIndex = _dragInsertIndex;

            ResetTimelineDragState();

            if (fromFrameIndex < 0 || insertIndex < 0 || !MoveFrame(fromFrameIndex, insertIndex)) {
                RebuildTimeline();
            }

            evt.StopPropagation();
        }

        /// <summary>
        /// フレーム用コンテキストメニューを表示する
        /// </summary>
        private void ShowFrameContextMenu(int frameIndex) {
            var menu = new GenericMenu();
            if (_clip != null && frameIndex >= 0 && frameIndex < _clip.FrameCount) {
                menu.AddItem(new GUIContent("Copy"), false, () => CopyFrameAt(frameIndex));
            }
            else {
                menu.AddDisabledItem(new GUIContent("Copy"));
            }

            if (_hasCopiedFrame && _selectedFrameIndex >= 0) {
                menu.AddItem(new GUIContent("Paste"), false, PasteCopiedFrameAfterSelected);
            }
            else {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }

            menu.AddItem(new GUIContent("Duplicate"), false, () => DuplicateFrameAt(frameIndex));
            menu.AddItem(new GUIContent("Clear Sprite"), false, () => ClearFrameSpriteAt(frameIndex));

            if (_clip != null && _clip.FrameCount > 0) {
                menu.AddItem(new GUIContent("Remove"), false, () => RemoveFrameAt(frameIndex));
            }
            else {
                menu.AddDisabledItem(new GUIContent("Remove"));
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// ルートのキー入力処理
        /// </summary>
        private void OnRootKeyDown(KeyDownEvent evt) {
            if (IsEditingTextInput(evt)) {
                return;
            }

            if (evt.keyCode == KeyCode.Space) {
                if (IsPreviewPlaybackToggleShortcut(evt)) {
                    TogglePreviewPlayback();
                    evt.StopPropagation();
                }

                return;
            }

            if (evt.keyCode == KeyCode.LeftArrow) {
                MoveSelectedFrameBy(-1);
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode == KeyCode.RightArrow) {
                MoveSelectedFrameBy(1);
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode != KeyCode.Delete && evt.keyCode != KeyCode.Backspace) {
                return;
            }

            DeleteSelectedFrameContent();
            evt.StopPropagation();
        }

        /// <summary>
        /// プレビュー再生トグルのショートカットとして処理してよいか
        /// </summary>
        /// <param name="evt">キー入力イベント</param>
        /// <returns>トグル処理してよい場合は true</returns>
        private static bool IsPreviewPlaybackToggleShortcut(KeyDownEvent evt) {
            if (evt == null || evt.altKey || evt.ctrlKey || evt.commandKey || evt.shiftKey) {
                return false;
            }

            return evt.target is not TextElement;
        }

        /// <summary>
        /// 選択中フレームを相対移動する
        /// </summary>
        private void MoveSelectedFrameBy(int offset) {
            if (_clip == null || _clip.FrameCount <= 0 || offset == 0) {
                return;
            }

            var currentIndex = _selectedFrameIndex >= 0 ? _selectedFrameIndex : 0;
            var nextIndex = Mathf.Clamp(currentIndex + offset, 0, _clip.FrameCount - 1);
            if (nextIndex == _selectedFrameIndex) {
                return;
            }

            _selectedFrameIndex = nextIndex;
            SyncPreviewToSelectedFrame();
            RefreshInspector();
            RebuildTimeline();
        }

        /// <summary>
        /// コマンド検証時処理
        /// </summary>
        private void OnValidateCommand(ValidateCommandEvent evt) {
            if (IsEditingTextInput(evt)) {
                return;
            }

            if (evt.commandName != "SoftDelete" &&
                evt.commandName != "Delete" &&
                evt.commandName != "Duplicate" &&
                evt.commandName != "Copy" &&
                evt.commandName != "Paste") {
                return;
            }

            evt.StopPropagation();
        }

        /// <summary>
        /// コマンド実行時処理
        /// </summary>
        private void OnExecuteCommand(ExecuteCommandEvent evt) {
            if (IsEditingTextInput(evt)) {
                return;
            }

            if (evt.commandName == "Copy") {
                CopySelectedFrame();
                evt.StopPropagation();
                return;
            }

            if (evt.commandName == "Paste") {
                PasteCopiedFrameAfterSelected();
                evt.StopPropagation();
                return;
            }

            if (evt.commandName == "Duplicate") {
                DuplicateFrameAt(_selectedFrameIndex);
                evt.StopPropagation();
                return;
            }

            if (evt.commandName != "SoftDelete" && evt.commandName != "Delete") {
                return;
            }

            DeleteSelectedFrameContent();
            evt.StopPropagation();
        }

        /// <summary>
        /// テキスト入力中かを判定する
        /// </summary>
        /// <param name="evt">対象イベント</param>
        private static bool IsEditingTextInput(EventBase evt) {
            if (evt?.target is not VisualElement element) {
                return false;
            }

            while (element != null) {
                if (element is TextElement ||
                    element is TextField ||
                    element is IntegerField ||
                    element is FloatField) {
                    return true;
                }

                element = element.parent;
            }

            return false;
        }

        /// <summary>
        /// タイムライン位置から挿入位置を取得する
        /// </summary>
        private int GetFrameInsertIndexFromTimelinePosition(float localPositionX) {
            var framePitch = FrameCellWidth + FrameCellGap;
            var rawIndex = Mathf.FloorToInt(Mathf.Max(0.0f, localPositionX) / framePitch);
            var offsetInCell = Mathf.Max(0.0f, localPositionX) - (rawIndex * framePitch);
            var insertIndex = offsetInCell > (FrameCellWidth * 0.5f) ? rawIndex + 1 : rawIndex;
            return Mathf.Clamp(insertIndex, 0, _clip != null ? _clip.FrameCount : 0);
        }

        /// <summary>
        /// フレームへ挿入マーカーを適用する
        /// </summary>
        private void ApplyFrameInsertionMarker(VisualElement frameElement, int frameIndex) {
            if (!_isDraggingFrame || _dragInsertIndex < 0) {
                return;
            }

            var markerColor = new Color(0.98f, 0.76f, 0.16f);
            if (_dragInsertIndex == frameIndex) {
                frameElement.style.borderLeftWidth = 4.0f;
                frameElement.style.borderLeftColor = markerColor;
            }

            if (_clip != null && _dragInsertIndex == _clip.FrameCount && frameIndex == _clip.FrameCount - 1) {
                frameElement.style.borderRightWidth = 4.0f;
                frameElement.style.borderRightColor = markerColor;
            }
        }
    }
}
