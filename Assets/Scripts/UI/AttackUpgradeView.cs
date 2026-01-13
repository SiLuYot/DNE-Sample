using RPC;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class AttackUpgradeView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _projectileButton;
        [SerializeField] private TextMeshProUGUI _projectileText;
        [SerializeField] private Button _missileButton;
        [SerializeField] private TextMeshProUGUI _missileText;
        [SerializeField] private Button _swordButton;
        [SerializeField] private TextMeshProUGUI _swordText;

        public AttackUpgradeType? PendingSelection { get; set; }

        private void Awake()
        {
            _projectileButton.onClick.AddListener(OnProjectileSelected);
            _missileButton.onClick.AddListener(OnMissileSelected);
            _swordButton.onClick.AddListener(OnSwordSelected);
        }

        public void Show(int projectileLevel, int missileLevel, int swordLevel)
        {
            _titleText.text = "Select Upgrade!";
            _projectileText.text = $"Normal Attack\n(Lv.{projectileLevel} → Lv.{projectileLevel + 1})";
            _missileText.text = $"Auto Missile Attack\n(Lv.{missileLevel} → Lv.{missileLevel + 1})";
            _swordText.text = $"Sword Attack\n(Lv.{swordLevel} → Lv.{swordLevel + 1})";
        }

        private void OnProjectileSelected()
        {
            PendingSelection = AttackUpgradeType.Projectile;
        }

        private void OnMissileSelected()
        {
            PendingSelection = AttackUpgradeType.Missile;
        }

        private void OnSwordSelected()
        {
            PendingSelection = AttackUpgradeType.Sword;
        }
    }
}