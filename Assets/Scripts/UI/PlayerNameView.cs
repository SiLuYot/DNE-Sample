using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace UI
{
    public class PlayerNameView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;

        private string _playerName;
        private int _level;

        public void SetName(string value)
        {
            _playerName = value;
            UpdateText();
        }

        public void SetLevel(int level)
        {
            _level = level;
            UpdateText();
        }

        private void UpdateText()
        {
            _nameText.text = $"Lv.{_level} {_playerName}";
        }

        public void UpdatePosition(float3 position)
        {
            transform.position = Camera.main.WorldToScreenPoint(position + new float3(0, 2.5f, 0));
        }
    }
}