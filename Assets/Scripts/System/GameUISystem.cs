using Component.Player;
using Component.UI;
using RPC;
using UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace System
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
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

            var upgradeCleanupQuery = SystemAPI.QueryBuilder()
                .WithAll<UIAttackUpgradeCleanupComponent>()
                .WithNone<PendingAttackUpgradeComponent>()
                .Build();

            using var upgradeCleanupEntities = upgradeCleanupQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in upgradeCleanupEntities)
            {
                var cleanupComp = EntityManager.GetComponentObject<UIAttackUpgradeCleanupComponent>(entity);
                if (cleanupComp.View != null)
                {
                    UnityEngine.Object.Destroy(cleanupComp.View.gameObject);
                }

                ecb.RemoveComponent<UIAttackUpgradeCleanupComponent>(entity);
            }

            var configEntity = _uiConfigQuery.GetSingletonEntity();
            var uiConfig = EntityManager.GetComponentObject<UIConfigComponent>(configEntity);

            var canvasEntity = _canvasQuery.GetSingletonEntity();
            var canvas = EntityManager.GetComponentObject<UICanvasComponent>(canvasEntity).CanvasReference;

            foreach (var (nameComp, expComp, entity) in SystemAPI
                         .Query<RefRO<PlayerNameComponent>, RefRO<PlayerExperienceComponent>>()
                         .WithNone<UICleanupComponent>()
                         .WithEntityAccess())
            {
                var uiObj = UnityEngine.Object.Instantiate(uiConfig.NamePrefab, canvas.transform);
                var uiScript = uiObj.GetComponent<PlayerNameView>();

                uiScript.SetName(nameComp.ValueRO.PlayerName.ToString());
                uiScript.SetLevel(expComp.ValueRO.Level);

                ecb.AddComponent(entity, new UICleanupComponent { View = uiScript });
            }

            foreach (var (pending, projectileAttack, missileAttack, swordAttack, entity) in SystemAPI
                         .Query<RefRO<PendingAttackUpgradeComponent>, RefRO<PlayerProjectileAttackComponent>, RefRO<PlayerMissileAttackComponent>, RefRO<PlayerSwordAttackComponent>>()
                         .WithAll<GhostOwnerIsLocal>()
                         .WithNone<UIAttackUpgradeCleanupComponent>()
                         .WithEntityAccess())
            {
                if (uiConfig.UpgradeViewPrefab == null)
                    continue;

                var uiObj = UnityEngine.Object.Instantiate(uiConfig.UpgradeViewPrefab, canvas.transform);
                var uiScript = uiObj.GetComponent<AttackUpgradeView>();

                if (uiScript == null)
                {
                    UnityEngine.Object.Destroy(uiObj);
                    continue;
                }

                uiScript.Show(projectileAttack.ValueRO.AttackLevel, missileAttack.ValueRO.AttackLevel, swordAttack.ValueRO.AttackLevel);
                ecb.AddComponent(entity, new UIAttackUpgradeCleanupComponent { View = uiScript });
            }

            foreach (var (pending, projectileAttack, missileAttack, expComp, entity) in SystemAPI
                         .Query<RefRO<PendingAttackUpgradeComponent>, RefRO<PlayerProjectileAttackComponent>, RefRO<PlayerMissileAttackComponent>, RefRO<PlayerExperienceComponent>>()
                         .WithAll<GhostOwnerIsLocal, UIAttackUpgradeCleanupComponent>()
                         .WithEntityAccess())
            {
                var upgradeCleanup = EntityManager.GetComponentObject<UIAttackUpgradeCleanupComponent>(entity);
                if (upgradeCleanup.View == null)
                    continue;

                if (expComp.ValueRO.LastUpgradedLevel >= pending.ValueRO.LastUpgradedLevel)
                {
                    ecb.RemoveComponent<PendingAttackUpgradeComponent>(entity);
                    continue;
                }

                if (upgradeCleanup.View.PendingSelection.HasValue)
                {
                    var rpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(rpcEntity, new AttackUpgradeRequest
                    {
                        UpgradeType = upgradeCleanup.View.PendingSelection.Value,
                        TargetLevel = pending.ValueRO.LastUpgradedLevel
                    });
                    ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

                    upgradeCleanup.View.PendingSelection = null;
                }
            }

            foreach (var (transform, expComp, entity) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<PlayerExperienceComponent>>()
                         .WithAll<UICleanupComponent>()
                         .WithEntityAccess())
            {
                var uiRef = EntityManager.GetComponentObject<UICleanupComponent>(entity);
                if (uiRef.View != null)
                {
                    uiRef.View.UpdatePosition(transform.ValueRO.Position);
                    uiRef.View.SetLevel(expComp.ValueRO.Level);
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