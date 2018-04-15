// SE Mod Core V4.1
// Author: MidSpace
// Copyright © MidSpace 2014-2018
//
// This is a framework to support primarily Command Chat style mods and some other mods.
//
// It has:
// * a basic communication system to send Binary serialized messages between the Server and Clients (not Client to Client).
// * Initialization routines with version checks to prevent out of date server mods to communicate with recently published mods on clients.
// * Server and Client side invokes chat commands.
// * frame counters to run at set intervals.
// 
// This code is licensed under the GNU General Public License, version 3

namespace MidSpace.ShipScan.SeModCore
{
    using Messages;
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    using System.Timers;
    using VRage.Game;
    using VRage.Game.Components;
    using VRage.Library.Utils;

    /// <summary>
    /// Chat command framework logic.
    /// This controls the initialization, load, save, and main event handling for the SE mod core.
    /// 
    /// Classes must add the [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)] attribute.
    /// </summary>
    //[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public abstract class MainChatCommandLogic : MySessionComponentBase //, IChatCommandLogic
    {
        #region fields

        private bool _isInitialized;
        private bool _isClientRegistered;
        private bool _isServerRegistered;
        // TODO: see if we can have two _messageHandler's for server and client. This will be an issue for the MP Host, and Offline where one might be all that will register.
        // TODO: I think I want to split the Commands in Client specific and Server specific, which would make this work.
        private readonly Action<byte[]> _messageHandler = new Action<byte[]>(HandleMessage);
        //internal static MainChatCommandLogic Instance;
        public Timer DelayedConnectionRequestTimer;
        private bool _delayedConnectionRequest;
        private bool _commandsRegistered;
        private int _frameCounter100;
        private int _frameCounter1000;

        /// <summary>
        /// This will temporarily store Client side details while the client is connected.
        /// It will receive periodic updates from the server.
        /// </summary>
        public ClientConfigBase ClientConfig = null;

        internal long PrivateCommunicationKey;

        /// <summary>
        /// This is used to indicate the base communication version.
        /// </summary>
        internal int ModCommunicationVersion;

        /// <summary>
        /// The is the Id which this mod registers itself for sending and receiving messages through SE. 
        /// </summary>
        internal ushort ConnectionId;

        internal LogEventType ClientLoggingLevel;
        internal string ClientLogFileName;

        internal LogEventType ServerLoggingLevel;
        internal string ServerLogFileName;

        /// <summary>
        ///  Used for filenames. Shouldn't have unfriendly characters.
        /// </summary>
        internal string ModName;

        /// <summary>
        /// Used for display boxes and friendly information.
        /// </summary>
        internal string ModTitle;

        /// <summary>
        /// Hardcoded list of SteamIDs, for testing stuff that hasn't been released to the public yet.
        /// It shouldn't be used to hide functionality away in the published mod, simply prevent
        /// incomplete or broken stuff from been used until it is ready.
        /// </summary>
        internal ulong[] ExperimentalCreatorList;

        #endregion

        #region IChatCommandLogic fields

        internal static MainChatCommandLogic Instance;
        //internal static IChatCommandLogic Instance;

        public abstract List<ChatCommand> GetAllChatCommands();
        public abstract ClientConfigBase GetConfig();
        public abstract void InitModSettings(out int modCommunicationVersion, out ushort connectionId, out LogEventType clientLoggingLevel, out string clientLogFileName, out LogEventType serverLoggingLevel, out string serverLogFileName, out string modName, out string modTitle, out ulong[] experimentalCreatorList);

        public virtual void UpdateBeforeFrame() { }
        public virtual void UpdateBeforeFrame100() { }
        public virtual void UpdateBeforeFrame1000() { }
        public virtual void UpdateAfterFrame() { }

        public virtual void ClientLoad() { }
        public virtual void ClientSave() { }
        public virtual void ServerLoad() { }
        public virtual void ServerSave() { }


        internal bool ResponseReceived { get; set; }

        /// <summary>
        /// Indicates that this Client sucessful received config from the server.
        /// </summary>
        internal bool IsConnected { get; set; }

        /// <summary>
        /// Indicates that this instance is a registered client, and all Game related API's are active.
        /// </summary>
        public bool IsClientRegistered => _isClientRegistered;

        /// <summary>
        /// Indicates that this instance is a register server.
        /// </summary>
        public bool IsServerRegistered => _isServerRegistered;

        // This is a dummy logger until Init() is called.
        public TextLogger ServerLogger { get; } = new TextLogger();

        // This is a dummy logger until Init() is called.
        public TextLogger ClientLogger { get; } = new TextLogger();

        #endregion

        #region constructor

        protected MainChatCommandLogic()
        {
            //TextLogger.WriteGameLog($"####### {ModConfigurationConsts.ModName} CTOR");
            Instance = this;
            PrivateCommunicationKey = MyRandom.Instance.NextLong();
        }

        #endregion

        #region attaching events and wiring up

        public sealed override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            InitModSettings(out ModCommunicationVersion, out ConnectionId, out ClientLoggingLevel, out ClientLogFileName, out ServerLoggingLevel, out ServerLogFileName, out ModName, out ModTitle, out ExperimentalCreatorList);
            Guard.IsNotZero(ModCommunicationVersion, "InitModSettings must supply ModCommunicationVersion");
            Guard.IsNotZero(ConnectionId, "InitModSettings must supply ConnectionId");
            Guard.IsNotEmpty(ClientLogFileName, "InitModSettings must supply ClientLogFileName");
            Guard.IsNotEmpty(ServerLogFileName, "InitModSettings must supply ServerLogFileName");
            Guard.IsNotEmpty(ModName, "InitModSettings must supply ModName");
            Guard.IsNotEmpty(ModTitle, "InitModSettings must supply ModTitle");


            //TextLogger.WriteGameLog($"####### {ModConfigurationConsts.ModName} INIT");

            //if (MyAPIGateway.Utilities == null)
            //    MyAPIGateway.Utilities = MyAPIUtilities.Static;

            //TextLogger.WriteGameLog($"####### TEST1 {MyAPIGateway.Utilities == null}");
            ////TextLogger.WriteGameLog($"####### TEST2 {MyAPIGateway.Utilities?.ConfigDedicated == null}");  // FAIL
            //TextLogger.WriteGameLog($"####### TEST3 {MyAPIGateway.Utilities?.GamePaths == null}");
            //TextLogger.WriteGameLog($"####### TEST3 {MyAPIGateway.Utilities?.GamePaths?.UserDataPath ?? "FAIL"}");

            //TextLogger.WriteGameLog($"####### TEST4 {MyAPIGateway.Utilities?.IsDedicated == null}");

            //TextLogger.WriteGameLog($"####### TEST5 {MyAPIGateway.Session == null}");
            ////TextLogger.WriteGameLog($"####### TEST6 {MyAPIGateway.Session?.Player == null}");    // FAIL
            //TextLogger.WriteGameLog($"####### TEST7 {MyAPIGateway.Session?.OnlineMode ?? null}");
            ////MyAPIGateway.Session.OnlineMode.Equals(MyOnlineModeEnum.OFFLINE)

            //TextLogger.WriteGameLog($"####### TEST8 {MyAPIGateway.Multiplayer == null}");
            //TextLogger.WriteGameLog($"####### TEST9 {MyAPIGateway.Multiplayer?.IsServer ?? null}");

            if (MyAPIGateway.Session.OnlineMode.Equals(MyOnlineModeEnum.OFFLINE) || MyAPIGateway.Multiplayer.IsServer)
                InitServer();
            if (!MyAPIGateway.Utilities.IsDedicated)
                InitClient();

            if (!_commandsRegistered)
            {
                foreach (ChatCommand command in GetAllChatCommands())
                    ChatCommandService.Register(command);
                _commandsRegistered = ChatCommandService.Init();
            }

            _isInitialized = true;

            if (_isServerRegistered)
                ServerLoad();

            if (_isClientRegistered)
                ClientLoad();

            ServerLogger.WriteInfo($"{ModName} is ready.");
            // Client is only `ready` after it has received the ClientConfigResponse. We log that in MessageClientConfig.
        }

