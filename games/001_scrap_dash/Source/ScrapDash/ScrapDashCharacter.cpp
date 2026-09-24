#include "ScrapDashCharacter.h"

#include "Camera/CameraComponent.h"
#include "Components/CapsuleComponent.h"
#include "Components/StaticMeshComponent.h"
#include "EnhancedInputComponent.h"
#include "EnhancedInputSubsystems.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "GameFramework/PlayerController.h"
#include "GameFramework/SpringArmComponent.h"
#include "GameFramework/GameUserSettings.h"
#include "InputAction.h"
#include "InputMappingContext.h"
#include "InputModifiers.h"
#include "Kismet/GameplayStatics.h"
#include "ScrapDashGameMode.h"

AScrapDashCharacter::AScrapDashCharacter()
{
    PrimaryActorTick.bCanEverTick = true;

    GetCapsuleComponent()->InitCapsuleSize(38.0f, 72.0f);

    UCharacterMovementComponent* Move = GetCharacterMovement();
    Move->MaxWalkSpeed = 680.0f;
    Move->JumpZVelocity = 900.0f;
    Move->GravityScale = 2.45f;
    Move->AirControl = 0.82f;
    Move->BrakingDecelerationWalking = 2400.0f;
    Move->GroundFriction = 7.0f;
    Move->SetPlaneConstraintEnabled(true);
    Move->SetPlaneConstraintNormal(FVector(0.0f, 1.0f, 0.0f));
    Move->bSnapToPlaneAtStart = true;

    JumpMaxHoldTime = 0.22f;

    BodyMesh = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("RobotBody"));
    BodyMesh->SetupAttachment(RootComponent);
    BodyMesh->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    BodyMesh->SetRelativeScale3D(FVector(0.52f, 0.36f, 0.70f));

    HeadMesh = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("RobotHead"));
    HeadMesh->SetupAttachment(RootComponent);
    HeadMesh->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    HeadMesh->SetRelativeLocation(FVector(0.0f, 0.0f, 52.0f));
    HeadMesh->SetRelativeScale3D(FVector(0.42f, 0.42f, 0.42f));

    UStaticMesh* CubeMesh = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cube.Cube"));
    UStaticMesh* SphereMesh = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Sphere.Sphere"));
    if (CubeMesh)
    {
        BodyMesh->SetStaticMesh(CubeMesh);
    }
    if (SphereMesh)
    {
        HeadMesh->SetStaticMesh(SphereMesh);
    }

    CameraBoom = CreateDefaultSubobject<USpringArmComponent>(TEXT("CameraBoom"));
    CameraBoom->SetupAttachment(RootComponent);
    CameraBoom->TargetArmLength = 1050.0f;
    CameraBoom->SetRelativeRotation(FRotator(-4.0f, -90.0f, 0.0f));
    CameraBoom->bDoCollisionTest = false;
    CameraBoom->bUsePawnControlRotation = false;
    CameraBoom->SetUsingAbsoluteRotation(true);

    SideCamera = CreateDefaultSubobject<UCameraComponent>(TEXT("SideCamera"));
    SideCamera->SetupAttachment(CameraBoom, USpringArmComponent::SocketName);
    SideCamera->bUsePawnControlRotation = false;
    SideCamera->FieldOfView = 58.0f;

    RuntimeInputContext = CreateDefaultSubobject<UInputMappingContext>(TEXT("RuntimeInputContext"));
    MoveAction = CreateDefaultSubobject<UInputAction>(TEXT("MoveAction"));
    JumpAction = CreateDefaultSubobject<UInputAction>(TEXT("JumpAction"));
    DashAction = CreateDefaultSubobject<UInputAction>(TEXT("DashAction"));
    PauseAction = CreateDefaultSubobject<UInputAction>(TEXT("PauseAction"));
    RestartAction = CreateDefaultSubobject<UInputAction>(TEXT("RestartAction"));
    FullscreenAction = CreateDefaultSubobject<UInputAction>(TEXT("FullscreenAction"));

    MoveAction->ValueType = EInputActionValueType::Axis1D;
    JumpAction->ValueType = EInputActionValueType::Boolean;
    DashAction->ValueType = EInputActionValueType::Boolean;
    PauseAction->ValueType = EInputActionValueType::Boolean;
    RestartAction->ValueType = EInputActionValueType::Boolean;
    FullscreenAction->ValueType = EInputActionValueType::Boolean;
    PauseAction->bTriggerWhenPaused = true;
    RestartAction->bTriggerWhenPaused = true;
    FullscreenAction->bTriggerWhenPaused = true;
}

