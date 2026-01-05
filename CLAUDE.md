# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**DNE-Sample** is a multiplayer top-down shooter built with Unity DOTS (Data-Oriented Technology Stack). It demonstrates modern Unity ECS architecture with client-server netcode, showcasing homing missiles, enemy AI, and client-side prediction.

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
├── Authoring/          # GameObject → Entity conversion (bakers)
├── Component/          # ECS components (pure data)
├── System/             # ECS systems (game logic)
│   └── Job/           # Parallel processing jobs
├── RPC/               # Remote Procedure Calls
└── UI/                # MonoBehaviour UI scripts
```

### Key Architectural Patterns

**1. ECS Component Separation:**
- **Authoring**: Design-time components that bake to entities (e.g., `PlayerAuthoring.cs`)
- **Component**: Runtime ECS data structs (e.g., `PlayerComponent.cs`)
- **System**: Game logic that processes components (e.g., `PlayerMovementSystem.cs`)

**2. Namespace Convention:**
```csharp
namespace Component.Player { }  // Components
namespace System { }            // Systems
namespace Authoring.Player { }  // Authoring
namespace RPC { }              // RPCs
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
// Example from ProjectileMovementJob.cs
[BurstCompile]
public partial struct ProjectileMovementJob : IJobEntity
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
4. **Ghost Replication**: Players/enemies are ghosts synchronized across clients

**Connection Flow:**
```
Client → GoInGameClientSystem creates RPC
       → Server GoInGameServerSystem receives RPC
       → Server spawns player entity
       → Server sets GhostOwner component
       → Player ghost replicates to all clients
```

### Scene Structure

- **Main Scene**: `Assets/Scenes/SampleScene.unity` (contains UI, camera, lighting)
- **SubScenes** (DOTS entities, baked at build time):
  - `PlayerSubScene.unity` (player spawner, projectile/missile prefabs)
  - `EnemySubScene.unity` (enemy spawner, spawn points)

SubScenes are loaded/unloaded at runtime and contain pure ECS entities.

## Common Development Patterns

### Adding a New Component

1. Create component struct in `Assets/Scripts/Component/`:
```csharp
using Unity.Entities;

public struct MyComponent : IComponentData
{
    public float Speed;
}
```

2. Create authoring script in `Assets/Scripts/Authoring/`:
```csharp
using Unity.Entities;
using UnityEngine;

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
```

3. Add authoring component to GameObject in Unity Editor

### Adding a New System

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace System
{
    // Choose appropriate update group and world filter
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [BurstCompile]
    public partial struct MySystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (myComp, trans) in SystemAPI
                .Query<RefRW<MyComponent>, RefRW<LocalTransform>>()
                .WithAll<Simulate>())  // Required for predicted entities
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

### Working with Jobs

For performance-critical code, extract logic to jobs:

```csharp
// In System file
public void OnUpdate(ref SystemState state)
{
    new MyJob
    {
        DeltaTime = SystemAPI.Time.DeltaTime
    }.ScheduleParallel();
}

[BurstCompile]
public partial struct MyJob : IJobEntity
{
    public float DeltaTime;

    public void Execute(RefRW<LocalTransform> trans, in MyComponent comp)
    {
        // Parallel execution per entity
    }
}
```

## Gameplay Systems Reference

### Player Systems
- **PlayerInputSystem** - Captures WASD/arrow keys
- **PlayerMovementSystem** - Predicted movement with boundary constraints (-15 to 15 units)
- **PlayerProjectileAttackServerSystem** - Spawns 3 projectiles at 10° spread
- **PlayerMissileAttackServerSystem** - Spawns homing missiles

### Enemy Systems
- **EnemySpawnServerSystem** - Spawns enemies every 3 seconds
- **EnemyChaseServerSystem** - Enemies chase nearest player at 4 units/sec

### Combat Systems
- **ProjectileMovementSystem** - Linear projectile movement
- **ProjectileCollisionSystem** - Collision detection with enemies
- **HomingMissileMovementSystem** - Two-phase missile behavior:
  - Launch phase: Arcs upward to launch height
  - Tracking phase: Homes in on nearest enemy
- **HomingMissileCollisionSystem** - Missile collision and destruction

### Utility Systems
- **PhysicsConstraintSystem** - Locks all physics to Y=0 (2D plane constraint)
- **GameUISystem** - Player name tags and camera following

## Important Code Locations

- **Player Input**: `Assets/Scripts/Component/Player/PlayerInputComponent.cs` (implements `IInputComponentData`)
- **RPC Definition**: `Assets/Scripts/RPC/GoInGameRequest.cs` (client join request)
- **Connection Handling**:
  - `Assets/Scripts/System/GoInGameClientSystem.cs` (client-side)
  - `Assets/Scripts/System/GoInGameServerSystem.cs` (server-side)
- **Rendering Settings**: `Assets/Settings/` (separate PC/Mobile URP assets)
- **Input Actions**: `Assets/InputSystem_Actions.inputactions`

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

### Build Errors
- DOTS requires Burst compilation - check for Burst errors in Console
- Ensure all managed references are removed from ECS components (use `Entity` instead of `GameObject`)
- SubScenes must be closed before building

### Performance Issues
- Move hot-path code to `IJobEntity` for parallel execution
- Use `[BurstCompile]` on systems and jobs
- Check `SystemAPI.Query<>` uses `RefRO<>` for read-only, `RefRW<>` for write access
