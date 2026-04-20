using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace OsmanliDivani.Game
{
    /// <summary>
    /// ESC tuşuna basıldığında bir panel açıp kapatır.
    /// Panelde "Tekrar Başlat" ve "Müzik Aç/Kapa" butonları bulunur.
    ///
    /// Kurulum:
    ///   1) Canvas altında bir Panel oluştur (kapalı başlasın). İçine 2 Button koy:
    ///      RestartButton ve MuteButton (üzerinde TMP_Text/Text label).
    ///   2) Boş GameObject'e bu bileşeni ekle ve Inspector'da bağla.
    ///   3) Müzik için sahnede PersistentAudio veya bir AudioSource olmalı.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Osmanli Divani/Pause Menu")]
    public class PauseMenu : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Açılıp kapanacak menü paneli (başta inaktif).")]
        [SerializeField] private GameObject _menuPanel;

        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _muteButton;
        [SerializeField] private Button _resumeButton;

        [Header("Müzik")]
        [Tooltip("Boş bırakılırsa PersistentAudio.Instance kullanılır.")]
        [SerializeField] private AudioSource _audioSource;

        [Header("Etiketler")]
        [SerializeField] private string _muteLabel = "Müziği Kapat";
        [SerializeField] private string _unmuteLabel = "Müziği Aç";

        [Header("Davranış")]
        [Tooltip("Menü açıkken Time.timeScale = 0 yap.")]
        [SerializeField] private bool _pauseTimeWhenOpen = true;

        private bool _isOpen;
        private VideoPlayer[] _pausedVideos;
        private AudioSource[] _pausedAudios;

        private void Awake()
        {
            if (_menuPanel != null) _menuPanel.SetActive(false);
            if (_restartButton != null) _restartButton.onClick.AddListener(Restart);
            if (_muteButton != null) _muteButton.onClick.AddListener(ToggleMute);
            if (_resumeButton != null) _resumeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (_restartButton != null) _restartButton.onClick.RemoveListener(Restart);
            if (_muteButton != null) _muteButton.onClick.RemoveListener(ToggleMute);
            if (_resumeButton != null) _resumeButton.onClick.RemoveListener(Close);

            // Sahne kapanırken time scale'i geri al
            if (_pauseTimeWhenOpen) Time.timeScale = 1f;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Toggle();
        }

        public void Toggle() { if (_isOpen) Close(); else Open(); }

        public void Open()
        {
            _isOpen = true;
            if (_menuPanel != null) _menuPanel.SetActive(true);
            if (_pauseTimeWhenOpen) Time.timeScale = 0f;

            // VideoPlayer'lar (Unscaled Game Time olabilir) ve AudioSource'lar
            // Time.timeScale'den etkilenmez; manuel pause et.
            _pausedVideos = FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None);
            for (int i = 0; i < _pausedVideos.Length; i++)
            {
                if (_pausedVideos[i] != null && _pausedVideos[i].isPlaying)
                    _pausedVideos[i].Pause();
            }

            _pausedAudios = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            for (int i = 0; i < _pausedAudios.Length; i++)
            {
                if (_pausedAudios[i] != null && _pausedAudios[i].isPlaying)
                    _pausedAudios[i].Pause();
            }

            UpdateMuteLabel();
        }

        public void Close()
        {
            _isOpen = false;
            if (_menuPanel != null) _menuPanel.SetActive(false);
            if (_pauseTimeWhenOpen) Time.timeScale = 1f;

            if (_pausedVideos != null)
            {
                for (int i = 0; i < _pausedVideos.Length; i++)
                {
                    if (_pausedVideos[i] != null) _pausedVideos[i].Play();
                }
                _pausedVideos = null;
            }
            if (_pausedAudios != null)
            {
                for (int i = 0; i < _pausedAudios.Length; i++)
                {
                    if (_pausedAudios[i] != null && !_pausedAudios[i].mute)
                        _pausedAudios[i].UnPause();
                }
                _pausedAudios = null;
            }
        }

        public void Restart()
        {
            if (_pauseTimeWhenOpen) Time.timeScale = 1f;

            // Persist eden audio source'lar pause durumunda kalmasın
            if (_pausedAudios != null)
            {
                for (int i = 0; i < _pausedAudios.Length; i++)
                {
                    if (_pausedAudios[i] != null && !_pausedAudios[i].mute)
                        _pausedAudios[i].UnPause();
                }
                _pausedAudios = null;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ToggleMute()
        {
            var src = ResolveAudioSource();
            if (src == null) return;
            src.mute = !src.mute;
            UpdateMuteLabel();
        }

        private AudioSource ResolveAudioSource()
        {
            if (_audioSource != null) return _audioSource;
            if (PersistentAudio.Instance != null) return PersistentAudio.Instance.Source;
            return null;
        }

        private void UpdateMuteLabel()
        {
            if (_muteButton == null) return;
            var src = ResolveAudioSource();
            string text = (src != null && src.mute) ? _unmuteLabel : _muteLabel;

            var tmp = _muteButton.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmp != null) { tmp.text = text; return; }
            var t = _muteButton.GetComponentInChildren<Text>(true);
            if (t != null) t.text = text;
        }
    }
}
