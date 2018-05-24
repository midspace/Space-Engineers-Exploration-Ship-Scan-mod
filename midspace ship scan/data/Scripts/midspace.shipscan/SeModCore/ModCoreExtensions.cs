namespace MidSpace.ShipScan.SeModCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Messages;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.ModAPI;

    internal static class ModCoreExtensions
    {
        #region security

        public static bool IsHost(this IMyPlayer player)
        {
            return MyAPIGateway.Multiplayer.IsServerPlayer(player.Client);
        }

        /// <summary>
        /// Determines if the player is an Administrator of the active game session.
        /// </summary>
        /// <param name="player"></param>
        /// <returns>True if is specified player is an Administrator in the active game.</returns>
        public static bool IsAdmin(this IMyPlayer player)
        {
            // Offline mode. You are the only player.
            if (MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE)
            {
                return true;
            }

            // Hosted game, and the player is hosting the server.
            if (player.IsHost())
            {
                return true;
            }

            return player.PromoteLevel == MyPromoteLevel.Owner ||  // 5 star
                player.PromoteLevel == MyPromoteLevel.Admin ||     // 4 star
                player.PromoteLevel == MyPromoteLevel.SpaceMaster; // 3 star
            // Otherwise Treat everyone as Normal Player.
        }

        public static uint UserSecurityLevel(this IMyPlayer player)
        {
            switch (player.PromoteLevel)
            {
                // 5 star
                case MyPromoteLevel.Owner: return ChatCommandSecurity.Owner;

                // 4 star
                case MyPromoteLevel.Admin: return ChatCommandSecurity.Admin;

                // 3 star
                case MyPromoteLevel.SpaceMaster: return ChatCommandSecurity.SpaceMaster;

                case MyPromoteLevel.Moderator: return ChatCommandSecurity.Moderator;

                case MyPromoteLevel.Scripter: return ChatCommandSecurity.Scripter;

                // normal player.
                case MyPromoteLevel.None: return ChatCommandSecurity.User;

                default: return ChatCommandSecurity.User;
            }
        }

        /// <summary>
        /// Determines if the player is an Author/Creator.
        /// This is used expressly for testing of commands that are not yet ready 
        /// to be released to the public, and should not be visible to the Help command list or accessible.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsExperimentalCreator(this IMyPlayer player)
        {
            return MainChatCommandLogic.Instance.ExperimentalCreatorList.Any(e => e == player.SteamUserId);
        }

        #endregion

        #region communication

        public static void ShowMessage(this IMyUtilities utilities, string sender, string messageText, params object[] args)
        {
            utilities.ShowMessage(sender, string.Format(messageText, args));
        }

        /// <summary>
        /// Sends a message to an specific player.  If steamId is set as 0, then it is sent to the current player.
        /// </summary>
        public static void SendMessage(this IMyUtilities utilities, ulong steamId, string sender, string messageText, params object[] args)
        {
            if (steamId == MyAPIGateway.Multiplayer.ServerId || (MyAPIGateway.Session.Player != null && steamId == MyAPIGateway.Session.Player.SteamUserId))
                utilities.ShowMessage(sender, messageText, args);
            else
                PushClientTextMessage.SendMessage(steamId, sender, messageText, args);
        }

        public static void SendMissionScreen(this IMyUtilities utilities, ulong steamId, string screenTitle = null, string currentObjectivePrefix = null, string currentObjective = null, string screenDescription = null, Action<ResultEnum> callback = null, string okButtonCaption = null)
        {
            if (steamId == MyAPIGateway.Multiplayer.ServerId || (MyAPIGateway.Session.Player != null && steamId == MyAPIGateway.Session.Player.SteamUserId))
                utilities.ShowMissionScreen(screenTitle, currentObjectivePrefix, currentObjective, screenDescription, callback, okButtonCaption);
            else
                PushClientDialogMessage.SendMessage(steamId, screenTitle, currentObjectivePrefix, screenDescription);
        }

        #endregion

        #region player

        public static IMyPlayer GetPlayer(this IMyPlayerCollection collection, ulong steamId)
        {
            var players = new List<IMyPlayer>();
            collection.GetPlayers(players, p => p.SteamUserId == steamId);
            return players.FirstOrDefault();
        }

        public static IMyPlayer FindPlayerBySteamId(this IMyPlayerCollection collection, ulong steamId)
        {
            var listplayers = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(listplayers, p => p.SteamUserId == steamId);
            return listplayers.FirstOrDefault();
        }

        //public static bool TryGetIdentity(this IMyPlayerCollection collection, long identityId, out IMyIdentity identity)
        //{
        //    var listIdentites = new List<IMyIdentity>();
        //    MyAPIGateway.Players.GetAllIdentites(listIdentites, p => p.IdentityId == identityId);
        //    identity = listIdentites.FirstOrDefault();
        //    return identity != null;
        //}

        #endregion
    }
}