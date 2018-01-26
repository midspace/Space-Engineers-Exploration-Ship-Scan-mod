// SE Mod Core V4.0
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

namespace midspace.shipscan
{
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
        public ClientConfig ClientConfig = null;

        internal long PrivateCommunicationKey;

        #endregion

        #region IChatCommandLogic fields

        internal static MainChatCommandLogic Instance;
        //internal static IChatCommandLogic Instance;

        public abstract List<ChatCommand> GetAllChatCommands();

        public virtual void ClientLoad() { }
        public virtual void ClientSave() { }
        public virtual void ServerLoad() { }
        public virtual void ServerSave() { }

        internal bool ResponseReceived { get; set; }
        internal bool IsConnected { get; set; }

        // This is a dummy logger until Init() is called.
        public bool IsClientRegistered => _isClientRegistered;
        // This is a dummy logger until Init() is called.
        public bool IsServerRegistered => _isServerRegistered;

        public TextLogger ServerLogger { get; } = new TextLogger();

        public TextLogger ClientLogger { get; } = new TextLogger();

        #endregion

        #region constructor

        public MainChatCommandLogic()
        {
            //TextLogger.WriteGameLog($"####### {ModConfigurationConsts.ModName} CTOR");
            Instance = this;
            PrivateCommunicationKey = MyRandom.Instance.NextLong();
        }

        #endregion


        #region attaching events and wiring up

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
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
        }

        private void InitClient()
        {
            _isClientRegistered = true;
            ClientLogger.Init(ModConfigurationConsts.ClientLogFileName, false, 0); // comment this out if logging is not required for the Client.
            ClientLogger.WriteStart($"{ModConfigurationConsts.ModName} Client Log Started");
            ClientLogger.WriteInfo($"{ModConfigurationConsts.ModName} Client Version {ModConfigurationConsts.ModCommunicationVersion}");

            MyAPIGateway.Utilities.MessageEntered += ChatMessageEntered;

            if (MyAPIGateway.Multiplayer.MultiplayerActive && !_isServerRegistered) // if not the server, also need to register the messagehandler.
            {
                ClientLogger.WriteStart("RegisterMessageHandler");
                MyAPIGateway.Multiplayer.RegisterMessageHandler(ModConfigurationConsts.ConnectionId, _messageHandler);
            }

            DelayedConnectionRequestTimer = new Timer(10000);
            DelayedConnectionRequestTimer.Elapsed += DelayedConnectionRequestTimer_Elapsed;
            DelayedConnectionRequestTimer.Start();

            // let the server know we are ready for connections
            MessageConnectionRequest.SendMessage(ModConfigurationConsts.ModCommunicationVersion, PrivateCommunicationKey);

            ClientLogger.Flush();
        }

        internal void CancelClientConnection()
        {
            if (ClientConfig != null)
            {
                if (DelayedConnectionRequestTimer != null)
                {
                    DelayedConnectionRequestTimer.Stop();
                    DelayedConnectionRequestTimer.Close();
                }
            }
        }

        private void InitServer()
        {
            _isServerRegistered = true;
            ServerLogger.Init(ModConfigurationConsts.ServerLogFileName, false, 0); // comment this out if logging is not required for the Server.
            ServerLogger.WriteStart($"{ModConfigurationConsts.ModName} Server Log Started");
            ServerLogger.WriteInfo($"{ModConfigurationConsts.ModName} Server Version {ModConfigurationConsts.ModCommunicationVersion}");
            if (ServerLogger.IsActive)
                TextLogger.WriteGameLog($"##Mod## {ModConfigurationConsts.ModName} Server Logging File: {ServerLogger.LogFile}");

            ServerLogger.WriteStart("RegisterMessageHandler");
            MyAPIGateway.Multiplayer.RegisterMessageHandler(ModConfigurationConsts.ConnectionId, _messageHandler);

            ServerLogger.Flush();
        }

        #endregion

        public override void UpdateBeforeSimulation()
        {
            _frameCounter100++;
            _frameCounter1000++;

            if (!_isInitialized)
                return;

            if (_delayedConnectionRequest)
            {
                ClientLogger.WriteInfo("Delayed Connection Request");
                _delayedConnectionRequest = false;
                MessageConnectionRequest.SendMessage(ModConfigurationConsts.ModCommunicationVersion, PrivateCommunicationKey);
            }

            ChatCommandService.UpdateBeforeSimulation();

            if (_frameCounter100 >= 100)
            {
                _frameCounter100 = 0;
                ChatCommandService.UpdateBeforeSimulation100();
            }

            if (_frameCounter1000 >= 1000)
            {
                _frameCounter1000 = 0;
                ChatCommandService.UpdateBeforeSimulation1000();
            }
        }

        #region detaching events

        protected override void UnloadData()
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
                    MyAPIGateway.Multiplayer.UnregisterMessageHandler(ModConfigurationConsts.ConnectionId, _messageHandler);
                }

                ClientLogger.WriteStop("Log Closed");
                ClientLogger.Terminate();
            }

            if (_isServerRegistered)
            {
                ServerLogger.WriteStop("UnregisterMessageHandler");
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(ModConfigurationConsts.ConnectionId, _messageHandler);

                ServerLogger.WriteStop("Log Closed");
                ServerLogger.Terminate();
            }
            
            if (_commandsRegistered)
                ChatCommandService.DisposeCommands();
        }

        public override void SaveData()
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