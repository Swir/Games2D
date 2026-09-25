#include "ScrapDashGameMode.h"

#include "GameFramework/CharacterMovementComponent.h"
#include "GameFramework/PlayerController.h"
#include "ScrapDashActors.h"
#include "ScrapDashCharacter.h"
#include "ScrapDashHUD.h"

AScrapDashGameMode::AScrapDashGameMode()
{
    DefaultPawnClass = AScrapDashCharacter::StaticClass();
    HUDClass = AScrapDashHUD::StaticClass();
}

void AScrapDashGameMode::BeginPlay()
{
    Super::BeginPlay();

    FActorSpawnParameters Params;
    Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
    GetWorld()->SpawnActor<AScrapDashLevelDirector>(
        AScrapDashLevelDirector::StaticClass(), FVector::ZeroVector, FRotator::ZeroRotator, Params);

    if (APlayerController* PC = GetWorld()->GetFirstPlayerController())
    {
        AScrapDashCharacter* Player = Cast<AScrapDashCharacter>(PC->GetPawn());
        if (!Player)
        {
            FActorSpawnParameters PlayerParams;
            PlayerParams.SpawnCollisionHandlingOverride =
                ESpawnActorCollisionHandlingMethod::AdjustIfPossibleButAlwaysSpawn;
            Player = GetWorld()->SpawnActor<AScrapDashCharacter>(
                AScrapDashCharacter::StaticClass(), CheckpointLocation, FRotator::ZeroRotator, PlayerParams);
            if (Player)
            {
                PC->Possess(Player);
            }
        }

        if (Player)
        {
            Player->RespawnAt(CheckpointLocation);
        }
    }
}

void AScrapDashGameMode::RegisterScrap()
{
    Objective.RegisterScrap();
}

void AScrapDashGameMode::CollectScrap()
{
    Objective.CollectScrap();
}

void AScrapDashGameMode::SetCheckpoint(const FVector& WorldLocation)
{
    CheckpointLocation = WorldLocation;
}

void AScrapDashGameMode::RespawnPlayer(AScrapDashCharacter* Player, const bool bCountAsDeath)
{
    if (!Player || bLevelComplete)
    {
        return;
    }

    if (bCountAsDeath)
    {
        ++DeathCount;
    }
    Player->RespawnAt(CheckpointLocation);
}

bool AScrapDashGameMode::TryCompleteLevel()
{
    if (bLevelComplete || !Objective.IsExitReady())
    {
        return false;
    }

    bLevelComplete = true;

    if (APlayerController* PC = GetWorld()->GetFirstPlayerController())
    {
        if (AScrapDashCharacter* Player = Cast<AScrapDashCharacter>(PC->GetPawn()))
        {
            Player->GetCharacterMovement()->DisableMovement();
        }
    }

    return true;
}
