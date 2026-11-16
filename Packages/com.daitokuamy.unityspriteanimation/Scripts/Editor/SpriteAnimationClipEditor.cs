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
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "_sprites", "_extensionSpriteLayers");
            DrawMainLayerFrames();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_extensionSpriteLayers"), new GUIContent("Extension Sprite Layers"), true);
            serializedObject.ApplyModifiedProperties();

            DrawFlipBookBlendWarnings(target as SpriteAnimationClip);

            EditorGUILayout.Space();

            if (GUILayout.Button(OpenWindowButtonLabel)) {
                SpriteAnimationClipEditorWindow.OpenWindow(target as SpriteAnimationClip);
            }
        }

        /// <summary>主レイヤーの枠数と各 Sprite を編集する</summary>
        private void DrawMainLayerFrames() {
            var sprites = serializedObject.FindProperty("_sprites");
            sprites.isExpanded = EditorGUILayout.Foldout(sprites.isExpanded, "Main Layer", true);
            if (!sprites.isExpanded) {
                return;
            }
            EditorGUI.indentLevel++;
            var count = Mathf.Max(0, EditorGUILayout.DelayedIntField("Total Frames", sprites.arraySize));
            if (count != sprites.arraySize) {
                var layers = serializedObject.FindProperty("_extensionSpriteLayers");
                for (var layerIndex = -1; layerIndex < layers.arraySize; layerIndex++) {
                    var frames = layerIndex < 0 ? sprites : layers.GetArrayElementAtIndex(layerIndex).FindPropertyRelative("_sprites");
                    var previousCount = frames.arraySize;
                    frames.arraySize = count;
                    for (var i = previousCount; i < count; i++) {
                        frames.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    }
                }
            }
            for (var i = 0; i < sprites.arraySize; i++) {
                EditorGUILayout.PropertyField(sprites.GetArrayElementAtIndex(i), new GUIContent($"Frame {i}"));
            }
            EditorGUILayout.HelpBox("Use the animation window to insert, remove, or reorder frames across all layers.", MessageType.Info);
            EditorGUI.indentLevel--;
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
