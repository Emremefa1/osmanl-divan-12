using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace OsmanliDivani.Video
{
    /// <summary>
    /// GameObject'e eklendiğinde:
    ///  - Canvas yoksa otomatik oluşturur, kendisini Canvas altına alır
    ///  - RawImage + VideoPlayer + RenderTexture kurulumunu otomatik yapar
    ///  - Inspector'a sürüklenen VideoClip'i Play On Awake + Loop ile oynatır
    ///
    /// Kullanım: Boş GameObject -> Add Component -> "Video / Auto Video Player"
    ///          -> Clip alanına MP4'ünü sürükle. Play'e bas. Bitti.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Video/Auto Video Player")]
    public class AutoVideoPlayer : MonoBehaviour
    {
        [Header("Video")]
        [Tooltip("MP4/WebM dosyasını sürükle.")]
        [SerializeField] private VideoClip _clip;

        [Header("Oynatma")]
        [SerializeField] private bool _loop = true;
        [SerializeField] private bool _playOnAwake = true;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        [Header("Texture")]
        [SerializeField, Min(16)] private int _width = 1920;
        [SerializeField, Min(16)] private int _height = 1080;

        private RawImage _rawImage;
        private VideoPlayer _player;
        private RenderTexture _renderTexture;

        private void Reset()
        {
            EnsureCanvasParent();
            EnsureComponents();
            ApplySettings();
        }

        private void OnEnable()
        {
            EnsureComponents();
            ApplySettings();
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            EnsureComponents();
            ApplySettings();
        }

        private void OnDestroy()
        {
            if (_renderTexture != null)
            {
                if (Application.isPlaying) Destroy(_renderTexture);
                else DestroyImmediate(_renderTexture);
            }
        }

        private void EnsureCanvasParent()
        {
            if (GetComponentInParent<Canvas>() != null) return;

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            transform.SetParent(canvas.transform, false);

            // RectTransform varsayılanını ayarla
            if (transform is RectTransform rt)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(800, 450);
                rt.anchoredPosition = Vector2.zero;
            }
        }

        private void EnsureComponents()
        {
            if (_rawImage == null)
            {
                _rawImage = GetComponent<RawImage>();
                if (_rawImage == null) _rawImage = gameObject.AddComponent<RawImage>();
            }

            if (_player == null)
            {
                _player = GetComponent<VideoPlayer>();
                if (_player == null) _player = gameObject.AddComponent<VideoPlayer>();
            }
        }

        private void ApplySettings()
        {
            int w = Mathf.Max(16, _width);
            int h = Mathf.Max(16, _height);

            // RenderTexture (gerekirse yeniden oluştur)
            if (_renderTexture == null || _renderTexture.width != w || _renderTexture.height != h)
            {
                if (_renderTexture != null)
                {
                    if (Application.isPlaying) Destroy(_renderTexture);
                    else DestroyImmediate(_renderTexture);
                }
                _renderTexture = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
                {
                    name = "AutoVideoPlayer_RT",
                    hideFlags = HideFlags.DontSave,
                };
                _renderTexture.Create();
            }

            // VideoPlayer
            _player.playOnAwake = _playOnAwake;
            _player.isLooping = _loop;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.targetTexture = _renderTexture;
            _player.audioOutputMode = VideoAudioOutputMode.Direct;
            _player.SetDirectAudioVolume(0, _volume);
            if (_player.clip != _clip) _player.clip = _clip;
            _player.waitForFirstFrame = true;
            _player.skipOnDrop = true;

            // RawImage
            _rawImage.texture = _renderTexture;
            _rawImage.color = Color.white;
        }
    }
}
