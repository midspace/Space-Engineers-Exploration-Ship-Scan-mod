namespace MidSpace.ShipScan.Commands
{
    using Messages;
    using Sandbox.ModAPI;
    using SeModCore;
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using VRage.Game.ModAPI;

    public class CommandScan : ChatCommand
    {
        public CommandScan()
            : base(ChatCommandSecurity.User, ChatCommandAccessibility.Client, "scan", new[] { "/scan", "/scan2", "/scan3" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.SendMessage(steamId, "/scan", "Scans for derelict ships using ship antenna.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            IMyPlayer player = MyAPIGateway.Session.Player;

            decimal minRange;
            var match = Regex.Match(messageText, @"/scan\s{1,}(?<MINRANGE>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                minRange = decimal.Parse(match.Groups["MINRANGE"].Value, CultureInfo.InvariantCulture);
                if (minRange <= 0) minRange = 0;
                PullInitiateScan.SendMessage(minRange, ScanType.MissionScreen, player.Controller.ControlledEntity.Entity.WorldMatrix, player.Controller.ControlledEntity.Entity.EntityId);
                return true;
            }

            match = Regex.Match(messageText, @"/scan2\s{1,}(?<MINRANGE>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                minRange = decimal.Parse(match.Groups["MINRANGE"].Value, CultureInfo.InvariantCulture);
                if (minRange <= 0) minRange = 0;
                PullInitiateScan.SendMessage(minRange, ScanType.ChatConsole, player.Controller.ControlledEntity.Entity.WorldMatrix, player.Controller.ControlledEntity.Entity.EntityId);
                return true;
            }

            match = Regex.Match(messageText, @"/scan3\s{1,}(?<MINRANGE>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                minRange = decimal.Parse(match.Groups["MINRANGE"].Value, CultureInfo.InvariantCulture);
                if (minRange <= 0) minRange = 0;
                PullInitiateScan.SendMessage(minRange, ScanType.GpsCoordinates, player.Controller.ControlledEntity.Entity.WorldMatrix, player.Controller.ControlledEntity.Entity.EntityId);
                return true;
            }

            if (messageText.Equals("/scan", StringComparison.InvariantCultureIgnoreCase))
            {
                PullInitiateScan.SendMessage(0, ScanType.MissionScreen, player.Controller.ControlledEntity.Entity.WorldMatrix, player.Controller.ControlledEntity.Entity.EntityId);
                return true;
            }
            if (messageText.Equals("/scan2", StringComparison.InvariantCultureIgnoreCase))
            {
                PullInitiateScan.SendMessage(0, ScanType.ChatConsole, player.Controller.ControlledEntity.Entity.WorldMatrix, player.Controller.ControlledEntity.Entity.EntityId);
                return true;
            }
            if (messageText.Equals("/scan3", StringComparison.InvariantCultureIgnoreCase))
            {
                PullInitiateScan.SendMessage(0, ScanType.GpsCoordinates, player.Controller.ControlledEntity.Entity.WorldMatrix, player.Controller.ControlledEntity.Entity.EntityId);
                return true;
            }

            return false;
        }
    }
}
