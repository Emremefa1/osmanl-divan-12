#if UNITY_EDITOR
using Gilzoide.LottiePlayer;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace OsmanliDivani.Lottie.EditorTools
{
    /// <summary>
    /// Project penceresinde bir Lottie JSON (LottieAnimationAsset) seçip
    /// sağ tık -> "Lottie / Create Player In Scene" deyince:
    ///   - Sahnede Canvas yoksa oluşturur
    ///   - Altına AutoLottiePlayer + ImageLottiePlayer GameObject ekler
    ///   - Seçili asset'i otomatik bağlar
    /// </summary>
    public static class LottieSceneCreator
    {
        private const string MenuPath = "Assets/Lottie/Create Player In Scene";

        [MenuItem(MenuPath, true)]
        private static bool Validate()
        {
            return Selection.GetFiltered<LottieAnimationAsset>(SelectionMode.Assets).Length > 0;
        }

        [MenuItem(MenuPath, false, 20)]
        private static void Create()
        {
            var assets = Selection.GetFiltered<LottieAnimationAsset>(SelectionMode.Assets);
            if (assets == null || assets.Length == 0)
            {
                return;
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");

                if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    var es = new GameObject("EventSystem",
                        typeof(UnityEngine.EventSystems.EventSystem),
                        typeof(UnityEngine.EventSystems.StandaloneInputModule));
                    Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
                }
            }

            GameObject lastCreated = null;
            foreach (var asset in assets)
            {
                var go = new GameObject(asset.name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Create Lottie Player");
                go.transform.SetParent(canvas.transform, false);

                var auto = Undo.AddComponent<AutoLottiePlayer>(go);
                var soAuto = new SerializedObject(auto);
                soAuto.FindProperty("_animation").objectReferenceValue = asset;
                soAuto.ApplyModifiedProperties();

                lastCreated = go;
            }

            if (lastCreated != null)
            {
                Selection.activeGameObject = lastCreated;
            }
        }
    }
}
#endif
