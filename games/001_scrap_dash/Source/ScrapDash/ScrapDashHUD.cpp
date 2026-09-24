#include "ScrapDashHUD.h"

#include "Engine/Canvas.h"
#include "Engine/Engine.h"
#include "Kismet/GameplayStatics.h"
#include "ScrapDashGameMode.h"

void AScrapDashHUD::DrawHUD()
{
    Super::DrawHUD();

    if (!Canvas)
    {
        return;
    }

    const AScrapDashGameMode* Mode = GetWorld()->GetAuthGameMode<AScrapDashGameMode>();
    if (!Mode)
    {
        return;
    }

    const FString ScrapLine = FString::Printf(
        TEXT("SCRAP  %d / %d"), Mode->GetCollectedScrap(), Mode->GetTotalScrap());
    const FString DeathLine = FString::Printf(TEXT("REBOOTS  %d"), Mode->GetDeathCount());

    DrawText(ScrapLine, FLinearColor(0.38f, 0.90f, 1.0f), 34.0f, 28.0f,
        GEngine->GetMediumFont(), 1.15f, false);
    DrawText(DeathLine, FLinearColor(1.0f, 0.78f, 0.16f), 34.0f, 60.0f,
        GEngine->GetSmallFont(), 1.0f, false);

    DrawText(TEXT("MOVE  A/D or Left Stick    JUMP  Space/A    DASH  Shift/B    RESTART  R/Y    FULLSCREEN  F11"),
        FLinearColor(0.72f, 0.78f, 0.85f), 34.0f, Canvas->ClipY - 42.0f,
        GEngine->GetSmallFont(), 0.85f, false);

    if (UGameplayStatics::IsGamePaused(this))
    {
        DrawText(TEXT("PAUSED"), FLinearColor::White,
            Canvas->ClipX * 0.5f - 72.0f, Canvas->ClipY * 0.36f,
            GEngine->GetLargeFont(), 1.6f, false);
    }

    if (Mode->IsLevelComplete())
    {
        DrawText(TEXT("CLOSING TIME CIRCUIT COMPLETE"),
            FLinearColor(0.38f, 0.90f, 1.0f),
            Canvas->ClipX * 0.5f - 250.0f, Canvas->ClipY * 0.42f,
            GEngine->GetLargeFont(), 1.45f, false);
        DrawText(TEXT("All scrap recovered. Demo vertical slice cleared."),
            FLinearColor::White,
            Canvas->ClipX * 0.5f - 220.0f, Canvas->ClipY * 0.50f,
            GEngine->GetMediumFont(), 1.0f, false);
    }
}
