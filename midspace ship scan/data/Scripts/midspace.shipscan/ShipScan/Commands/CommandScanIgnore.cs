namespace midspace.shipscan
{
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class CommandScanIgnore : ChatCommand
    {
        public CommandScanIgnore()
            : base(ChatCommandSecurity.User, ChatCommandAccessibility.Client, "ignore", new[] { "/ignore" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            ScanSettingsEntity scanSettings = ScanDataManager.GetPlayerScanData(steamId);

            var ignoreList = new List<string>();
            if (scanSettings.IgnoreJunk) ignoreList.Add(MassCategory.Junk.ToString());
            if (scanSettings.IgnoreTiny) ignoreList.Add(MassCategory.Tiny.ToString());
            if (scanSettings.IgnoreSmall) ignoreList.Add(MassCategory.Small.ToString());
            if (scanSettings.IgnoreLarge) ignoreList.Add(MassCategory.Large.ToString());
            if (scanSettings.IgnoreHuge) ignoreList.Add(MassCategory.Huge.ToString());
            if (scanSettings.IgnoreEnormous) ignoreList.Add(MassCategory.Enormous.ToString());
            if (scanSettings.IgnoreRidiculous) ignoreList.Add(MassCategory.Ridiculous.ToString());

            var description = $@"/ignore <type> <state>
            Function: Ignores the specified mass type by allowing you switch it on or off.
              <type> must be one of ""Junk, Tiny, Small, Large, Huge, Enormous, Ridiculous"".
              <state> must be ""On"" or ""Off"".

            ie.
              /ignore Junk On
              /ignore Enormous Off
            Abbreviations can be used.
              /ignore ju 1
              /ignore en 0

            Currently Ignoring: {string.Join(", ", ignoreList)}

            ";

            MyAPIGateway.Utilities.ShowMissionScreen("/Ignore Help", null, " ", description, null, "OK");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var commands = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (commands.Length < 3)
            {
                Help(steamId, false);
                return true;
            }

            var categories = (IEnumerable<MassCategory>)Enum.GetValues(typeof(MassCategory));
            string massclass = null;

            foreach (var category in categories)
            {
                if (category.ToString().Equals(commands[1], StringComparison.InvariantCultureIgnoreCase))
                {
                    massclass = category.ToString();
                    break;
                }
            }

            if (massclass == null)
            {
                var matches = categories.Where(s => s.ToString().StartsWith(commands[1], StringComparison.InvariantCultureIgnoreCase)).Distinct().ToArray();
                if (matches.Length == 1)
                {
                    massclass = matches.FirstOrDefault().ToString();
                }
            }

            if (massclass == null)
            {
                Help(steamId, false);
                return true;
            }

            bool setIgnore;
            if (commands[2].TryWordParseBool(out setIgnore))
            {
                MessageSetIgnore.SendMessage(massclass, setIgnore);
                return true;
            }

            Help(steamId, false);
            return true;
        }
    }
}
