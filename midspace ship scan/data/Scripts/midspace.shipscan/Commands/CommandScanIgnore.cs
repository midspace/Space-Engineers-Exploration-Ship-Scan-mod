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
            var config = MainChatCommandLogic.Instance.Settings.Config;
            var ignoreList = new List<string>();
            if (config.IgnoreJunk) ignoreList.Add(MassCategory.Junk.ToString());
            if (config.IgnoreTiny) ignoreList.Add(MassCategory.Tiny.ToString());
            if (config.IgnoreSmall) ignoreList.Add(MassCategory.Small.ToString());
            if (config.IgnoreLarge) ignoreList.Add(MassCategory.Large.ToString());
            if (config.IgnoreHuge) ignoreList.Add(MassCategory.Huge.ToString());
            if (config.IgnoreEnormous) ignoreList.Add(MassCategory.Enormous.ToString());
            if (config.IgnoreRidiculous) ignoreList.Add(MassCategory.Ridiculous.ToString());

            var description =
string.Format(@"/ignore <type> <state>
Function: Ignores the specified mass type by allowing you switch it on or off.
  <type> must be one of ""Junk, Tiny, Small, Large, Huge, Enormous, Ridiculous"".
  <state> must be ""On"" or ""Off"".

ie.
  /ignore Junk On
  /ignore Enormous Off
Abbreviations can be used.
  /ignore ju 1
  /ignore en 0

Currently Ignoring: {0}

", String.Join(", ", ignoreList));

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

            bool? setIgnore = null;

            if (commands[2].Equals("on", StringComparison.InvariantCultureIgnoreCase) || commands[2].Equals("1", StringComparison.InvariantCultureIgnoreCase))
            {
                setIgnore = true;
            }

            if (commands[2].Equals("off", StringComparison.InvariantCultureIgnoreCase) || commands[2].Equals("0", StringComparison.InvariantCultureIgnoreCase))
            {
                setIgnore = false;
            }

            if (!setIgnore.HasValue)
            {
                Help(steamId, false);
                return true;
            }

            if (massclass == MassCategory.Junk.ToString())
                MainChatCommandLogic.Instance.Settings.Config.IgnoreJunk = setIgnore.Value;
            if (massclass == MassCategory.Tiny.ToString())
                MainChatCommandLogic.Instance.Settings.Config.IgnoreTiny = setIgnore.Value;
            if (massclass == MassCategory.Small.ToString())
                MainChatCommandLogic.Instance.Settings.Config.IgnoreSmall = setIgnore.Value;
            if (massclass == MassCategory.Large.ToString())
                MainChatCommandLogic.Instance.Settings.Config.IgnoreLarge = setIgnore.Value;
            if (massclass == MassCategory.Huge.ToString())
                MainChatCommandLogic.Instance.Settings.Config.IgnoreHuge = setIgnore.Value;
            if (massclass == MassCategory.Enormous.ToString())
                MainChatCommandLogic.Instance.Settings.Config.IgnoreEnormous = setIgnore.Value;
            if (massclass == MassCategory.Ridiculous.ToString())
                MainChatCommandLogic.Instance.Settings.Config.IgnoreRidiculous = setIgnore.Value;

            MyAPIGateway.Utilities.ShowMessage(string.Format("Ignore {0}", massclass), setIgnore.Value ? "On" : "Off");

            return true;
        }
    }
}
