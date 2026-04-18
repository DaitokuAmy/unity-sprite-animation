using UnityEngine;

namespace UnitySpriteAnimation {
    /// <summary>
    /// SpriteAnimationClip を再生するための基底コンポーネント
    /// </summary>
    public abstract class SpriteAnimator : MonoBehaviour {
        private const float FlipBookBlendEpsilon = 0.0001f;

        [SerializeField, Tooltip("初期再生する SpriteAnimationClip")]
        private SpriteAnimationClip _animationClip;

        [SerializeField, Tooltip("有効化時に再生を開始するか")]
        private bool _playOnEnabled = true;

        private float _timeScale = 1.0f;
        private float _currentTime;
        private int _currentFrameIndex = -1;
        private bool _isPlaying;
        private bool _isFlipBookBlending;
        private float _flipBookBlendElapsedTime;
        private Sprite _currentSprite;
        private Sprite _previousSprite;
        private bool _isExternalControlActive;
        private bool _hasExternalControlState;
        private SpriteAnimationClip _externalControlPreviousAnimationClip;
        private float _externalControlPreviousTime;
        private bool _externalControlPreviousIsPlaying;

        /// <summary>再生速度倍率</summary>
        public float TimeScale {
            get => _timeScale;
            set => _timeScale = Mathf.Max(0.0f, value);
        }

        /// <summary>現在のアニメーションクリップ</summary>
        public SpriteAnimationClip AnimationClip => _animationClip;

        /// <summary>有効化時に自動再生するか</summary>
        public bool PlayOnEnabled {
            get => _playOnEnabled;
            set => _playOnEnabled = value;
        }

        /// <summary>再生中か</summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>現在の経過時間</summary>
        public float CurrentTime => _currentTime;

        /// <summary>現在のフレーム番号</summary>
        public int CurrentFrameIndex => _currentFrameIndex;

        /// <summary>
        /// FlipBookBlend 用の2枚目を使えるか
        /// </summary>
        protected abstract bool CanUseFlipBookBlend { get; }

        /// <summary>
        /// 表示対象へ Sprite を単体適用する
        /// </summary>
        /// <param name="sprite">適用する Sprite</param>
        protected abstract void ApplySingleSprite(Sprite sprite);

        /// <summary>
        /// 表示対象へ FlipBookBlend 状態を適用する
        /// </summary>
        /// <param name="fromSprite">遷移元Sprite</param>
        /// <param name="toSprite">遷移先Sprite</param>
        /// <param name="fadeProgress">0.0-1.0 の補間率</param>
        protected abstract void ApplyFlipBookBlend(Sprite fromSprite, Sprite toSprite, float fadeProgress);

        /// <summary>
        /// 再生クリップを差し替える
        /// </summary>
        /// <param name="clip">差し替える再生クリップ</param>
        public void SetAnimationClip(SpriteAnimationClip clip) {
            _animationClip = clip;
            ResetPlaybackState();
            RefreshSprite(force: true);
        }

        /// <summary>
        /// 現在設定されているクリップで再生する
        /// </summary>
        public void Play() {
            ResetPlaybackState();
            _isPlaying = _animationClip != null && _animationClip.CanPlay;
            RefreshSprite(force: true);
        }

        /// <summary>
        /// 一時停止する
        /// </summary>
        public void Pause() {
            _isPlaying = false;
        }

        /// <summary>
        /// 一時停止状態から再開する
        /// </summary>
        public void Resume() {
            if (_animationClip == null || !_animationClip.CanPlay) {
                return;
            }

            _isPlaying = true;
        }

        /// <summary>
        /// 再生を停止して先頭フレームへ戻す
        /// </summary>
        public void Stop() {
            ResetPlaybackState();
            RefreshSprite(force: true);
        }

        /// <summary>
        /// 外部制御を開始する
        /// </summary>
        internal void BeginExternalControl() {
            if (_isExternalControlActive) {
                return;
            }

            _externalControlPreviousAnimationClip = _animationClip;
            _externalControlPreviousTime = _currentTime;
            _externalControlPreviousIsPlaying = _isPlaying;
            _hasExternalControlState = true;
            _isExternalControlActive = true;
            _isPlaying = false;
        }

