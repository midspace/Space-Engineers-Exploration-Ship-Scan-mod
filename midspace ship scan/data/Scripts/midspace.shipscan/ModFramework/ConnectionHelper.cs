namespace midspace.shipscan
{
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    using VRage.Game.ModAPI;

    /// <summary>
    /// Conains useful methods and fields for organizing the connections.
    /// </summary>
    public static class ConnectionHelper
    {
        #region fields

        public static List<byte> ClientMessageCache = new List<byte>();
        public static Dictionary<ulong, List<byte>> ServerMessageCache = new Dictionary<ulong, List<byte>>();

        #endregion

        #region connections to server

        /// <summary>
        /// Creates and sends an entity with the given information for the server. Never call this on DS instance!
        /// </summary>

        public static void SendMessageToServer(ModMessageBase message)
        {
            message.Side = MessageSide.ServerSide;
            if (MyAPIGateway.Multiplayer.IsServer)
                message.SenderSteamId = MyAPIGateway.Multiplayer.ServerId;
            if (MyAPIGateway.Session.Player != null)
            {
                message.SenderSteamId = MyAPIGateway.Session.Player.SteamUserId;
                message.SenderDisplayName = MyAPIGateway.Session.Player.DisplayName;
            }
            message.SenderLanguage = (int)MyAPIGateway.Session.Config.Language;
            try
            {
                byte[] byteData = MyAPIGateway.Utilities.SerializeToBinary(message);
                MyAPIGateway.Multiplayer.SendMessageToServer(ModConfigurationConsts.ConnectionId, byteData);
            }
            catch (Exception ex)
            {
                MainChatCommandLogic.Instance.ClientLogger.WriteException(ex, "Could not send message to Server.");
                //TODO: send exception detail to Server.
            }
        }

        #endregion

        #region connections to all

        /// <summary>
        /// Creates and sends an entity with the given information for the server and all players.
        /// </summary>
        /// <param name="message"></param>
        public static void SendMessageToAll(ModMessageBase message)
        {
            if (MyAPIGateway.Multiplayer.IsServer)
                message.SenderSteamId = MyAPIGateway.Multiplayer.ServerId;
            if (MyAPIGateway.Session.Player != null)
            {
                message.SenderSteamId = MyAPIGateway.Session.Player.SteamUserId;
                message.SenderDisplayName = MyAPIGateway.Session.Player.DisplayName;
            }
            message.SenderLanguage = (int)MyAPIGateway.Session.Config.Language;

            if (!MyAPIGateway.Multiplayer.IsServer)
                SendMessageToServer(message);
            SendMessageToAllPlayers(message);
        }

        #endregion

        #region connections to clients

        public static void SendMessageToPlayer(ulong steamId, ModMessageBase message)
        {
            if (MyAPIGateway.Multiplayer.IsServer)
                message.SenderSteamId = MyAPIGateway.Multiplayer.ServerId;
            MainChatCommandLogic.Instance.ServerLogger.WriteVerbose("SendMessageToPlayer {0} {1} {2}.", steamId, message.Side, message.GetType().Name);
            message.Side = MessageSide.ClientSide;
            try
            {
                byte[] byteData = MyAPIGateway.Utilities.SerializeToBinary(message);
                MyAPIGateway.Multiplayer.SendMessageTo(ModConfigurationConsts.ConnectionId, byteData, steamId);
            }
            catch (Exception ex)
            {
                MainChatCommandLogic.Instance.ServerLogger.WriteException(ex);
                MainChatCommandLogic.Instance.ClientLogger.WriteException(ex);
                //TODO: send exception detail to Server if on Client.
            }
        }

        public static void SendMessageToAllPlayers(ModMessageBase messageContainer)
        {
            //MyAPIGateway.Multiplayer.SendMessageToOthers(StandardClientId, System.Text.Encoding.Unicode.GetBytes(ConvertData(content))); <- does not work as expected ... so it doesn't work at all?
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null);
            foreach (IMyPlayer player in players)
                SendMessageToPlayer(player.SteamUserId, messageContainer);
        }

        #endregion

        #region processing

        /// <summary>
        /// Server side execution of the actions defined in the data.
        /// </summary>
        /// <param name="rawData"></param>
        public static void ProcessData(byte[] rawData)
        {
            MainChatCommandLogic.Instance.ClientLogger.WriteStart("Start Message Deserialization");
            MainChatCommandLogic.Instance.ServerLogger.WriteStart("Start Message Deserialization");
            ModMessageBase message;

            try
            {
                message = MyAPIGateway.Utilities.SerializeFromBinary<ModMessageBase>(rawData);
            }
            catch (Exception ex)
            {
                MainChatCommandLogic.Instance.ClientLogger.WriteException(ex, $"Message cannot Deserialize. Message length: {rawData.Length}");
                MainChatCommandLogic.Instance.ServerLogger.WriteException(ex, $"Message cannot Deserialize. Message length: {rawData.Length}");
                return;
            }

            MainChatCommandLogic.Instance.ClientLogger.WriteStop("End Message Deserialization");
            MainChatCommandLogic.Instance.ServerLogger.WriteStop("End Message Deserialization");

            if (message != null)
            {
                try
                {
                    message.InvokeProcessing();
                }
                catch (Exception ex)
                {
                    MainChatCommandLogic.Instance.ClientLogger.WriteException(ex, $"Processing message exception. Side: {message.Side}");
                    MainChatCommandLogic.Instance.ServerLogger.WriteException(ex, $"Processing message exception. Side: {message.Side}");
                }
            }
        }

        #endregion
    }
}
