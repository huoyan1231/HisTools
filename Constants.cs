using System.IO;
using BepInEx;

namespace HisTools;

public static class Constants
{
    public const string PluginName = "HisTools";
    public const string PluginVersion = "0.3.2";
    public const string PluginGuid = "com.cyfral.HisTools";

    public static class Animation
    {
        public const float Duration = 0.15f;
        public const float Cooldown = 0.25f;
        public const float MaxBackgroundAlpha = 0.90f;
    }

    public static class Paths
    {
        private const string Routes = "Routes";
        private const string Settings = "Settings";
        private const string SpeedrunStats = "SpeedrunStats";
        private const string FeaturesStateFile = "features_state.json";
        private const string RoutesStateFile = "routes_state.json";

        public static string ConfigDir => Path.Combine(BepInEx.Paths.BepInExRootPath, PluginName);
        public static string PluginDllDir => Path.GetDirectoryName(Plugin.Instance.Info.Location) ?? string.Empty;
        public static string RoutesPathDir => Path.Combine(ConfigDir, Routes);
        public static string RoutesStateConfigFilePath => Path.Combine(ConfigDir, RoutesStateFile);
        public static string SettingsConfigPath => Path.Combine(ConfigDir, Settings);
        public static string FeaturesStateConfigFilePath => Path.Combine(ConfigDir, FeaturesStateFile);
        public static string SpeedrunStatsDir => Path.Combine(ConfigDir, SpeedrunStats);
    }

    public static class UI
    {
        public const int CanvasSortOrder = 250;
        public const string MenuObjectName = "HisTools_HisToolsFeaturesMenu";
        public const string SettingsPanelName = "HisTools_SettingsPanelController";
        public const string CategoriesContainerName = "HisTools_CategoriesContainer";
    }
}