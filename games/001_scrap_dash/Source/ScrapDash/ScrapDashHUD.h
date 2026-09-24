#pragma once

#include "CoreMinimal.h"
#include "GameFramework/HUD.h"
#include "ScrapDashHUD.generated.h"

UCLASS()
class SCRAPDASH_API AScrapDashHUD : public AHUD
{
    GENERATED_BODY()
public:
    virtual void DrawHUD() override;
};
