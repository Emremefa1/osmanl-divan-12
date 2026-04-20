using UnityEngine;

namespace OsmanliDivani.Game
{
    /// <summary>
    /// Sahneler arası yaşayan tek bir AudioSource (singleton).
    /// İlk sahneye eklediğinde DontDestroyOnLoad olur ve sonraki sahnelerde
    /// aynı GameObject korunur. Aynı isimde ikinci bir kopya oluşturulursa
    /// (örn. başka sahnede tekrar varsa) yenisi yok edilir.
    ///
    /// Kullanım:
    ///   1) Boş GameObject -> Add Component -> Audio Source (klip + Loop + Play On Awake).
    ///   2) Aynı GameObject'e bu bileşeni ekle.
    ///   3) Çalış. İlk sahnede oluşur, sonraki sahnelerde tekrar oluşmaz.
    ///
    /// İstediğin yerden: PersistentAudio.Instance.Source ile erişebilirsin.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("Osmanli Divani/Persistent Audio")]
    public class PersistentAudio : MonoBehaviour
    {
        [Tooltip("Aynı tag/identifier'a sahip ikinci bir kopya gelirse yok edilir.")]
        [SerializeField] private string _identifier = "Music";

        public static PersistentAudio Instance { get; private set; }
        public AudioSource Source { get; private set; }
        public string Identifier => _identifier;

        private void Awake()
        {
            if (Instance != null && Instance != this && Instance._identifier == _identifier)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Source = GetComponent<AudioSource>();
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
