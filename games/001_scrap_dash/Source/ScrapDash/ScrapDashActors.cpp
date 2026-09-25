#include "ScrapDashActors.h"

#include "Components/BoxComponent.h"
#include "Components/SceneComponent.h"
#include "Components/SphereComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/StaticMeshActor.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "ScrapDashCharacter.h"
#include "ScrapDashGameMode.h"

namespace
{
UStaticMesh* LoadCube()
{
    return LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cube.Cube"));
}

UStaticMesh* LoadSphere()
{
    return LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Sphere.Sphere"));
}
}

AScrapCollectible::AScrapCollectible()
{
    PrimaryActorTick.bCanEverTick = true;

    Trigger = CreateDefaultSubobject<USphereComponent>(TEXT("Trigger"));
    RootComponent = Trigger;
    Trigger->InitSphereRadius(38.0f);
    Trigger->SetCollisionProfileName(TEXT("OverlapAllDynamic"));
    Trigger->OnComponentBeginOverlap.AddDynamic(this, &AScrapCollectible::OnOverlap);

    Visual = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Visual"));
    Visual->SetupAttachment(RootComponent);
    Visual->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    Visual->SetRelativeScale3D(FVector(0.32f));
    Visual->SetStaticMesh(LoadSphere());
}

void AScrapCollectible::BeginPlay()
{
    Super::BeginPlay();
    if (AScrapDashGameMode* Mode = GetWorld()->GetAuthGameMode<AScrapDashGameMode>())
    {
        Mode->RegisterScrap();
    }
}

void AScrapCollectible::Tick(float DeltaSeconds)
{
    Super::Tick(DeltaSeconds);
    AddActorLocalRotation(FRotator(0.0f, 120.0f * DeltaSeconds, 0.0f));
}

void AScrapCollectible::OnOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor,
    UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep,
    const FHitResult& SweepResult)
{
    if (Cast<AScrapDashCharacter>(OtherActor))
    {
        if (AScrapDashGameMode* Mode = GetWorld()->GetAuthGameMode<AScrapDashGameMode>())
        {
            Mode->CollectScrap();
        }
        Destroy();
    }
}

AScrapHazard::AScrapHazard()
{
    PrimaryActorTick.bCanEverTick = false;

    Trigger = CreateDefaultSubobject<UBoxComponent>(TEXT("Trigger"));
    RootComponent = Trigger;
    Trigger->SetBoxExtent(FVector(90.0f, 90.0f, 35.0f));
    Trigger->SetCollisionProfileName(TEXT("OverlapAllDynamic"));
    Trigger->OnComponentBeginOverlap.AddDynamic(this, &AScrapHazard::OnOverlap);

    Visual = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Visual"));
    Visual->SetupAttachment(RootComponent);
    Visual->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    Visual->SetRelativeScale3D(FVector(1.8f, 1.4f, 0.5f));
    Visual->SetStaticMesh(LoadCube());
}

bool AScrapHazard::ResolvePlayerContact(AScrapDashCharacter* Player)
{
    return Player && Player->Die();
}

void AScrapHazard::OnOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor,
    UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep,
    const FHitResult& SweepResult)
{
    ResolvePlayerContact(Cast<AScrapDashCharacter>(OtherActor));
}

AScrapEnemy::AScrapEnemy()
{
    PrimaryActorTick.bCanEverTick = true;

    Trigger = CreateDefaultSubobject<UBoxComponent>(TEXT("Trigger"));
    RootComponent = Trigger;
    Trigger->SetBoxExtent(FVector(42.0f, 65.0f, 55.0f));
    Trigger->SetCollisionProfileName(TEXT("OverlapAllDynamic"));
    Trigger->OnComponentBeginOverlap.AddDynamic(this, &AScrapEnemy::OnOverlap);

    Visual = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Visual"));
    Visual->SetupAttachment(RootComponent);
    Visual->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    Visual->SetRelativeScale3D(FVector(0.65f, 0.45f, 0.9f));
    Visual->SetStaticMesh(LoadCube());
}

