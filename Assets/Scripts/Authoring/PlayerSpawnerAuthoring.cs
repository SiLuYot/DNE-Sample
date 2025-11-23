using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSpawnerAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject _playerPrefab;

    class Baker : Baker<PlayerSpawnerAuthoring>
    {
        public override void Bake(PlayerSpawnerAuthoring authoring)
        {
            PlayerSpawnerComponent component = default(PlayerSpawnerComponent);
            component.Player = GetEntity(authoring._playerPrefab, TransformUsageFlags.Dynamic);
            
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}