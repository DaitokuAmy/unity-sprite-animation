using UnityEditor;
using UnityEngine;

namespace UnitySpriteAnimation.Editor {
    /// <summary>
    /// SpriteAnimationClip 用のインスペクタ拡張
    /// </summary>
    [CustomEditor(typeof(SpriteAnimationClip))]
    public sealed class SpriteAnimationClipEditor : UnityEditor.Editor {
        private static readonly GUIContent OpenWindowButtonLabel = new("Open Sprite Animation Window");

        /// <inheritdoc />
        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            DrawFlipBookBlendWarnings(target as SpriteAnimationClip);

            EditorGUILayout.Space();

            if (GUILayout.Button(OpenWindowButtonLabel)) {
                SpriteAnimationClipEditorWindow.OpenWindow(target as SpriteAnimationClip);
            }
        }

        /// <summary>
        /// FlipBookBlend 設定に関する警告を表示する
        /// </summary>
        /// <param name="clip">対象Clip</param>
        private static void DrawFlipBookBlendWarnings(SpriteAnimationClip clip) {
            if (clip == null || !clip.EnableFlipBookBlend) {
                return;
            }

            var frameDuration = 1.0f / Mathf.Max(0.01f, clip.FrameRate);
            if (clip.FlipBookBlendDuration <= frameDuration) {
                return;
            }

            EditorGUILayout.HelpBox(
                $"FlipBookBlendDuration is too long for the current FrameRate. Runtime playback clamps it below one frame ({frameDuration:0.###} sec).",
                MessageType.Warning);
        }
    }
}
