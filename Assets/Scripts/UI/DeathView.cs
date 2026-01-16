using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class DeathView : MonoBehaviour
    {
        [SerializeField] private Button _respawnButton;

        public bool RequestRespawn { get; set; }

        private void Awake()
        {
            _respawnButton.onClick.AddListener(OnRespawnClicked);
        }

        private void OnDestroy()
        {
            _respawnButton.onClick.RemoveListener(OnRespawnClicked);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RequestRespawn = false;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnRespawnClicked()
        {
            RequestRespawn = true;
        }
    }
}