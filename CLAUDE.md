# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**DNE-Sample** is a multiplayer top-down shooter built with Unity DOTS (Data-Oriented Technology Stack). It demonstrates modern Unity ECS architecture with client-server netcode, featuring experience/leveling systems, multiple attack types, and client-side prediction.

**Tech Stack:**
- Unity 6000.2.13f1
- Unity Entities (ECS) 1.4.3
- Unity NetCode for Entities 1.9.3
- Unity Physics 1.4.3
- Unity Entities Graphics 1.4.16
- Universal Render Pipeline (URP) 17.2.0
- Unity Input System 1.14.2

## Build & Development Commands

### Opening the Project
Open `DNE-Sample.sln` in your IDE or open the folder in Unity Hub with Unity 6000.2.13f1.

### Building
Unity builds the game through the Editor:
- **Build Location**: `Build/` directory
- **Output Executable**: `Dots-Netcode-Entities-Sample.exe`
- Build through Unity Editor: `File → Build Settings → Build`

### Testing Multiplayer
This project uses **Unity Multiplayer Playmode** for testing:
- No need to build separate client/server executables during development
- Press Play in Unity Editor to launch with multiple virtual players
- The system automatically creates 3 worlds:
  - **ServerSimulation** (authoritative server)
  - **ClientSimulation** (local client)
  - **ThinClientSimulation** (optional test clients)

### Running Tests
Unity tests can be run through the Test Runner:
- Open via `Window → General → Test Runner`

## Architecture & Code Organization

### Directory Structure

```
Assets/Scripts/
├── Authoring/              # GameObject → Entity conversion (bakers)
│   ├── Enemy/             # EnemyAuthoring, EnemySpawnerAuthoring, EnemySpawnPointAuthoring
│   ├── Experience/        # ExperienceOrbAuthoring, ExperienceOrbSpawnerAuthoring
│   ├── HomingMissile/     # HomingMissileAuthoring
│   ├── Player/            # PlayerAuthoring, PlayerInputAuthoring, spawner authorings
│   ├── Projectile/        # ProjectileAuthoring
│   └── Sword/             # SwordAuthoring
├── Component/              # ECS components (pure data)
│   ├── Enemy/             # EnemyComponent, EnemyDeadTag, EnemyKnockbackComponent
│   ├── Experience/        # ExperienceOrbComponent, ExperienceOrbSpawnerComponent
│   ├── HomingMissile/     # HomingMissileComponent, HomingMissileSpawnerComponent
│   ├── Player/            # PlayerComponent, PlayerInputComponent, attack components
│   ├── Projectile/        # ProjectileComponent, ProjectileSpawnerComponent
│   ├── Sword/             # SwordComponent, SwordOwnerComponent, SwordSpawnerComponent
│   └── UI/                # UI-related components
├── System/                 # ECS systems (game logic)
│   ├── Enemy/             # Spawn, chase, knockback, death systems
│   ├── Experience/        # Collection, movement systems
│   ├── GoInGame/          # Client/server connection systems
│   ├── HomingMissile/     # Attack, movement, collision systems
│   ├── Player/            # Input, movement, death, respawn, level systems
│   ├── Projectile/        # Attack, movement, collision systems
│   └── Sword/             # Attack, movement, trigger systems
├── RPC/                    # Remote Procedure Calls
├── Type/                   # Enums and type definitions
└── UI/                     # MonoBehaviour UI scripts
```

### Key Architectural Patterns

**1. ECS Component Separation:**
- **Authoring**: Design-time components that bake to entities (e.g., `PlayerAuthoring.cs`)
- **Component**: Runtime ECS data structs (e.g., `PlayerComponent.cs`)
- **System**: Game logic that processes components (e.g., `PlayerMovementSystem.cs`)

**2. Namespace Convention:**
```csharp
namespace Component.Player { }     // Components
namespace System.Player { }        // Systems (categorized by feature)
namespace Authoring.Player { }     // Authoring
namespace RPC { }                  // RPCs
namespace Type { }                 // Enums and types
```

**3. Burst Compilation:**
All systems and jobs must be Burst-compatible:
```csharp
[BurstCompile]
public partial struct PlayerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state) { }
}
```