        /// <summary>
        /// 外部制御を終了する
        /// </summary>
        internal void EndExternalControl() {
            if (!_isExternalControlActive) {
                return;
            }

            _isExternalControlActive = false;
            if (!_hasExternalControlState) {
                return;
            }

            _animationClip = _externalControlPreviousAnimationClip;
            _currentTime = _externalControlPreviousTime;
            _isPlaying = _externalControlPreviousIsPlaying;
            _hasExternalControlState = false;
            _currentFrameIndex = -1;
            _isFlipBookBlending = false;
            _flipBookBlendElapsedTime = 0.0f;
            _previousSprite = null;
            RefreshSprite(force: true);
        }

        /// <summary>
        /// 指定クリップを指定時刻で評価する
        /// </summary>
        /// <param name="clip">評価するクリップ</param>
        /// <param name="time">評価時刻</param>
        public void Evaluate(SpriteAnimationClip clip, float time) {
            _animationClip = clip;
            _currentTime = SanitizeTime(clip, time);
            _isPlaying = false;
            _currentFrameIndex = -1;
            _isFlipBookBlending = false;
            _flipBookBlendElapsedTime = 0.0f;
            _previousSprite = null;
            RefreshSprite(force: true);
        }

        /// <summary>
        /// 生成時処理
        /// </summary>
        protected virtual void AwakeInternal() {
        }

        /// <summary>
        /// 有効化時処理
        /// </summary>
        protected virtual void OnEnableInternal() {
        }

        /// <summary>
        /// 後更新処理
        /// </summary>
        protected virtual void LateUpdateInternal() {
        }

        /// <summary>
        /// 生成時処理
        /// </summary>
        private void Awake() {
            AwakeInternal();
            RefreshSprite(force: true);
        }

        /// <summary>
        /// 有効化時処理
        /// </summary>
        private void OnEnable() {
            if (_playOnEnabled) {
                Play();
            }

            OnEnableInternal();
        }

        /// <summary>
        /// 後更新処理
        /// </summary>
        private void LateUpdate() {
            if (_isExternalControlActive) {
                LateUpdateInternal();
                return;
            }

            if (!_isPlaying || _animationClip == null) {
                LateUpdateInternal();
                return;
            }

            var duration = _animationClip.Duration;
            if (duration <= 0.0f) {
                Stop();
                LateUpdateInternal();
                return;
            }

            var deltaTime = UnityEngine.Time.deltaTime * Mathf.Max(0.0f, _timeScale);
            if (deltaTime <= 0.0f) {
                LateUpdateInternal();
                return;
            }

            _currentTime += deltaTime;
            if (_animationClip.Loop) {
                _currentTime = Mathf.Repeat(_currentTime, duration);
                RefreshSprite(force: false);
                UpdateFlipBookBlend(UnityEngine.Time.deltaTime);
                LateUpdateInternal();
                return;
            }

            if (_currentTime >= duration) {
                _currentTime = duration;
                RefreshSprite(force: false);
                UpdateFlipBookBlend(UnityEngine.Time.deltaTime);
                _isPlaying = false;
                LateUpdateInternal();
                return;
            }

            RefreshSprite(force: false);
            UpdateFlipBookBlend(UnityEngine.Time.deltaTime);
            LateUpdateInternal();
        }

        /// <summary>
        /// 再生状態を初期化する
        /// </summary>
        private void ResetPlaybackState() {
            _isPlaying = false;
            _currentTime = 0.0f;
            _currentFrameIndex = -1;
            _isFlipBookBlending = false;
            _flipBookBlendElapsedTime = 0.0f;
            _currentSprite = null;
            _previousSprite = null;
        }

        /// <summary>
        /// 現在時間に対応する Sprite を反映する
        /// </summary>
        /// <param name="force">同一フレームでも反映するか</param>
        private void RefreshSprite(bool force) {
            if (_animationClip == null || !_animationClip.CanPlay) {
                _currentFrameIndex = -1;
                _currentSprite = null;
                _previousSprite = null;
                _isFlipBookBlending = false;
                ApplySingleSprite(null);
                return;
            }

            var frameIndex = _animationClip.GetFrameIndex(_currentTime);
            if (!force && frameIndex == _currentFrameIndex) {
                return;
            }

            var previousFrameIndex = _currentFrameIndex;
            var previousSprite = _currentSprite;
            _currentFrameIndex = frameIndex;
            _currentSprite = _animationClip.GetSprite(_currentTime);

            if (CanStartFlipBookBlend(previousFrameIndex)) {
                _previousSprite = previousSprite;
                _flipBookBlendElapsedTime = 0.0f;
                _isFlipBookBlending = true;
                ApplyFlipBookBlend(_previousSprite, _currentSprite, 0.0f);
                return;
            }

            _previousSprite = null;
            _isFlipBookBlending = false;
            ApplySingleSprite(_currentSprite);
        }

