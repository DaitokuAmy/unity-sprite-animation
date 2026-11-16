using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnitySpriteAnimation.Editor;
using Object = UnityEngine.Object;

namespace UnitySpriteAnimation.Tests {
    /// <summary>既存データの保持とレイヤー間の同期を検証する</summary>
    public sealed class SpriteAnimationLayerTests {
        private readonly List<Object> _objects = new();
        private Sprite _first;
        private Sprite _second;
        private Sprite _shadow;
        private SpriteAnimationClip _clip;
        private string _assetPath;

        /// <summary>共通の主レイヤーと影レイヤーを作成する</summary>
        [SetUp]
        public void SetUp() {
            var texture = Track(new Texture2D(4, 4));
            _first = Track(Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f, 100.0f, 0, SpriteMeshType.FullRect));
            _second = Track(Sprite.Create(texture, new Rect(2, 0, 2, 2), Vector2.one * 0.5f, 100.0f, 0, SpriteMeshType.FullRect));
            _shadow = Track(Sprite.Create(texture, new Rect(0, 2, 2, 2), Vector2.one * 0.5f, 100.0f, 0, SpriteMeshType.FullRect));
            _clip = Track(ScriptableObject.CreateInstance<SpriteAnimationClip>());
            Set(_clip, "_sprites", new[] { _first, _second, _first });
            var layer = new SpriteAnimationLayer();
            Set(layer, "_layerName", "Shadow");
            Set(layer, "_sprites", new[] { _shadow, null, _second });
            Set(_clip, "_extensionSpriteLayers", new[] { layer });
        }

        /// <summary>生成したオブジェクトを破棄する</summary>
        [TearDown]
        public void TearDown() {
            Undo.ClearAll();
            if (!string.IsNullOrEmpty(_assetPath)) {
                AssetDatabase.DeleteAsset(_assetPath);
                _assetPath = null;
            }
            for (var i = _objects.Count - 1; i >= 0; i--) {
                if (_objects[i] != null) {
                    Object.DestroyImmediate(_objects[i]);
                }
            }
            _objects.Clear();
        }

        /// <summary>拡張レイヤーのない既存形式の Sprite 配列を保持する</summary>
        [Test]
        public void LegacyDataRetainsMainSprites() {
            Set(_clip, "_extensionSpriteLayers", null);
            _assetPath = $"Assets/layer_compatibility_{Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(_clip, _assetPath);
            AssetDatabase.AddObjectToAsset(_first.texture, _clip);
            AssetDatabase.AddObjectToAsset(_first, _clip);
            AssetDatabase.AddObjectToAsset(_second, _clip);
            AssetDatabase.AddObjectToAsset(_shadow, _clip);
            AssetDatabase.SaveAssets();
            var json = EditorJsonUtility.ToJson(_clip);
            var legacyJson = System.Text.RegularExpressions.Regex.Replace(json, ",?\"_extensionSpriteLayers\":\\[\\]", "");
            Assert.That(legacyJson, Does.Not.Contain("_extensionSpriteLayers"));
            var restored = Track(ScriptableObject.CreateInstance<SpriteAnimationClip>());
            EditorJsonUtility.FromJsonOverwrite(legacyJson, restored);
            CollectionAssert.AreEqual(_clip.Sprites, restored.Sprites);
            Assert.That(restored.GetSprite(0.0f), Is.SameAs(_first));
            Assert.That(restored.ExtensionSpriteLayers, Is.Empty);
        }

        /// <summary>主レイヤーに枠数を揃え、空欄と重複名を解消する</summary>
        [Test]
        public void ValidationUsesMainFrameCountAndUniqueNames() {
            var first = _clip.ExtensionSpriteLayers[0];
            Set(first, "_sprites", new[] { _shadow });
            var duplicate = new SpriteAnimationLayer();
            Set(duplicate, "_layerName", "Shadow");
            var empty = new SpriteAnimationLayer();
            Set(_clip, "_extensionSpriteLayers", new[] { first, duplicate, empty });
            Invoke(_clip, "OnValidate");
            Assert.That(first.Sprites.Count, Is.EqualTo(3));
            Assert.That(first.GetSprite(1), Is.Null);
            Assert.That(first.GetSprite(0), Is.SameAs(_shadow));
            Assert.That(duplicate.LayerName, Is.Not.EqualTo(first.LayerName));
            Assert.That(empty.LayerName, Is.Not.Empty);
            Assert.That(_clip.FrameCount, Is.EqualTo(3));
        }