**4. Parallel Processing:**
Performance-critical logic uses `IJobEntity`:
```csharp
[BurstCompile]
public partial struct ProjectileMovementServerJob : IJobEntity
{
    public float DeltaTime;
    public void Execute(RefRW<LocalTransform> trans, in ProjectileComponent projectile) { }
}
```

### NetCode Architecture

**Client-Server with Client-Side Prediction:**

1. **Input System**: `PlayerInputComponent` implements `IInputComponentData` for replicated input
2. **Prediction**: Systems in `PredictedSimulationSystemGroup` run on both client and server
3. **Authority**: Combat resolution is server-authoritative (systems with `[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]`)
4. **Ghost Replication**: Players/enemies/experience orbs are ghosts synchronized across clients

**Connection Flow:**
```
Client → GoInGameClientSystem creates RPC
       → Server GoInGameServerSystem receives RPC
       → Server spawns player entity with attack components
       → Server sets GhostOwner component
       → Player ghost replicates to all clients
```

### Scene Structure

- **Main Scene**: `Assets/Scenes/SampleScene.unity` (contains UI, camera, lighting)
- **SubScenes** (DOTS entities, baked at build time):
  - `PlayerSubScene.unity` (player spawner, projectile/missile/sword prefabs)
  - `EnemySubScene.unity` (enemy spawner, spawn points, experience orb spawner)

SubScenes are loaded/unloaded at runtime and contain pure ECS entities.

## Common Development Patterns

### Adding a New Component

1. Create component struct in `Assets/Scripts/Component/{Category}/`:
```csharp
using Unity.Entities;
using Unity.NetCode;

namespace Component.MyFeature
{
    [GhostComponent(PrefabType = GhostPrefabType.All)]  // If needs replication
    public struct MyComponent : IComponentData
    {
        [GhostField] public float Speed;  // Replicated field
        public float LocalOnlyData;       // Not replicated
    }
}
```

2. Create authoring script in `Assets/Scripts/Authoring/{Category}/`:
```csharp
using Unity.Entities;
using UnityEngine;

namespace Authoring.MyFeature
{
    public class MyAuthoring : MonoBehaviour
    {
        public float Speed = 5f;

        class Baker : Baker<MyAuthoring>
        {
            public override void Bake(MyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MyComponent { Speed = authoring.Speed });
            }
        }
    }
}
```

3. Add authoring component to GameObject in Unity Editor

### Adding a New System

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace System.MyFeature
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]  // Server only
    [BurstCompile]
    public partial struct MyServerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<MyComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (myComp, trans, entity) in SystemAPI
                .Query<RefRW<MyComponent>, RefRW<LocalTransform>>()
                .WithEntityAccess())
            {
                // System logic here
            }
        }
    }
}
```

### System Update Groups (NetCode)

- `GhostInputSystemGroup` - Capture and send player inputs
- `PredictedSimulationSystemGroup` - Runs on both client (predicted) and server
- `SimulationSystemGroup` - Standard Unity ECS update
- Use `WithAll<Simulate>()` in queries for predicted entities

### World Filtering

```csharp
// Server only
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]

// Client only
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]

// Both client and server (predicted)
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
```

### Adding a New RPC

```csharp
using Unity.NetCode;

