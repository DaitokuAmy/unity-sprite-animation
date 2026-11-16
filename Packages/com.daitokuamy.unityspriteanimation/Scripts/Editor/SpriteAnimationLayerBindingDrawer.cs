using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UnitySpriteAnimation.Editor {
    /// <summary>Animator に対応する表示先型でレイヤー参照を編集する</summary>
    [CustomPropertyDrawer(typeof(SpriteAnimationLayerBinding))]
    public sealed class SpriteAnimationLayerBindingDrawer : PropertyDrawer {
        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return (EditorGUIUtility.singleLineHeight * 2.0f) + EditorGUIUtility.standardVerticalSpacing;
        }

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("_layerName"), new GUIContent("Layer Name"));
            position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
            var target = property.FindPropertyRelative("_target");
            var targetType = property.serializedObject.targetObject is ImageSpriteAnimator ? typeof(Image) : typeof(SpriteRenderer);
            EditorGUI.BeginChangeCheck();
            var value = EditorGUI.ObjectField(position, "Target", target.objectReferenceValue, targetType, true);
            if (EditorGUI.EndChangeCheck()) {
                target.objectReferenceValue = value;
            }
            EditorGUI.EndProperty();
        }
    }
}