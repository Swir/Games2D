namespace ScrapDash
{
    public static class LevelDefinition
    {
        public const string LevelName = "Closing Time Circuit";
        public const int TotalScrap = 5;
        public const int ScrapRequiredForFinish = 3;
        public const int HazardCount = 3;
        public const int EnemyCount = 1;
        public const int MovingPlatformCount = 1;
        public const int MagnetZoneCount = 1;
        public const int SpringPadCount = 1;
        public const int CheckpointCount = 1;

        public static bool IsStructurallyComplete()
        {
            return TotalScrap >= ScrapRequiredForFinish
                && ScrapRequiredForFinish > 0
                && HazardCount > 0
                && EnemyCount > 0
                && MovingPlatformCount > 0
                && MagnetZoneCount > 0
                && SpringPadCount > 0
                && CheckpointCount > 0;
        }
    }
}
