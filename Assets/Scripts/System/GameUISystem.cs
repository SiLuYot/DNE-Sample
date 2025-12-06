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
        private EntityQuery _cameraTargetQuery;

        protected override void OnCreate()
        {
            _uiConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UIConfigComponent>());
            _canvasQuery = GetEntityQuery(ComponentType.ReadOnly<MainCanvasTag>());

            _cameraTargetQuery = GetEntityQuery(
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<LocalPlayerTag>()
            );

            RequireForUpdate(_uiConfigQuery);
            RequireForUpdate(_canvasQuery);
        }

        protected override void OnUpdate()
        {
            var ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);

            var cleanupQuery = SystemAPI.QueryBuilder()
                .WithAll<PlayerUICleanup>()
                .WithNone<LocalToWorld>()
                .Build();

            using var cleanupEntities = cleanupQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in cleanupEntities)
            {
                var cleanupComp = EntityManager.GetComponentObject<PlayerUICleanup>(entity);
                if (cleanupComp.View != null)
                {
                    UnityEngine.Object.Destroy(cleanupComp.View.gameObject);
                }

                ecb.RemoveComponent<PlayerUICleanup>(entity);
            }

            var configEntity = _uiConfigQuery.GetSingletonEntity();
            var uiConfig = EntityManager.GetComponentObject<UIConfigComponent>(configEntity);

            var canvasEntity = _canvasQuery.GetSingletonEntity();
            var canvas = EntityManager.GetComponentObject<UICanvasComponent>(canvasEntity).CanvasReference;

            foreach (var (playerData, entity) in SystemAPI.Query<RefRO<PlayerNameComponent>>()
                         .WithNone<PlayerUICleanup>()
                         .WithEntityAccess())
            {
                var uiObj = UnityEngine.Object.Instantiate(uiConfig.NamePrefab, canvas.transform);
                var uiScript = uiObj.GetComponent<PlayerNameView>();

                uiScript.SetName(playerData.ValueRO.PlayerName.ToString());

                ecb.AddComponent(entity, new PlayerUICleanup { View = uiScript });
            }
            
            foreach (var (transform, entity) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<PlayerUICleanup>()
                         .WithEntityAccess())
            {
                var uiRef = EntityManager.GetComponentObject<PlayerUICleanup>(entity);
                if (uiRef.View != null)
                {
                    uiRef.View.UpdatePosition(transform.ValueRO.Position);
                }
            }
            
            if (!_cameraTargetQuery.IsEmpty)
            {
                var targetEntity = _cameraTargetQuery.GetSingletonEntity();
                var targetTransform = EntityManager.GetComponentData<LocalToWorld>(targetEntity);
                Camera.main.transform.position = targetTransform.Position + new float3(0, 12f, -5f);
            }
        }
    }
}