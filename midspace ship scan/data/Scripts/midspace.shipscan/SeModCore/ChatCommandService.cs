namespace MidSpace.ShipScan.SeModCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Messages;
    using Sandbox.ModAPI;
    using VRage.Game;

    /// <summary>
    /// The Chat command service does most of the heavy work in organising and processing the ChatCommands.
    /// </summary>
    internal static class ChatCommandService
    {
        #region fields and properties

        private static readonly Dictionary<string, ChatCommand> Commands = new Dictionary<string, ChatCommand>();
        private static bool _isInitialized;
        public static uint UserSecurity { get; set; }

        #endregion

        #region methods

        /// <summary>
        /// Initilizes the Service, fetching the security level of the user, and 
        /// instructing the <see cref="MainChatCommandLogic"/> that it is ready to process chat commands.
        /// </summary>
        public static bool Init()
        {
            var session = MyAPIGateway.Session;
            UserSecurity = ChatCommandSecurity.User;

            if (MyAPIGateway.Multiplayer.IsServer || session.OnlineMode.Equals(MyOnlineModeEnum.OFFLINE)) // DS and Host.
            //if (MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Utilities.IsDedicated)
            {
                UserSecurity = ChatCommandSecurity.Admin;
                //_isInitialized = true;
                //return _isInitialized;
            }

            //// Only set this in Single Player, in Multi Player we need to wait until the server sends us our level. 
            //// On Local Server this will be read in during the creation of the server cfg.
            //if (session.Player.IsAdmin() && session.OnlineMode.Equals(MyOnlineModeEnum.OFFLINE))
            //if (session.Player.IsAdmin())
            //    _userSecurity = ChatCommandSecurity.Admin;

            _isInitialized = true;
            return _isInitialized;
        }

        /// <summary>
        /// Register the specified ChatCommand.
        /// Commands can only be registered once.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="chatCommand"></param>
        public static void Register<T>(T chatCommand) where T : ChatCommand
        {
            // An exception is thrown in here at this time on purpose.
            // As this will occur during the loading of the World, instead of during gameplay.
            // This is to prevent coders to adding duplicate ChatCommands.

            if (Commands.Any(c => c.GetType() == typeof(T)) || Commands.ContainsKey(chatCommand.Name))
                throw new Exception($"ChatCommand Type {typeof(T)} is already registered");

            foreach (string command in chatCommand.Commands)
                if (Commands.Any(pair => pair.Value.Commands.Any(s => s.Equals(command))))
                    throw new Exception($"ChatCommand '{command}' already registered");

            Commands.Add(chatCommand.Name, chatCommand);
        }

        public static void DisposeCommands()
        {
            foreach (ChatCommand command in Commands.Values)
                command.Dispose();
        }

        /// <summary>
        /// Returns the list of chat commands that the only a person with standard User security can use.
        /// </summary>
        /// <returns></returns>
        public static string[] GetUserListCommands(ulong steamId)
        {
            return Commands.Where(c => HasRight(steamId, c.Value) && c.Value.Security == ChatCommandSecurity.User).Select(c => c.Key).ToArray();
        }

        public static string[] GetNonUserListCommands(ulong steamId)
        {
            return Commands.Where(c => HasRight(steamId, c.Value) && c.Value.Security > ChatCommandSecurity.User).Select(c => c.Key).ToArray();
        }

        public static bool Help(ulong steamId, string commandName, bool brief)
        {
            foreach (var command in Commands.Where(command => HasRight(steamId, command.Value) && command.Key.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
            {
                command.Value.Help(steamId, brief);
                return true;
            }

            return false;
        }

        /// <summary>
        /// This will use _commandShortcuts dictionary to only run the specific ChatCommands that has the specified command text registered.
        /// </summary>
        /// <param name="messageText"></param>
        /// <returns>Returns true if a valid command was found and successfuly invoked.</returns>
        public static bool ProcessClientMessage(string messageText)
        {
            if (!_isInitialized || string.IsNullOrEmpty(messageText))
                return false;

            var commands = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (commands.Length == 0)
                return false;

            var comandList = Commands.Where(k => k.Value.Commands.Any(a => a.Equals(commands[0], StringComparison.InvariantCultureIgnoreCase)));
            foreach (KeyValuePair<string, ChatCommand> command in comandList)
            {
                //if (MainChatCommandLogic.Instance.BlockCommandExecution)
                //{
                //    MyAPIGateway.Utilities.ShowMessage("Permission", "Loading permissions... Please try again later.");
                //    return true;
                //}

                if (command.Value.Security == uint.MaxValue)
                {
                    // this command has been disabled from use.
                    return false;
                }

                if (!HasRight(MyAPIGateway.Session.Player.SteamUserId, command.Value))
                {
                    MyAPIGateway.Utilities.ShowMessage("Permission", "You do not have the permission to use this command.");
                    return true;
                }

                if (command.Value.HasFlag(ChatCommandAccessibility.SingleplayerOnly) && MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE)
                {
                    MyAPIGateway.Utilities.ShowMessage("Command Service", "Command disabled in online mode.");
                    return true;
                }

                if (command.Value.HasFlag(ChatCommandAccessibility.MultiplayerOnly) && MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE)
                {
                    MyAPIGateway.Utilities.ShowMessage("Command Service", "Command disabled in offline mode.");
                    return true;
                }

                //if (command.Value.HasFlag(ChatCommandAccessibility.Server))
                //{
                //    // Send message to server to process.
                //    ConnectionHelper.SendMessageToServer(new MessageChatCommand()
                //    {
                //        PlayerId = MyAPIGateway.Session.Player.IdentityId,
                //        TextCommand = messageText
                //    });
                //    return true;
                //}
                if ((command.Value.HasFlag(ChatCommandAccessibility.Server) && MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE) ||
                    (command.Value.HasFlag(ChatCommandAccessibility.Server) && MyAPIGateway.Multiplayer.IsServer) ||
                    command.Value.HasFlag(ChatCommandAccessibility.Client))
                {
                    //MyAPIGateway.Utilities.ShowMessage("CHECK", "Command Client: {0}", command.Value.Flag);
                    try
                    {
                        if (!MainChatCommandLogic.Instance.IsConnected)
                        {
                            MyAPIGateway.Utilities.ShowMessage("Please wait", $"Cannot execute command yet: {messageText}");
                            return true;
                        }

                        if (command.Value.Invoke(MyAPIGateway.Session.Player.SteamUserId, MyAPIGateway.Session.Player.IdentityId, messageText))
                            return true;

                        MyAPIGateway.Utilities.ShowMessage("Command failed", string.Format("Execution of command {0} failed. Use '/help {0}' for receiving a detailed instruction.", command.Value.Name));
                        command.Value.Help(0, true);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MainChatCommandLogic.Instance.ClientLogger.WriteException(ex, $"Occurred attempting to run {command.Value.GetType().Name} '{command.Key}' ");

                        // Exception handling to prevent any crash in the ChatCommand's reaching the user.
                        // Additional information for developers
                        if (MyAPIGateway.Session.Player.IsExperimentalCreator())
                        {
                            MyAPIGateway.Utilities.ShowMissionScreen($"Error in {command.Value.Name}", "Input: ", messageText, ex.ToString());
                            TextLogger.WriteGameLog($"##Mod## {MainChatCommandLogic.Instance.ModName} Exception caught. Message: {ex}");
                            continue;
                        }

                        var message = ex.Message.Replace("\r", " ").Replace("\n", " ");
                        message = message.Substring(0, Math.Min(message.Length, 50));
                        MyAPIGateway.Utilities.ShowMessage("Error", "Occurred attempting to run {0} '{1}'.\r\n{2}", command.Value.GetType().Name, command.Key, message);
                    }
                }
                else if (command.Value.HasFlag(ChatCommandAccessibility.Server))
                {
                    //MyAPIGateway.Utilities.ShowMessage("CHECK", "Command Server: {0}", command.Value.Flag);

                    // Send message to server to process.
                    PullChatCommand.SendMessage(MyAPIGateway.Session.Player.IdentityId, messageText);
                    return true;
                }
                else
                {
                    MyAPIGateway.Utilities.ShowMessage("Command", "Has not been correctly registered as either Client or Server.");
                }
            }

            return false;
        }

        public static bool ProcessServerMessage(ulong steamId, long playerId, string messageText)
        {
            //MyAPIGateway.Utilities.SendMessage(steamId, "CHECK", "ProcessServerMessage");

            if (!_isInitialized || string.IsNullOrEmpty(messageText))
            {
                //MyAPIGateway.Utilities.SendMessage(steamId, "CHECK", "ProcessServerMessage failed: not Initialized.");
                return false;
            }

            var commands = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (commands.Length == 0)
            {
                //MyAPIGateway.Utilities.SendMessage(steamId, "CHECK", "ProcessServerMessage failed: no commands.");
                return false;
            }

            var comandList = Commands.Where(k => k.Value.Commands.Any(a => a.Equals(commands[0], StringComparison.InvariantCultureIgnoreCase))).ToArray();

            if (!comandList.Any())
                MyAPIGateway.Utilities.SendMessage(steamId, "CHECK", "ProcessServerMessage failed: matching commandList Empty.");

            foreach (var command in comandList)
            {
                //if (MainChatCommandLogic.Instance.BlockCommandExecution)
                //{
                //    MyAPIGateway.Utilities.SendMessage(steamId, "Permission", "Loading permissions... Please try again later.");
                //    return true;
                //}

                if (!HasRight(steamId, command.Value))
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Permission", "You do not have the permission to use this command.");
                    return true;
                }

                if (command.Value.HasFlag(ChatCommandAccessibility.SingleplayerOnly) && MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Command Service", "Command disabled in online mode.");
                    return true;
                }

                if (command.Value.HasFlag(ChatCommandAccessibility.MultiplayerOnly) && MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Command Service", "Command disabled in offline mode.");
                    return true;
                }

                if (command.Value.HasFlag(ChatCommandAccessibility.Server))
                {
                    try
                    {
                        //MyAPIGateway.Utilities.SendMessage(steamId, "CHECK", "ProcessServerMessage trying command.");
                        if (command.Value.Invoke(steamId, playerId, messageText))
                            return true;

                        MyAPIGateway.Utilities.SendMessage(steamId, "Command failed", string.Format("Execution of command {0} failed. Use '/help {0}' for receiving a detailed instruction.", command.Value.Name));
                        command.Value.Help(steamId, true);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MainChatCommandLogic.Instance.ServerLogger.WriteException(ex, $"Occurred attempting to run {command.Value.GetType().Name} '{command.Key}' ");

                        // Exception handling to prevent any crash in the ChatCommand's reaching the user.
                        // Additional information for developers
                        if (MainChatCommandLogic.Instance.ExperimentalCreatorList.Any(e => e == steamId))
                        {
                            MyAPIGateway.Utilities.SendMissionScreen(steamId, $"Error in {command.Value.Name}", "Input: ", messageText, ex.ToString());
                            TextLogger.WriteGameLog($"##Mod## {MainChatCommandLogic.Instance.ModName} Exception caught. Message: {ex}");
                            continue;
                        }

                        var message = ex.Message.Replace("\r", " ").Replace("\n", " ");
                        message = message.Substring(0, Math.Min(message.Length, 50));
                        MyAPIGateway.Utilities.SendMessage(steamId, "Error", "Occurred attempting to run {0} '{1}'.\r\n{2}", command.Value.GetType().Name, command.Key,  message);
                    }
                }
                else
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Command", "Has not been correctly registered as either Client or Server.");
                }
            }

            //MyAPIGateway.Utilities.SendMessage(steamId, "CHECK", "ProcessServerMessage failed: fallthrough.");
            return false;
        }

        public static void UpdateBeforeSimulation()
        {
            if (!_isInitialized)
                return;

            //foreach (var command in Commands.Where(command => (command.Security & _userSecurity) != ChatCommandSecurity.None))
            //{
            //    command.UpdateBeforeSimulation();
            //}

            // TODO: cannot check HasRight, as the it's the server. Perhaps add | isDedicatedServer.
            foreach (var command in Commands) //.Where(command => HasRight(steamId, command.Value)))
            {
                command.Value.UpdateBeforeSimulation();
            }
        }

        public static void UpdateBeforeSimulation100()
        {
            if (!_isInitialized)
                return;

            // TODO: cannot check HasRight, as the it's the server. Perhaps add | isDedicatedServer.
            foreach (var command in Commands) //.Where(command => HasRight(steamId, command.Value)))
            {
                command.Value.UpdateBeforeSimulation100();
            }
        }

        public static void UpdateBeforeSimulation1000()
        {
            if (!_isInitialized)
                return;

            // TODO: cannot check HasRight, as the it's the server. Perhaps add | isDedicatedServer.
            foreach (var command in Commands) //.Where(command => HasRight(steamId, command.Value)))
            {
                command.Value.UpdateBeforeSimulation1000();
            }
        }

        public static bool HasRight(ulong steamId, ChatCommand command)
        {
            if (command.HasFlag(ChatCommandAccessibility.Experimental))
                return MainChatCommandLogic.Instance.ExperimentalCreatorList.Any(e => e == steamId) && command.Security <= UserSecurity;

            return command.Security <= UserSecurity;
        }

        ///// <summary>
        ///// Permissions system.
        ///// </summary>
        ///// <param name="command"></param>
        ///// <returns></returns>
        //public static bool UpdateCommandSecurity(CommandStruct command)
        //{
        //    if (!Commands.ContainsKey(command.Name))
        //        return false;

        //    Commands[command.Name].Security = command.NeededLevel;

        //    return true;
        //}

        public static bool IsCommandRegistered(string commandName)
        {
            return Commands.Any(pair => pair.Value.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
        }

        ///// <summary>
        ///// Sets or resets the stored Player security level.
        ///// </summary>
        //public static void UpdatePlayerSecurity()
        //{
        //    Logger.WriteGameLog("####### CALL UpdatePlayerSecurity ");
        //    var session = MyAPIGateway.Session;

        //    if (MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Utilities.IsDedicated)
        //    {
        //        _userSecurity = ChatCommandSecurity.Admin;
        //    }
        //    else
        //    {
        //        //only set this in sp, in mp we need to wait until the server sends us our level. On LS this will be read in during the creation of the server cfg.
        //        if (session.Player.IsAdmin() && session.OnlineMode.Equals(MyOnlineModeEnum.OFFLINE))
        //            _userSecurity = ChatCommandSecurity.Admin;
        //        else
        //            _userSecurity = ChatCommandSecurity.User;
        //    }

        //    Logger.WriteGameLog("####### Security = {0}", _userSecurity);
        //}

        #endregion
    }
}