namespace RPC
{
    public struct MyRequest : IRpcCommand
    {
        public int SomeData;
    }
}
```

Processing RPC on server:
```csharp
foreach (var (reqSrc, reqData, reqEntity) in SystemAPI
    .Query<RefRO<ReceiveRpcCommandRequest>, RefRO<MyRequest>>()
    .WithEntityAccess())
{
    var networkId = SystemAPI.GetComponent<NetworkId>(reqSrc.ValueRO.SourceConnection);
    // Process request...
    ecb.DestroyEntity(reqEntity);
}
```

## Gameplay Systems Reference

### Player Systems
- **PlayerInputSystem** - Captures WASD/arrow keys and aim direction
- **PlayerMovementSystem** - Predicted movement with boundary constraints (-15 to 15 units), speed 6 units/sec
- **PlayerLevelClientSystem** - Detects level milestones (every 5 levels) and triggers upgrade UI
- **PlayerDeathServerSystem** - Handles player death, sends RPC to client
- **PlayerRespawnServerSystem** - Processes respawn requests
- **PlayerEnemyTriggerServerSystem** - Detects collision between player and enemies

### Enemy Systems
- **EnemySpawnServerSystem** - Spawns enemies periodically from spawn points
- **EnemyChaseServerSystem** - Enemies chase nearest player
- **EnemyKnockbackServerSystem** - Applies knockback when enemies are hit
- **EnemyDeathServerSystem** - Handles enemy death and spawns experience orbs

### Attack Systems
- **ProjectileAttackServerSystem** - Spawns multiple projectiles in spread pattern
- **ProjectileMovementServerSystem** - Linear projectile movement
- **ProjectileCollisionServerSystem** - Projectile-enemy collision detection
- **HomingMissileAttackServerSystem** - Auto-fires homing missiles periodically
- **HomingMissileMovementServerSystem** - Two-phase missile behavior (launch arc → tracking)
- **HomingMissileCollisionServerSystem** - Missile-enemy collision
- **SwordAttackServerSystem** - Spawns rotating sword around player
- **SwordMovementServerSystem** - Sword rotation following player
- **SwordTriggerServerSystem** - Sword-enemy collision with durability

### Experience & Upgrade Systems
- **ExperienceMovementServerSystem** - Experience orbs move toward nearby players
- **ExperienceCollectionServerSystem** - Orb collection and level calculation (100 exp per level)
- **AttackUpgradeServerSystem** - Processes upgrade selection RPC (Projectile/Missile/Sword)

### Utility Systems
- **PhysicsConstraintSystem** - Locks all physics to Y=0 (2D plane constraint)
- **GameUISystem** - Player name tags and camera following

## Important Code Locations

- **Player Input**: `Assets/Scripts/Component/Player/PlayerInputComponent.cs` (implements `IInputComponentData`)
- **Attack Components**:
  - `PlayerProjectileAttackComponent.cs` - Projectile attack level/cooldown
  - `PlayerMissileAttackComponent.cs` - Missile attack level/cooldown
  - `PlayerSwordAttackComponent.cs` - Sword attack level/cooldown
- **Experience**: `PlayerExperienceComponent.cs` - CurrentExperience, Level, LastUpgradedLevel
- **Upgrade Types**: `Assets/Scripts/Type/AttackUpgradeType.cs` (Projectile, Missile, Sword)
- **RPC Definitions**:
  - `GoInGameRequest.cs` - Client join request with player name
  - `AttackUpgradeRequest.cs` - Upgrade selection
  - `PlayerDeathRequest.cs` / `PlayerRespawnRequest.cs` - Death/respawn flow
- **Connection Handling**:
  - `Assets/Scripts/System/GoInGame/GoInGameClientSystem.cs`
  - `Assets/Scripts/System/GoInGame/GoInGameServerSystem.cs`
- **UI Scripts**:
  - `GameView.cs` - Lobby/in-game UI switching
  - `AttackUpgradeView.cs` - Upgrade selection UI
  - `DeathView.cs` - Death/respawn UI
  - `PlayerNameView.cs` - Floating name tags

## Unity Services Integration

The project uses Unity Authentication for player naming:
```csharp
await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
```

Player names are passed via `GoInGameRequest` RPC and displayed above characters in-game.

## Troubleshooting

### Entities Not Appearing
- Check that SubScenes are open and entities are baked (look for `.entities` files)
- Verify authoring components have proper `Baker<T>` implementation
- Ensure entities have required components (e.g., `LocalTransform`, rendering components)

### Netcode Issues
- Verify systems use correct `WorldSystemFilter` for client/server execution
- Check that ghosts have `GhostAuthoringComponent` in the prefab
- Ensure input components implement `IInputComponentData`
- Use `WithAll<Simulate>()` in queries for predicted entities
- Check `GhostField` attributes on components that need replication

### Build Errors
- DOTS requires Burst compilation - check for Burst errors in Console
- Ensure all managed references are removed from ECS components (use `Entity` instead of `GameObject`)
- SubScenes must be closed before building

### Performance Issues
- Move hot-path code to `IJobEntity` for parallel execution
- Use `[BurstCompile]` on systems and jobs
- Check `SystemAPI.Query<>` uses `RefRO<>` for read-only, `RefRW<>` for write access
- Use `EntityCommandBuffer` from `EndSimulationEntityCommandBufferSystem` for deferred structural changes
