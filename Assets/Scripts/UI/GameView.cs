using Component;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace UI
{
    public class GameView : MonoBehaviour
    {
        [SerializeField] private GameObject _lobbyRoot;
        [SerializeField] private GameObject _inGameRoot;

        private World _world;
        private EntityManager _em;

        private void Start()
        {
            _world = ClientServerBootstrap.ClientWorld;
            _em = _world.EntityManager;
            
            _lobbyRoot.SetActive(true);
            _inGameRoot.SetActive(false);
        }

        private void Update()
        {
            var query = _em.CreateEntityQuery(typeof(PlayerConnectionComponent));
            if (!query.TryGetSingletonEntity<PlayerConnectionComponent>(out var entity))
                return;

            var connection = _em.GetComponentData<PlayerConnectionComponent>(entity);
            if (!connection.Updated)
                return;

            _lobbyRoot.SetActive(!connection.IsConnected);
            _inGameRoot.SetActive(connection.IsConnected);

            connection.Updated = false;
            _em.SetComponentData(entity, connection);
        }
    }
}