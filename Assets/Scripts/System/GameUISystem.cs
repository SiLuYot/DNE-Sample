using Component.Player;
using Component.UI;
using UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace System
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation |
                       WorldSystemFilterFlags.Presentation)]
    public partial class GameUISystem : SystemBase
    {
        private EntityQuery _uiConfigQuery;
        private EntityQuery _canvasQuery;

        protected override void OnCreate()
        {
            _uiConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UIConfigComponent>());
            _canvasQuery = GetEntityQuery(ComponentType.ReadOnly<UICanvasTag>());

            RequireForUpdate(_uiConfigQuery);
            RequireForUpdate(_canvasQuery);
        }

        protected override void OnUpdate()
        {
            var ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);

            var cleanupQuery = SystemAPI.QueryBuilder()
                .WithAll<UICleanupComponent>()
                .WithNone<LocalToWorld>()
                .Build();

            using var cleanupEntities = cleanupQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in cleanupEntities)
            {
                var cleanupComp = EntityManager.GetComponentObject<UICleanupComponent>(entity);
                if (cleanupComp.View != null)
                {
                    UnityEngine.Object.Destroy(cleanupComp.View.gameObject);
                }

                ecb.RemoveComponent<UICleanupComponent>(entity);
            }

            var configEntity = _uiConfigQuery.GetSingletonEntity();
            var uiConfig = EntityManager.GetComponentObject<UIConfigComponent>(configEntity);

            var canvasEntity = _canvasQuery.GetSingletonEntity();
            var canvas = EntityManager.GetComponentObject<UICanvasComponent>(canvasEntity).CanvasReference;

            foreach (var (nameComp, entity) in SystemAPI
                         .Query<RefRO<PlayerNameComponent>>()
                         .WithNone<UICleanupComponent>()
                         .WithEntityAccess())
            {
                var uiObj = UnityEngine.Object.Instantiate(uiConfig.NamePrefab, canvas.transform);
                var uiScript = uiObj.GetComponent<PlayerNameView>();

                uiScript.SetName(nameComp.ValueRO.PlayerName.ToString());

                ecb.AddComponent(entity, new UICleanupComponent { View = uiScript });
            }

            foreach (var (transform, entity) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<UICleanupComponent>()
                         .WithEntityAccess())
            {
                var uiRef = EntityManager.GetComponentObject<UICleanupComponent>(entity);
                if (uiRef.View != null)
                {
                    uiRef.View.UpdatePosition(transform.ValueRO.Position);
                }
            }

            foreach (var (transform, entity) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<GhostOwnerIsLocal>()
                         .WithEntityAccess())
            {
                Camera.main.transform.position = transform.ValueRO.Position + new float3(0, 13f, -3f);
            }
        }
    }
}