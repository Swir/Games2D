#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "Engine/World.h"
#include "ScrapDashActors.generated.h"

class AScrapDashCharacter;
class UBoxComponent;
class USphereComponent;
class UStaticMeshComponent;

UCLASS()
class SCRAPDASH_API AScrapCollectible : public AActor
{
    GENERATED_BODY()
public:
    AScrapCollectible();
    virtual void Tick(float DeltaSeconds) override;
protected:
    virtual void BeginPlay() override;
private:
    UFUNCTION()
    void OnOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor,
        UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep,
        const FHitResult& SweepResult);

    UPROPERTY(VisibleAnywhere)
    TObjectPtr<USphereComponent> Trigger;

    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UStaticMeshComponent> Visual;
};

UCLASS()
class SCRAPDASH_API AScrapHazard : public AActor
{
    GENERATED_BODY()
public:
    AScrapHazard();
private:
    UFUNCTION()
    void OnOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor,
        UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep,
        const FHitResult& SweepResult);

    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UBoxComponent> Trigger;

    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UStaticMeshComponent> Visual;
};

UCLASS()
class SCRAPDASH_API AScrapEnemy : public AActor
{
    GENERATED_BODY()
public:
    AScrapEnemy();
    virtual void Tick(float DeltaSeconds) override;
protected:
    virtual void BeginPlay() override;
private:
    UFUNCTION()
    void OnOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor,
        UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep,
        const FHitResult& SweepResult);

    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UBoxComponent> Trigger;

    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UStaticMeshComponent> Visual;

    FVector PatrolOrigin = FVector::ZeroVector;
    float PatrolDirection = 1.0f;

    UPROPERTY(EditAnywhere, Category="SCRAP DASH")
    float PatrolDistance = 260.0f;

    UPROPERTY(EditAnywhere, Category="SCRAP DASH")
    float PatrolSpeed = 190.0f;
};

UCLASS()
class SCRAPDASH_API AScrapMovingPlatform : public AActor
{
    GENERATED_BODY()
public:
    AScrapMovingPlatform();
    virtual void Tick(float DeltaSeconds) override;
protected:
    virtual void BeginPlay() override;
private:
    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UStaticMeshComponent> PlatformMesh;

    FVector Origin = FVector::ZeroVector;
    float RuntimeSeconds = 0.0f;

    UPROPERTY(EditAnywhere, Category="SCRAP DASH")
    float TravelDistance = 300.0f;

    UPROPERTY(EditAnywhere, Category="SCRAP DASH")
    float TravelSpeed = 1.25f;
};

UCLASS()
class SCRAPDASH_API AScrapMagnetZone : public AActor
{
    GENERATED_BODY()
public:
    AScrapMagnetZone();
    virtual void Tick(float DeltaSeconds) override;

    void EngagePlayer(AScrapDashCharacter* Player);
    bool HasCapturedPlayer() const { return ActivePlayer.IsValid(); }

private:
    UFUNCTION()
    void OnOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor,
        UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep,
        const FHitResult& SweepResult);

    UFUNCTION()
    void OnEndOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor,
        UPrimitiveComponent* OtherComp, int32 OtherBodyIndex);

    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UBoxComponent> Trigger;

    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UStaticMeshComponent> Visual;

    UPROPERTY(EditAnywhere, Category="SCRAP DASH")
    float LiftVelocity = 1280.0f;

    UPROPERTY(EditAnywhere, Category="SCRAP DASH")
    float CenteringStrength = 5.0f;

    UPROPERTY(EditAnywhere, Category="SCRAP DASH")
    float MaxCenteringSpeed = 420.0f;

    TWeakObjectPtr<AScrapDashCharacter> ActivePlayer;
};

UCLASS()
class SCRAPDASH_API AScrapCheckpoint : public AActor
{
    GENERATED_BODY()
public:
    AScrapCheckpoint();
private:
    UFUNCTION()
    void OnOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor,
        UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep,
        const FHitResult& SweepResult);

    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UBoxComponent> Trigger;

    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UStaticMeshComponent> Visual;
};

UCLASS()
class SCRAPDASH_API AScrapFinishGate : public AActor
{
    GENERATED_BODY()
public:
    AScrapFinishGate();
private:
    UFUNCTION()
    void OnOverlap(UPrimitiveComponent* OverlappedComponent, AActor* OtherActor,
        UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep,
        const FHitResult& SweepResult);

    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UBoxComponent> Trigger;

    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UStaticMeshComponent> Visual;
};

UCLASS()
class SCRAPDASH_API AScrapDashLevelDirector : public AActor
{
    GENERATED_BODY()
public:
    AScrapDashLevelDirector();
protected:
    virtual void BeginPlay() override;
private:
    void SpawnBlock(const FVector& Location, const FVector& Scale) const;

    template<typename T>
    T* SpawnGameplayActor(const FVector& Location) const
    {
        FActorSpawnParameters Params;
        Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
        return GetWorld()->SpawnActor<T>(T::StaticClass(), Location, FRotator::ZeroRotator, Params);
    }
};
