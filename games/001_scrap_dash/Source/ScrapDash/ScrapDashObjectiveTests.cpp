#if WITH_DEV_AUTOMATION_TESTS

#include "Misc/AutomationTest.h"
#include "ScrapDashGameMode.h"

IMPLEMENT_SIMPLE_AUTOMATION_TEST(
    FScrapDashObjectiveProgressTest,
    "ScrapDash.Gameplay.ObjectiveProgress",
    EAutomationTestFlags::EditorContext | EAutomationTestFlags::EngineFilter)

bool FScrapDashObjectiveProgressTest::RunTest(const FString& Parameters)
{
    FScrapObjectiveProgress Objective;
    for (int32 Index = 0; Index < 5; ++Index)
    {
        Objective.RegisterScrap();
    }

    TestEqual(TEXT("Five registered scraps remain required"), Objective.GetRemainingScrap(), 5);
    TestFalse(TEXT("Exit stays locked before collection"), Objective.IsExitReady());

    for (int32 Index = 0; Index < 4; ++Index)
    {
        Objective.CollectScrap();
    }
    TestEqual(TEXT("One scrap remains after four pickups"), Objective.GetRemainingScrap(), 1);
    TestFalse(TEXT("Exit remains locked with one scrap missing"), Objective.IsExitReady());

    Objective.CollectScrap();
    TestEqual(TEXT("All scraps are counted"), Objective.GetCollectedScrap(), 5);
    TestTrue(TEXT("Exit becomes ready after all five scraps"), Objective.IsExitReady());

    Objective.CollectScrap();
    TestEqual(TEXT("Extra pickup cannot exceed the registered total"), Objective.GetCollectedScrap(), 5);
    return true;
}

#endif
