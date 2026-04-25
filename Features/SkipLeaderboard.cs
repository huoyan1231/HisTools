using WK_huoyan1231COMLib;
using HisTools.Features.Controllers;

namespace HisTools.Features;

public class SkipLeaderboard : FeatureBase
{
    private static readonly string RequesterId = Constants.PluginGuid;

    public SkipLeaderboard() : base("SkipLeaderboard", "Skip leaderboard upload via COMLib")
    {
        AddSetting(new BoolSetting(this, "Enabled on start",
            "Automatically disable leaderboard when game starts (requires COMLib)", true));
    }

    public override void OnEnable()
    {
        try
        {
            LeaderboardManager.DisableForThisRun(RequesterId);
            Utils.Logger.Info("[SkipLeaderboard] Leaderboards disabled for this run.");
        }
        catch (System.Exception ex)
        {
            Utils.Logger.Error($"[SkipLeaderboard] Failed to disable leaderboards: {ex.Message}");
        }
    }

    public override void OnDisable()
    {
        try
        {
            LeaderboardManager.TryRestore(RequesterId);
            Utils.Logger.Info("[SkipLeaderboard] Leaderboard disable request removed.");
        }
        catch (System.Exception ex)
        {
            Utils.Logger.Error($"[SkipLeaderboard] Failed to restore leaderboards: {ex.Message}");
        }
    }
}
