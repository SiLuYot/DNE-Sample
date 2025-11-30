using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace UI
{
    public class PlayerNameView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;

        public void SetName(string value)
        {
            _nameText.text = value;
        }

        public void UpdatePosition(float3 position)
        {
            transform.position = Camera.main.WorldToScreenPoint(position + new float3(0, 2.5f, 0));
        }
    }
}