        /// <summary>両表示形式で None と Clip 切替を同じ時刻で反映する</summary>
        /// <param name="useImage">Image を使用するか</param>
        [TestCase(false)]
        [TestCase(true)]
        public void EvaluationSynchronizesTargetsAndClearsMissingLayers(bool useImage) {
            var main = Track(new GameObject("Main"));
            var shadow = Track(new GameObject("Shadow"));
            Component target;
            SpriteAnimator animator;
            if (useImage) {
                main.AddComponent<Image>();
                animator = main.AddComponent<ImageSpriteAnimator>();
                target = shadow.AddComponent<Image>();
            }
            else {
                main.AddComponent<SpriteRenderer>();
                animator = main.AddComponent<RendererSpriteAnimator>();
                target = shadow.AddComponent<SpriteRenderer>();
            }
            var binding = new SpriteAnimationLayerBinding();
            Set(binding, "_layerName", "Shadow");
            Set(binding, "_target", target);
            Set(animator, "_extensionLayerBindings", new[] { binding });
            animator.Evaluate(_clip, 0.0f);
            Assert.That(TargetSprite(target), Is.SameAs(_shadow));
            animator.Evaluate(_clip, 1.0f / _clip.FrameRate);
            Assert.That(TargetSprite(target), Is.Null);
            Assert.That(target is Image image ? image.enabled : ((SpriteRenderer)target).enabled, Is.False);
            animator.Evaluate(_clip, 2.0f / _clip.FrameRate);
            Assert.That(TargetSprite(target), Is.SameAs(_second));
            var empty = Track(ScriptableObject.CreateInstance<SpriteAnimationClip>());
            animator.SetAnimationClip(empty);
            Assert.That(TargetSprite(target), Is.Null);
            Assert.That(animator.IsPlaying, Is.False);
        }

        /// <summary>主表示先がなくても拡張レイヤーを評価して再生する</summary>
        /// <param name="useImage">Image を使用するか</param>
        [TestCase(false)]
        [TestCase(true)]
        public void MissingMainTargetStillUpdatesExtensionLayers(bool useImage) {
            var main = Track(new GameObject("Main"));
            var shadow = Track(new GameObject("Shadow"));
            shadow.transform.SetParent(main.transform);
            var animator = useImage
                ? (SpriteAnimator)main.AddComponent<ImageSpriteAnimator>()
                : main.AddComponent<RendererSpriteAnimator>();
            Assert.That(main.GetComponent<Image>(), Is.Null);
            Assert.That(main.GetComponent<SpriteRenderer>(), Is.Null);
            Component target = useImage ? (Component)shadow.AddComponent<Image>() : shadow.AddComponent<SpriteRenderer>();
            var binding = new SpriteAnimationLayerBinding();
            Set(binding, "_layerName", "Shadow");
            Set(binding, "_target", target);
            Set(animator, "_extensionLayerBindings", new[] { binding });
            animator.Evaluate(_clip, 0.0f);
            Assert.That(TargetSprite(target), Is.SameAs(_shadow));
            animator.Play();
            animator.ManualUpdate(1.1f / _clip.FrameRate);
            Assert.That(TargetSprite(target), Is.Null);
            animator.Stop();
            Assert.That(TargetSprite(target), Is.SameAs(_shadow));
            animator.Evaluate(null, 0.0f);
            Assert.That(TargetSprite(target), Is.Null);
        }

        /// <summary>主レイヤーが空なら拡張レイヤーだけでは再生しない</summary>
        [Test]
        public void EmptyMainCannotPlay() {
            Set(_clip, "_sprites", Array.Empty<Sprite>());
            Assert.That(_clip.CanPlay, Is.False);
            Assert.That(_clip.GetFrameIndex(0.0f), Is.EqualTo(-1));
        }

