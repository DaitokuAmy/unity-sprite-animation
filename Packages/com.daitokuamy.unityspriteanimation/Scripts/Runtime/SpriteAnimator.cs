using UnityEngine;

namespace UnitySpriteAnimation {
    /// <summary>
    /// SpriteAnimationClip を再生するための基底コンポーネント
    /// </summary>
    public abstract class SpriteAnimator : MonoBehaviour {
        private const float FlipBookBlendEpsilon = 0.0001f;

        /// <summary>
        /// SpriteAnimator の更新タイミング
        /// </summary>
        public enum UpdateMode {
            /// <summary>LateUpdate で更新する</summary>
            LateUpdate = 0,

            /// <summary>Update で更新する</summary>
            Update = 1,

            /// <summary>手動で更新する</summary>
            ManualUpdate = 2
        }

        [SerializeField, Tooltip("初期再生する SpriteAnimationClip")]
        private SpriteAnimationClip _animationClip;

        [SerializeField, Tooltip("有効化時に再生を開始するか")]
        private bool _playOnEnabled = true;

        [SerializeField, Tooltip("再生更新を行うタイミング")]
        private UpdateMode _updateMode = UpdateMode.LateUpdate;

        private float _timeScale = 1.0f;
        private float _currentTime;
        private int _currentFrameIndex = -1;
        private bool _isPlaying;
        private bool _isFlipBookBlending;
        private float _flipBookBlendElapsedTime;
        private Sprite _currentSprite;
        private Sprite _previousSprite;
        private bool _hasLoopOverride;
        private bool _loopOverride;
        private bool _isExternalControlActive;
        private bool _hasExternalControlState;
        private SpriteAnimationClip _externalControlPreviousAnimationClip;
        private float _externalControlPreviousTime;
        private bool _externalControlPreviousIsPlaying;
        private bool _externalControlPreviousHasLoopOverride;
        private bool _externalControlPreviousLoopOverride;

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

