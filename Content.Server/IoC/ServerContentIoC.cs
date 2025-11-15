using Content.Server._NF.Auth;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Notes;
using Content.Server.Afk;
using Content.Server.Chat.Managers;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Server.Discord;
using Content.Server.Discord.DiscordLink;
using Content.Server.Discord.WebhookMessages;
using Content.Server.EUI;
using Content.Server.GhostKick;
using Content.Server.Info;
using Content.Server.Mapping;
using Content.Server.Maps;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Players.JobWhitelist;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Players.RateLimiting;
using Content.Server.Preferences.Managers;
using Content.Server.ServerInfo;
using Content.Server.ServerUpdates;
using Content.Server.Voting.Managers;
using Content.Server.Worldgen.Tools;
using Content.Shared.Administration.Logs;
using Content.Shared.Administration.Managers;
using Content.Shared.Chat;
using Content.Shared.IoC;
using Content.Shared.Kitchen;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Players.RateLimiting;

<<<<<<< HEAD
namespace Content.Server.IoC
{
    internal static class ServerContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<IChatManager, ChatManager>();
            IoCManager.Register<ISharedChatManager, ChatManager>();
            IoCManager.Register<IChatSanitizationManager, ChatSanitizationManager>();
            IoCManager.Register<IServerPreferencesManager, ServerPreferencesManager>();
            IoCManager.Register<IServerDbManager, ServerDbManager>();
            IoCManager.Register<RecipeManager, RecipeManager>();
            IoCManager.Register<INodeGroupFactory, NodeGroupFactory>();
            IoCManager.Register<IConnectionManager, ConnectionManager>();
            IoCManager.Register<ServerUpdateManager>();
            IoCManager.Register<IAdminManager, AdminManager>();
            IoCManager.Register<ISharedAdminManager, AdminManager>();
            IoCManager.Register<EuiManager, EuiManager>();
            IoCManager.Register<IVoteManager, VoteManager>();
            IoCManager.Register<IPlayerLocator, PlayerLocator>();
            IoCManager.Register<IAfkManager, AfkManager>();
            IoCManager.Register<IGameMapManager, GameMapManager>();
            IoCManager.Register<RulesManager, RulesManager>();
            IoCManager.Register<IBanManager, BanManager>();
            IoCManager.Register<ContentNetworkResourceManager>();
            IoCManager.Register<IAdminNotesManager, AdminNotesManager>();
            IoCManager.Register<GhostKickManager>();
            IoCManager.Register<ISharedAdminLogManager, AdminLogManager>();
            IoCManager.Register<IAdminLogManager, AdminLogManager>();
            IoCManager.Register<PlayTimeTrackingManager>();
            IoCManager.Register<UserDbDataManager>();
            IoCManager.Register<ServerInfoManager>();
            IoCManager.Register<PoissonDiskSampler>();
            IoCManager.Register<DiscordWebhook>();
            IoCManager.Register<VoteWebhooks>();
            IoCManager.Register<ServerDbEntryManager>();
            IoCManager.Register<ISharedPlaytimeManager, PlayTimeTrackingManager>();
            IoCManager.Register<ServerApi>();
            IoCManager.Register<JobWhitelistManager>();
            IoCManager.Register<PlayerRateLimitManager>();
            IoCManager.Register<SharedPlayerRateLimitManager, PlayerRateLimitManager>();
            IoCManager.Register<MappingManager>();
            IoCManager.Register<IWatchlistWebhookManager, WatchlistWebhookManager>();
            IoCManager.Register<ConnectionManager>();
            IoCManager.Register<MultiServerKickManager>();
            IoCManager.Register<CVarControlManager>();
            IoCManager.Register<MiniAuthManager>(); //Frontier
=======
namespace Content.Server.IoC;
>>>>>>> e917c8e067e70fa369bf8f1f393a465dc51caee8

internal static class ServerContentIoC
{
    public static void Register(IDependencyCollection deps)
    {
        SharedContentIoC.Register(deps);
        deps.Register<IChatManager, ChatManager>();
        deps.Register<ISharedChatManager, ChatManager>();
        deps.Register<IChatSanitizationManager, ChatSanitizationManager>();
        deps.Register<IServerPreferencesManager, ServerPreferencesManager>();
        deps.Register<IServerDbManager, ServerDbManager>();
        deps.Register<RecipeManager, RecipeManager>();
        deps.Register<INodeGroupFactory, NodeGroupFactory>();
        deps.Register<IConnectionManager, ConnectionManager>();
        deps.Register<ServerUpdateManager>();
        deps.Register<IAdminManager, AdminManager>();
        deps.Register<ISharedAdminManager, AdminManager>();
        deps.Register<EuiManager, EuiManager>();
        deps.Register<IVoteManager, VoteManager>();
        deps.Register<IPlayerLocator, PlayerLocator>();
        deps.Register<IAfkManager, AfkManager>();
        deps.Register<IGameMapManager, GameMapManager>();
        deps.Register<RulesManager, RulesManager>();
        deps.Register<IBanManager, BanManager>();
        deps.Register<ContentNetworkResourceManager>();
        deps.Register<IAdminNotesManager, AdminNotesManager>();
        deps.Register<GhostKickManager>();
        deps.Register<ISharedAdminLogManager, AdminLogManager>();
        deps.Register<IAdminLogManager, AdminLogManager>();
        deps.Register<PlayTimeTrackingManager>();
        deps.Register<UserDbDataManager>();
        deps.Register<ServerInfoManager>();
        deps.Register<PoissonDiskSampler>();
        deps.Register<DiscordWebhook>();
        deps.Register<VoteWebhooks>();
        deps.Register<ServerDbEntryManager>();
        deps.Register<ISharedPlaytimeManager, PlayTimeTrackingManager>();
        deps.Register<ServerApi>();
        deps.Register<JobWhitelistManager>();
        deps.Register<PlayerRateLimitManager>();
        deps.Register<SharedPlayerRateLimitManager, PlayerRateLimitManager>();
        deps.Register<MappingManager>();
        deps.Register<IWatchlistWebhookManager, WatchlistWebhookManager>();
        deps.Register<ConnectionManager>();
        deps.Register<MultiServerKickManager>();
        deps.Register<CVarControlManager>();
        deps.Register<DiscordLink>();
        deps.Register<DiscordChatLink>();
    }
}