        /// <summary>全レイヤーの挿入、移動、複製、削除、拡張で対応を保持する</summary>
        [Test]
        public void EditorFrameOperationsKeepLayersAligned() {
            var window = Track(ScriptableObject.CreateInstance<SpriteAnimationClipEditorWindow>());
            Invoke(window, "SetClip", _clip);
            Invoke(window, "InsertFrameAfter", 0);
            CollectionAssert.AreEqual(new[] { _shadow, null, null, _second }, _clip.ExtensionSpriteLayers[0].Sprites);
            Invoke(window, "MoveFrame", 3, 0);
            CollectionAssert.AreEqual(new[] { _second, _shadow, null, null }, _clip.ExtensionSpriteLayers[0].Sprites);
            Invoke(window, "DuplicateFrameAt", 1);
            CollectionAssert.AreEqual(new[] { _second, _shadow, _shadow, null, null }, _clip.ExtensionSpriteLayers[0].Sprites);
            Invoke(window, "RemoveFrameAt", 3);
            Assert.That(_clip.FrameCount, Is.EqualTo(4));
            CollectionAssert.AreEqual(new[] { _second, _shadow, _shadow, null }, _clip.ExtensionSpriteLayers[0].Sprites);
            Invoke(window, "SetTotalFrameCount", 6);
            CollectionAssert.AreEqual(new[] { _second, _shadow, _shadow, null, null, null }, _clip.ExtensionSpriteLayers[0].Sprites);
            Assert.That(_clip.Sprites[5], Is.Null);
        }

        /// <summary>削除操作は画像をクリアした後、空のコマを全レイヤーから削除する</summary>
        [Test]
        public void DeleteClearsSpriteBeforeRemovingEmptyFrame() {
            var window = Track(ScriptableObject.CreateInstance<SpriteAnimationClipEditorWindow>());
            Invoke(window, "SetClip", _clip);
            Set(window, "_selectedFrameIndex", 0);
            Invoke(window, "DeleteSelectedFrameContent");
            CollectionAssert.AreEqual(new[] { null, _second, _first }, _clip.Sprites);
            CollectionAssert.AreEqual(new[] { _shadow, null, _second }, _clip.ExtensionSpriteLayers[0].Sprites);
            Invoke(window, "DeleteSelectedFrameContent");
            CollectionAssert.AreEqual(new[] { _second, _first }, _clip.Sprites);
            CollectionAssert.AreEqual(new[] { null, _second }, _clip.ExtensionSpriteLayers[0].Sprites);
        }

        /// <summary>None のフレームを削除しても隣の枠を巻き込まない</summary>
        [Test]
        public void RemovingNoneDeletesExactlyOneFrame() {
            Set(_clip, "_sprites", new[] { _first, null, _second });
            var window = Track(ScriptableObject.CreateInstance<SpriteAnimationClipEditorWindow>());
            Invoke(window, "SetClip", _clip);
            Invoke(window, "RemoveFrameAt", 1);
            CollectionAssert.AreEqual(new[] { _first, _second }, _clip.Sprites);
            CollectionAssert.AreEqual(new[] { _shadow, _second }, _clip.ExtensionSpriteLayers[0].Sprites);
        }

        /// <summary>拡張レイヤー選択中はコマの追加、削除、複製、総数変更を行わない</summary>
        [Test]
        public void ExtensionSelectionCannotChangeFrameCount() {
            var window = Track(ScriptableObject.CreateInstance<SpriteAnimationClipEditorWindow>());
            Invoke(window, "SetClip", _clip);
            Set(window, "_selectedFrameIndex", 1);
            Invoke(window, "CopySelectedFrame");
            Set(window, "_selectedLayerIndex", 0);
            Invoke(window, "InsertFrameAfter", 1);
            Invoke(window, "InsertFrameAfter", 1, _first);
            Invoke(window, "DuplicateFrameAt", 1);
            Invoke(window, "RemoveFrameAt", 1);
            Invoke(window, "DeleteSelectedFrameContent");
            Invoke(window, "SetTotalFrameCount", 5);
            Invoke(window, "ClearFrames");
            CollectionAssert.AreEqual(new[] { _first, _second, _first }, _clip.Sprites);
            CollectionAssert.AreEqual(new[] { _shadow, null, _second }, _clip.ExtensionSpriteLayers[0].Sprites);
            Set(window, "_selectedFrameIndex", 0);
            Invoke(window, "DeleteSelectedFrameContent");
            Assert.That(_clip.ExtensionSpriteLayers[0].GetSprite(0), Is.Null);
            Assert.That(_clip.FrameCount, Is.EqualTo(3));
        }