        private void InitClient()
        {
            _isClientRegistered = true;
            ClientLogger.Init(ClientLogFileName, ClientLoggingLevel, false, 0); // comment this out if logging is not required for the Client.
            ClientLogger.WriteInfo($"{ModName} Client Log Started");
            ClientLogger.WriteInfo($"{ModName} Client Version {ModCommunicationVersion}");
            if (ClientLogger.IsActive)
                TextLogger.WriteGameLog($"##Mod## {ModName} Client Logging File: {ClientLogger.LogFile}");

            MyAPIGateway.Utilities.MessageEntered += ChatMessageEntered;

            if (MyAPIGateway.Multiplayer.MultiplayerActive && !_isServerRegistered) // if not the server, also need to register the messagehandler.
            {
                ClientLogger.WriteStart("RegisterMessageHandler");
                MyAPIGateway.Multiplayer.RegisterMessageHandler(ConnectionId, _messageHandler);
            }

            // Offline connections can be re-attempted quickly. Online games needs to wait longer.
            DelayedConnectionRequestTimer = new Timer(MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE ? 500 : 10000);
            DelayedConnectionRequestTimer.Elapsed += DelayedConnectionRequestTimer_Elapsed;
            DelayedConnectionRequestTimer.Start();

            // let the server know we are ready for connections
            MessageConnectionRequest.SendMessage(ModCommunicationVersion, PrivateCommunicationKey);

            ClientLogger.Flush();
        }

