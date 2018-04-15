namespace MidSpace.ShipScan
{
    using Commands;
    using Entities;
    using SeModCore;
    using System.Collections.Generic;
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
    public class MyShipScanModLogic : MainChatCommandLogic
    {
        public ScanServerEntity ServerData;

        public override void InitModSettings(out int modCommunicationVersion, out ushort connectionId, out LogEventType clientLoggingLevel, out string clientLogFileName, out LogEventType serverLoggingLevel, out string serverLogFileName, out string modName, out string modTitle, out ulong[] experimentalCreatorList)
        {
            modCommunicationVersion = ShipScanConsts.ModCommunicationVersion;
            connectionId = ShipScanConsts.ConnectionId;
            clientLoggingLevel = ShipScanConsts.ClientLoggingLevel;
            clientLogFileName = ShipScanConsts.ClientLogFileName;
            serverLoggingLevel = ShipScanConsts.ServerLoggingLevel;
            serverLogFileName = ShipScanConsts.ServerLogFileName;
            modName = ShipScanConsts.ModName;
            modTitle = ShipScanConsts.ModTitle;
            experimentalCreatorList = ShipScanConsts.ExperimentalCreatorList;
        }

        // TODO: create an interface for MainChatCommandLogic and this to implement it.
        public override List<ChatCommand> GetAllChatCommands()
        {
            return new List<ChatCommand>
            {
                // New command classes must be added in here.
                new CommandScan(),
                new CommandScanIgnore(),
                new CommandScanTrack(),
                new CommandScanClear()
            };
        }

        public override void ServerLoad()
        {
            ServerData = ScanDataManager.LoadData();
        }

        public override void ServerSave()
        {
            if (ServerData != null)
            {
                ScanDataManager.SaveData(ServerData);
            }
        }

        public override ClientConfigBase GetConfig()
        {
            return MidSpace.ShipScan.ClientConfig.FetchClientResponse();
        }
    }
}