        /// <summary>拡張レイヤーへの貼り付けは選択コマだけを書き換える</summary>
        [Test]
        public void ExtensionPasteReplacesOnlySelectedSprite() {
            var window = Track(ScriptableObject.CreateInstance<SpriteAnimationClipEditorWindow>());
            Invoke(window, "SetClip", _clip);
            Set(window, "_selectedLayerIndex", 0);
            Set(window, "_selectedFrameIndex", 0);
            Invoke(window, "CopySelectedFrame");
            Set(window, "_selectedFrameIndex", 1);
            Invoke(window, "PasteCopiedFrameAfterSelected");
            CollectionAssert.AreEqual(new[] { _first, _second, _first }, _clip.Sprites);
            CollectionAssert.AreEqual(new[] { _shadow, _shadow, _second }, _clip.ExtensionSpriteLayers[0].Sprites);
            Assert.That(_clip.FrameCount, Is.EqualTo(3));
        }

        /// <summary>拡張レイヤーへの入力では主レイヤーの枠を増やさない</summary>
        [Test]
        public void ExtensionEditingUsesExistingMainFrames() {
            var window = Track(ScriptableObject.CreateInstance<SpriteAnimationClipEditorWindow>());
            Invoke(window, "SetClip", _clip);
            Set(window, "_selectedLayerIndex", 0);
            Invoke(window, "SetFrameSprites", 2, new[] { _shadow, _shadow });
            Assert.That(_clip.FrameCount, Is.EqualTo(3));
            Assert.That(_clip.ExtensionSpriteLayers[0].GetSprite(2), Is.SameAs(_shadow));
            Assert.That(_clip.Sprites[2], Is.SameAs(_first));
            Invoke(window, "ClearFrameSpriteAt", 2);
            Assert.That(_clip.FrameCount, Is.EqualTo(3));
            Assert.That(_clip.ExtensionSpriteLayers[0].GetSprite(2), Is.Null);
        }

        /// <summary>主レイヤーが None でも影の Material で補間する</summary>
        [Test]
        public void ExtensionBlendsIndependentlyAndReleasesMaterial() {
            Set(_clip, "_sprites", new Sprite[] { null, null });
            Set(_clip.ExtensionSpriteLayers[0], "_sprites", new[] { _shadow, _second });
            Set(_clip, "_frameRate", 1.0f);
            Set(_clip, "_enableFlipBookBlend", true);
            Set(_clip, "_flipBookBlendDuration", 0.5f);
            var main = Track(new GameObject("Main"));
            main.AddComponent<SpriteRenderer>();
            var animator = main.AddComponent<RendererSpriteAnimator>();
            var shadow = Track(new GameObject("Shadow"));
            var renderer = shadow.AddComponent<SpriteRenderer>();
            var shader = Shader.Find("Unity Sprite Animation/Sprite Unlit");
            Assert.That(shader, Is.Not.Null);
            var source = Track(new Material(shader));
            renderer.sharedMaterial = source;
            var binding = new SpriteAnimationLayerBinding();
            Set(binding, "_layerName", "Shadow");
            Set(binding, "_target", renderer);
            Set(animator, "_extensionLayerBindings", new[] { binding });
            animator.SetAnimationClip(_clip);
            animator.Play();
            animator.ManualUpdate(0.99f);
            animator.ManualUpdate(0.02f);
            Assert.That(renderer.sprite, Is.SameAs(_second));
            var runtimeMaterial = renderer.sharedMaterial;
            Assert.That(runtimeMaterial, Is.Not.SameAs(source));
            var blend = runtimeMaterial.GetVector("_FlipBookBlendParams");
            Assert.That(blend.x, Is.EqualTo(1.0f), $"Packing: {_shadow.packingMode}/{_second.packingMode}; supported: {MaterialUtility.SupportsMaterial(source)}; frame: {animator.CurrentFrameIndex}");
            Assert.That(blend.y, Is.InRange(0.0f, 0.99f));
            // EditMode では Awake を経由しないため破棄時処理を明示的に実行する
            Invoke(animator, "OnDestroy");
            Object.DestroyImmediate(main);
            Assert.That(renderer.sharedMaterial, Is.SameAs(source));
            Assert.That(runtimeMaterial == null, Is.True);
        }

