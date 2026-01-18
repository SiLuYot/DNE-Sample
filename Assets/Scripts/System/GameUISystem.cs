using Component.Player;
using Component.UI;
using RPC;
using UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Services.Authentication;
using Unity.Transforms;
using UnityEngine;

namespace System
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class GameUISystem : SystemBase
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

            var configEntity = _uiConfigQuery.GetSingletonEntity();
            var uiConfig = EntityManager.GetComponentObject<UIConfigComponent>(configEntity);

            var canvasEntity = _canvasQuery.GetSingletonEntity();
            var canvas = EntityManager.GetComponentObject<UICanvasComponent>(canvasEntity).CanvasReference;

            CleanupPlayerNameUI(ecb);
            CleanupUpgradeUI(ecb);

            if (_connectedQuery.IsEmpty)
            {
                CleanupDeathUI(ecb);
                return;
            }

            UpdatePlayerNameUI(ecb, uiConfig, canvas);
            UpdateUpgradeUI(ecb, uiConfig, canvas);
            UpdateDeathUI(ecb, uiConfig, canvas);
            UpdateCamera();
        }

        private void CleanupPlayerNameUI(EntityCommandBuffer ecb)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<UICleanupComponent>()
                .WithNone<LocalToWorld>()
                .Build();

            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var cleanup = EntityManager.GetComponentObject<UICleanupComponent>(entity);
                if (cleanup.View != null)
                    UnityEngine.Object.Destroy(cleanup.View.gameObject);

                ecb.RemoveComponent<UICleanupComponent>(entity);
            }
        }

        private void CleanupUpgradeUI(EntityCommandBuffer ecb)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<UIAttackUpgradeCleanupComponent>()
                .WithNone<PendingAttackUpgradeComponent>()
                .Build();

            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var cleanup = EntityManager.GetComponentObject<UIAttackUpgradeCleanupComponent>(entity);
                if (cleanup.View != null)
                    UnityEngine.Object.Destroy(cleanup.View.gameObject);

                ecb.RemoveComponent<UIAttackUpgradeCleanupComponent>(entity);
            }
        }

        private void CleanupDeathUI(EntityCommandBuffer ecb)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<UIDeathCleanupComponent>()
                .Build();

            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var cleanup = EntityManager.GetComponentObject<UIDeathCleanupComponent>(entity);
                if (cleanup.View != null)
                    UnityEngine.Object.Destroy(cleanup.View.gameObject);

                ecb.RemoveComponent<UIDeathCleanupComponent>(entity);
            }
        }

        private void UpdatePlayerNameUI(EntityCommandBuffer ecb, UIConfigComponent uiConfig, Canvas canvas)
        {
            foreach (var (nameComp, expComp, entity) in SystemAPI
                         .Query<RefRO<PlayerNameComponent>, RefRO<PlayerExperienceComponent>>()
                         .WithNone<UICleanupComponent>()
                         .WithEntityAccess())
            {
                var uiObj = UnityEngine.Object.Instantiate(uiConfig.NamePrefab, canvas.transform);
                var view = uiObj.GetComponent<PlayerNameView>();

                view.SetName(nameComp.ValueRO.PlayerName.ToString());
                view.SetLevel(expComp.ValueRO.Level);

                ecb.AddComponent(entity, new UICleanupComponent { View = view });
            }

            foreach (var (transform, expComp, entity) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<PlayerExperienceComponent>>()
                         .WithAll<UICleanupComponent>()
                         .WithEntityAccess())
            {
                var cleanup = EntityManager.GetComponentObject<UICleanupComponent>(entity);
                if (cleanup.View != null)
                {
                    cleanup.View.UpdatePosition(transform.ValueRO.Position);
                    cleanup.View.SetLevel(expComp.ValueRO.Level);
                }
            }
        }

        private void UpdateUpgradeUI(EntityCommandBuffer ecb, UIConfigComponent uiConfig, Canvas canvas)
        {
            if (uiConfig.UpgradeViewPrefab == null)
                return;

            foreach (var (pending, projectile, missile, sword, entity) in SystemAPI
                         .Query<RefRO<PendingAttackUpgradeComponent>, RefRO<PlayerProjectileAttackComponent>,
                                RefRO<PlayerMissileAttackComponent>, RefRO<PlayerSwordAttackComponent>>()
                         .WithAll<GhostOwnerIsLocal>()
                         .WithNone<UIAttackUpgradeCleanupComponent>()
                         .WithEntityAccess())
            {
                var uiObj = UnityEngine.Object.Instantiate(uiConfig.UpgradeViewPrefab, canvas.transform);
                var view = uiObj.GetComponent<AttackUpgradeView>();

                if (view == null)
                {
                    UnityEngine.Object.Destroy(uiObj);
                    continue;
                }

                view.Show(projectile.ValueRO.AttackLevel, missile.ValueRO.AttackLevel, sword.ValueRO.AttackLevel);
                ecb.AddComponent(entity, new UIAttackUpgradeCleanupComponent { View = view });
            }

            foreach (var (pending, expComp, entity) in SystemAPI
                         .Query<RefRO<PendingAttackUpgradeComponent>, RefRO<PlayerExperienceComponent>>()
                         .WithAll<GhostOwnerIsLocal, UIAttackUpgradeCleanupComponent>()
                         .WithEntityAccess())
            {
                var cleanup = EntityManager.GetComponentObject<UIAttackUpgradeCleanupComponent>(entity);
                if (cleanup.View == null)
                    continue;

                if (expComp.ValueRO.LastUpgradedLevel >= pending.ValueRO.LastUpgradedLevel)
                {
                    ecb.RemoveComponent<PendingAttackUpgradeComponent>(entity);
                    continue;
                }

                if (cleanup.View.PendingSelection.HasValue)
                {
                    var rpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(rpcEntity, new AttackUpgradeRequest
                    {
                        UpgradeType = cleanup.View.PendingSelection.Value,
                        TargetLevel = pending.ValueRO.LastUpgradedLevel
                    });
                    ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

                    cleanup.View.PendingSelection = null;
                }
            }
        }

        private void UpdateDeathUI(EntityCommandBuffer ecb, UIConfigComponent uiConfig, Canvas canvas)
        {
            foreach (var (reqSrc, reqData, reqEntity) in SystemAPI
                         .Query<RefRO<ReceiveRpcCommandRequest>, RefRO<PlayerDeathRequest>>()
                         .WithEntityAccess())
            {
                var connectionEntity = reqSrc.ValueRO.SourceConnection;

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
                var view = uiObj.GetComponent<DeathView>();

                if (view == null)
                {
                    UnityEngine.Object.Destroy(uiObj);
                    ecb.DestroyEntity(reqEntity);
                    continue;
                }

                view.Show();
                ecb.AddComponent(connectionEntity, new UIDeathCleanupComponent { View = view });
                ecb.DestroyEntity(reqEntity);
            }

            foreach (var (networkId, entity) in SystemAPI
                         .Query<RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame, UIDeathCleanupComponent>()
                         .WithEntityAccess())
            {
                var cleanup = EntityManager.GetComponentObject<UIDeathCleanupComponent>(entity);
                if (cleanup.View == null)
                    continue;

                if (cleanup.View.RequestRespawn)
                {
                    var rpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(rpcEntity, new PlayerRespawnRequest
                    {
                        PlayerName = AuthenticationService.Instance.PlayerName
                    });
                    ecb.AddComponent(rpcEntity, new SendRpcCommandRequest { TargetConnection = entity });

                    CleanupDeathUI(ecb);
                }
            }
        }

        private void UpdateCamera()
        {
            foreach (var transform in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<GhostOwnerIsLocal>())
            {
                Camera.main.transform.position = transform.ValueRO.Position + new float3(0, 13f, -3f);
            }
        }
    }
}
