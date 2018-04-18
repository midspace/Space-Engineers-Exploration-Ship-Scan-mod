// SE Mod Core V4.2
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

        private bool _calledFirstFrame;
        private bool _isInitialized;
        private bool _isClientRegistered;
        private bool _isServerRegistered;
        private readonly Action<byte[]> _clientMessageHandler = HandleClientMessage;
        private readonly Action<byte[]> _serverMessageHandler = HandleServerMessage;

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
        /// The is the Id which this mod registers the server side for receiving messages through SE. 
        /// </summary>
        internal ushort ServerConnectionId;

        /// <summary>
        /// The is the Id which this mod registers the client side for receiving messages through SE. 
        /// </summary>
        internal ushort ClientConnectionId;

        internal LogEventType ClientLoggingLevel;
        internal string ClientLogFileName;

        internal LogEventType ServerLoggingLevel;
        internal string ServerLogFileName;

        /// <summary>
        ///  Used in logs, and might be used for filenames. Shouldn't have unfriendly characters.
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

        internal static MainChatCommandLogic Instance;
        //internal static IChatCommandLogic Instance;

        #endregion

        #region IChatCommandLogic fields

        public abstract List<ChatCommand> GetAllChatCommands();
        public abstract ClientConfigBase GetConfig();

        /// <summary>
        /// This initializes most of the settings requied for your mod to run.
        /// </summary>
        /// <param name="modCommunicationVersion">This is used to indicate the base communication version.</param>
        /// <param name="serverConnectionId"><para> The is the Id which this mod registers the server side for receiving messages through SE.</para>
        /// <para>It should be unique in the world. If another mod shares the same number and both mods are loaded, you will have problems.</para>
        /// <para>This cannot be the same as the <see cref="clientConnectionId"/>.</para></param>
        /// <param name="serverLoggingLevel">The level of logging you which to do to the automatic logging file on the server.</param>
        /// <param name="serverLogFileName">The name of the server logging file including extension. This will be written to the Storage path on the server.</param>
        /// <param name="clientConnectionId"><para>The is the Id which this mod registers the client side for receiving messages through SE.</para>
        /// <para>It should be unique in the world. If another mod shares the same number and both mods are loaded, you will have problems.</para>
        /// <para>This cannot be the same as the <see cref="serverConnectionId"/>.</para></param>
        /// <param name="clientLoggingLevel">The level of logging you which to do to the automatic logging file on the client.</param>
        /// <param name="clientLogFileName">The name of the client logging file including extension. This will be written to the Storage path on the client.</param>
        /// <param name="modName">Used in logs, and might be used for filenames. Shouldn't have unfriendly characters.</param>
        /// <param name="modTitle">Used for display boxes and friendly information.</param>
        /// <param name="experimentalCreatorList"><para>Hardcoded list of SteamIDs, for testing stuff that hasn't been released to the public yet.</para>
        /// <para>It shouldn't be used to hide functionality away in the published mod, simply prevent incomplete or broken stuff from been used until it is ready.</para></param>
        public abstract void InitModSettings(out int modCommunicationVersion, out ushort serverConnectionId, out LogEventType serverLoggingLevel, out string serverLogFileName, out ushort clientConnectionId, out LogEventType clientLoggingLevel, out string clientLogFileName, out string modName, out string modTitle, out ulong[] experimentalCreatorList);

        /// <summary>
        /// This is called on first frame on the Client that the mod is ready to run stuff.
        /// </summary>
        public virtual void FirstFrame() { }

        public virtual void UpdateBeforeFrame() { }
        public virtual void UpdateBeforeFrame100() { }
        public virtual void UpdateBeforeFrame1000() { }
        public virtual void UpdateAfterFrame() { }

        public virtual void ClientLoad() { }
        public virtual void ClientSave() { }

        /// <summary>
        /// This is run when the Client unloads. This is useful for detaching from event handlers that you don't want hanging around.
        /// </summary>
        public virtual void ClientUnload() { }
        public virtual void ServerLoad() { }
        public virtual void ServerSave() { }

        /// <summary>
        /// This is run when the Server unloads. This is useful for detaching from event handlers that you don't want hanging around.
        /// </summary>
        public virtual void ServerUnload() { }


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
            InitModSettings(out ModCommunicationVersion, out ServerConnectionId, out ServerLoggingLevel, out ServerLogFileName, out ClientConnectionId, out ClientLoggingLevel, out ClientLogFileName, out ModName, out ModTitle, out ExperimentalCreatorList);
            Guard.IsNotZero(ModCommunicationVersion, "SEModCore: InitModSettings must supply ModCommunicationVersion.");
            Guard.IsNotZero(ServerConnectionId, "SEModCore: InitModSettings must supply ServerConnectionId.");
            Guard.IsNotEmpty(ServerLogFileName, "SEModCore: InitModSettings must supply ServerLogFileName.");
            Guard.IsNotZero(ClientConnectionId, "SEModCore: InitModSettings must supply ClientConnectionId.");
            Guard.IsNotEmpty(ClientLogFileName, "SEModCore: InitModSettings must supply ClientLogFileName.");
            Guard.IsNotEmpty(ModName, "SEModCore: InitModSettings must supply ModName.");
            Guard.IsNotEmpty(ModTitle, "SEModCore: InitModSettings must supply ModTitle.");
            Guard.IsNotEqual(ServerConnectionId, ClientConnectionId, $"SEModCore: ServerConnectionId [{ServerConnectionId}] and ClientConnectionId [{ClientConnectionId}] must not be the same.");


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

            ServerLogger.WriteInfo($"{ModName} Server is ready.");
            // Client is only `ready` after it has received the ClientConfigResponse. We log that in MessageClientConfig.
        }

        private void InitClient()
        {
            _isClientRegistered = true;
            ClientLogger.Init(ClientLogFileName, ClientLoggingLevel, false, MyAPIGateway.Session.OnlineMode.Equals(MyOnlineModeEnum.OFFLINE) ? 0 : 5); // comment this out if logging is not required for the Client.
            ClientLogger.WriteInfo($"{ModName} Client Log Started");
            ClientLogger.WriteInfo($"{ModName} Client Version {ModCommunicationVersion}");
            //if (ClientLogger.IsActive) // TODO: determine is this is needed any more?
                TextLogger.WriteGameLog($"##Mod## {ModName} Client Logging File: {ClientLogger.LogFile}");

            MyAPIGateway.Utilities.MessageEntered += ChatMessageEntered;

            ClientLogger.WriteStart("RegisterMessageHandler");
            MyAPIGateway.Multiplayer.RegisterMessageHandler(ClientConnectionId, _clientMessageHandler);

            // Offline connections can be re-attempted quickly. Online games needs to wait longer.
            DelayedConnectionRequestTimer = new Timer(MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE ? 500 : 10000);
            DelayedConnectionRequestTimer.Elapsed += DelayedConnectionRequestTimer_Elapsed;
            DelayedConnectionRequestTimer.Start();

            // let the server know we are ready for connections
            PullConnectionRequest.SendMessage(ModCommunicationVersion, PrivateCommunicationKey);

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
            ServerLogger.Init(ServerLogFileName, ServerLoggingLevel, false, MyAPIGateway.Session.OnlineMode.Equals(MyOnlineModeEnum.OFFLINE) ? 0 : 5); // comment this out if logging is not required for the Server.
            ServerLogger.WriteInfo($"{ModName} Server Log Started");
            ServerLogger.WriteInfo($"{ModName} Server Version {ModCommunicationVersion}");
            //if (ServerLogger.IsActive) //if (ClientLogger.IsActive) // TODO: determine is this is needed any more?
                TextLogger.WriteGameLog($"##Mod## {ModName} Server Logging File: {ServerLogger.LogFile}");

            ServerLogger.WriteStart("RegisterMessageHandler");
            MyAPIGateway.Multiplayer.RegisterMessageHandler(ServerConnectionId, _serverMessageHandler);

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
                PullConnectionRequest.SendMessage(ModCommunicationVersion, PrivateCommunicationKey);
            }

            if (!_calledFirstFrame && IsConnected)
            {
                _calledFirstFrame = true;
                FirstFrame();
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

                ClientLogger.WriteStop("UnregisterMessageHandler");
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(ClientConnectionId, _clientMessageHandler);

                ClientUnload();

                ClientLogger.WriteInfo("Log Closed");
                ClientLogger.Terminate();
            }

            if (_isServerRegistered)
            {
                ServerLogger.WriteStop("UnregisterMessageHandler");
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(ServerConnectionId, _serverMessageHandler);

                ServerUnload();

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

        private static void HandleServerMessage(byte[] rawData)
        {
            Instance.ServerLogger.WriteStart("HandleMessage Start Deserialization");
            PullMessageBase message;

            try
            {
                message = MyAPIGateway.Utilities.SerializeFromBinary<PullMessageBase>(rawData);
            }
            catch (Exception ex)
            {
                Instance.ServerLogger.WriteException(ex, $"Message cannot Deserialize. Message length: {rawData.Length}");
                if (ex.Message.IndexOf("No parameterless constructor", StringComparison.OrdinalIgnoreCase) >= 0)
                    Instance.ServerLogger.WriteError("Make sure that any PullMessage that you defined, both inherits from PullMessageBase, and that PullMessageBase has a ProtoIncludeAttribute set for it.");

                return;
            }

            Instance.ServerLogger.WriteStop("HandleMessage End Message Deserialization");

            if (message != null)
            {
                try
                {
                    message.InvokeProcessing();
                }
                catch (Exception ex)
                {
                    Instance.ServerLogger.WriteException(ex, $"Processing message exception. Side: {message.Side}");
                }
            }
        }

        private static void HandleClientMessage(byte[] rawData)
        {
            Instance.ClientLogger.WriteStart("HandleMessage Start Message Deserialization");
            PushMessageBase message;

            try
            {
                message = MyAPIGateway.Utilities.SerializeFromBinary<PushMessageBase>(rawData);
            }
            catch (Exception ex)
            {
                Instance.ClientLogger.WriteException(ex, $"Message cannot Deserialize. Message length: {rawData.Length}");
                if (ex.Message.IndexOf("No parameterless constructor", StringComparison.OrdinalIgnoreCase) >= 0)
                    Instance.ServerLogger.WriteError("Make sure that any PushMessage that you defined, both inherits from PushMessageBase, and that PushMessageBase has a ProtoIncludeAttribute set for it.");
                return;
            }

            Instance.ClientLogger.WriteStop("HandleMessage End Message Deserialization");

            if (message != null)
            {
                try
                {
                    message.InvokeProcessing();
                }
                catch (Exception ex)
                {
                    Instance.ClientLogger.WriteException(ex, $"Processing message exception. Side: {message.Side}");
                }
            }
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