        /// <summary>全レイヤーのフレーム編集をまとめて Undo する</summary>
        [Test]
        public void UndoRestoresAllLayers() {
            var window = Track(ScriptableObject.CreateInstance<SpriteAnimationClipEditorWindow>());
            Invoke(window, "SetClip", _clip);
            Undo.IncrementCurrentGroup();
            Invoke(window, "InsertFrameAfter", 0);
            Undo.FlushUndoRecordObjects();
            Assert.That(_clip.FrameCount, Is.EqualTo(4));
            Undo.PerformUndo();
            Assert.That(_clip.FrameCount, Is.EqualTo(3));
            CollectionAssert.AreEqual(new[] { _shadow, null, _second }, _clip.ExtensionSpriteLayers[0].Sprites);
        }

        /// <summary>配置先を持たない既存データは Back として読み込む</summary>
        [Test]
        public void LegacyLayerDefaultsToBack() {
            var layer = JsonUtility.FromJson<SpriteAnimationLayer>("{\"_layerName\":\"Legacy\",\"_sprites\":[]}");
            Assert.That(layer.LayerName, Is.EqualTo("Legacy"));
            Assert.That(layer.Placement, Is.EqualTo(SpriteAnimationLayer.LayerPlacement.Back));
        }

        /// <summary>配置先ごとの配列順で主レイヤーを挟んで合成する</summary>
        [Test]
        public void PreviewDrawsBackMainFrontInArrayOrder() {
            var back = _clip.ExtensionSpriteLayers[0];
            var front = CreateLayer("Weapon", SpriteAnimationLayer.LayerPlacement.Front, _first);
            var secondBack = CreateLayer("Ground", SpriteAnimationLayer.LayerPlacement.Back, _second);
            var secondFront = CreateLayer("Effect", SpriteAnimationLayer.LayerPlacement.Front, _shadow);
            Set(_clip, "_extensionSpriteLayers", new[] { back, front, secondBack, secondFront });
            var window = Track(ScriptableObject.CreateInstance<SpriteAnimationClipEditorWindow>());
            Invoke(window, "SetClip", _clip);
            CollectionAssert.AreEqual(new[] { 0, 2, -1, 1, 3 }, (List<int>)Invoke(window, "GetLayerDrawOrder"));
        }

        /// <summary>グループ内の順番変更では名前とフレームの対応を保持する</summary>
        [Test]
        public void MovingLayerPreservesKeysFramesAndOtherGroupOrder() {
            var back = _clip.ExtensionSpriteLayers[0];
            var front = CreateLayer("Weapon", SpriteAnimationLayer.LayerPlacement.Front, _first);
            var secondBack = CreateLayer("Ground", SpriteAnimationLayer.LayerPlacement.Back, _second);
            var secondFront = CreateLayer("Effect", SpriteAnimationLayer.LayerPlacement.Front, _shadow);
            Set(_clip, "_extensionSpriteLayers", new[] { back, front, secondBack, secondFront });
            var window = Track(ScriptableObject.CreateInstance<SpriteAnimationClipEditorWindow>());
            Invoke(window, "SetClip", _clip);
            Set(window, "_selectedLayerIndex", 0);
            Invoke(window, "MoveSelectedLayer", 1);
            Assert.That(_clip.ExtensionSpriteLayers[2].LayerName, Is.EqualTo("Shadow"));
            CollectionAssert.AreEqual(new[] { _shadow, null, _second }, _clip.GetExtensionSpriteLayer("Shadow").Sprites);
            CollectionAssert.AreEqual(new[] { 1, 2, -1, 0, 3 }, (List<int>)Invoke(window, "GetLayerDrawOrder"));
            Invoke(window, "MoveSelectedLayer", 1);
            Assert.That(_clip.ExtensionSpriteLayers[2].LayerName, Is.EqualTo("Shadow"));
            Invoke(window, "MoveSelectedLayer", -1);
            Assert.That(_clip.ExtensionSpriteLayers[1].LayerName, Is.EqualTo("Shadow"));
            Assert.That(_clip.ExtensionSpriteLayers[0].LayerName, Is.EqualTo("Weapon"));
            Assert.That(_clip.ExtensionSpriteLayers[3].LayerName, Is.EqualTo("Effect"));
        }

