using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace OsmanliDivani.Game
{
    /// <summary>
    /// Seçim sahnesinin yöneticisi.
    /// - Tüm CharacterButton'ları izler.
    /// - Bir karakter seçim yapınca seçim + dönüş videosunu oynatır.
    /// - Tüm karakterlerden en az bir seçim yapıldığında "Divanı Sonlandır"
    ///   butonunu gösterir; tıklanınca bitiş videosunu oynatıp sahneyi reload eder.
    ///
    /// Kurulum:
    ///   1) Boş bir GameObject'e bu bileşeni ekle.
    ///   2) VideoStoryPlayer'ı bağla (sahnede bir tane olmalı).
    ///   3) Sahnedeki tüm CharacterButton'ları "Characters" listesine ekle.
    ///   4) "End Divan Button" (Canvas altında, başta gizli) ve "End Divan Clip"i bağla.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Osmanli Divani/Divan Scene Controller")]
    public class DivanSceneController : MonoBehaviour
    {
        [Header("Bağlantılar")]
        [SerializeField] private VideoStoryPlayer _videoPlayer;
        [SerializeField] private List<CharacterButton> _characters = new List<CharacterButton>();

        [Header("Bitiş")]
        [Tooltip("Tüm karakterlerle en az bir seçim yapıldığında görünecek buton.")]
        [SerializeField] private Button _endDivanButton;

        [Tooltip("Bitiş butonuna basıldığında oynatılacak video.")]
        [SerializeField] private VideoClip _endDivanClip;

        [Tooltip("Bitiş videosu sonrasında yüklenecek sahne adı. Boş bırakılırsa mevcut sahne reload edilir.")]
        [SerializeField] private string _sceneToLoadAfterEnd = "";

        public bool IsBusy { get; private set; }

        private void Awake()
        {
            if (_videoPlayer == null) _videoPlayer = FindFirstObjectByType<VideoStoryPlayer>();
            if (_endDivanButton != null)
            {
                _endDivanButton.gameObject.SetActive(false);
                _endDivanButton.onClick.AddListener(OnEndDivanClicked);
            }
        }

        private void OnDestroy()
        {
            if (_endDivanButton != null) _endDivanButton.onClick.RemoveListener(OnEndDivanClicked);
        }

        public void PlayChoiceSequence(CharacterButton source, IList<VideoClip> sequence)
        {
            if (IsBusy || _videoPlayer == null) return;

            SetCharactersInteractable(false);
            IsBusy = true;

            _videoPlayer.PlaySequence(sequence, () =>
            {
                IsBusy = false;
                SetCharactersInteractable(true);
                RefreshEndButton();
            });
        }

        private void SetCharactersInteractable(bool value)
        {
            for (int i = 0; i < _characters.Count; i++)
            {
                if (_characters[i] != null) _characters[i].SetInteractable(value);
            }
        }

        private void RefreshEndButton()
        {
            if (_endDivanButton == null) return;

            bool allUsed = _characters.Count > 0;
            for (int i = 0; i < _characters.Count; i++)
            {
                if (_characters[i] == null || !_characters[i].HasUsedAtLeastOnce)
                {
                    allUsed = false;
                    break;
                }
            }

            if (allUsed && !_endDivanButton.gameObject.activeSelf)
            {
                _endDivanButton.gameObject.SetActive(true);
            }
        }

        private void OnEndDivanClicked()
        {
            if (IsBusy) return;
            IsBusy = true;
            SetCharactersInteractable(false);
            if (_endDivanButton != null) _endDivanButton.interactable = false;

            _videoPlayer.PlayClip(_endDivanClip, () =>
            {
                if (string.IsNullOrEmpty(_sceneToLoadAfterEnd))
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                else
                {
                    SceneManager.LoadScene(_sceneToLoadAfterEnd);
                }
            });
        }
    }
}
