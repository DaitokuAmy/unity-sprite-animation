using UnityEngine.Playables;

namespace UnitySpriteAnimation {
    /// <summary>
    /// SpriteAnimationTrack の最終評価を行う Mixer
    /// </summary>
    public sealed class SpriteAnimationTrackMixerBehaviour : PlayableBehaviour {
        private SpriteAnimator _lastSpriteAnimator;

        /// <inheritdoc />
        public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
            if (playerData is not SpriteAnimator spriteAnimator) {
                RestoreLastSpriteAnimator();
                return;
            }

            if (_lastSpriteAnimator != null && _lastSpriteAnimator != spriteAnimator) {
                _lastSpriteAnimator.EndExternalControl();
            }

            _lastSpriteAnimator = spriteAnimator;

            SpriteAnimationPlayableBehaviour selectedBehaviour = null;
            var selectedTime = 0.0;
            var selectedWeight = 0.0f;

            var inputCount = playable.GetInputCount();
            for (var i = 0; i < inputCount; i++) {
                var inputWeight = playable.GetInputWeight(i);
                if (inputWeight <= 0.0f) {
                    continue;
                }

                var inputPlayable = (ScriptPlayable<SpriteAnimationPlayableBehaviour>)playable.GetInput(i);
                var behaviour = inputPlayable.GetBehaviour();
                if (behaviour?.AnimationClip == null) {
                    continue;
                }

                if (inputWeight < selectedWeight) {
                    continue;
                }

                selectedBehaviour = behaviour;
                selectedTime = inputPlayable.GetTime();
                selectedWeight = inputWeight;
            }

            if (selectedBehaviour?.AnimationClip == null) {
                spriteAnimator.EndExternalControl();
                return;
            }

            spriteAnimator.BeginExternalControl();
            spriteAnimator.Evaluate(selectedBehaviour.AnimationClip, (float)selectedTime);
        }

        /// <inheritdoc />
        public override void OnGraphStop(Playable playable) {
            RestoreLastSpriteAnimator();
        }

        /// <inheritdoc />
        public override void OnPlayableDestroy(Playable playable) {
            RestoreLastSpriteAnimator();
        }

        /// <summary>
        /// 直近で制御していた SpriteAnimator の外部制御を解除する
        /// </summary>
        private void RestoreLastSpriteAnimator() {
            if (_lastSpriteAnimator == null) {
                return;
            }

            _lastSpriteAnimator.EndExternalControl();
            _lastSpriteAnimator = null;
        }
    }
}