        /// <summary>配置先の変更と Undo で名前と Sprite を維持する</summary>
        [Test]
        public void PlacementChangeAndUndoPreserveLayerData() {
            var window = Track(ScriptableObject.CreateInstance<SpriteAnimationClipEditorWindow>());
            Invoke(window, "SetClip", _clip);
            Set(window, "_selectedLayerIndex", 0);
            Undo.IncrementCurrentGroup();
            Invoke(window, "SetSelectedLayerPlacement", SpriteAnimationLayer.LayerPlacement.Front);
            Undo.FlushUndoRecordObjects();
            var layer = _clip.GetExtensionSpriteLayer("Shadow");
            Assert.That(layer.Placement, Is.EqualTo(SpriteAnimationLayer.LayerPlacement.Front));
            CollectionAssert.AreEqual(new[] { -1, 0 }, (List<int>)Invoke(window, "GetLayerDrawOrder"));
            CollectionAssert.AreEqual(new[] { _shadow, null, _second }, layer.Sprites);
            Undo.PerformUndo();
            Assert.That(_clip.GetExtensionSpriteLayer("Shadow").Placement, Is.EqualTo(SpriteAnimationLayer.LayerPlacement.Back));
            CollectionAssert.AreEqual(new[] { 0, -1 }, (List<int>)Invoke(window, "GetLayerDrawOrder"));
            var selection = typeof(SpriteAnimationClipEditorWindow).GetField("_selectedLayerIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(selection.GetValue(window), Is.EqualTo(0));
        }

        /// <summary>配置先と Sprite を持つレイヤーを作成する</summary>
        /// <param name="name">レイヤー名</param>
        /// <param name="placement">配置先</param>
        /// <param name="sprite">先頭フレームの Sprite</param>
        private SpriteAnimationLayer CreateLayer(string name, SpriteAnimationLayer.LayerPlacement placement, Sprite sprite) {
            var layer = new SpriteAnimationLayer();
            Set(layer, "_layerName", name);
            Set(layer, "_placement", placement);
            Set(layer, "_sprites", new[] { sprite, null, null });
            return layer;
        }

        /// <summary>テスト用オブジェクトを破棄対象に登録する</summary>
        /// <param name="value">登録対象</param>
        private T Track<T>(T value) where T : Object {
            _objects.Add(value);
            return value;
        }

        /// <summary>表示先の Sprite を取得する</summary>
        /// <param name="target">表示先</param>
        private Sprite TargetSprite(Component target) {
            return target is Image image ? image.sprite : ((SpriteRenderer)target).sprite;
        }

        /// <summary>非公開フィールドへテストデータを設定する</summary>
        /// <param name="target">設定対象</param>
        /// <param name="name">フィールド名</param>
        /// <param name="value">設定値</param>
        private void Set(object target, string name, object value) {
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null) {
                field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        /// <summary>引数型に対応する非公開メソッドを実行する</summary>
        /// <param name="target">実行対象</param>
        /// <param name="name">メソッド名</param>
        /// <param name="args">引数</param>
        private object Invoke(object target, string name, params object[] args) {
            foreach (var method in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)) {
                if (method.Name != name || method.GetParameters().Length != args.Length) {
                    continue;
                }
                return method.Invoke(target, args);
            }
            throw new MissingMethodException(name);
        }
    }
}