using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnitySpriteAnimation.Editor {
    /// <summary>
    /// SpriteAnimationClipEditorWindow のプレビュー関連処理
    /// </summary>
    public sealed partial class SpriteAnimationClipEditorWindow {
        private const string PreviewShaderName = "Unity Sprite Animation/UI Default";
        private const int PreviewShaderPassIndex = 0;
        private const int SpriteMeshPreviewMaxTextureSize = 2048;
        private const bool EnablePreviewFlipBookBlendSimulation = false;

        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private static readonly int MainTexPropertyId = Shader.PropertyToID("_MainTex");
        private static readonly int BlendOpPropertyId = Shader.PropertyToID("_BlendOp");
        private static readonly int SrcBlendPropertyId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendPropertyId = Shader.PropertyToID("_DstBlend");
        private static readonly int SrcBlendAlphaPropertyId = Shader.PropertyToID("_SrcBlendAlpha");
        private static readonly int DstBlendAlphaPropertyId = Shader.PropertyToID("_DstBlendAlpha");
        private static readonly int PrevTexPropertyId = Shader.PropertyToID("_PrevTex");
        private static readonly int CurrentTexUVRectPropertyId = Shader.PropertyToID("_CurrentTexUVRect");
        private static readonly int PrevTexUVRectPropertyId = Shader.PropertyToID("_PrevTexUVRect");
        private static readonly int FlipBookBlendParamsPropertyId = Shader.PropertyToID("_FlipBookBlendParams");

        private Material _previewMaterial;

        /// <summary>
        /// プレビュー用アイコン種別
        /// </summary>
        private enum PreviewButtonIcon {
            First,
            Previous,
            Play,
            Pause,
            Next,
            Last,
        }

        /// <summary>
        /// プレビュー再生開始
        /// </summary>
        private void PlayPreview() {
            if (_clip == null || !_clip.CanPlay) {
                return;
            }

            _isPreviewPlaying = true;
            _isPreviewPaused = false;
            var startTime = !_clip.Loop && _previewTime >= _clip.Duration ? 0.0f : _previewTime;
            SetPreviewTime(startTime, syncSelectedFrame: true);
            ResetPreviewFlipBookBlendState(_clip.GetFrameIndex(_previewTime));
            _previewStartTime = EditorApplication.timeSinceStartup - _previewTime;
            RefreshPreview();
        }

        /// <summary>
        /// プレビュー再生一時停止
        /// </summary>
        private void PausePreview() {
            if (!_isPreviewPlaying) {
                return;
            }

            _isPreviewPlaying = false;
            _isPreviewPaused = true;
            SetPreviewTime(GetCurrentPreviewTime(), syncSelectedFrame: true);
            ResetPreviewFlipBookBlendState(_clip != null ? _clip.GetFrameIndex(_previewTime) : -1);
            RefreshPreview();
        }

        /// <summary>
        /// プレビュー停止
        /// </summary>
        private void StopPreview() {
            _isPreviewPlaying = false;
            _isPreviewPaused = false;
            SetPreviewTime(0.0f, syncSelectedFrame: true);
            ResetPreviewFlipBookBlendState(_clip != null ? _clip.GetFrameIndex(_previewTime) : -1);
            RefreshPreview();
        }

        /// <summary>
        /// 先頭フレームへ移動
        /// </summary>
        private void MovePreviewToFirstFrame() {
            if (_clip == null || !_clip.CanPlay) {
                return;
            }

            _isPreviewPlaying = false;
            _isPreviewPaused = false;
            SetPreviewTime(0.0f, syncSelectedFrame: true);
            ResetPreviewFlipBookBlendState(_clip.GetFrameIndex(_previewTime));
            RefreshPreview();
        }

        /// <summary>
        /// 前フレームへ移動
        /// </summary>
        private void MovePreviewToPreviousFrame() {
            MovePreviewByFrame(-1);
        }

        /// <summary>
        /// 次フレームへ移動
        /// </summary>
        private void MovePreviewToNextFrame() {
            MovePreviewByFrame(1);
        }

        /// <summary>
        /// 末尾フレームへ移動
        /// </summary>
        private void MovePreviewToLastFrame() {
            if (_clip == null || !_clip.CanPlay) {
                return;
            }

            _isPreviewPlaying = false;
            _isPreviewPaused = false;
            var lastFrameIndex = Mathf.Max(0, _clip.FrameCount - 1);
            SetPreviewTime(lastFrameIndex / Mathf.Max(0.01f, _clip.FrameRate), syncSelectedFrame: true);
            ResetPreviewFlipBookBlendState(_clip.GetFrameIndex(_previewTime));
            RefreshPreview();
        }

        /// <summary>
        /// フレーム単位でプレビュー位置を移動する
        /// </summary>
        /// <param name="offset">移動フレーム量</param>
        private void MovePreviewByFrame(int offset) {
            if (_clip == null || !_clip.CanPlay) {
                return;
            }

            _isPreviewPlaying = false;
            _isPreviewPaused = false;

            var currentFrameIndex = Mathf.Max(0, _clip.GetFrameIndex(_previewTime));
            var nextFrameIndex = Mathf.Clamp(currentFrameIndex + offset, 0, Mathf.Max(0, _clip.FrameCount - 1));
            SetPreviewTime(nextFrameIndex / Mathf.Max(0.01f, _clip.FrameRate), syncSelectedFrame: true);
            ResetPreviewFlipBookBlendState(_clip.GetFrameIndex(_previewTime));
            RefreshPreview();
        }

        /// <summary>
        /// プレビュー再生状態を切り替える
        /// </summary>
        private void TogglePreviewPlayback() {
            if (_isPreviewPlaying) {
                PausePreview();
                return;
            }

            if (_isPreviewPaused && _clip != null && _clip.CanPlay) {
                _isPreviewPlaying = true;
                _isPreviewPaused = false;
                ResetPreviewFlipBookBlendState(_clip.GetFrameIndex(_previewTime));
                _previewStartTime = EditorApplication.timeSinceStartup - _previewTime;
                RefreshPreview();
                return;
            }

            PlayPreview();
        }

        /// <summary>
        /// Editor更新処理
        /// </summary>
        private void OnEditorUpdate() {
            if (!_isPreviewPlaying || _clip == null || !_clip.CanPlay) {
                return;
            }

            var previousFrameIndex = _previewPlaybackFrameIndex;
            SetPreviewTime(GetCurrentPreviewTime(), syncSelectedFrame: false);
            UpdatePreviewFlipBookBlendState(previousFrameIndex);
            if (!_clip.Loop && _previewTime >= _clip.Duration) {
                _isPreviewPlaying = false;
                _isPreviewPaused = false;
                SetPreviewTime(_clip.Duration, syncSelectedFrame: true);
                ResetPreviewFlipBookBlendState(_clip.GetFrameIndex(_previewTime));
            }

            SyncSelectedFrameToPreview();
            RefreshPreview();
        }

        /// <summary>
        /// Undo/Redo時処理
        /// </summary>
        private void OnUndoRedoPerformed() {
            if (_clip == null) {
                return;
            }

            SetClip(_clip);
        }

        /// <summary>
        /// プレビュー領域描画
        /// </summary>
        private void DrawPreviewGui() {
            var rect = GUILayoutUtility.GetRect(10.0f, 10000.0f, 10.0f, 10000.0f);
            EditorGUI.DrawRect(rect, _previewBackgroundColor);
            var contentRect = new Rect(rect.x, rect.y, rect.width, Mathf.Max(0.0f, rect.height - PreviewBottomPadding));
            HandlePreviewScaleScrollWheel(contentRect);

            if (_clip == null) {
                EditorGUI.DropShadowLabel(contentRect, "SpriteAnimationClip を選択してください");
            }
            else {
                DrawPreviewSprite(contentRect);
            }

            if (_clip != null) {
                var infoRect = new Rect(rect.x + 8.0f, rect.y + 8.0f, rect.width - 16.0f, 36.0f);
                var stateLabel = _isPreviewPlaying ? "Playing" : "Stopped";
                EditorGUI.DropShadowLabel(infoRect, $"{stateLabel}  Time: {_previewTime:0.000}s  Frame: {_clip.GetFrameIndex(_previewTime)}");
            }

            DrawPreviewPlaybackButtons(rect);
        }

        /// <summary>
        /// プレビュー下部の再生ボタンを描画する
        /// </summary>
        /// <param name="rect">プレビュー領域</param>
        private void DrawPreviewPlaybackButtons(Rect rect) {
            const float buttonSize = 36.0f;
            const float spacing = 6.0f;
            const float bottomMargin = 10.0f;

            var totalWidth = (buttonSize * 5.0f) + (spacing * 4.0f);
            var startX = rect.center.x - (totalWidth * 0.5f);
            var y = rect.yMax - buttonSize - bottomMargin;

            if (DrawPreviewIconButton(new Rect(startX, y, buttonSize, buttonSize), PreviewButtonIcon.First, "First Frame")) {
                MovePreviewToFirstFrame();
            }

            if (DrawPreviewIconButton(new Rect(startX + ((buttonSize + spacing) * 1.0f), y, buttonSize, buttonSize), PreviewButtonIcon.Previous, "Previous Frame")) {
                MovePreviewToPreviousFrame();
            }

            if (DrawPreviewIconButton(new Rect(startX + ((buttonSize + spacing) * 2.0f), y, buttonSize, buttonSize), _isPreviewPlaying ? PreviewButtonIcon.Pause : PreviewButtonIcon.Play,
                    _isPreviewPlaying ? "Pause Preview" : "Play Preview")) {
                TogglePreviewPlayback();
            }

            if (DrawPreviewIconButton(new Rect(startX + ((buttonSize + spacing) * 3.0f), y, buttonSize, buttonSize), PreviewButtonIcon.Next, "Next Frame")) {
                MovePreviewToNextFrame();
            }

            if (DrawPreviewIconButton(new Rect(startX + ((buttonSize + spacing) * 4.0f), y, buttonSize, buttonSize), PreviewButtonIcon.Last, "Last Frame")) {
                MovePreviewToLastFrame();
            }
        }

        /// <summary>
        /// プレビュー用の Sprite を描画する
        /// </summary>
        /// <param name="rect">描画領域</param>
        private void DrawPreviewSprite(Rect rect) {
            if (_clip == null || !_clip.CanPlay) {
                return;
            }

            var currentSprite = _clip.GetSprite(_previewTime);
            if (currentSprite == null) {
                return;
            }

            if (TryGetPreviewFlipBookBlend(out var previousSprite, out var fadeProgress) && EnsurePreviewMaterial()) {
                DrawPreviewSpriteWithMaterial(rect, currentSprite, previousSprite, fadeProgress);
                return;
            }

            DrawSpritePreview(rect, currentSprite, _previewScale);
        }

        /// <summary>
        /// プレビュー描画用の FlipBookBlend 情報を取得する
        /// </summary>
        /// <param name="previousSprite">遷移元Sprite</param>
        /// <param name="fadeProgress">補間率</param>
        private bool TryGetPreviewFlipBookBlend(out Sprite previousSprite, out float fadeProgress) {
            previousSprite = null;
            fadeProgress = 1.0f;

            if (!EnablePreviewFlipBookBlendSimulation || !_isPreviewPlaying || _clip == null || !_clip.EnableFlipBookBlend || !_clip.CanPlay || _clip.FrameCount <= 1) {
                return false;
            }

            var frameDuration = 1.0f / Mathf.Max(0.01f, _clip.FrameRate);
            var effectiveDuration = Mathf.Min(_clip.FlipBookBlendDuration, Mathf.Max(0.0f, frameDuration - 0.0001f));
            if (effectiveDuration <= 0.0f) {
                return false;
            }

            var currentFrameIndex = _clip.GetFrameIndex(_previewTime);
            if (currentFrameIndex != _previewBlendFrameIndex) {
                return false;
            }

            if (currentFrameIndex <= 0 && !_clip.Loop) {
                return false;
            }

            if (!_clip.Loop && _previewTime >= _clip.Duration) {
                return false;
            }

            var localTime = _clip.Loop ? Mathf.Repeat(_previewTime, frameDuration) : Mathf.Repeat(Mathf.Clamp(_previewTime, 0.0f, _clip.Duration), frameDuration);
            if (localTime >= effectiveDuration) {
                _previewBlendFrameIndex = -1;
                return false;
            }

            var previousFrameIndex = currentFrameIndex > 0 ? currentFrameIndex - 1 : _clip.FrameCount - 1;
            if (previousFrameIndex < 0 || previousFrameIndex >= _clip.FrameCount) {
                return false;
            }

            previousSprite = _clip.Sprites[previousFrameIndex];
            fadeProgress = Mathf.Clamp01(localTime / effectiveDuration);
            return true;
        }

        /// <summary>
        /// Preview の FlipBookBlend 状態を初期化する
        /// </summary>
        /// <param name="currentFrameIndex">現在フレーム</param>
        private void ResetPreviewFlipBookBlendState(int currentFrameIndex) {
            _previewPlaybackFrameIndex = currentFrameIndex;
            _previewBlendFrameIndex = -1;
        }

        /// <summary>
        /// Preview 再生中の FlipBookBlend 状態を更新する
        /// </summary>
        /// <param name="previousFrameIndex">前回フレーム</param>
        private void UpdatePreviewFlipBookBlendState(int previousFrameIndex) {
            if (_clip == null || !_clip.CanPlay) {
                ResetPreviewFlipBookBlendState(-1);
                return;
            }

            var currentFrameIndex = _clip.GetFrameIndex(_previewTime);
            if (currentFrameIndex == previousFrameIndex) {
                return;
            }

            _previewPlaybackFrameIndex = currentFrameIndex;
            _previewBlendFrameIndex = previousFrameIndex >= 0 ? currentFrameIndex : -1;
        }

        /// <summary>
        /// Spriteプレビュー描画
        /// </summary>
        private void DrawSpritePreview(Rect rect, Sprite sprite, float scaleMultiplier) {
            DrawSpritePreview(rect, sprite, scaleMultiplier, 1.0f);
        }

        /// <summary>
        /// Spriteプレビュー描画
        /// </summary>
        /// <param name="rect">描画先</param>
        /// <param name="sprite">描画するSprite</param>
        /// <param name="scaleMultiplier">表示倍率</param>
        /// <param name="alpha">描画Alpha</param>
        private void DrawSpritePreview(Rect rect, Sprite sprite, float scaleMultiplier, float alpha) {
            var texture = sprite.texture;
            if (texture == null) {
                return;
            }

            var spriteRect = sprite.rect;
            var drawRect = GetFitRect(rect, spriteRect.size, scaleMultiplier);
            if (EnsurePreviewMaterial()) {
                _previewMaterial.SetColor(ColorPropertyId, new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(alpha)));
                _previewMaterial.SetTexture(MainTexPropertyId, texture);
                MaterialUtility.ResetProperties(_previewMaterial, sprite);
                if (TryDrawSpriteMeshPreview(drawRect, sprite, _previewMaterial)) {
                    return;
                }
            }

            var uvRect = GetSpritePreviewUVRect(sprite);
            var uv = new Rect(uvRect.x, uvRect.y, uvRect.z, uvRect.w);

            var previousColor = GUI.color;
            var color = previousColor;
            color.a *= Mathf.Clamp01(alpha);
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(drawRect, texture, uv, true);
            GUI.color = previousColor;
        }

        /// <summary>
        /// Material を使って Sprite プレビューを描画する
        /// </summary>
        /// <param name="rect">描画先</param>
        /// <param name="currentSprite">現在 Sprite</param>
        /// <param name="previousSprite">遷移元 Sprite</param>
        /// <param name="fadeProgress">補間率</param>
        private void DrawPreviewSpriteWithMaterial(Rect rect, Sprite currentSprite, Sprite previousSprite, float fadeProgress) {
            if (_previewMaterial == null || currentSprite == null || currentSprite.texture == null || Event.current.type != EventType.Repaint) {
                return;
            }

            _previewMaterial.SetColor(ColorPropertyId, Color.white);
            _previewMaterial.SetTexture(MainTexPropertyId, currentSprite.texture);
            ApplyPreviewFlipBookBlendProperties(_previewMaterial, currentSprite, previousSprite, fadeProgress);

            var uvRect = GetSpritePreviewUVRect(currentSprite);
            var sourceRect = new Rect(uvRect.x, uvRect.y, uvRect.z, uvRect.w);
            var drawRect = GetFitRect(rect, currentSprite.rect.size, _previewScale);
            if (TryDrawSpriteMeshPreview(drawRect, currentSprite, _previewMaterial)) {
                return;
            }

            Graphics.DrawTexture(drawRect, currentSprite.texture, sourceRect, 0, 0, 0, 0, Color.white, _previewMaterial, PreviewShaderPassIndex);
        }

        /// <summary>
        /// プレビュー描画用の Sprite UV rect を取得する
        /// </summary>
        /// <param name="sprite">対象 Sprite</param>
        /// <returns>UV rect</returns>
        private static Vector4 GetSpritePreviewUVRect(Sprite sprite) {
            return MaterialUtility.GetSpriteUVRect(sprite);
        }

        /// <summary>
        /// SpriteMesh を RenderTexture に描画してから GUI に貼り付ける
        /// </summary>
        /// <param name="drawRect">描画先</param>
        /// <param name="sprite">描画する Sprite</param>
        /// <param name="material">描画に使う Material</param>
        /// <returns>描画できた場合 true</returns>
        private static bool TryDrawSpriteMeshPreview(Rect drawRect, Sprite sprite, Material material) {
            if (Event.current.type != EventType.Repaint ||
                sprite == null ||
                material == null ||
                drawRect.width <= 0.0f ||
                drawRect.height <= 0.0f) {
                return false;
            }

            var vertices = sprite.vertices;
            var triangles = sprite.triangles;
            var uv = sprite.uv;
            if (!CanDrawSpriteMesh(vertices, triangles, uv)) {
                return false;
            }

            var textureWidth = Mathf.Clamp(Mathf.CeilToInt(drawRect.width), 1, SpriteMeshPreviewMaxTextureSize);
            var textureHeight = Mathf.Clamp(Mathf.CeilToInt(drawRect.height), 1, SpriteMeshPreviewMaxTextureSize);
            var previewTexture = RenderTexture.GetTemporary(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32);

            try {
                var previousRenderTexture = RenderTexture.active;
                var didBegin = false;
                var didPushMatrix = false;
                var didRender = false;
                var previousBlendOp = material.GetFloat(BlendOpPropertyId);
                var previousSrcBlend = material.GetFloat(SrcBlendPropertyId);
                var previousDstBlend = material.GetFloat(DstBlendPropertyId);
                var previousSrcBlendAlpha = material.GetFloat(SrcBlendAlphaPropertyId);
                var previousDstBlendAlpha = material.GetFloat(DstBlendAlphaPropertyId);

                try {
                    RenderTexture.active = previewTexture;
                    GL.PushMatrix();
                    didPushMatrix = true;
                    GL.Clear(true, true, Color.clear);
                    GL.LoadPixelMatrix(0.0f, textureWidth, 0.0f, textureHeight);
                    SetPreviewRenderTextureBlend(material);

                    if (material.SetPass(PreviewShaderPassIndex)) {
                        GL.Begin(GL.TRIANGLES);
                        didBegin = true;
                        GL.Color(Color.white);
                        for (var i = 0; i < triangles.Length; i++) {
                            var vertexIndex = triangles[i];
                            var position = GetSpriteMeshPreviewPosition(sprite, vertices[vertexIndex], new Vector2(textureWidth, textureHeight));
                            var textureCoord = uv[vertexIndex];
                            GL.TexCoord2(textureCoord.x, textureCoord.y);
                            GL.Vertex3(position.x, position.y, 0.0f);
                        }

                        didRender = true;
                    }
                }
                finally {
                    if (didBegin) {
                        GL.End();
                    }

                    if (didPushMatrix) {
                        GL.PopMatrix();
                    }

                    RestorePreviewBlend(
                        material,
                        previousBlendOp,
                        previousSrcBlend,
                        previousDstBlend,
                        previousSrcBlendAlpha,
                        previousDstBlendAlpha);
                    RenderTexture.active = previousRenderTexture;
                }

                if (!didRender) {
                    return false;
                }

                var previousColor = GUI.color;
                GUI.color = Color.white;
                GUI.DrawTexture(drawRect, previewTexture, ScaleMode.StretchToFill, true);
                GUI.color = previousColor;
                return true;
            }
            finally {
                RenderTexture.ReleaseTemporary(previewTexture);
            }
        }

        /// <summary>
        /// RenderTexture へ Sprite の色をそのまま書き込む Blend 設定を適用する
        /// </summary>
        /// <param name="material">更新対象 Material</param>
        private static void SetPreviewRenderTextureBlend(Material material) {
            material.SetFloat(BlendOpPropertyId, (float)BlendOp.Add);
            material.SetFloat(SrcBlendPropertyId, (float)BlendMode.One);
            material.SetFloat(DstBlendPropertyId, (float)BlendMode.Zero);
            material.SetFloat(SrcBlendAlphaPropertyId, (float)BlendMode.One);
            material.SetFloat(DstBlendAlphaPropertyId, (float)BlendMode.Zero);
        }

        /// <summary>
        /// Preview Material の Blend 設定を戻す
        /// </summary>
        private static void RestorePreviewBlend(Material material, float blendOp, float srcBlend, float dstBlend, float srcBlendAlpha, float dstBlendAlpha) {
            material.SetFloat(BlendOpPropertyId, blendOp);
            material.SetFloat(SrcBlendPropertyId, srcBlend);
            material.SetFloat(DstBlendPropertyId, dstBlend);
            material.SetFloat(SrcBlendAlphaPropertyId, srcBlendAlpha);
            material.SetFloat(DstBlendAlphaPropertyId, dstBlendAlpha);
        }

        /// <summary>
        /// SpriteMesh を描画できるか判定する
        /// </summary>
        /// <param name="vertices">頂点配列</param>
        /// <param name="triangles">三角形インデックス配列</param>
        /// <param name="uv">UV 配列</param>
        /// <returns>描画できる場合 true</returns>
        private static bool CanDrawSpriteMesh(Vector2[] vertices, ushort[] triangles, Vector2[] uv) {
            if (vertices == null || triangles == null || uv == null || vertices.Length <= 0 || triangles.Length <= 0 || uv.Length <= 0 || triangles.Length % 3 != 0) {
                return false;
            }

            for (var i = 0; i < triangles.Length; i++) {
                var vertexIndex = triangles[i];
                if (vertexIndex >= vertices.Length || vertexIndex >= uv.Length) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// SpriteMesh 頂点をプレビュー用のピクセル座標へ変換する
        /// </summary>
        /// <param name="sprite">対象 Sprite</param>
        /// <param name="vertex">SpriteMesh 頂点</param>
        /// <param name="drawSize">描画サイズ</param>
        private static Vector2 GetSpriteMeshPreviewPosition(Sprite sprite, Vector2 vertex, Vector2 drawSize) {
            var spriteRect = sprite.rect;
            if (spriteRect.width <= 0.0f || spriteRect.height <= 0.0f) {
                return Vector2.zero;
            }

            var pixelsPerUnit = Mathf.Max(0.01f, sprite.pixelsPerUnit);
            var pixelPosition = (vertex * pixelsPerUnit) + sprite.pivot;
            return new Vector2(
                pixelPosition.x / spriteRect.width * drawSize.x,
                pixelPosition.y / spriteRect.height * drawSize.y);
        }

        /// <summary>
        /// プレビュー用 Material へ FlipBookBlend property を適用する
        /// </summary>
        /// <param name="material">更新対象 Material</param>
        /// <param name="currentSprite">現在 Sprite</param>
        /// <param name="previousSprite">遷移元 Sprite</param>
        /// <param name="fadeProgress">補間率</param>
        private static void ApplyPreviewFlipBookBlendProperties(Material material, Sprite currentSprite, Sprite previousSprite, float fadeProgress) {
            if (material == null) {
                return;
            }

            var previousTexture = previousSprite != null && previousSprite.texture != null
                ? previousSprite.texture
                : Texture2D.blackTexture;

            material.SetTexture(PrevTexPropertyId, previousTexture);
            material.SetVector(CurrentTexUVRectPropertyId, GetSpritePreviewUVRect(currentSprite));
            material.SetVector(PrevTexUVRectPropertyId, GetSpritePreviewUVRect(previousSprite));
            material.SetVector(
                FlipBookBlendParamsPropertyId,
                new Vector4(previousSprite != null && currentSprite != null ? 1.0f : 0.0f, Mathf.Clamp01(fadeProgress), 0.0f, 0.0f));
        }

        /// <summary>
        /// プレビュー用 Material を確保する
        /// </summary>
        /// <returns>確保できた場合 true</returns>
        private bool EnsurePreviewMaterial() {
            if (_previewMaterial != null) {
                return MaterialUtility.SupportsMaterial(_previewMaterial);
            }

            var shader = Shader.Find(PreviewShaderName);
            if (shader == null) {
                return false;
            }

            _previewMaterial = new Material(shader) {
                hideFlags = HideFlags.HideAndDontSave,
            };
            ConfigurePreviewMaterial(_previewMaterial);
            return MaterialUtility.SupportsMaterial(_previewMaterial);
        }

        /// <summary>
        /// プレビュー用 Material を解放する
        /// </summary>
        private void ReleasePreviewMaterial() {
            if (_previewMaterial == null) {
                return;
            }

            DestroyImmediate(_previewMaterial);
            _previewMaterial = null;
        }

        /// <summary>
        /// プレビュー描画用の Material 設定を適用する
        /// </summary>
        /// <param name="material">設定対象 Material</param>
        private static void ConfigurePreviewMaterial(Material material) {
            if (material == null) {
                return;
            }

            material.SetColor("_Color", Color.white);
            material.SetFloat("_Blend", 0.0f);
            material.SetFloat("_BlendOp", (float)BlendOp.Add);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_ColorMask", 15.0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetVector("_TextureSampleAdd", Vector4.zero);
            material.SetVector("_ClipRect", new Vector4(-32767.0f, -32767.0f, 32767.0f, 32767.0f));
            material.SetFloat("_UIMaskSoftnessX", 0.0f);
            material.SetFloat("_UIMaskSoftnessY", 0.0f);
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHAMODULATE_ON");
            material.DisableKeyword("UNITY_UI_CLIP_RECT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        /// <summary>
        /// 内接矩形を取得
        /// </summary>
        private static Rect GetFitRect(Rect rect, Vector2 contentSize, float scaleMultiplier) {
            if (contentSize.x <= 0.0f || contentSize.y <= 0.0f) {
                return rect;
            }

            var safeScale = Mathf.Max(0.01f, scaleMultiplier);
            var scale = Mathf.Min(rect.width / contentSize.x, rect.height / contentSize.y);
            var size = contentSize * (scale * safeScale);
            size.x = Mathf.Min(size.x, rect.width * safeScale);
            size.y = Mathf.Min(size.y, rect.height * safeScale);
            return new Rect(rect.center - (size * 0.5f), size);
        }

        /// <summary>
        /// ツールバー用アイコンを取得
        /// </summary>
        private static Texture GetToolbarIcon(string iconName) {
            var iconContent = EditorGUIUtility.IconContent(iconName);
            if (iconContent?.image != null) {
                return iconContent.image;
            }

            return EditorGUIUtility.FindTexture(iconName);
        }

        /// <summary>
        /// プレビュー用アイコンボタンを描画する
        /// </summary>
        private static bool DrawPreviewIconButton(Rect rect, PreviewButtonIcon icon, string tooltip) {
            var clicked = GUI.Button(rect, new GUIContent(string.Empty, tooltip));
            var iconRect = new Rect(rect.x + 9.0f, rect.y + 9.0f, rect.width - 18.0f, rect.height - 18.0f);
            DrawPreviewButtonIcon(iconRect, icon);
            return clicked;
        }

        /// <summary>
        /// プレビュー用アイコンを描画する
        /// </summary>
        private static void DrawPreviewButtonIcon(Rect rect, PreviewButtonIcon icon) {
            var texture = GetPreviewButtonIconTexture(icon);
            if (texture != null) {
                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
                return;
            }

            var iconColor = EditorGUIUtility.isProSkin
                ? new Color(0.92f, 0.92f, 0.92f)
                : new Color(0.15f, 0.15f, 0.15f);

            switch (icon) {
                case PreviewButtonIcon.First:
                    DrawFirstIcon(rect, iconColor);
                    break;
                case PreviewButtonIcon.Previous:
                    DrawPreviousIcon(rect, iconColor);
                    break;
                case PreviewButtonIcon.Play:
                    DrawPlayIcon(rect, iconColor);
                    break;
                case PreviewButtonIcon.Pause:
                    DrawPauseIcon(rect, iconColor);
                    break;
                case PreviewButtonIcon.Next:
                    DrawNextIcon(rect, iconColor);
                    break;
                case PreviewButtonIcon.Last:
                    DrawLastIcon(rect, iconColor);
                    break;
            }
        }

        /// <summary>
        /// プレビュー用の高解像度アイコンを取得する
        /// </summary>
        private static Texture GetPreviewButtonIconTexture(PreviewButtonIcon icon) {
            string[] iconNames;
            switch (icon) {
                case PreviewButtonIcon.First:
                    iconNames = new[] { "Animation.FirstKey", "Animation.FirstKey@2x" };
                    break;
                case PreviewButtonIcon.Previous:
                    iconNames = new[] { "Animation.PrevKey", "Animation.PrevKey@2x" };
                    break;
                case PreviewButtonIcon.Play:
                    iconNames = new[] { "d_PlayButton@2x", "PlayButton@2x", "d_PlayButton", "PlayButton" };
                    break;
                case PreviewButtonIcon.Pause:
                    iconNames = new[] { "d_PauseButton@2x", "PauseButton@2x", "d_PauseButton", "PauseButton" };
                    break;
                case PreviewButtonIcon.Next:
                    iconNames = new[] { "Animation.NextKey", "Animation.NextKey@2x" };
                    break;
                case PreviewButtonIcon.Last:
                    iconNames = new[] { "Animation.LastKey", "Animation.LastKey@2x" };
                    break;
                default:
                    iconNames = Array.Empty<string>();
                    break;
            }

            for (var i = 0; i < iconNames.Length; i++) {
                var texture = GetToolbarIcon(iconNames[i]);
                if (texture != null) {
                    return texture;
                }
            }

            return null;
        }

        /// <summary>
        /// 再生アイコンを描画する
        /// </summary>
        private static void DrawPlayIcon(Rect rect, Color color) {
            Handles.BeginGUI();
            var previousColor = Handles.color;
            Handles.color = color;
            Handles.DrawAAConvexPolygon(
                new Vector3(rect.xMin, rect.yMin, 0.0f),
                new Vector3(rect.xMin, rect.yMax, 0.0f),
                new Vector3(rect.xMax, rect.center.y, 0.0f));
            Handles.color = previousColor;
            Handles.EndGUI();
        }

        /// <summary>
        /// 先頭移動アイコンを描画する
        /// </summary>
        private static void DrawFirstIcon(Rect rect, Color color) {
            var barWidth = rect.width * 0.12f;
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, barWidth, rect.height), color);
            DrawPreviousIcon(new Rect(rect.xMin + (barWidth * 1.6f), rect.yMin, rect.width - (barWidth * 1.6f), rect.height), color);
        }

        /// <summary>
        /// 1つ前移動アイコンを描画する
        /// </summary>
        private static void DrawPreviousIcon(Rect rect, Color color) {
            Handles.BeginGUI();
            var previousColor = Handles.color;
            Handles.color = color;
            Handles.DrawAAConvexPolygon(
                new Vector3(rect.xMax, rect.yMin, 0.0f),
                new Vector3(rect.xMax, rect.yMax, 0.0f),
                new Vector3(rect.xMin, rect.center.y, 0.0f));
            Handles.color = previousColor;
            Handles.EndGUI();
        }

        /// <summary>
        /// 一時停止アイコンを描画する
        /// </summary>
        private static void DrawPauseIcon(Rect rect, Color color) {
            var barWidth = rect.width * 0.28f;
            var gap = rect.width * 0.16f;
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, barWidth, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMin + barWidth + gap, rect.yMin, barWidth, rect.height), color);
        }

        /// <summary>
        /// 1つ次移動アイコンを描画する
        /// </summary>
        private static void DrawNextIcon(Rect rect, Color color) {
            DrawPlayIcon(rect, color);
        }

        /// <summary>
        /// 末尾移動アイコンを描画する
        /// </summary>
        private static void DrawLastIcon(Rect rect, Color color) {
            var barWidth = rect.width * 0.12f;
            DrawNextIcon(new Rect(rect.xMin, rect.yMin, rect.width - (barWidth * 1.6f), rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - barWidth, rect.yMin, barWidth, rect.height), color);
        }

        /// <summary>
        /// タイムライン上のフレームプレビュー描画
        /// </summary>
        private void DrawTimelineFramePreview(int frameIndex) {
            DrawTimelineFramePreview(frameIndex, 1.0f);
        }

        /// <summary>
        /// タイムライン上のフレームプレビュー描画
        /// </summary>
        /// <param name="frameIndex">フレーム番号</param>
        /// <param name="alpha">Sprite の表示 Alpha</param>
        private void DrawTimelineFramePreview(int frameIndex, float alpha) {
            var rect = GUILayoutUtility.GetRect(10.0f, 10000.0f, 10.0f, 10000.0f);
            EditorGUI.DrawRect(rect, new Color(0.14f, 0.14f, 0.14f));

            var sprite = GetFrameSprite(frameIndex);
            if (sprite == null) {
                EditorGUI.DropShadowLabel(rect, "Empty");
                return;
            }

            DrawSpritePreview(rect, sprite, 1.0f, alpha);
        }

        /// <summary>
        /// タイムライン用のフレーム名表示文字列を取得する
        /// </summary>
        private static string GetTimelineFrameLabelText(Sprite sprite) {
            if (sprite == null) {
                return "(empty)";
            }

            if (sprite.name.Length <= TimelineLabelMaxCharacters) {
                return sprite.name;
            }

            return $"{sprite.name[..(TimelineLabelMaxCharacters - 3)]}...";
        }

        /// <summary>
        /// プレビュー更新
        /// </summary>
        private void RefreshPreview() {
            if (_previewScaleLabel != null) {
                _previewScaleLabel.text = $"{_previewScale:0.00}x";
            }

            if (_previewScaleSlider != null) {
                _previewScaleSlider.SetValueWithoutNotify(_previewScale);
            }

            var seekValue = GetPreviewSeekValue();
            if (_previewSeekSlider != null) {
                _previewSeekSlider.SetValueWithoutNotify(seekValue);
                _previewSeekSlider.SetEnabled(_clip != null && _clip.CanPlay && _clip.Duration > 0.0f);
            }

            if (_previewSeekLabel != null) {
                _previewSeekLabel.text = $"{seekValue * 100.0f:0}%";
            }

            if (_previewBackgroundColorField != null) {
                _previewBackgroundColorField.SetValueWithoutNotify(_previewBackgroundColor);
            }

            _previewContainer?.MarkDirtyRepaint();
            Repaint();
        }

        /// <summary>
        /// プレビュー拡大率を設定する
        /// </summary>
        /// <param name="scale">拡大率</param>
        private void SetPreviewScale(float scale) {
            _previewScale = Mathf.Clamp(scale, PreviewScaleMin, PreviewScaleMax);
            RefreshPreview();
        }

        /// <summary>
        /// プレビュー再生位置を割合で設定する
        /// </summary>
        /// <param name="seekValue">0.0-1.0 の再生位置</param>
        private void SetPreviewSeek(float seekValue) {
            if (_clip == null || !_clip.CanPlay || _clip.Duration <= 0.0f) {
                SetPreviewTime(0.0f, syncSelectedFrame: true);
                ResetPreviewFlipBookBlendState(-1);
                RefreshPreview();
                return;
            }

            SetPreviewTime(GetPreviewTimeFromSeekValue(seekValue), syncSelectedFrame: true);
            ResetPreviewFlipBookBlendState(_clip.GetFrameIndex(_previewTime));
            RefreshPreview();
        }

        /// <summary>
        /// 現在のプレビュー再生位置を割合で取得する
        /// </summary>
        private float GetPreviewSeekValue() {
            if (_clip == null || !_clip.CanPlay || _clip.Duration <= 0.0f) {
                return 0.0f;
            }

            return Mathf.Clamp01(_previewTime / _clip.Duration);
        }

        /// <summary>
        /// 割合からプレビュー時刻を取得する
        /// </summary>
        /// <param name="seekValue">0.0-1.0 の再生位置</param>
        private float GetPreviewTimeFromSeekValue(float seekValue) {
            if (_clip == null || !_clip.CanPlay || _clip.Duration <= 0.0f) {
                return 0.0f;
            }

            var normalizedSeekValue = Mathf.Clamp01(seekValue);
            if (_clip.Loop && normalizedSeekValue >= 1.0f) {
                return Mathf.Max(0.0f, _clip.Duration - 0.0001f);
            }

            return normalizedSeekValue * _clip.Duration;
        }

        /// <summary>
        /// プレビュー領域のホイール操作で拡大率を更新する
        /// </summary>
        /// <param name="rect">操作対象領域</param>
        private void HandlePreviewScaleScrollWheel(Rect rect) {
            var evt = Event.current;
            if (evt == null || evt.type != EventType.ScrollWheel || !rect.Contains(evt.mousePosition)) {
                return;
            }

            SetPreviewScale(_previewScale - (evt.delta.y * PreviewScaleWheelStep));
            evt.Use();
        }

        /// <summary>
        /// 選択フレームへプレビュー位置を同期する
        /// </summary>
        private void SyncPreviewToSelectedFrame() {
            if (_clip == null || !_clip.CanPlay || _selectedFrameIndex < 0) {
                return;
            }

            var nextPreviewTime = _selectedFrameIndex / Mathf.Max(0.01f, _clip.FrameRate);
            if (!_clip.Loop) {
                nextPreviewTime = Mathf.Clamp(nextPreviewTime, 0.0f, _clip.Duration);
            }

            SetPreviewTime(nextPreviewTime, syncSelectedFrame: false);
            ResetPreviewFlipBookBlendState(_clip.GetFrameIndex(_previewTime));
            RefreshPreview();
        }

        /// <summary>
        /// プレビュー時間を更新する
        /// </summary>
        private void SetPreviewTime(float previewTime, bool syncSelectedFrame) {
            if (_clip != null) {
                previewTime = Mathf.Clamp(previewTime, 0.0f, _clip.Duration);
            }

            _previewTime = previewTime;
            if (_isPreviewPlaying) {
                _previewStartTime = EditorApplication.timeSinceStartup - _previewTime;
            }

            if (syncSelectedFrame) {
                SyncSelectedFrameToPreview();
            }
        }

        /// <summary>
        /// プレビュー再生位置へ選択フレームを同期する
        /// </summary>
        private void SyncSelectedFrameToPreview() {
            if (_clip == null || !_clip.CanPlay) {
                return;
            }

            var previewFrameIndex = _clip.GetFrameIndex(_previewTime);
            if (previewFrameIndex < 0 || previewFrameIndex == _selectedFrameIndex) {
                return;
            }

            _selectedFrameIndex = previewFrameIndex;
            RefreshInspector();
            RebuildTimeline();
        }

        /// <summary>
        /// 現在のプレビュー時刻を取得
        /// </summary>
        private float GetCurrentPreviewTime() {
            if (_clip == null) {
                return 0.0f;
            }

            var elapsed = (float)(EditorApplication.timeSinceStartup - _previewStartTime);
            if (_clip.Loop && _clip.Duration > 0.0f) {
                return Mathf.Repeat(elapsed, _clip.Duration);
            }

            return Mathf.Clamp(elapsed, 0.0f, _clip.Duration);
        }
    }
}
