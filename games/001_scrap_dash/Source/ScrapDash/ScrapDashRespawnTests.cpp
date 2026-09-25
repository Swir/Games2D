#if WITH_DEV_AUTOMATION_TESTS

#include "Misc/AutomationTest.h"
#include "ScrapDashCharacter.h"

IMPLEMENT_SIMPLE_AUTOMATION_TEST(
    FScrapDashRespawnGuardTest,
    "ScrapDash.Gameplay.RespawnGuard",
    EAutomationTestFlags::EditorContext | EAutomationTestFlags::EngineFilter)

bool FScrapDashRespawnGuardTest::RunTest(const FString& Parameters)
{
    FScrapRespawnGuard Guard;
    Guard.Arm(10.0f, 0.75f);

    TestFalse(TEXT("Protected immediately after respawn"), Guard.CanReceiveLethalHit(10.0f));
    TestFalse(TEXT("Protected until the configured boundary"), Guard.CanReceiveLethalHit(10.74f));
    TestTrue(TEXT("Protection expires at the configured boundary"), Guard.CanReceiveLethalHit(10.75f));

    Guard.Arm(20.0f, -1.0f);
    TestTrue(TEXT("Negative protection duration clamps to zero"), Guard.CanReceiveLethalHit(20.0f));
    return true;
}

#endif
