#if WITH_DEV_AUTOMATION_TESTS

#include "Camera/CameraComponent.h"
#include "Editor.h"
#include "EngineUtils.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Misc/AutomationTest.h"
#include "ScrapDashActors.h"
#include "ScrapDashCharacter.h"
#include "ScrapDashGameMode.h"
#include "Tests/AutomationCommon.h"
#include "Tests/AutomationEditorCommon.h"

namespace
{
template <typename TActor>
int32 CountActors(UWorld* World)
{
    int32 Count = 0;
    for (TActorIterator<TActor> It(World); It; ++It)
    {
        ++Count;
    }
    return Count;
}
}

DEFINE_LATENT_AUTOMATION_COMMAND_ONE_PARAMETER(
    FVerifyScrapDashRuntimeAssembly,
    FAutomationTestBase*,
    Test);

bool FVerifyScrapDashRuntimeAssembly::Update()
{
    UWorld* World = GEditor ? GEditor->PlayWorld : nullptr;
    if (!World)
    {
        Test->AddError(TEXT("PIE world was not created"));
        return true;
    }

    AScrapDashGameMode* Mode = World->GetAuthGameMode<AScrapDashGameMode>();
    Test->TestNotNull(TEXT("SCRAP DASH game mode is active"), Mode);

    AScrapDashCharacter* Player = nullptr;
    for (TActorIterator<AScrapDashCharacter> It(World); It; ++It)
    {
        Player = *It;
        break;
    }
    Test->TestNotNull(TEXT("Scrap robot spawns in PIE"), Player);

    if (Player)
    {
        UCharacterMovementComponent* Movement = Player->GetCharacterMovement();
        Test->TestTrue(TEXT("Player stays on the 2.5D plane"), Movement->bConstrainToPlane);
        Test->TestEqual(TEXT("Gameplay plane normal is the Y axis"),
            Movement->GetPlaneConstraintNormal(), FVector(0.0f, 1.0f, 0.0f));
        Test->TestNotNull(TEXT("Side camera is attached"), Player->FindComponentByClass<UCameraComponent>());
    }

    Test->TestEqual(TEXT("Five scrap collectibles spawn"),
        CountActors<AScrapCollectible>(World), 5);
    Test->TestEqual(TEXT("Hazard spawns"),
        CountActors<AScrapHazard>(World), 1);
    Test->TestEqual(TEXT("Patrol enemy spawns"),
        CountActors<AScrapEnemy>(World), 1);
    Test->TestEqual(TEXT("Moving platform section spawns"),
        CountActors<AScrapMovingPlatform>(World), 1);
    Test->TestEqual(TEXT("Magnet Lift spawns"),
        CountActors<AScrapMagnetZone>(World), 1);
    Test->TestEqual(TEXT("Checkpoint spawns"),
        CountActors<AScrapCheckpoint>(World), 1);
    Test->TestEqual(TEXT("Finish gate spawns"),
        CountActors<AScrapFinishGate>(World), 1);

    if (Mode)
    {
        Test->TestEqual(TEXT("Runtime objective registers all scrap"),
            Mode->GetTotalScrap(), 5);
        Test->TestFalse(TEXT("Exit starts locked"), Mode->IsExitReady());
        for (int32 Index = 0; Index < 5; ++Index)
        {
            Mode->CollectScrap();
        }
        Test->TestTrue(TEXT("Exit powers after five scrap"), Mode->IsExitReady());
        Test->TestTrue(TEXT("Finish produces the win state"), Mode->TryCompleteLevel());
        Test->TestTrue(TEXT("Level remains complete"), Mode->IsLevelComplete());
    }

    return true;
}

IMPLEMENT_SIMPLE_AUTOMATION_TEST(
    FScrapDashLevelRuntimeAssemblyTest,
    "ScrapDash.Level1.RuntimeAssembly",
    EAutomationTestFlags::EditorContext | EAutomationTestFlags::EngineFilter)

bool FScrapDashLevelRuntimeAssemblyTest::RunTest(const FString& Parameters)
{
    AutomationOpenMap(TEXT("/Engine/Maps/Entry"));
    ADD_LATENT_AUTOMATION_COMMAND(FStartPIECommand(true));
    ADD_LATENT_AUTOMATION_COMMAND(FWaitLatentCommand(1.0f));
    ADD_LATENT_AUTOMATION_COMMAND(FVerifyScrapDashRuntimeAssembly(this));
    ADD_LATENT_AUTOMATION_COMMAND(FEndPlayMapCommand());
    return true;
}

#endif
