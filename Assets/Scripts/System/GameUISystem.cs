using Component;
using UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace System
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.Presentation)]
    public partial class GameUISystem : SystemBase
    {
        private EntityQuery _uiConfigQuery;
        private EntityQuery _canvasQuery;
        private EntityQuery _updateQuery;
        private EntityQuery _cameraTargetQuery;

        protected override void OnCreate()
        {
            _uiConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UIConfigComponent>());
            _canvasQuery = GetEntityQuery(ComponentType.ReadOnly<MainCanvasTag>());

            _updateQuery = GetEntityQuery(
                ComponentType.ReadOnly<LocalToWorld>(),
                new ComponentType(typeof(PlayerNameView), ComponentType.AccessMode.ReadOnly));

            _cameraTargetQuery = GetEntityQuery(
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<LocalPlayerTag>()
            );

            RequireForUpdate(_uiConfigQuery);
            RequireForUpdate(_canvasQuery);
        }

        protected override void OnUpdate()
        {
            var configEntity = _uiConfigQuery.GetSingletonEntity();
            var uiConfig = EntityManager.GetComponentObject<UIConfigComponent>(configEntity);

            var canvasEntity = _canvasQuery.GetSingletonEntity();
            var canvasContainer = EntityManager.GetComponentObject<UICanvasComponent>(canvasEntity);

            var canvas = canvasContainer.CanvasReference;
            var uiPrefab = uiConfig.NamePrefab;

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);

            foreach (var (playerData, entity) in SystemAPI.Query<RefRO<PlayerNameComponent>>()
                         .WithNone<PlayerNameView>()
                         .WithEntityAccess())
            {
                var uiObj = UnityEngine.Object.Instantiate(uiPrefab, canvas.transform);

                var uiScript = uiObj.GetComponent<PlayerNameView>();
                uiScript.SetName(playerData.ValueRO.Name.ToString());

                ecb.AddComponent(entity, uiScript);
            }

            using var entities = _updateQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var transform = EntityManager.GetComponentData<LocalToWorld>(entity);
                var uiScript = EntityManager.GetComponentObject<PlayerNameView>(entity);

                uiScript.UpdatePosition(transform.Position);
            }

            if (!_cameraTargetQuery.IsEmpty)
            {
                var targetEntity = _cameraTargetQuery.GetSingletonEntity();
                var targetTransform = EntityManager.GetComponentData<LocalToWorld>(targetEntity);
                var targetPosition = targetTransform.Position + new float3(0, 12f, -5f);

                Camera.main.transform.position = targetPosition;
            }
        }
    }
}