using UnrealBuildTool;
using System.Collections.Generic;

public class ScrapDashEditorTarget : TargetRules
{
    public ScrapDashEditorTarget(TargetInfo Target) : base(Target)
    {
        Type = TargetType.Editor;
        DefaultBuildSettings = BuildSettingsVersion.Latest;
        IncludeOrderVersion = EngineIncludeOrderVersion.Latest;
        ExtraModuleNames.Add("ScrapDash");
    }
}
