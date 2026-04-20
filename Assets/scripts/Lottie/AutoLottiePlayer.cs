using Gilzoide.LottiePlayer;
using UnityEngine;
using UnityEngine.UI;

namespace OsmanliDivani.Lottie
{
    /// <summary>
    /// Sahneye eklenip Inspector'dan bir Lottie JSON (LottieAnimationAsset)
    /// sürüklendiğinde otomatik oynatan basit bileşen.
    ///
    /// - Canvas yoksa otomatik oluşturur.
    /// - ImageLottiePlayer yoksa otomatik ekler.
    /// - Asset her değiştiğinde anında günceller (Editor'da da).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Lottie/Auto Lottie Player")]
    public class AutoLottiePlayer : MonoBehaviour
    {
        [Header("Animasyon")]
        [Tooltip("Sürükle-bırak ile bir Lottie JSON dosyası ekleyin.")]
        [SerializeField] private LottieAnimationAsset _animation;

        [Header("Oynatma")]
        [SerializeField] private bool _loop = true;
        [SerializeField] private bool _playOnStart = true;

        [Header("Texture")]
        [SerializeField, Min(2)] private int _width = 256;
        [SerializeField, Min(2)] private int _height = 256;
        [SerializeField] private bool _keepAspect = true;

        private ImageLottiePlayer _player;
        private LottieAnimationAsset _lastAssigned;

        private void Reset()
        {
            EnsureCanvasParent();
            EnsurePlayer();
        }

        private void OnEnable()
        {
            EnsurePlayer();
            ApplyToPlayer();
            if (_playOnStart && Application.isPlaying && _animation != null)
            {
                _player.Play();
            }
        }

        private void OnValidate()
        {
            // Inspector'da değer değişince anında uygula
            if (_player == null)
            {
                _player = GetComponent<ImageLottiePlayer>();
            }
            if (_player != null)
            {
                ApplyToPlayer();
            }
        }

        private void EnsurePlayer()
        {
            if (_player == null)
            {
                _player = GetComponent<ImageLottiePlayer>();
                if (_player == null)
                {
                    _player = gameObject.AddComponent<ImageLottiePlayer>();
                }
            }
        }

        private void EnsureCanvasParent()
        {
            // Bu GameObject bir Canvas altında değilse, sahnedeki ilk Canvas'a taşı;
            // o da yoksa yeni bir Canvas oluştur.
            if (GetComponentInParent<Canvas>() != null)
            {
                return;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            transform.SetParent(canvas.transform, false);
        }

        private void ApplyToPlayer()
        {
            // Reflection yerine public API'leri kullan
            if (_animation != _lastAssigned)
            {
                _player.SetAnimationAsset(_animation);
                _lastAssigned = _animation;
            }

            // Diğer alanlar private SerializeField olduğundan
            // SerializedObject ile yazıyoruz (yalnızca Editor'da).
#if UNITY_EDITOR
            var so = new UnityEditor.SerializedObject(_player);
            so.FindProperty("_loop").boolValue = _loop;
            so.FindProperty("_width").intValue = _width;
            so.FindProperty("_height").intValue = _height;
            so.FindProperty("_keepAspect").boolValue = _keepAspect;
            so.ApplyModifiedPropertiesWithoutUndo();
#endif
        }
    }
}
