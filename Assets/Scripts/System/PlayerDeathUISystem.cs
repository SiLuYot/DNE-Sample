using Component.UI;
using RPC;
using UI;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Services.Authentication;

namespace System
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class PlayerDeathUISystem : SystemBase
    {
        private EntityQuery _uiConfigQuery;
        private EntityQuery _canvasQuery;
        private EntityQuery _connectedQuery;

        protected override void OnCreate()
        {
            _uiConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UIConfigComponent>());
            _canvasQuery = GetEntityQuery(ComponentType.ReadOnly<UICanvasTag>());

            _connectedQuery = SystemAPI.QueryBuilder()
                .WithAny<NetworkStreamConnection>()
                .Build();
            
            RequireForUpdate(_uiConfigQuery);
            RequireForUpdate(_canvasQuery);
        }

        protected override void OnUpdate()
        {
            var ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);

            if (_connectedQuery.IsEmpty)
            {
                CleanupDeathView(ecb);
                return;
            }

            var configEntity = _uiConfigQuery.GetSingletonEntity();
            var uiConfig = EntityManager.GetComponentObject<UIConfigComponent>(configEntity);

            var canvasEntity = _canvasQuery.GetSingletonEntity();
            var canvas = EntityManager.GetComponentObject<UICanvasComponent>(canvasEntity).CanvasReference;

            // 1. 사망 RPC 수신 처리 - Death UI 생성
            foreach (var (reqSrc, reqData, reqEntity) in SystemAPI
                         .Query<RefRO<ReceiveRpcCommandRequest>, RefRO<PlayerDeathRpc>>()
                         .WithEntityAccess())
            {
                var connectionEntity = reqSrc.ValueRO.SourceConnection;

                // 이미 Death UI가 있는지 확인
                if (EntityManager.HasComponent<UIDeathCleanupComponent>(connectionEntity))
                {
                    ecb.DestroyEntity(reqEntity);
                    continue;
                }

                if (uiConfig.DeathViewPrefab == null)
                {
                    ecb.DestroyEntity(reqEntity);
                    continue;
                }

                var uiObj = UnityEngine.Object.Instantiate(uiConfig.DeathViewPrefab, canvas.transform);
                var uiScript = uiObj.GetComponent<DeathView>();

                if (uiScript == null)
                {
                    UnityEngine.Object.Destroy(uiObj);
                    ecb.DestroyEntity(reqEntity);
                    continue;
                }

                uiScript.Show();
                ecb.AddComponent(connectionEntity, new UIDeathCleanupComponent { View = uiScript });
                ecb.DestroyEntity(reqEntity);
            }

            // 2. 리스폰 버튼 클릭 처리
            foreach (var (networkId, entity) in SystemAPI
                         .Query<RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame, UIDeathCleanupComponent>()
                         .WithEntityAccess())
            {
                var deathCleanup = EntityManager.GetComponentObject<UIDeathCleanupComponent>(entity);
                if (deathCleanup.View == null)
                    continue;

                if (deathCleanup.View.RequestRespawn)
                {
                    // RespawnRequest RPC 전송
                    var rpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(rpcEntity,
                        new RespawnRequest { PlayerName = AuthenticationService.Instance.PlayerName });
                    ecb.AddComponent(rpcEntity, new SendRpcCommandRequest { TargetConnection = entity });

                    CleanupDeathView(ecb);
                }
            }
        }

        private void CleanupDeathView(EntityCommandBuffer ecb)
        {
            var cleanupQuery = SystemAPI.QueryBuilder()
                .WithAll<UIDeathCleanupComponent>()
                .Build();

            using var cleanupEntities = cleanupQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in cleanupEntities)
            {
                var cleanupComp = EntityManager.GetComponentObject<UIDeathCleanupComponent>(entity);
                if (cleanupComp.View != null)
                {
                    UnityEngine.Object.Destroy(cleanupComp.View.gameObject);
                }

                ecb.RemoveComponent<UIDeathCleanupComponent>(entity);
            }
        }
    }
}