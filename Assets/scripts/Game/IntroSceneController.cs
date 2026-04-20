using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace OsmanliDivani.Game
{
    /// <summary>
    /// Başlangıç sahnesinin yöneticisi.
    /// Akış:
    ///   1) Sahne açılır açılmaz "Intro Clip" oynar.
    ///   2) Bittiğinde "Divanı Başlat" butonu görünür hale gelir (son kare ekranda kalır).
    ///   3) Butona tıklanınca "Transition Clip" oynar.
    ///   4) Sonrasında "Selection Scene Name" sahnesi yüklenir.
    ///
    /// Kurulum:
    ///   1) Boş bir GameObject'e bu bileşeni ekle.
    ///   2) VideoStoryPlayer'ı bağla.
    ///   3) "Start Button"u (başta gizli) ve clip'leri ata.
    ///   4) Build Settings'e seçim sahnesini eklemeyi unutma.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Osmanli Divani/Intro Scene Controller")]
    public class IntroSceneController : MonoBehaviour
    {
        [Header("Bağlantılar")]
        [SerializeField] private VideoStoryPlayer _videoPlayer;
        [SerializeField] private Button _startButton;

        [Header("Videolar")]
        [Tooltip("Sahne açılınca oynayan giriş videosu (örn. başlangıç.mp4).")]
        [SerializeField] private VideoClip _introClip;

        [Tooltip("Başlat butonuna basıldığında oynatılan geçiş videosu (örn. padısahhhh.mp4). Boşsa direkt sahne yüklenir.")]
        [SerializeField] private VideoClip _transitionClip;

        [Header("Sahne")]
        [Tooltip("Geçiş sonrası yüklenecek seçim sahnesinin adı.")]
        [SerializeField] private string _selectionSceneName = "SampleScene";

        private bool _busy;

        private void Awake()
        {
            if (_videoPlayer == null) _videoPlayer = FindFirstObjectByType<VideoStoryPlayer>();
            if (_startButton != null)
            {
                _startButton.gameObject.SetActive(false);
                _startButton.onClick.AddListener(OnStartClicked);
            }
        }

        private void Start()
        {
            if (_videoPlayer == null) return;
            _busy = true;
            _videoPlayer.PlayClip(_introClip, () =>
            {
                _busy = false;
                if (_startButton != null) _startButton.gameObject.SetActive(true);
            });
        }

        private void OnDestroy()
        {
            if (_startButton != null) _startButton.onClick.RemoveListener(OnStartClicked);
        }

        private void OnStartClicked()
        {
            if (_busy) return;
            _busy = true;
            if (_startButton != null) _startButton.interactable = false;

            _videoPlayer.PlayClip(_transitionClip, () =>
            {
                SceneManager.LoadScene(_selectionSceneName);
            });
        }
    }
}
