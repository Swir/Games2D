using UnrealBuildTool;

public class ScrapDashTests : ModuleRules
{
    public ScrapDashTests(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

        PrivateDependencyModuleNames.AddRange(new[]
        {
            "Core",
            "CoreUObject",
            "Engine",
            "ScrapDash",
            "UnrealEd"
        });
    }
}
