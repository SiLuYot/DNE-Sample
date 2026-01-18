# DNE-Sample

Unity DOTS 기반 멀티플레이어 탑다운 슈터 샘플 프로젝트

## 시연 영상
https://github.com/user-attachments/assets/67de6cd2-64ae-4134-8657-880cabfc1712

---

## 개요

Unity ECS(Entity Component System) 아키텍처와 NetCode를 활용한 멀티플레이어 탑다운 슈터입니다. 클라이언트-서버 모델, 클라이언트 사이드 예측, 경험치/레벨 시스템, 다양한 공격 타입을 구현하고 있습니다.

## 기술 스택

| 기술 | 버전 |
|------|------|
| Unity | 6000.2.13f1 |
| Unity Entities (ECS) | 1.4.3 |
| Unity NetCode for Entities | 1.9.3 |
| Unity Physics | 1.4.3 |
| Universal Render Pipeline | 17.2.0 |
| Input System | 1.14.2 |

## 주요 기능

### 멀티플레이어
- **클라이언트-서버 구조**: 권위적 서버 모델
- **클라이언트 사이드 예측**: 지연 보상을 위한 입력 예측 시스템
- **Ghost 복제**: 플레이어, 적, 경험치 오브가 모든 클라이언트에 자동 동기화

### 공격 시스템
- **투사체(Projectile)**: 마우스 방향으로 다수의 탄환 발사
- **호밍 미사일(Missile)**: 자동 발사, 상승 후 가장 가까운 적 추적
- **검(Sword)**: 플레이어 주변을 회전하며 적을 공격

### 성장 시스템
- **경험치 오브**: 적 처치 시 드롭, 근접 시 플레이어에게 이동 후 획득
- **레벨업**: 100 경험치당 1레벨 상승
- **공격 업그레이드**: 5레벨마다 투사체/미사일/검 중 선택하여 강화

### 전투 시스템
- **적 AI**: 가장 가까운 플레이어를 추적
- **넉백**: 적이 공격받으면 밀려남
- **죽음/부활**: 적과 충돌 시 사망, 부활 가능

## 아키텍처

```
Assets/Scripts/
├── Authoring/          # GameObject → Entity 변환 (Baker)
│   ├── Enemy/
│   ├── Experience/
│   ├── HomingMissile/
│   ├── Player/
│   ├── Projectile/
│   └── Sword/
├── Component/          # ECS 컴포넌트 (순수 데이터)
│   ├── Enemy/
│   ├── Experience/
│   ├── HomingMissile/
│   ├── Player/
│   ├── Projectile/
│   ├── Sword/
│   └── UI/
├── System/             # ECS 시스템 (게임 로직)
│   ├── Enemy/
│   ├── Experience/
│   ├── GoInGame/
│   ├── HomingMissile/
│   ├── Player/
│   ├── Projectile/
│   └── Sword/
├── RPC/                # 원격 프로시저 호출
├── Type/               # Enum 정의
└── UI/                 # MonoBehaviour UI 스크립트
```

### ECS 패턴

**Component**: 데이터만 저장하는 구조체
```csharp
public struct PlayerComponent : IComponentData
{
    public float Speed;
}
```

**System**: 컴포넌트를 처리하는 게임 로직
```csharp
[BurstCompile]
public partial struct PlayerMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state) { ... }
}
```

**Authoring**: 에디터에서 설정 가능한 GameObject 컴포넌트
```csharp
public class PlayerAuthoring : MonoBehaviour
{
    class Baker : Baker<PlayerAuthoring> { ... }
}
```

### NetCode 구조

- **입력 동기화**: `IInputComponentData`를 통한 입력 복제
- **예측 시뮬레이션**: `PredictedSimulationSystemGroup`에서 클라이언트/서버 동시 실행
- **권위적 서버**: 전투 판정, 경험치 처리는 서버에서만 처리
- **Ghost 복제**: 플레이어, 적, 경험치 오브가 모든 클라이언트에 자동 동기화

## 시작하기

### 요구사항
- Unity 6000.2.13f1

### 실행 방법
1. Unity Hub에서 프로젝트 열기
2. `Assets/Scenes/SampleScene.unity` 씬 열기
3. Play 버튼으로 실행

에디터에서 Play 시 자동으로 3개의 월드가 생성됩니다:
- **ServerSimulation**: 권위적 서버
- **ClientSimulation**: 로컬 클라이언트

### 조작법
- **이동**: WASD 또는 방향키
- **투사체**: 마우스 방향으로 자동 발사
- **미사일**: 가장 가까운 적을 자동으로 추적  
- **검**: 마우스 방향으로 자동 회전 공격

## 프로젝트 구조

### 씬 구성
- `SampleScene.unity`: 메인 씬 (UI, 카메라, 조명)
- `PlayerSubScene.unity`: 플레이어 스포너, 투사체/미사일/검 프리팹
- `EnemySubScene.unity`: 적 스포너, 스폰 포인트, 경험치 오브 스포너

### 주요 시스템

| 카테고리 | 시스템 | 설명 |
|----------|--------|------|
| **Player** | PlayerMovementSystem | 이동 및 경계 제한 (-15 ~ 15) |
| | PlayerLevelClientSystem | 레벨업 감지 및 업그레이드 UI 트리거 |
| | PlayerDeathServerSystem | 플레이어 사망 처리 |
| **Enemy** | EnemySpawnServerSystem | 적 주기적 스폰 |
| | EnemyChaseServerSystem | 플레이어 추적 |
| | EnemyKnockbackServerSystem | 넉백 처리 |
| **Attack** | ProjectileAttackServerSystem | 투사체 발사 |
| | HomingMissileAttackServerSystem | 호밍 미사일 자동 발사 |
| | SwordAttackServerSystem | 회전 검 생성 |
| **Experience** | ExperienceCollectionServerSystem | 경험치 수집 및 레벨 계산 |
| | ExperienceMovementServerSystem | 경험치 오브 이동 |
| **Upgrade** | AttackUpgradeServerSystem | 공격 업그레이드 처리 |

### RPC 목록

| RPC | 설명 |
|-----|------|
| GoInGameRequest | 클라이언트 접속 요청 (플레이어 이름 포함) |
| AttackUpgradeRequest | 공격 업그레이드 선택 |
| PlayerDeathRequest | 플레이어 사망 알림 |
| PlayerRespawnRequest | 플레이어 부활 요청 |

## 라이선스

MIT License