        internal void CancelClientConnection()
        {
            if (ClientConfig != null)
            {
                ClientLogger.WriteStart("Canceling further Connection Request.");
                if (DelayedConnectionRequestTimer != null)
                {
                    DelayedConnectionRequestTimer.Stop();
                    DelayedConnectionRequestTimer.Close();
                }
                _delayedConnectionRequest = false;
            }
        }

        private void InitServer()
        {
            _isServerRegistered = true;
            ServerLogger.Init(ServerLogFileName, ServerLoggingLevel, false, 0); // comment this out if logging is not required for the Server.
            ServerLogger.WriteInfo($"{ModName} Server Log Started");
            ServerLogger.WriteInfo($"{ModName} Server Version {ModCommunicationVersion}");
            if (ServerLogger.IsActive)
                TextLogger.WriteGameLog($"##Mod## {ModName} Server Logging File: {ServerLogger.LogFile}");

            ServerLogger.WriteStart("RegisterMessageHandler");
            MyAPIGateway.Multiplayer.RegisterMessageHandler(ConnectionId, _messageHandler);

            ServerLogger.Flush();
        }

        #endregion

        public sealed override void UpdateBeforeSimulation()
        {
            _frameCounter100++;
            _frameCounter1000++;

            if (!_isInitialized)
                return;

            if (_delayedConnectionRequest)
            {
                ClientLogger.WriteInfo("Delayed Connection Request");
                _delayedConnectionRequest = false;
                MessageConnectionRequest.SendMessage(ModCommunicationVersion, PrivateCommunicationKey);
            }

            UpdateBeforeFrame();
            ChatCommandService.UpdateBeforeSimulation();

            if (_frameCounter100 >= 100)
            {
                _frameCounter100 = 0;
                UpdateBeforeFrame100();
                ChatCommandService.UpdateBeforeSimulation100();
            }

            if (_frameCounter1000 >= 1000)
            {
                _frameCounter1000 = 0;
                UpdateBeforeFrame1000();
                ChatCommandService.UpdateBeforeSimulation1000();
            }
        }

        public sealed override void UpdateAfterSimulation()
        {
            UpdateAfterFrame();
        }

        #region detaching events

        protected sealed override void UnloadData()
        {
            //TextLogger.WriteGameLog($"####### {ModConfigurationConsts.ModName} UNLOAD");

            ClientLogger.WriteStop("Shutting down");
            ServerLogger.WriteStop("Shutting down");

            if (_isClientRegistered)
            {
                if (DelayedConnectionRequestTimer != null)
                {
                    DelayedConnectionRequestTimer.Stop();
                    DelayedConnectionRequestTimer.Close();
                }

                if (MyAPIGateway.Utilities != null)
                    MyAPIGateway.Utilities.MessageEntered -= ChatMessageEntered;

                if (!_isServerRegistered) // if not the server, also need to unregister the messagehandler.
                {
                    ClientLogger.WriteStop("UnregisterMessageHandler");
                    MyAPIGateway.Multiplayer.UnregisterMessageHandler(ConnectionId, _messageHandler);
                }

                ClientLogger.WriteInfo("Log Closed");
                ClientLogger.Terminate();
            }

            if (_isServerRegistered)
            {
                ServerLogger.WriteStop("UnregisterMessageHandler");
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(ConnectionId, _messageHandler);

                ServerLogger.WriteInfo("Log Closed");
                ServerLogger.Terminate();
            }

            if (_commandsRegistered)
                ChatCommandService.DisposeCommands();
        }

        public sealed override void SaveData()
        {
            if (_isServerRegistered)
            {
                ServerLogger.WriteStop("SaveData");
                ServerSave();
            }

            if (_isClientRegistered)
            {
                ClientLogger.WriteStop("SaveData");
                ClientSave();
            }
        }

        #endregion

        #region message processing

        private static void HandleMessage(byte[] message)
        {
            if (Instance.IsServerRegistered)
                Instance.ServerLogger.WriteVerbose("HandleMessage");
            if (Instance.IsClientRegistered)
                Instance.ClientLogger.WriteVerbose("HandleMessage");
            ConnectionHelper.ProcessData(message);
        }

        private void ChatMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (ChatCommandService.ProcessClientMessage(messageText))
                sendToOthers = false;
        }

        #endregion

        private void DelayedConnectionRequestTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (ClientConfig == null)
                _delayedConnectionRequest = true;
            else if (ClientConfig != null && DelayedConnectionRequestTimer != null)
            {
                DelayedConnectionRequestTimer.Stop();
                DelayedConnectionRequestTimer.Close();
            }
        }

    }
}