void AScrapDashCharacter::BeginPlay()
{
    Super::BeginPlay();
    InstallRuntimeInputMap();
}

void AScrapDashCharacter::InstallRuntimeInputMap()
{
    if (bInputMapInstalled || !RuntimeInputContext)
    {
        return;
    }

    auto AddNegativeMove = [this](const FKey& Key)
    {
        FEnhancedActionKeyMapping& Mapping = RuntimeInputContext->MapKey(MoveAction, Key);
        Mapping.Modifiers.Add(NewObject<UInputModifierNegate>(RuntimeInputContext));
    };

    AddNegativeMove(EKeys::A);
    AddNegativeMove(EKeys::Left);
    AddNegativeMove(EKeys::Gamepad_DPad_Left);

    RuntimeInputContext->MapKey(MoveAction, EKeys::D);
    RuntimeInputContext->MapKey(MoveAction, EKeys::Right);
    RuntimeInputContext->MapKey(MoveAction, EKeys::Gamepad_DPad_Right);
    RuntimeInputContext->MapKey(MoveAction, EKeys::Gamepad_LeftX);

    RuntimeInputContext->MapKey(JumpAction, EKeys::SpaceBar);
    RuntimeInputContext->MapKey(JumpAction, EKeys::W);
    RuntimeInputContext->MapKey(JumpAction, EKeys::Up);
    RuntimeInputContext->MapKey(JumpAction, EKeys::Gamepad_FaceButton_Bottom);

    RuntimeInputContext->MapKey(DashAction, EKeys::LeftShift);
    RuntimeInputContext->MapKey(DashAction, EKeys::Gamepad_FaceButton_Right);

    RuntimeInputContext->MapKey(PauseAction, EKeys::Escape);
    RuntimeInputContext->MapKey(PauseAction, EKeys::Gamepad_Special_Right);

    RuntimeInputContext->MapKey(RestartAction, EKeys::R);
    RuntimeInputContext->MapKey(RestartAction, EKeys::Gamepad_FaceButton_Top);

    RuntimeInputContext->MapKey(FullscreenAction, EKeys::F11);

    if (APlayerController* PC = Cast<APlayerController>(Controller))
    {
        if (ULocalPlayer* LocalPlayer = PC->GetLocalPlayer())
        {
            if (UEnhancedInputLocalPlayerSubsystem* Subsystem =
                ULocalPlayer::GetSubsystem<UEnhancedInputLocalPlayerSubsystem>(LocalPlayer))
            {
                Subsystem->ClearAllMappings();
                Subsystem->AddMappingContext(RuntimeInputContext, 0);
            }
        }
    }

    bInputMapInstalled = true;
}

void AScrapDashCharacter::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
    Super::SetupPlayerInputComponent(PlayerInputComponent);

    if (UEnhancedInputComponent* Enhanced = Cast<UEnhancedInputComponent>(PlayerInputComponent))
    {
        Enhanced->BindAction(MoveAction, ETriggerEvent::Triggered, this, &AScrapDashCharacter::Move);
        Enhanced->BindAction(JumpAction, ETriggerEvent::Started, this, &AScrapDashCharacter::JumpStarted);
        Enhanced->BindAction(JumpAction, ETriggerEvent::Completed, this, &AScrapDashCharacter::JumpReleased);
        Enhanced->BindAction(DashAction, ETriggerEvent::Started, this, &AScrapDashCharacter::DashStarted);
        Enhanced->BindAction(PauseAction, ETriggerEvent::Started, this, &AScrapDashCharacter::TogglePause);
        Enhanced->BindAction(RestartAction, ETriggerEvent::Started, this, &AScrapDashCharacter::RestartCheckpoint);
        Enhanced->BindAction(FullscreenAction, ETriggerEvent::Started, this, &AScrapDashCharacter::ToggleFullscreen);
    }
}

