#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "InputActionValue.h"
#include "ScrapDashCharacter.generated.h"

class UCameraComponent;
class USpringArmComponent;
class UStaticMeshComponent;
class UInputAction;
class UInputMappingContext;

UCLASS()
class SCRAPDASH_API AScrapDashCharacter : public ACharacter
{
    GENERATED_BODY()

public:
    AScrapDashCharacter();

    virtual void Tick(float DeltaSeconds) override;
    virtual void SetupPlayerInputComponent(UInputComponent* PlayerInputComponent) override;

    void RespawnAt(const FVector& WorldLocation);
    void Die();

protected:
    virtual void BeginPlay() override;

private:
    void InstallRuntimeInputMap();
    void Move(const FInputActionValue& Value);
    void JumpStarted(const FInputActionValue& Value);
    void JumpReleased(const FInputActionValue& Value);
    void DashStarted(const FInputActionValue& Value);
    void TogglePause(const FInputActionValue& Value);
    void RestartCheckpoint(const FInputActionValue& Value);
    void TryConsumeBufferedJump();

    UPROPERTY(VisibleAnywhere, Category="SCRAP DASH|Camera")
    TObjectPtr<USpringArmComponent> CameraBoom;

    UPROPERTY(VisibleAnywhere, Category="SCRAP DASH|Camera")
    TObjectPtr<UCameraComponent> SideCamera;

    UPROPERTY(VisibleAnywhere, Category="SCRAP DASH|Visual")
    TObjectPtr<UStaticMeshComponent> BodyMesh;

    UPROPERTY(VisibleAnywhere, Category="SCRAP DASH|Visual")
    TObjectPtr<UStaticMeshComponent> HeadMesh;

    UPROPERTY(Transient)
    TObjectPtr<UInputMappingContext> RuntimeInputContext;

    UPROPERTY(Transient)
    TObjectPtr<UInputAction> MoveAction;

    UPROPERTY(Transient)
    TObjectPtr<UInputAction> JumpAction;

    UPROPERTY(Transient)
    TObjectPtr<UInputAction> DashAction;

    UPROPERTY(Transient)
    TObjectPtr<UInputAction> PauseAction;

    UPROPERTY(Transient)
    TObjectPtr<UInputAction> RestartAction;

    UPROPERTY(EditDefaultsOnly, Category="SCRAP DASH|Movement")
    float DashSpeed = 1450.0f;

    UPROPERTY(EditDefaultsOnly, Category="SCRAP DASH|Movement")
    float DashVerticalBoost = 80.0f;

    UPROPERTY(EditDefaultsOnly, Category="SCRAP DASH|Movement")
    float DashCooldown = 0.35f;

    UPROPERTY(EditDefaultsOnly, Category="SCRAP DASH|Movement")
    float CoyoteTime = 0.14f;

    UPROPERTY(EditDefaultsOnly, Category="SCRAP DASH|Movement")
    float JumpBufferTime = 0.16f;

    UPROPERTY(EditDefaultsOnly, Category="SCRAP DASH|Movement")
    float KillZ = -800.0f;

    bool bInputMapInstalled = false;
    bool bDashAvailable = true;
    float LastGroundedTime = -1000.0f;
    float JumpBufferedUntil = -1000.0f;
    float DashReadyTime = 0.0f;
    float LastMoveDirection = 1.0f;
};
