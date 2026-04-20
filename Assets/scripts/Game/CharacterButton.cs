using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace OsmanliDivani.Game
{
    /// <summary>
    /// Bir karakteri (Vezir, Sadrazam vb.) temsil eder.
    /// - Karakter görseli üzerinde tıklanabilir bir alan (Button) olur.
    /// - Tıklanınca karakterin üstünde seçim butonları açılır.
    /// - Bir seçim seçilince DivanSceneController'a bildirir; controller
    ///   önce seçim videosunu, ardından karakterin "yere dönme" videosunu
    ///   sırayla oynatır.
    ///
    /// Kurulum:
    ///   1) Sahnede karakterin RectTransform'una (RawImage/Image) bu bileşeni ekle.
    ///   2) Inspector'da: DisplayName, ChoicesParent (boş bir GameObject - butonların
    ///      konacağı container), ChoiceButtonPrefab (TMP_Text/Text içeren bir Button prefab),
    ///      Choices listesi (her biri Label + Clip), ReturnClip ve Controller'ı bağla.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Osmanli Divani/Character Button")]
    [RequireComponent(typeof(RectTransform))]
    public class CharacterButton : MonoBehaviour
    {
        [Serializable]
        public class Choice
        {
            [Tooltip("Butonda görünecek metin.")]
            public string Label;

            [Tooltip("Bu seçim seçildiğinde oynatılacak video.")]
            public VideoClip Clip;
        }

        [Header("Karakter")]
        [SerializeField] private string _displayName = "Karakter";

        [Header("Bağlantılar")]
        [Tooltip("Seçim sahnesi yöneticisi. Boş bırakılırsa sahnede otomatik bulunur.")]
        [SerializeField] private DivanSceneController _controller;

        [Tooltip("Bu karaktere ait tıklanabilir alan (Button). Boşsa kendisinden alınır/eklenir.")]
        [SerializeField] private Button _hitButton;

        [Tooltip("Seçim butonlarının altına ekleneceği parent (genelde karakterin üstünde duran bir panel).")]
        [SerializeField] private RectTransform _choicesParent;

        [Tooltip("Seçim butonu prefabı. Üzerinde Button + (TMP_Text veya Text) olmalı.")]
        [SerializeField] private Button _choiceButtonPrefab;

        [Header("Seçimler")]
        [SerializeField] private List<Choice> _choices = new List<Choice>();

        [Header("Yere Dönme")]
        [Tooltip("Seçim videosu bittikten sonra oynatılacak 'yerine dönme' videosu.")]
        [SerializeField] private VideoClip _returnClip;

        private readonly List<Button> _spawnedButtons = new List<Button>();
        private bool _choicesOpen;

        public string DisplayName => _displayName;
        /// <summary>Bu karakterle en az bir seçim yapıldı mı?</summary>
        public bool HasUsedAtLeastOnce { get; private set; }

        private void Awake()
        {
            if (_controller == null) _controller = FindFirstObjectByType<DivanSceneController>();
            if (_hitButton == null)
            {
                _hitButton = GetComponent<Button>();
                if (_hitButton == null) _hitButton = gameObject.AddComponent<Button>();
            }
            // Görünmez tıklama alanı için saydam bir grafik gerekir
            if (GetComponent<Graphic>() == null)
            {
                var img = gameObject.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f);
                img.raycastTarget = true;
            }

            _hitButton.onClick.AddListener(OnCharacterClicked);
            HideChoices();
        }

        private void OnDestroy()
        {
            if (_hitButton != null) _hitButton.onClick.RemoveListener(OnCharacterClicked);
            ClearSpawnedButtons();
        }

        public void SetInteractable(bool value)
        {
            if (_hitButton != null) _hitButton.interactable = value;
            if (!value && _choicesOpen) HideChoices();
        }

        private void OnCharacterClicked()
        {
            if (_controller != null && _controller.IsBusy) return;
            if (_choicesOpen) { HideChoices(); return; }
            ShowChoices();
        }

        private void ShowChoices()
        {
            ClearSpawnedButtons();
            if (_choicesParent == null || _choiceButtonPrefab == null)
            {
                Debug.LogWarning($"[{name}] Choices parent veya buton prefabı atanmamış.", this);
                return;
            }

            _choicesParent.gameObject.SetActive(true);

            for (int i = 0; i < _choices.Count; i++)
            {
                var choice = _choices[i];
                var btn = Instantiate(_choiceButtonPrefab, _choicesParent);
                btn.gameObject.SetActive(true);

                // Etiketi güncelle (TMP veya UGUI Text destekle)
                var tmp = btn.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (tmp != null) tmp.text = choice.Label;
                else
                {
                    var txt = btn.GetComponentInChildren<Text>(true);
                    if (txt != null) txt.text = choice.Label;
                }

                var captured = choice;
                btn.onClick.AddListener(() => OnChoiceSelected(captured));
                _spawnedButtons.Add(btn);
            }

            _choicesOpen = true;
        }

        private void HideChoices()
        {
            ClearSpawnedButtons();
            if (_choicesParent != null) _choicesParent.gameObject.SetActive(false);
            _choicesOpen = false;
        }

        private void ClearSpawnedButtons()
        {
            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                if (_spawnedButtons[i] != null) Destroy(_spawnedButtons[i].gameObject);
            }
            _spawnedButtons.Clear();
        }

        private void OnChoiceSelected(Choice choice)
        {
            HideChoices();
            HasUsedAtLeastOnce = true;

            var sequence = new List<VideoClip>(2);
            if (choice.Clip != null) sequence.Add(choice.Clip);
            if (_returnClip != null) sequence.Add(_returnClip);

            if (_controller != null)
            {
                _controller.PlayChoiceSequence(this, sequence);
            }
        }
    }
}