void AScrapEnemy::BeginPlay()
{
    Super::BeginPlay();
    PatrolOrigin = GetActorLocation();
}

void AScrapEnemy::Tick(float DeltaSeconds)
{
    Super::Tick(DeltaSeconds);

    FVector Location = GetActorLocation();
    Location.X += PatrolDirection * PatrolSpeed * DeltaSeconds;
    const float Offset = Location.X - PatrolOrigin.X;
    if (FMath::Abs(Offset) >= PatrolDistance)
    {
        PatrolDirection *= -1.0f;
        Location.X = PatrolOrigin.X + FMath::Clamp(Offset, -PatrolDistance, PatrolDistance);
    }
    SetActorLocation(Location);
}

bool AScrapEnemy::ResolvePlayerContact(AScrapDashCharacter* Player)
{
    if (!Player)
    {
        return false;
    }

    if (Player->IsDashAttacking())
    {
        Destroy();
        return true;
    }

    Player->Die();
    return false;
}

void AScrapEnemy::OnOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor,
    UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep,
    const FHitResult& SweepResult)
{
    ResolvePlayerContact(Cast<AScrapDashCharacter>(OtherActor));
}

AScrapMovingPlatform::AScrapMovingPlatform()
{
    PrimaryActorTick.bCanEverTick = true;

    SceneRoot = CreateDefaultSubobject<USceneComponent>(TEXT("Root"));
    RootComponent = SceneRoot;

    PlatformMesh = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Platform"));
    PlatformMesh->SetupAttachment(SceneRoot);
    PlatformMesh->SetStaticMesh(LoadCube());
    PlatformMesh->SetCollisionProfileName(TEXT("BlockAll"));
    PlatformMesh->SetMobility(EComponentMobility::Movable);
    PlatformMesh->SetRelativeScale3D(FVector(2.2f, 1.0f, 0.28f));

    RiderTrigger = CreateDefaultSubobject<UBoxComponent>(TEXT("RiderTrigger"));
    RiderTrigger->SetupAttachment(SceneRoot);
    RiderTrigger->SetBoxExtent(FVector(220.0f, 90.0f, 70.0f));
    RiderTrigger->SetRelativeLocation(FVector(0.0f, 0.0f, 72.0f));
    RiderTrigger->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
    RiderTrigger->SetCollisionResponseToAllChannels(ECR_Ignore);
    RiderTrigger->SetCollisionResponseToChannel(ECC_Pawn, ECR_Overlap);
    RiderTrigger->OnComponentBeginOverlap.AddDynamic(this, &AScrapMovingPlatform::OnRiderEnter);
    RiderTrigger->OnComponentEndOverlap.AddDynamic(this, &AScrapMovingPlatform::OnRiderExit);
}

void AScrapMovingPlatform::BeginPlay()
{
    Super::BeginPlay();
    Origin = GetActorLocation();
}

void AScrapMovingPlatform::Tick(float DeltaSeconds)
{
    Super::Tick(DeltaSeconds);

    RuntimeSeconds += DeltaSeconds;
    FVector Location = Origin;
    Location.X += FMath::Sin(RuntimeSeconds * TravelSpeed) * TravelDistance;
    SetActorLocation(Location, true);
}

void AScrapMovingPlatform::AttachRider(AScrapDashCharacter* Player)
{
    if (Player)
    {
        Player->SetBase(PlatformMesh);
    }
}

void AScrapMovingPlatform::DetachRider(AScrapDashCharacter* Player)
{
    if (Player && Player->GetMovementBase() == PlatformMesh)
    {
        Player->SetBase(nullptr);
    }
}

bool AScrapMovingPlatform::IsCarrying(const AScrapDashCharacter* Player) const
{
    return Player && Player->GetMovementBase() == PlatformMesh;
}

void AScrapMovingPlatform::OnRiderEnter(UPrimitiveComponent* OverlappedComponent,
    AActor* OtherActor, UPrimitiveComponent* OtherComp, int32 OtherBodyIndex,
    bool bFromSweep, const FHitResult& SweepResult)
{
    AttachRider(Cast<AScrapDashCharacter>(OtherActor));
}