void AScrapDashCharacter::Tick(float DeltaSeconds)
{
    Super::Tick(DeltaSeconds);

    const float Now = GetWorld()->GetTimeSeconds();
    if (GetCharacterMovement()->IsMovingOnGround())
    {
        LastGroundedTime = Now;
        if (Now >= DashReadyTime)
        {
            bDashAvailable = true;
        }
        TryConsumeBufferedJump();
    }

    if (GetActorLocation().Z < KillZ)
    {
        Die();
    }
}

void AScrapDashCharacter::Move(const FInputActionValue& Value)
{
    const float Axis = FMath::Clamp(Value.Get<float>(), -1.0f, 1.0f);
    if (!FMath::IsNearlyZero(Axis))
    {
        LastMoveDirection = FMath::Sign(Axis);
        SetActorRotation(FRotator(0.0f, LastMoveDirection > 0.0f ? 0.0f : 180.0f, 0.0f));
    }
    AddMovementInput(FVector::ForwardVector, Axis);
}

void AScrapDashCharacter::JumpStarted(const FInputActionValue& Value)
{
    JumpBufferedUntil = GetWorld()->GetTimeSeconds() + JumpBufferTime;
    TryConsumeBufferedJump();
}

void AScrapDashCharacter::JumpReleased(const FInputActionValue& Value)
{
    StopJumping();
}

void AScrapDashCharacter::TryConsumeBufferedJump()
{
    const float Now = GetWorld()->GetTimeSeconds();
    if (Now > JumpBufferedUntil)
    {
        return;
    }

    const bool bGrounded = GetCharacterMovement()->IsMovingOnGround();
    const bool bInsideCoyoteWindow = (Now - LastGroundedTime) <= CoyoteTime;
    if (bGrounded || bInsideCoyoteWindow)
    {
        Jump();
        JumpBufferedUntil = -1000.0f;
    }
}

void AScrapDashCharacter::DashStarted(const FInputActionValue& Value)
{
    const float Now = GetWorld()->GetTimeSeconds();
    if (!bDashAvailable || Now < DashReadyTime)
    {
        return;
    }

    bDashAvailable = false;
    DashReadyTime = Now + DashCooldown;
    const float Direction = FMath::IsNearlyZero(LastMoveDirection) ? 1.0f : FMath::Sign(LastMoveDirection);
    LaunchCharacter(FVector(Direction * DashSpeed, 0.0f, DashVerticalBoost), true, false);
}

void AScrapDashCharacter::TogglePause(const FInputActionValue& Value)
{
    if (APlayerController* PC = Cast<APlayerController>(Controller))
    {
        PC->SetPause(!UGameplayStatics::IsGamePaused(this));
    }
}

void AScrapDashCharacter::RestartCheckpoint(const FInputActionValue& Value)
{
    if (APlayerController* PC = Cast<APlayerController>(Controller))
    {
        PC->SetPause(false);
    }

    if (AScrapDashGameMode* Mode = GetWorld()->GetAuthGameMode<AScrapDashGameMode>())
    {
        Mode->RespawnPlayer(this);
    }
}

void AScrapDashCharacter::ToggleFullscreen(const FInputActionValue& Value)
{
    if (UGameUserSettings* Settings = UGameUserSettings::GetGameUserSettings())
    {
        const EWindowMode::Type Current = Settings->GetFullscreenMode();
        Settings->SetFullscreenMode(Current == EWindowMode::Windowed
            ? EWindowMode::Fullscreen
            : EWindowMode::Windowed);
        Settings->ApplySettings(false);
        Settings->SaveSettings();
    }
}

void AScrapDashCharacter::RespawnAt(const FVector& WorldLocation)
{
    SetActorLocation(WorldLocation, false, nullptr, ETeleportType::TeleportPhysics);
    GetCharacterMovement()->StopMovementImmediately();
    GetCharacterMovement()->SetMovementMode(MOVE_Walking);
    LastGroundedTime = GetWorld()->GetTimeSeconds();
    JumpBufferedUntil = -1000.0f;
    DashReadyTime = 0.0f;
    bDashAvailable = true;
}

void AScrapDashCharacter::Die()
{
    if (AScrapDashGameMode* Mode = GetWorld()->GetAuthGameMode<AScrapDashGameMode>())
    {
        Mode->RespawnPlayer(this);
    }
}
