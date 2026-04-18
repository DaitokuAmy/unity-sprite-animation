using UnityEngine.Playables;

namespace UnitySpriteAnimation {
    /// <summary>
    /// SpriteAnimationTrack の clip 単位データ
    /// </summary>
    public sealed class SpriteAnimationPlayableBehaviour : PlayableBehaviour {
        /// <summary>再生する SpriteAnimationClip</summary>
        public SpriteAnimationClip AnimationClip { get; set; }
    }
}