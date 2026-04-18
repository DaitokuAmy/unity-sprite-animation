using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnitySpriteAnimation.Editor {
    /// <summary>
    /// Sprite Unlit shader 用のインスペクタ拡張
    /// </summary>
    public sealed class SpriteUnlitShaderGUI : ShaderGUI {
        private enum SurfaceType {
            Opaque = 0,
            Transparent = 1
        }

        private enum BlendMode {
            Alpha = 0,
            Premultiply = 1,
            Additive = 2,
            Multiply = 3
        }

        private sealed class Styles {
            public static readonly string[] SurfaceTypeNames = { "Opaque", "Transparent" };
            public static readonly string[] BlendModeNames = { "Alpha", "Premultiply", "Additive", "Multiply" };
            public static readonly GUIContent SurfaceOptionsHeader = EditorGUIUtility.TrTextContent("Surface Options");
            public static readonly GUIContent SurfaceType = EditorGUIUtility.TrTextContent("Surface Type");
            public static readonly GUIContent BlendMode = EditorGUIUtility.TrTextContent("Blending Mode");
            public static readonly GUIContent AlphaClip = EditorGUIUtility.TrTextContent("Alpha Clipping");
            public static readonly GUIContent Cutoff = EditorGUIUtility.TrTextContent("Threshold");
            public static readonly GUIContent SurfaceInputsHeader = EditorGUIUtility.TrTextContent("Surface Inputs");
            public static readonly GUIContent BaseMap = EditorGUIUtility.TrTextContent("Base Map");
            public static readonly GUIContent FlipBookBlendHeader = EditorGUIUtility.TrTextContent("FlipBook Blend");
            public static readonly GUIContent RuntimeControlledMessage = EditorGUIUtility.TrTextContent("These properties are controlled at runtime by RendererSpriteAnimator.");
            public static readonly GUIContent PreviousTexture = EditorGUIUtility.TrTextContent("Previous Texture");
            public static readonly GUIContent CurrentUvRect = EditorGUIUtility.TrTextContent("Current UV Rect");
            public static readonly GUIContent PreviousUvRect = EditorGUIUtility.TrTextContent("Previous UV Rect");
            public static readonly GUIContent BlendParams = EditorGUIUtility.TrTextContent("Blend Params");
            public static readonly GUIContent AdvancedOptionsHeader = EditorGUIUtility.TrTextContent("Advanced Options");
        }

        private MaterialEditor _materialEditor;
        private MaterialProperty _mainTex;
        private MaterialProperty _color;
        private MaterialProperty _surface;
        private MaterialProperty _blend;
        private MaterialProperty _alphaClip;
        private MaterialProperty _cutoff;
        private MaterialProperty _prevTex;
        private MaterialProperty _currentTexUVRect;
        private MaterialProperty _prevTexUVRect;
        private MaterialProperty _flipBookBlendParams;
        private bool _isFirstTimeApply = true;

        /// <inheritdoc />
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
            _materialEditor = materialEditor;

            FindProperties(properties);

            if (materialEditor.target is not Material) {
                return;
            }

            EditorGUI.BeginChangeCheck();

            DrawSurfaceOptions();

            EditorGUILayout.Space();

            DrawSurfaceInputs();

            EditorGUILayout.Space();

            DrawFlipBookBlendInputs();

            EditorGUILayout.Space();

            DrawAdvancedOptions();

            if (EditorGUI.EndChangeCheck() || _isFirstTimeApply) {
                ApplyMaterialSettings();
                _isFirstTimeApply = false;
            }
        }

        /// <inheritdoc />
        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader) {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            SetupMaterialBlendMode(material);
        }

        /// <summary>
        /// 使用する shader property を解決する
        /// </summary>
        /// <param name="properties">描画対象の property 一覧</param>
        private void FindProperties(MaterialProperty[] properties) {
            _mainTex = FindProperty("_MainTex", properties);
            _color = FindProperty("_Color", properties, false);
            _surface = FindProperty("_Surface", properties, false);
            _blend = FindProperty("_Blend", properties, false);
            _alphaClip = FindProperty("_AlphaClip", properties, false);
            _cutoff = FindProperty("_Cutoff", properties, false);
            _prevTex = FindProperty("_PrevTex", properties, false);
            _currentTexUVRect = FindProperty("_CurrentTexUVRect", properties, false);
            _prevTexUVRect = FindProperty("_PrevTexUVRect", properties, false);
            _flipBookBlendParams = FindProperty("_FlipBookBlendParams", properties, false);
        }

        /// <summary>
        /// Surface Options セクションを描画する
        /// </summary>
        private void DrawSurfaceOptions() {
            EditorGUILayout.LabelField(Styles.SurfaceOptionsHeader, EditorStyles.boldLabel);
            DrawPopupProperty(_surface, Styles.SurfaceType, Styles.SurfaceTypeNames);

            if ((SurfaceType)_surface.floatValue == SurfaceType.Transparent) {
                DrawPopupProperty(_blend, Styles.BlendMode, Styles.BlendModeNames);
            }

            DrawToggleProperty(_alphaClip, Styles.AlphaClip);

            if (_alphaClip is { floatValue: >= 0.5f }) {
                DrawShaderProperty(_cutoff, Styles.Cutoff);
            }
        }

        /// <summary>
        /// Surface Inputs セクションを描画する
        /// </summary>
        private void DrawSurfaceInputs() {
            EditorGUILayout.LabelField(Styles.SurfaceInputsHeader, EditorStyles.boldLabel);
            _materialEditor.TexturePropertySingleLine(Styles.BaseMap, _mainTex, _color);
        }

        /// <summary>
        /// FlipBookBlend 用のランタイム制御 property を描画する
        /// </summary>
        private void DrawFlipBookBlendInputs() {
            EditorGUILayout.LabelField(Styles.FlipBookBlendHeader, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Styles.RuntimeControlledMessage.text, MessageType.Info);

            using (new EditorGUI.DisabledGroupScope(true)) {
                DrawOptionalTextureProperty(_prevTex, Styles.PreviousTexture);
                DrawOptionalVectorProperty(_currentTexUVRect, Styles.CurrentUvRect);
                DrawOptionalVectorProperty(_prevTexUVRect, Styles.PreviousUvRect);
                DrawOptionalVectorProperty(_flipBookBlendParams, Styles.BlendParams);
            }
        }

        /// <summary>
        /// Advanced Options セクションを描画する
        /// </summary>
        private void DrawAdvancedOptions() {
            EditorGUILayout.LabelField(Styles.AdvancedOptionsHeader, EditorStyles.boldLabel);
            _materialEditor.EnableInstancingField();
            _materialEditor.DoubleSidedGIField();
        }

        /// <summary>
        /// 存在する場合のみテクスチャ property を描画する
        /// </summary>
        /// <param name="property">対象 property</param>
        /// <param name="label">表示ラベル</param>
        private void DrawOptionalTextureProperty(MaterialProperty property, GUIContent label) {
            if (property == null) {
                return;
            }

            _materialEditor.TexturePropertySingleLine(label, property);
        }

        /// <summary>
        /// 存在する場合のみ Vector property を描画する
        /// </summary>
        /// <param name="property">対象 property</param>
        /// <param name="label">表示ラベル</param>
        private void DrawOptionalVectorProperty(MaterialProperty property, GUIContent label) {
            if (property == null) {
                return;
            }

            _materialEditor.VectorProperty(property, label.text);
        }

        /// <summary>
        /// Popup 形式で MaterialProperty を描画する
        /// </summary>
        /// <param name="property">対象 property</param>
        /// <param name="label">表示ラベル</param>
        /// <param name="displayedOptions">選択肢一覧</param>
        private void DrawPopupProperty(MaterialProperty property, GUIContent label, string[] displayedOptions) {
            if (property == null) {
                return;
            }

            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            var selectedIndex = EditorGUILayout.Popup(label, (int)property.floatValue, displayedOptions);
            if (EditorGUI.EndChangeCheck()) {
                property.floatValue = selectedIndex;
            }

            EditorGUI.showMixedValue = false;
        }

        /// <summary>
        /// 通常の shader property を描画する
        /// </summary>
        /// <param name="property">対象 property</param>
        /// <param name="label">表示ラベル</param>
        private void DrawShaderProperty(MaterialProperty property, GUIContent label) {
            if (property == null) {
                return;
            }

            _materialEditor.ShaderProperty(property, label);
        }

        /// <summary>
        /// Toggle 形式で MaterialProperty を描画する
        /// </summary>
        /// <param name="property">対象 property</param>
        /// <param name="label">表示ラベル</param>
        private void DrawToggleProperty(MaterialProperty property, GUIContent label) {
            if (property == null) {
                return;
            }

            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            var value = EditorGUILayout.Toggle(label, property.floatValue >= 0.5f);
            if (EditorGUI.EndChangeCheck()) {
                property.floatValue = value ? 1.0f : 0.0f;
            }

            EditorGUI.showMixedValue = false;
        }

        /// <summary>
        /// 編集対象 Material に Blend 設定を適用する
        /// </summary>
        private void ApplyMaterialSettings() {
            foreach (var target in _materialEditor.targets) {
                if (target is Material material) {
                    SetupMaterialBlendMode(material);
                }
            }
        }

        /// <summary>
        /// Material の SurfaceType と BlendMode に応じて描画設定を更新する
        /// </summary>
        /// <param name="material">更新対象 Material</param>
        private static void SetupMaterialBlendMode(Material material) {
            if (material == null) {
                return;
            }

            var surfaceType = SurfaceType.Transparent;
            if (material.HasProperty("_Surface")) {
                surfaceType = (SurfaceType)material.GetFloat("_Surface");
            }

            var blendMode = BlendMode.Alpha;
            if (material.HasProperty("_Blend")) {
                blendMode = (BlendMode)material.GetFloat("_Blend");
            }

            var alphaClip = material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") >= 0.5f;

            material.SetFloat("_BlendOp", (float)BlendOp.Add);
            material.SetFloat("_Cull", (float)CullMode.Off);

            var renderQueue = surfaceType == SurfaceType.Opaque
                ? (int)RenderQueue.Geometry
                : (int)RenderQueue.Transparent;

            if (alphaClip) {
                material.EnableKeyword("_ALPHATEST_ON");
            }
            else {
                material.DisableKeyword("_ALPHATEST_ON");
            }

            if (surfaceType == SurfaceType.Opaque) {
                material.SetOverrideTag("RenderType", alphaClip ? "TransparentCutout" : "Opaque");
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.Zero);
                material.SetFloat("_ZWrite", 1.0f);
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.DisableKeyword("_ALPHAMODULATE_ON");

                if (alphaClip) {
                    renderQueue = (int)RenderQueue.AlphaTest;
                }
            }
            else {
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat("_ZWrite", 0.0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

                var srcBlendRgb = UnityEngine.Rendering.BlendMode.SrcAlpha;
                var dstBlendRgb = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                var srcBlendAlpha = UnityEngine.Rendering.BlendMode.One;
                var dstBlendAlpha = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                var useAlphaModulate = false;

                switch (blendMode) {
                    case BlendMode.Alpha:
                        srcBlendRgb = UnityEngine.Rendering.BlendMode.SrcAlpha;
                        dstBlendRgb = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                        srcBlendAlpha = UnityEngine.Rendering.BlendMode.One;
                        dstBlendAlpha = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                        break;
                    case BlendMode.Premultiply:
                        srcBlendRgb = UnityEngine.Rendering.BlendMode.One;
                        dstBlendRgb = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                        srcBlendAlpha = UnityEngine.Rendering.BlendMode.One;
                        dstBlendAlpha = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                        break;
                    case BlendMode.Additive:
                        srcBlendRgb = UnityEngine.Rendering.BlendMode.SrcAlpha;
                        dstBlendRgb = UnityEngine.Rendering.BlendMode.One;
                        srcBlendAlpha = UnityEngine.Rendering.BlendMode.One;
                        dstBlendAlpha = UnityEngine.Rendering.BlendMode.One;
                        break;
                    case BlendMode.Multiply:
                        srcBlendRgb = UnityEngine.Rendering.BlendMode.DstColor;
                        dstBlendRgb = UnityEngine.Rendering.BlendMode.Zero;
                        srcBlendAlpha = UnityEngine.Rendering.BlendMode.Zero;
                        dstBlendAlpha = UnityEngine.Rendering.BlendMode.One;
                        useAlphaModulate = true;
                        break;
                }

                material.SetFloat("_SrcBlend", (float)srcBlendRgb);
                material.SetFloat("_DstBlend", (float)dstBlendRgb);
                material.SetFloat("_SrcBlendAlpha", (float)srcBlendAlpha);
                material.SetFloat("_DstBlendAlpha", (float)dstBlendAlpha);

                if (blendMode == BlendMode.Premultiply) {
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                }
                else {
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                }

                if (useAlphaModulate) {
                    material.EnableKeyword("_ALPHAMODULATE_ON");
                }
                else {
                    material.DisableKeyword("_ALPHAMODULATE_ON");
                }
            }

            if (material.renderQueue != renderQueue) {
                material.renderQueue = renderQueue;
            }
        }
    }
}
