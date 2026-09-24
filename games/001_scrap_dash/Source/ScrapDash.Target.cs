using UnrealBuildTool;
using System.Collections.Generic;

public class ScrapDashTarget : TargetRules
{
    public ScrapDashTarget(TargetInfo Target) : base(Target)
    {
        Type = TargetType.Game;
        DefaultBuildSettings = BuildSettingsVersion.Latest;
        IncludeOrderVersion = EngineIncludeOrderVersion.Latest;
        ExtraModuleNames.Add("ScrapDash");
    }
}
