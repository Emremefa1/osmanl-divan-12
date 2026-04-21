using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace OsmanliDivani.Game
{
    /// <summary>
    /// Tam ekran tek bir video oynatıcı. Sahnedeki Canvas altına kendisini
    /// otomatik yerleştirir, RawImage + VideoPlayer + RenderTexture kurar.
    /// Tek bir clip ya da bir clip dizisini sırayla oynatabilir; bitiminde
    /// verilen callback'i çağırır.
    ///
    /// Kullanım:
    ///   storyPlayer.PlayClip(clip, () => { /* bitti */ });
    ///   storyPlayer.PlaySequence(new [] { c1, c2 }, () => { /* hepsi bitti */ });
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Osmanli Divani/Video Story Player")]
    public class VideoStoryPlayer : MonoBehaviour
    {
        [Header("Texture")]
        [SerializeField, Min(16)] private int _width = 1920;
        [SerializeField, Min(16)] private int _height = 1080;

        [Header("Ses")]
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        [Header("Hız")]
        [Tooltip("Oynatma hızı çarpanı. 1 = normal, 0.5 = yarı hız, 2 = iki kat.")]
        [SerializeField, Range(0.1f, 4f)] private float _playbackSpeed = 1f;

        [Header("Davranış")]
        [Tooltip("Hiçbir şey oynamadığında RawImage'ı gizle.")]
        [SerializeField] private bool _hideWhenIdle = false;

        [Tooltip("Boşken son kareyi göster (true). False ise siyah/şeffaf olur.")]
        [SerializeField] private bool _keepLastFrame = true;

        private RawImage _rawImage;
        private VideoPlayer _player;
        private RenderTexture _renderTexture;
        private Coroutine _sequenceRoutine;

        public bool IsPlaying => _player != null && _player.isPlaying;

        private void Awake()
        {
            EnsureCanvasParent();
            EnsureComponents();
            ApplySettings();
            if (_hideWhenIdle) _rawImage.enabled = false;
        }

        private void OnDestroy()
        {
            if (_renderTexture != null)
            {
                if (Application.isPlaying) Destroy(_renderTexture);
                else DestroyImmediate(_renderTexture);
            }
        }

        /// <summary>Tek bir clip oynatır.</summary>
        public void PlayClip(VideoClip clip, Action onComplete = null)
        {
            if (clip == null)
            {
                onComplete?.Invoke();
                return;
            }
            PlaySequence(new[] { clip }, onComplete);
        }

        /// <summary>Birden fazla clip'i sırayla oynatır.</summary>
        public void PlaySequence(IList<VideoClip> clips, Action onComplete = null)
        {
            if (_sequenceRoutine != null) StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = StartCoroutine(SequenceRoutine(clips, onComplete));
        }

        public void Stop()
        {
            if (_sequenceRoutine != null)
            {
                StopCoroutine(_sequenceRoutine);
                _sequenceRoutine = null;
            }
            if (_player != null) _player.Stop();
            if (_hideWhenIdle && _rawImage != null) _rawImage.enabled = false;
        }

        private IEnumerator SequenceRoutine(IList<VideoClip> clips, Action onComplete)
        {
            EnsureComponents();
            ApplySettings();
            _rawImage.enabled = true;

            for (int i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                if (clip == null) continue;

                bool finished = false;
                void OnEnd(VideoPlayer vp) { finished = true; }

                _player.Stop();
                _player.clip = clip;
                _player.isLooping = false;
                _player.frame = 0;

                _player.loopPointReached -= OnEnd;
                _player.loopPointReached += OnEnd;

                _player.Prepare();
                while (!_player.isPrepared) yield return null;

                _player.Play();

                // Oynatmanın gerçekten başlamasını bekle (ilk frame ilerlesin)
                long startFrame = _player.frame;
                float safetyTimer = 0f;
                while (!finished && _player.frame <= startFrame && safetyTimer < 1f)
                {
                    safetyTimer += Time.unscaledDeltaTime;
                    yield return null;
                }

                // Bitiş event'ini bekle
                while (!finished) yield return null;

                _player.loopPointReached -= OnEnd;
            }

            _sequenceRoutine = null;

            if (!_keepLastFrame)
            {
                _player.Stop();
            }
            if (_hideWhenIdle) _rawImage.enabled = false;

            onComplete?.Invoke();
        }

        private void EnsureCanvasParent()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    var canvasGo = new GameObject("Canvas",
                        typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    canvas = canvasGo.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = -10; // arka planda
                }
                transform.SetParent(canvas.transform, false);
            }

            // RectTransform'u tam ekran stretch yap
            if (transform is RectTransform rt)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
            }
        }

        private void EnsureComponents()
        {
            if (_rawImage == null)
            {
                _rawImage = GetComponent<RawImage>();
                if (_rawImage == null) _rawImage = gameObject.AddComponent<RawImage>();
                _rawImage.raycastTarget = false; // tıklamaları engellemesin
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

            if (_renderTexture == null || _renderTexture.width != w || _renderTexture.height != h)
            {
                if (_renderTexture != null)
                {
                    if (Application.isPlaying) Destroy(_renderTexture);
                    else DestroyImmediate(_renderTexture);
                }
                _renderTexture = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
                {
                    name = "VideoStoryPlayer_RT",
                    hideFlags = HideFlags.DontSave,
                };
                _renderTexture.Create();
            }

            _player.playOnAwake = false;
            _player.isLooping = false;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.targetTexture = _renderTexture;
            _player.audioOutputMode = VideoAudioOutputMode.Direct;
            _player.SetDirectAudioVolume(0, _volume);
            _player.waitForFirstFrame = true;
            _player.skipOnDrop = true;
            _player.playbackSpeed = Mathf.Max(0.01f, _playbackSpeed);

            _rawImage.texture = _renderTexture;
            _rawImage.color = Color.white;
        }
    }
}
