using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySpriteAnimation.Editor {
    /// <summary>
    /// SpriteAnimationClip 編集用ウィンドウ
    /// </summary>
    public sealed partial class SpriteAnimationClipEditorWindow : EditorWindow {
        /// <summary>タイムラインのフレーム幅</summary>
        private const float FrameCellWidth = 84.0f;

        /// <summary>タイムラインのフレーム高さ</summary>
        private const float FrameCellHeight = 116.0f;

        /// <summary>タイムラインサムネイルの高さ</summary>
        private const float TimelinePreviewHeight = 56.0f;

        /// <summary>タイムラインラベルの高さ</summary>
        private const float TimelineLabelHeight = 32.0f;

        /// <summary>タイムラインラベルの最大文字数</summary>
        private const int TimelineLabelMaxCharacters = 18;

        /// <summary>インスペクタ領域の幅</summary>
        private const float InspectorWidth = 320.0f;

        /// <summary>プレビュー下部の操作ボタン分の余白</summary>
        private const float PreviewBottomPadding = 56.0f;

        /// <summary>プレビュー拡大率の最小値</summary>
        private const float PreviewScaleMin = 0.1f;

        /// <summary>プレビュー拡大率の最大値</summary>
        private const float PreviewScaleMax = 1.0f;

        /// <summary>マウスホイール1単位あたりのプレビュー拡大率変化量</summary>
        private const float PreviewScaleWheelStep = 0.05f;

        /// <summary>Timeline フレームドラッグ開始距離</summary>
        private const float TimelineDragStartDistance = 4.0f;

        [SerializeField] private SpriteAnimationClip _clip;
        [SerializeField] private int _selectedFrameIndex = -1;
        [SerializeField] private bool _hasCopiedFrame;
        [SerializeField] private Sprite _copiedFrameSprite;
        [SerializeField] private Color _previewBackgroundColor = new(0.10f, 0.10f, 0.10f, 1.0f);

        private readonly List<VisualElement> _frameElements = new();
        private SerializedObject _serializedClip;
        private SerializedProperty _frameRateProperty;
        private SerializedProperty _loopProperty;
        private SerializedProperty _enableFlipBookBlendProperty;
        private SerializedProperty _flipBookBlendDurationProperty;
        private SerializedProperty _spritesProperty;

        private ObjectField _clipField;
        private IMGUIContainer _previewContainer;
        private VisualElement _inspectorContainer;
        private Label _inspectorTitleLabel;
        private Button _inspectorRemoveButton;
        private HelpBox _inspectorHelpBox;
        private ObjectField _inspectorSpriteField;
        private Label _inspectorEmptyLabel;
        private Label _inspectorNameLabel;
        private Label _inspectorTextureSizeLabel;
        private Label _inspectorRectLabel;
        private Label _inspectorPivotLabel;
        private ScrollView _timelineScrollView;
        private VisualElement _timelineContent;
        private IntegerField _timelineTotalFramesField;
        private Label _timelineDurationLabel;
        private HelpBox _timelineFlipBookBlendWarningHelpBox;
        private Toggle _timelineLoopToggle;
        private Toggle _timelineFlipBookBlendToggle;
        private FloatField _timelineFlipBookBlendDurationField;
        private FloatField _timelineFrameRateField;
        private Slider _previewScaleSlider;
        private Label _previewScaleLabel;
        private Slider _previewSeekSlider;
        private Label _previewSeekLabel;
        private ColorField _previewBackgroundColorField;
        private int _pendingDragFrameIndex = -1;
        private Vector2 _pendingDragMousePosition;
        private int _dragFrameIndex = -1;
        private int _dragInsertIndex = -1;
        private bool _isFrameDragPending;
        private bool _isDraggingFrame;
        private bool _isPreviewPlaying;
        private bool _isPreviewPaused;
        private float _previewTime;
        private double _previewStartTime;
        private int _previewPlaybackFrameIndex = -1;
        private int _previewBlendFrameIndex = -1;
        private float _previewScale = 1.0f;

        /// <summary>
        /// Window を開く
        /// </summary>
        [MenuItem("Window/Unity Sprite Animation/Sprite Animation Clip Editor")]
        private static void Open() {
            OpenWindow(null);
        }

        /// <summary>
        /// Window を開く
        /// </summary>
        /// <param name="clip">初期設定する Clip</param>
        internal static void OpenWindow(SpriteAnimationClip clip) {
            var window = GetWindow<SpriteAnimationClipEditorWindow>();
            window.titleContent = new GUIContent("Sprite Animation");
            window.minSize = new Vector2(960.0f, 640.0f);
            window.SetClip(clip);
            window.Show();
        }

        /// <summary>
        /// ProjectView 上のアセットオープン時処理
        /// </summary>
        /// <param name="instanceId">アセットID</param>
        /// <param name="line">行番号</param>
        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceId, int line) {
            var clip = EditorUtility.InstanceIDToObject(instanceId) as SpriteAnimationClip;
            if (clip == null) {
                return false;
            }

            OpenWindow(clip);
            return true;
        }

        /// <summary>
        /// 有効化時処理
        /// </summary>
        private void OnEnable() {
            EditorApplication.update += OnEditorUpdate;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        /// <summary>
        /// フォーカス取得時処理
        /// </summary>
        private void OnFocus() {
            FocusRootElement();
        }

        /// <summary>
        /// 無効化時処理
        /// </summary>
        private void OnDisable() {
            EditorApplication.update -= OnEditorUpdate;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            ReleasePreviewMaterial();
        }

        /// <summary>
        /// GUI を生成する
        /// </summary>
        private void CreateGUI() {
            rootVisualElement.Clear();
            rootVisualElement.focusable = true;
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.flexGrow = 1.0f;
            rootVisualElement.style.paddingLeft = 8.0f;
            rootVisualElement.style.paddingRight = 8.0f;
            rootVisualElement.style.paddingTop = 8.0f;
            rootVisualElement.style.paddingBottom = 8.0f;
            RegisterRootCallbacks();

            BuildToolbar(rootVisualElement);
            BuildMainArea(rootVisualElement);
            BuildTimelineArea(rootVisualElement);

            SetClip(_clip);
            FocusRootElement();
        }

        /// <summary>
        /// ルート要素のコールバックを登録する
        /// </summary>
        private void RegisterRootCallbacks() {
            rootVisualElement.UnregisterCallback<KeyDownEvent>(OnRootKeyDown);
            rootVisualElement.UnregisterCallback<ValidateCommandEvent>(OnValidateCommand);
            rootVisualElement.UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            rootVisualElement.UnregisterCallback<MouseMoveEvent>(OnRootMouseMove);
            rootVisualElement.UnregisterCallback<MouseUpEvent>(OnRootMouseUp);
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnRootKeyDown);
            rootVisualElement.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            rootVisualElement.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            rootVisualElement.RegisterCallback<MouseMoveEvent>(OnRootMouseMove);
            rootVisualElement.RegisterCallback<MouseUpEvent>(OnRootMouseUp);
        }

        /// <summary>
        /// ルート要素へフォーカスを戻す
        /// </summary>
        private void FocusRootElement() {
            if (rootVisualElement.panel == null) {
                return;
            }

            rootVisualElement.schedule.Execute(() => {
                if (rootVisualElement.panel == null) {
                    return;
                }

                rootVisualElement.Focus();
            });
        }

        /// <summary>
        /// Clip を設定する
        /// </summary>
        /// <param name="clip">設定する Clip</param>
        private void SetClip(SpriteAnimationClip clip) {
            _clip = clip;
            _serializedClip = _clip != null ? new SerializedObject(_clip) : null;
            _frameRateProperty = _serializedClip?.FindProperty("_frameRate");
            _loopProperty = _serializedClip?.FindProperty("_loop");
            _enableFlipBookBlendProperty = _serializedClip?.FindProperty("_enableFlipBookBlend");
            _flipBookBlendDurationProperty = _serializedClip?.FindProperty("_flipBookBlendDuration");
            _spritesProperty = _serializedClip?.FindProperty("_sprites");
            _selectedFrameIndex = _clip != null && _clip.FrameCount > 0 ? Mathf.Clamp(_selectedFrameIndex, 0, _clip.FrameCount - 1) : -1;
            ResetPreviewFlipBookBlendState(_clip != null ? _clip.GetFrameIndex(_previewTime) : -1);

            if (_clipField != null && _clipField.value != _clip) {
                _clipField.SetValueWithoutNotify(_clip);
            }

            ResetTimelineDragState();
            RefreshClipViews();
        }

        /// <summary>
        /// Clip の表示を更新する
        /// </summary>
        private void RefreshClipViews() {
            RefreshInspector();
            RebuildTimeline();
            RefreshTimelineHeader();
            RefreshPreview();
        }

        /// <summary>
        /// タイムラインのドラッグ状態をリセットする
        /// </summary>
        private void ResetTimelineDragState() {
            _pendingDragFrameIndex = -1;
            _pendingDragMousePosition = Vector2.zero;
            _dragFrameIndex = -1;
            _dragInsertIndex = -1;
            _isFrameDragPending = false;
            _isDraggingFrame = false;
            if (rootVisualElement?.panel != null && rootVisualElement.HasMouseCapture()) {
                rootVisualElement.ReleaseMouse();
            }
        }

        /// <summary>
        /// フレーム編集可能か
        /// </summary>
        private bool CanEditClipFrames() {
            return _clip != null && _serializedClip != null && _spritesProperty != null;
        }

        /// <summary>
        /// Clip 編集を開始する
        /// </summary>
        /// <param name="undoName">Undo 名</param>
        /// <param name="includeWindowState">Window 状態も Undo 対象に含めるか</param>
        private void BeginClipEdit(string undoName, bool includeWindowState) {
            Undo.RecordObject(_clip, undoName);
            if (includeWindowState) {
                Undo.RecordObject(this, undoName);
            }

            _serializedClip.Update();
        }

        /// <summary>
        /// Clip 編集を反映する
        /// </summary>
        private void ApplyClipEdit() {
            _serializedClip.ApplyModifiedProperties();
            EditorUtility.SetDirty(_clip);
        }
    }
}