void AScrapMovingPlatform::OnRiderExit(UPrimitiveComponent* OverlappedComponent,
    AActor* OtherActor, UPrimitiveComponent* OtherComp, int32 OtherBodyIndex)
{
    DetachRider(Cast<AScrapDashCharacter>(OtherActor));
}

AScrapMagnetZone::AScrapMagnetZone()
{
    PrimaryActorTick.bCanEverTick = true;
    PrimaryActorTick.TickGroup = TG_PrePhysics;

    Trigger = CreateDefaultSubobject<UBoxComponent>(TEXT("Trigger"));
    RootComponent = Trigger;
    Trigger->SetBoxExtent(FVector(130.0f, 90.0f, 170.0f));
    Trigger->SetCollisionProfileName(TEXT("OverlapAllDynamic"));
    Trigger->OnComponentBeginOverlap.AddDynamic(this, &AScrapMagnetZone::OnOverlap);
    Trigger->OnComponentEndOverlap.AddDynamic(this, &AScrapMagnetZone::OnEndOverlap);

    Visual = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Visual"));
    Visual->SetupAttachment(RootComponent);
    Visual->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    Visual->SetRelativeScale3D(FVector(2.4f, 1.4f, 3.2f));
    Visual->SetStaticMesh(LoadCube());
}

void AScrapMagnetZone::EngagePlayer(AScrapDashCharacter* Player)
{
    ActivePlayer = Player;
}

void AScrapMagnetZone::Tick(float DeltaSeconds)
{
    Super::Tick(DeltaSeconds);

    AScrapDashCharacter* Player = ActivePlayer.Get();
    if (!Player)
    {
        return;
    }

    UCharacterMovementComponent* Movement = Player->GetCharacterMovement();
    FVector Velocity = Movement->Velocity;
    const float HorizontalError = GetActorLocation().X - Player->GetActorLocation().X;
    const float TargetHorizontalSpeed = FMath::Clamp(
        HorizontalError * CenteringStrength, -MaxCenteringSpeed, MaxCenteringSpeed);

    Velocity.X = FMath::FInterpTo(
        Velocity.X, TargetHorizontalSpeed, DeltaSeconds, CenteringStrength);
    Velocity.Y = 0.0f;
    Velocity.Z = FMath::Max(Velocity.Z, LiftVelocity);
    Movement->Velocity = Velocity;
}

void AScrapMagnetZone::OnOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor,
    UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep,
    const FHitResult& SweepResult)
{
    if (AScrapDashCharacter* Player = Cast<AScrapDashCharacter>(OtherActor))
    {
        EngagePlayer(Player);
    }
}

void AScrapMagnetZone::OnEndOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor,
    UPrimitiveComponent* OtherComp, int32 OtherBodyIndex)
{
    if (OtherActor == ActivePlayer.Get())
    {
        ActivePlayer.Reset();
    }
}

AScrapCheckpoint::AScrapCheckpoint()
{
    PrimaryActorTick.bCanEverTick = false;

    Trigger = CreateDefaultSubobject<UBoxComponent>(TEXT("Trigger"));
    RootComponent = Trigger;
    Trigger->SetBoxExtent(FVector(70.0f, 90.0f, 110.0f));
    Trigger->SetCollisionProfileName(TEXT("OverlapAllDynamic"));
    Trigger->OnComponentBeginOverlap.AddDynamic(this, &AScrapCheckpoint::OnOverlap);

    Visual = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Visual"));
    Visual->SetupAttachment(RootComponent);
    Visual->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    Visual->SetRelativeScale3D(FVector(0.24f, 0.24f, 2.2f));
    Visual->SetStaticMesh(LoadCube());
}

void AScrapCheckpoint::OnOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor,
    UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep,
    const FHitResult& SweepResult)
{
    if (Cast<AScrapDashCharacter>(OtherActor))
    {
        if (AScrapDashGameMode* Mode = GetWorld()->GetAuthGameMode<AScrapDashGameMode>())
        {
            Mode->SetCheckpoint(GetActorLocation() + FVector(0.0f, 0.0f, 120.0f));
        }
    }
}

