using Component;
using TMPro;
using Unity.Entities;
using Unity.NetCode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class GameView : MonoBehaviour
    {
        [SerializeField] private GameObject _lobbyRoot;
        [SerializeField] private GameObject _inGameRoot;

        [SerializeField] private TMP_InputField _nickName;
        [SerializeField] private Button _nameChange;

        private bool _isRequested = false;
        private EntityQuery _connectedQuery;
        private EntityQuery _disconnectedQuery;

        private void Start()
        {
            _connectedQuery = ClientServerBootstrap.ClientWorld.EntityManager
                .CreateEntityQuery(typeof(PlayerConnectedEvent));

            _disconnectedQuery = ClientServerBootstrap.ClientWorld.EntityManager
                .CreateEntityQuery(typeof(PlayerDisconnectedEvent));

            HandleDisconnection();

            _nameChange.onClick.AddListener(OnClickNameChange);
            _nickName.text = AuthenticationService.Instance.PlayerName;
        }

        private void Update()
        {
            if (!_connectedQuery.IsEmpty)
                HandleConnection();

            if (!_disconnectedQuery.IsEmpty)
                HandleDisconnection();
        }

        private void HandleConnection()
        {
            _lobbyRoot.SetActive(false);
            _inGameRoot.SetActive(true);
        }

        private void HandleDisconnection()
        {
            _lobbyRoot.SetActive(true);
            _inGameRoot.SetActive(false);
        }

        private async void ChangeNameAsync(string playerName)
        {
            if (_isRequested)
                return;

            _isRequested = true;
            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
            _isRequested = false;

            _nickName.text = AuthenticationService.Instance.PlayerName;
        }

        private void OnClickNameChange()
        {
            if (string.IsNullOrEmpty(_nickName.text))
                return;

            ChangeNameAsync(_nickName.text);
        }
    }
}