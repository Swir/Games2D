#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "ScrapDashGameMode.generated.h"

class AScrapDashCharacter;

struct FScrapObjectiveProgress
{
    void RegisterScrap() { ++TotalScrap; }

    void CollectScrap()
    {
        CollectedScrap = FMath::Clamp(CollectedScrap + 1, 0, TotalScrap);
    }

    int32 GetCollectedScrap() const { return CollectedScrap; }
    int32 GetTotalScrap() const { return TotalScrap; }
    int32 GetRemainingScrap() const { return FMath::Max(0, TotalScrap - CollectedScrap); }
    bool IsExitReady() const { return TotalScrap > 0 && CollectedScrap >= TotalScrap; }

private:
    int32 CollectedScrap = 0;
    int32 TotalScrap = 0;
};

UCLASS()
class SCRAPDASH_API AScrapDashGameMode : public AGameModeBase
{
    GENERATED_BODY()

public:
    AScrapDashGameMode();

    virtual void BeginPlay() override;

    void RegisterScrap();
    void CollectScrap();
    void SetCheckpoint(const FVector& WorldLocation);
    void RespawnPlayer(AScrapDashCharacter* Player, bool bCountAsDeath = true);
    bool TryCompleteLevel();

    int32 GetCollectedScrap() const { return Objective.GetCollectedScrap(); }
    int32 GetTotalScrap() const { return Objective.GetTotalScrap(); }
    int32 GetRemainingScrap() const { return Objective.GetRemainingScrap(); }
    int32 GetDeathCount() const { return DeathCount; }
    bool IsExitReady() const { return Objective.IsExitReady(); }
    bool IsLevelComplete() const { return bLevelComplete; }

private:
    FScrapObjectiveProgress Objective;
    int32 DeathCount = 0;
    bool bLevelComplete = false;
    FVector CheckpointLocation = FVector(0.0f, 0.0f, 120.0f);
};