        /// <summary>
        /// FlipBookBlend 開始可能か
        /// </summary>
        /// <param name="previousFrameIndex">遷移前フレーム番号</param>
        private bool CanStartFlipBookBlend(int previousFrameIndex) {
            if (previousFrameIndex < 0) {
                return false;
            }

            return GetEffectiveFlipBookBlendDuration() > 0.0f;
        }

        /// <summary>
        /// 実際に使用する FlipBookBlend 時間を取得する
        /// </summary>
        private float GetEffectiveFlipBookBlendDuration() {
            if (_animationClip == null ||
                !_animationClip.EnableFlipBookBlend ||
                !_animationClip.CanPlay ||
                !CanUseFlipBookBlend) {
                return 0.0f;
            }

            var safeTimeScale = Mathf.Max(0.0f, _timeScale);
            if (safeTimeScale <= 0.0f) {
                return 0.0f;
            }

            var frameDuration = 1.0f / Mathf.Max(0.01f, _animationClip.FrameRate);
            var frameDurationAfterTimeScale = frameDuration / safeTimeScale;
            var maxFlipBookBlendDuration = Mathf.Max(0.0f, frameDurationAfterTimeScale - FlipBookBlendEpsilon);
            return Mathf.Min(_animationClip.FlipBookBlendDuration, maxFlipBookBlendDuration);
        }

        /// <summary>
        /// FlipBookBlend 状態を更新する
        /// </summary>
        /// <param name="deltaTime">今回の経過時間</param>
        private void UpdateFlipBookBlend(float deltaTime) {
            if (!_isFlipBookBlending) {
                return;
            }

            var duration = GetEffectiveFlipBookBlendDuration();
            if (duration <= 0.0f) {
                _isFlipBookBlending = false;
                _previousSprite = null;
                ApplySingleSprite(_currentSprite);
                return;
            }

            _flipBookBlendElapsedTime += Mathf.Max(0.0f, deltaTime);
            var fadeProgress = Mathf.Clamp01(_flipBookBlendElapsedTime / duration);
            ApplyFlipBookBlend(_previousSprite, _currentSprite, fadeProgress);

            if (fadeProgress < 1.0f) {
                return;
            }

            _isFlipBookBlending = false;
            _previousSprite = null;
            ApplySingleSprite(_currentSprite);
        }

        /// <summary>
        /// 指定クリップに対して安全な評価時刻へ補正する
        /// </summary>
        /// <param name="clip">評価対象クリップ</param>
        /// <param name="time">補正前の時刻</param>
        /// <returns>補正後の時刻</returns>
        private static float SanitizeTime(SpriteAnimationClip clip, float time) {
            if (clip == null || !clip.CanPlay) {
                return 0.0f;
            }

            if (clip.Loop) {
                return Mathf.Repeat(time, clip.Duration);
            }

            return Mathf.Clamp(time, 0.0f, clip.Duration);
        }

        /// <summary>
        /// FlipBookBlend 用の Material property を更新する
        /// </summary>
        /// <param name="material">更新対象 Material</param>
        /// <param name="currentSprite">現在表示する Sprite</param>
        /// <param name="previousSprite">遷移元 Sprite</param>
        /// <param name="fadeProgress">0.0-1.0 の補間率</param>
        protected void ApplyFlipBookBlendMaterialProperties(Material material, Sprite currentSprite, Sprite previousSprite, float fadeProgress) {
            MaterialUtility.ApplyProperties(material, currentSprite, previousSprite, fadeProgress);
        }

        /// <summary>
        /// FlipBookBlend 用の Material property を通常表示状態へ戻す
        /// </summary>
        /// <param name="material">更新対象 Material</param>
        /// <param name="currentSprite">現在表示する Sprite</param>
        protected void ResetFlipBookBlendMaterialProperties(Material material, Sprite currentSprite) {
            MaterialUtility.ResetProperties(material, currentSprite);
        }

        /// <summary>
        /// FlipBookBlend 用 property を持つ Material か判定する
        /// </summary>
        /// <param name="material">判定対象 Material</param>
        /// <returns>対応している場合 true</returns>
        protected bool SupportsFlipBookBlendMaterial(Material material) {
            return MaterialUtility.SupportsMaterial(material);
        }
    }
}
