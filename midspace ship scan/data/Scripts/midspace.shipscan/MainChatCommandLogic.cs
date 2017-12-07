namespace midspace.shipscan
{
    using Sandbox.Common;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Timers;
    using VRage.Common.Utils;
    using VRage.Game;
    using VRage.Game.Components;
    using VRage.Library.Utils;

    /// <summary>
    /// Adds special chat commands, allowing the player to get their position, date, time, change their location on the map.
    /// Author: Midspace. AKA Screaming Angels.
    /// 
    /// The main Steam workshop link to this mod is:
    /// http://steamcommunity.com/sharedfiles/filedetails/?id=364731781
    /// 
    /// My other Steam workshop items:
    /// http://steamcommunity.com/id/ScreamingAngels/myworkshopfiles/?appid=244850
    /// </summary>
    /// <example>
    /// To use, simply open the chat window, and enter "/command", where command is one of the specified.
    /// Enter "/help" or "/help command" for more detail on individual commands.
    /// Chat commands do not have to start with "/". This model allows practically any text to become a command.
    /// Each ChatCommand can determine what it's own allowable command is.
    /// </example>
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MainChatCommandLogic : MySessionComponentBase
    {
        #region fields

        private bool _isInitialized;
        private bool _isClientRegistered;
        private bool _isServerRegistered;
        private readonly Action<byte[]> _messageHandler = new Action<byte[]>(HandleMessage);
        public static MainChatCommandLogic Instance;
        public TextLogger ServerLogger = new TextLogger(); // This is a dummy logger until Init() is called.
        public TextLogger ClientLogger = new TextLogger(); // This is a dummy logger until Init() is called.
        public Timer DelayedConnectionRequestTimer;
        private bool _delayedConnectionRequest;
        private bool _commandsRegistered;
        private int frameCounter100;
        private int frameCounter1000;

        /// <summary>
        /// This will temporarily store Client side details while the client is connected.
        /// It will receive periodic updates from the server.
        /// </summary>
        public ClientConfig ClientConfig = null;

        public ScanSettings Settings;

        internal long PrivateCommunicationKey;

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
                // New command classes must be added in here.
                ChatCommandService.Register(new CommandScan());
                ChatCommandService.Register(new CommandScanIgnore());
                ChatCommandService.Register(new CommandScanTrack());
                ChatCommandService.Register(new CommandScanClear());
                _commandsRegistered = ChatCommandService.Init();
            }

            _isInitialized = true; // Set this first to block any other calls from UpdateAfterSimulation().

            if (_isClientRegistered)
            {
                Settings = new ScanSettings();
                Settings.Load();
            }
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
            frameCounter100++;
            frameCounter1000++;

            if (!_isInitialized)
                return;

            if (_delayedConnectionRequest)
            {
                ClientLogger.WriteInfo("Delayed Connection Request");
                _delayedConnectionRequest = false;
                MessageConnectionRequest.SendMessage(ModConfigurationConsts.ModCommunicationVersion, PrivateCommunicationKey);
            }

            ChatCommandService.UpdateBeforeSimulation();

            if (frameCounter100 >= 100)
            {
                frameCounter100 = 0;
                ChatCommandService.UpdateBeforeSimulation100();
            }

            if (frameCounter1000 >= 1000)
            {
                frameCounter1000 = 0;
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
            ClientLogger.WriteStop("SaveData");
            ServerLogger.WriteStop("SaveData");

            if (_isServerRegistered)
            {
                // Only save the speed back to the server duruing world save.
                //var xmlValue = MyAPIGateway.Utilities.SerializeToXML(EnvironmentComponent);
                //MyAPIGateway.Utilities.SetVariable("MidspaceEnvironmentComponent", xmlValue);
            }

            if (Settings != null)
            {
                Settings.Save();
            }
        }

        #endregion

        #region message processing

        private static void HandleMessage(byte[] message)
        {
            Instance.ServerLogger.WriteVerbose("HandleMessage");
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