AScrapFinishGate::AScrapFinishGate()
{
    PrimaryActorTick.bCanEverTick = false;

    Trigger = CreateDefaultSubobject<UBoxComponent>(TEXT("Trigger"));
    RootComponent = Trigger;
    Trigger->SetBoxExtent(FVector(90.0f, 100.0f, 170.0f));
    Trigger->SetCollisionProfileName(TEXT("OverlapAllDynamic"));
    Trigger->OnComponentBeginOverlap.AddDynamic(this, &AScrapFinishGate::OnOverlap);

    Visual = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Visual"));
    Visual->SetupAttachment(RootComponent);
    Visual->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    Visual->SetRelativeScale3D(FVector(0.28f, 1.0f, 3.4f));
    Visual->SetStaticMesh(LoadCube());
}

void AScrapFinishGate::OnOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor,
    UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep,
    const FHitResult& SweepResult)
{
    if (Cast<AScrapDashCharacter>(OtherActor))
    {
        if (AScrapDashGameMode* Mode = GetWorld()->GetAuthGameMode<AScrapDashGameMode>())
        {
            Mode->TryCompleteLevel();
        }
    }
}

AScrapDashLevelDirector::AScrapDashLevelDirector()
{
    PrimaryActorTick.bCanEverTick = false;
}

void AScrapDashLevelDirector::SpawnBlock(const FVector& Location, const FVector& Scale) const
{
    FActorSpawnParameters Params;
    Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;

    if (AStaticMeshActor* Block = GetWorld()->SpawnActor<AStaticMeshActor>(
        AStaticMeshActor::StaticClass(), Location, FRotator::ZeroRotator, Params))
    {
        UStaticMeshComponent* Mesh = Block->GetStaticMeshComponent();
        Mesh->SetMobility(EComponentMobility::Movable);
        Mesh->SetStaticMesh(LoadCube());
        Mesh->SetCollisionProfileName(TEXT("BlockAll"));
        Block->SetActorScale3D(Scale);
    }
}

void AScrapDashLevelDirector::BeginPlay()
{
    Super::BeginPlay();

    // Closing Time Circuit: code-built vertical slice so gameplay can run before final art/maps.
    SpawnBlock(FVector(360.0f, 0.0f, -90.0f), FVector(9.5f, 1.4f, 0.45f));
    SpawnBlock(FVector(1110.0f, 0.0f, 30.0f), FVector(2.0f, 1.4f, 0.34f));
    SpawnBlock(FVector(1940.0f, 0.0f, 360.0f), FVector(6.5f, 1.4f, 0.42f));
    SpawnBlock(FVector(2620.0f, 0.0f, 120.0f), FVector(4.0f, 1.4f, 0.42f));

    SpawnGameplayActor<AScrapHazard>(FVector(880.0f, 0.0f, -20.0f));
    SpawnGameplayActor<AScrapEnemy>(FVector(590.0f, 0.0f, 20.0f));

    SpawnGameplayActor<AScrapCollectible>(FVector(180.0f, 0.0f, 80.0f));
    SpawnGameplayActor<AScrapCollectible>(FVector(620.0f, 0.0f, 90.0f));
    SpawnGameplayActor<AScrapCollectible>(FVector(1120.0f, 0.0f, 180.0f));
    SpawnGameplayActor<AScrapCollectible>(FVector(1830.0f, 0.0f, 570.0f));
    SpawnGameplayActor<AScrapCollectible>(FVector(2460.0f, 0.0f, 300.0f));

    SpawnGameplayActor<AScrapMovingPlatform>(FVector(1360.0f, 0.0f, 150.0f));
    SpawnGameplayActor<AScrapMagnetZone>(FVector(1625.0f, 0.0f, 170.0f));
    SpawnGameplayActor<AScrapCheckpoint>(FVector(2090.0f, 0.0f, 515.0f));
    SpawnGameplayActor<AScrapFinishGate>(FVector(2860.0f, 0.0f, 330.0f));
}
