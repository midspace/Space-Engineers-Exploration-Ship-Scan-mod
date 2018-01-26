namespace midspace.shipscan
{
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

        // TODO: create an interface for MainChatCommandLogic and this to implement it.
        public override List<ChatCommand> GetAllChatCommands()
        {
            List<ChatCommand> commands = new List<ChatCommand>();
            // New command classes must be added in here.

            commands.Add(new CommandScan());
            commands.Add(new CommandScanIgnore());
            commands.Add(new CommandScanTrack());
            commands.Add(new CommandScanClear());
            return commands;
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
    }
}
