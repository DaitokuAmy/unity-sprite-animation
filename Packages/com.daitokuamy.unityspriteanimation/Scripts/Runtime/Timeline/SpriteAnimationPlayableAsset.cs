using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnitySpriteAnimation {
    /// <summary>
    /// SpriteAnimationClip を Timeline clip として再生するための PlayableAsset
    /// </summary>
    public sealed class SpriteAnimationPlayableAsset : PlayableAsset, ITimelineClipAsset {
        [SerializeField, Tooltip("再生する SpriteAnimationClip")]
        private SpriteAnimationClip _animationClip;

        /// <summary>再生する SpriteAnimationClip</summary>
        public SpriteAnimationClip AnimationClip => _animationClip;

        /// <inheritdoc />
        public override double duration => _animationClip != null ? _animationClip.Duration : base.duration;

        /// <inheritdoc />
        public ClipCaps clipCaps => ClipCaps.ClipIn | ClipCaps.SpeedMultiplier | ClipCaps.Looping | ClipCaps.Extrapolation;

        /// <inheritdoc />
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
            var playable = ScriptPlayable<SpriteAnimationPlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.AnimationClip = _animationClip;
            return playable;
        }
    }
}