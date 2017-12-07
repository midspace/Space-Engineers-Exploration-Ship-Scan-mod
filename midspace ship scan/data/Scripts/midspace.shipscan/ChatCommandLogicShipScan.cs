namespace midspace.shipscan
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;
    using Sandbox.Common;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRage.Common.Utils;
    using VRage.Game.Components;

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
    public class ChatCommandLogicShipScan : MySessionComponentBase
    {
        #region fields

        private bool _isInitilized;
        private Timer _timer;
        private bool _1000MsTimerElapsed;

        #endregion

        public static ChatCommandLogicShipScan Instance;
        public ScanSettings Settings;

        public ChatCommandLogicShipScan()
        {
            Instance = this;
        }

        #region attaching events and wiring up

        public override void UpdateBeforeSimulation()
        {
            // This needs to wait until the MyAPIGateway.Session.Player is created, as running on a Dedicated server can cause issues.
            // It would be nicer to just read a property that indicates this is a dedicated server, and simply return.
            if (!_isInitilized && MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null)
            {
                Settings = new ScanSettings();
                Init();
                Settings.Load();
            }

            if (MyAPIGateway.Utilities != null && MyAPIGateway.Multiplayer != null
                && MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Multiplayer.IsServer)
                return;

            if (_1000MsTimerElapsed)
            {
                _1000MsTimerElapsed = false;
                ChatCommandService.UpdateBeforeSimulation1000();
            }

            ChatCommandService.UpdateBeforeSimulation();
        }

        public override void SaveData()
        {
            if (Settings != null)
            {
                Settings.Save();
            }
        }

        protected override void UnloadData()
        {
            DetachEvents();
        }

        #endregion

        private void Init()
        {
            _isInitilized = true; // Set this first to block any other calls from UpdateBeforeSimulation().
            MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;

            // New command classes must be added in here.

            ChatCommandService.Register(new CommandScan());
            ChatCommandService.Register(new CommandScanIgnore());
            ChatCommandService.Register(new CommandScanTrack());
            ChatCommandService.Register(new CommandScanClear());

            _timer = new Timer(1000);
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
            // Attach any other events here.

            ChatCommandService.Init();
        }

        #region detaching events

        private void DetachEvents()
        {
            if (MyAPIGateway.Utilities != null && MyAPIGateway.Multiplayer != null &&
                MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Utilities.IsDedicated)
                return;

            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= TimerOnElapsed;
            }
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            // Run timed events that do not affect Threading in game.
            _1000MsTimerElapsed = true;

            // DO NOT SET ANY IN GAME API CALLS HERE. AT ALL!
        }

        #endregion

        #region message processing

        private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {
            if (ChatCommandService.ProcessMessage(messageText))
            {
                sendToOthers = false;
            }
        }

        #endregion
    }
}