        /// <summary>再生更新を行うタイミング</summary>
        public UpdateMode PlaybackUpdateMode {
            get => _updateMode;
            set => _updateMode = value;
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
        /// 更新処理
        /// </summary>
        protected virtual void UpdateInternal() {
        }

        /// <summary>
        /// 後更新処理
        /// </summary>
        protected virtual void LateUpdateInternal() {
        }

        /// <summary>
        /// 再生クリップを差し替える
        /// </summary>
        /// <param name="clip">差し替える再生クリップ</param>
        public void SetAnimationClip(SpriteAnimationClip clip) {
            _animationClip = clip;
            ClearLoopOverride();
            ResetPlaybackState();
            RefreshSprite(force: true);
        }

        /// <summary>
        /// 現在設定されているクリップで再生する
        /// </summary>
        public void Play() {
            ClearLoopOverride();
            PlayInternal();
        }

        /// <summary>
        /// 現在設定されているクリップでループ指定を上書きして再生する
        /// </summary>
        /// <param name="loop">ループ再生するか</param>
        public void Play(bool loop) {
            SetLoopOverride(loop);
            PlayInternal();
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
        /// 手動で再生状態を更新する
        /// </summary>
        /// <param name="deltaTime">今回の経過時間</param>
        public void ManualUpdate(float deltaTime) {
            UpdatePlayback(deltaTime);
        }

        /// <summary>
        /// 指定クリップを指定時刻で評価する
        /// </summary>
        /// <param name="clip">評価するクリップ</param>
        /// <param name="time">評価時刻</param>
        public void Evaluate(SpriteAnimationClip clip, float time) {
            EvaluateInternal(clip, time, hasLoopOverride: false, loop: false);
        }

        /// <summary>
        /// 指定クリップを指定時刻でループ指定を上書きして評価する
        /// </summary>
        /// <param name="clip">評価するクリップ</param>
        /// <param name="time">評価時刻</param>
        /// <param name="loop">ループ再生として評価するか</param>
        public void Evaluate(SpriteAnimationClip clip, float time, bool loop) {
            EvaluateInternal(clip, time, hasLoopOverride: true, loop: loop);
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
            _externalControlPreviousHasLoopOverride = _hasLoopOverride;
            _externalControlPreviousLoopOverride = _loopOverride;
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
            _hasLoopOverride = _externalControlPreviousHasLoopOverride;
            _loopOverride = _externalControlPreviousLoopOverride;
            _hasExternalControlState = false;
            _currentFrameIndex = -1;
            _isFlipBookBlending = false;
            _flipBookBlendElapsedTime = 0.0f;
            _previousSprite = null;
            RefreshSprite(force: true);
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
        /// 更新処理
        /// </summary>
        private void Update() {
            if (_updateMode != UpdateMode.Update) {
                return;
            }

            UpdatePlayback(Time.deltaTime);
            UpdateInternal();
        }

        /// <summary>
        /// 後更新処理
        /// </summary>
        private void LateUpdate() {
            if (_updateMode != UpdateMode.LateUpdate) {
                return;
            }

            UpdatePlayback(Time.deltaTime);
            LateUpdateInternal();
        }

        /// <summary>
        /// 再生状態を更新する
        /// </summary>
        /// <param name="deltaTime">今回の経過時間</param>
        private void UpdatePlayback(float deltaTime) {
            if (_isExternalControlActive) {
                return;
            }

            if (!_isPlaying || _animationClip == null) {
                return;
            }

            var duration = _animationClip.Duration;
            if (duration <= 0.0f) {
                Stop();
                return;
            }

            var safeDeltaTime = Mathf.Max(0.0f, deltaTime);
            var scaledDeltaTime = safeDeltaTime * Mathf.Max(0.0f, _timeScale);
            if (scaledDeltaTime <= 0.0f) {
                return;
            }

            _currentTime += scaledDeltaTime;
            if (GetEffectiveLoop(_animationClip)) {
                _currentTime = Mathf.Repeat(_currentTime, duration);
                RefreshSprite(force: false);
                UpdateFlipBookBlend(safeDeltaTime);
                return;
            }

            if (_currentTime >= duration) {
                _currentTime = duration;
                RefreshSprite(force: false);
                UpdateFlipBookBlend(safeDeltaTime);
                _isPlaying = false;
                return;
            }

            RefreshSprite(force: false);
            UpdateFlipBookBlend(safeDeltaTime);
        }

        /// <summary>
        /// 現在設定されているクリップで再生する
        /// </summary>
        private void PlayInternal() {
            ResetPlaybackState();
            _isPlaying = _animationClip != null && _animationClip.CanPlay;
            RefreshSprite(force: true);
        }

        /// <summary>
        /// 指定クリップを指定時刻で評価する
        /// </summary>
        /// <param name="clip">評価するクリップ</param>
        /// <param name="time">評価時刻</param>
        /// <param name="hasLoopOverride">ループ指定を上書きするか</param>
        /// <param name="loop">上書きするループ指定</param>
        private void EvaluateInternal(SpriteAnimationClip clip, float time, bool hasLoopOverride, bool loop) {
            _animationClip = clip;
            _hasLoopOverride = hasLoopOverride;
            _loopOverride = loop;
            _currentTime = SanitizeTime(clip, time, GetEffectiveLoop(clip));
            _isPlaying = false;
            _currentFrameIndex = -1;
            _isFlipBookBlending = false;
            _flipBookBlendElapsedTime = 0.0f;
            _previousSprite = null;
            RefreshSprite(force: true);
        }

        /// <summary>
        /// ループ上書き指定を設定する
        /// </summary>
        /// <param name="loop">上書きするループ指定</param>
        private void SetLoopOverride(bool loop) {
            _hasLoopOverride = true;
            _loopOverride = loop;
        }

        /// <summary>
        /// ループ上書き指定を解除する
        /// </summary>
        private void ClearLoopOverride() {
            _hasLoopOverride = false;
            _loopOverride = false;
        }

        /// <summary>
        /// 実際に使用するループ指定を取得する
        /// </summary>
        /// <param name="clip">対象クリップ</param>
        /// <returns>ループ再生する場合 true</returns>
        private bool GetEffectiveLoop(SpriteAnimationClip clip) {
            return _hasLoopOverride ? _loopOverride : clip != null && clip.Loop;
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

            var loop = GetEffectiveLoop(_animationClip);
            var frameIndex = _animationClip.GetFrameIndex(_currentTime, loop);
            if (!force && frameIndex == _currentFrameIndex) {
                return;
            }

            var previousFrameIndex = _currentFrameIndex;
            var previousSprite = _currentSprite;
            _currentFrameIndex = frameIndex;
            _currentSprite = _animationClip.GetSprite(_currentTime, loop);

            if (CanStartFlipBookBlend(previousFrameIndex, previousSprite, _currentSprite)) {
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
        /// <param name="previousSprite">遷移元 Sprite</param>
        /// <param name="currentSprite">遷移先 Sprite</param>
        private bool CanStartFlipBookBlend(int previousFrameIndex, Sprite previousSprite, Sprite currentSprite) {
            if (previousFrameIndex < 0) {
                return false;
            }

            if (!CanUseFlipBookBlendSprites(previousSprite, currentSprite)) {
                return false;
            }

            return GetEffectiveFlipBookBlendDuration() > 0.0f;
        }

        /// <summary>
        /// 指定 Sprite の組み合わせで FlipBookBlend を使えるか判定する
        /// </summary>
        /// <param name="previousSprite">遷移元 Sprite</param>
        /// <param name="currentSprite">遷移先 Sprite</param>
        /// <returns>FlipBookBlend を使える場合 true</returns>
        private bool CanUseFlipBookBlendSprites(Sprite previousSprite, Sprite currentSprite) {
            return IsFlipBookBlendCompatibleSprite(previousSprite) &&
                   IsFlipBookBlendCompatibleSprite(currentSprite);
        }

        /// <summary>
        /// Sprite が FlipBookBlend で扱える packing か判定する
        /// </summary>
        /// <param name="sprite">判定対象 Sprite</param>
        /// <returns>FlipBookBlend と互換性がある場合 true</returns>
        private bool IsFlipBookBlendCompatibleSprite(Sprite sprite) {
            if (sprite == null) {
                return false;
            }

            return sprite.packingMode != SpritePackingMode.Tight;
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

            if (!CanUseFlipBookBlendSprites(_previousSprite, _currentSprite)) {
                _isFlipBookBlending = false;
                _previousSprite = null;
                ApplySingleSprite(_currentSprite);
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
        /// <param name="loop">ループ再生として補正するか</param>
        /// <returns>補正後の時刻</returns>
        private float SanitizeTime(SpriteAnimationClip clip, float time, bool loop) {
            if (clip == null || !clip.CanPlay) {
                return 0.0f;
            }

            if (loop) {
                return Mathf.Repeat(time, clip.Duration);
            }

            return Mathf.Clamp(time, 0.0f, clip.Duration);
        }
    }
}
