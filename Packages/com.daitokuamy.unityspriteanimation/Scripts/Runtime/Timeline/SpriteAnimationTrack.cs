using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnitySpriteAnimation {
    /// <summary>
    /// SpriteAnimator を Timeline から駆動する Track
    /// </summary>
    [TrackBindingType(typeof(SpriteAnimator))]
    [TrackClipType(typeof(SpriteAnimationPlayableAsset))]
    [TrackColor(0.4f, 0.8f, 0.6f)]
    public sealed class SpriteAnimationTrack : TrackAsset {
        /// <inheritdoc />
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
            return ScriptPlayable<SpriteAnimationTrackMixerBehaviour>.Create(graph, inputCount);
        }
    }
}