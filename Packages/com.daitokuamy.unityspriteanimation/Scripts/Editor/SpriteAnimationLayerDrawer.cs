using UnityEditor;
using UnityEngine;

namespace UnitySpriteAnimation.Editor {
    /// <summary>主レイヤーと同じ枠数で拡張レイヤーを編集する</summary>
    [CustomPropertyDrawer(typeof(SpriteAnimationLayer))]
    public sealed class SpriteAnimationLayerDrawer : PropertyDrawer {
        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            var count = (property.serializedObject.targetObject as SpriteAnimationClip)?.FrameCount ?? 0;
            var lines = property.isExpanded ? count + 3 : 1;
            return lines * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            position.height = EditorGUIUtility.singleLineHeight;
            var name = property.FindPropertyRelative("_layerName");
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, name.stringValue, true);
            if (property.isExpanded) {
                EditorGUI.indentLevel++;
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(position, name, new GUIContent("Layer Name", "Renaming requires updating Animator bindings. Names must be nonempty and unique."));
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("_placement"), new GUIContent("Preview Placement"));
                var sprites = property.FindPropertyRelative("_sprites");
                var count = (property.serializedObject.targetObject as SpriteAnimationClip)?.FrameCount ?? 0;
                var previousCount = sprites.arraySize;
                if (previousCount != count) {
                    sprites.arraySize = count;
                    for (var i = previousCount; i < count; i++) {
                        sprites.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    }
                }
                for (var i = 0; i < count; i++) {
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(position, sprites.GetArrayElementAtIndex(i), new GUIContent($"Frame {i}"));
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }
    }
}