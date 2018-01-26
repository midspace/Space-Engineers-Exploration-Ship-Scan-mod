namespace midspace.shipscan
{
    /// <summary>
    /// Defines the base for all ChatCommands.
    /// </summary>
    public abstract class ChatCommand
    {
        #region properties

        /// <summary>
        /// Required access level of a player to see and use this ChatCommand.
        /// </summary>
        public uint Security { get; private set; }

        /// <summary>
        /// Required access level of a player to see and use this ChatCommand.
        /// </summary>
        public ChatCommandAccessibility Flag { get; private set; }

        /// <summary>
        /// The name of the ChatCommand as it will appear in the Help list.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// List of all valid commands that is allowed by this ChatCommand.
        /// </summary>
        public string[] Commands { get; private set; }

        #endregion

        #region constructors

        /// <summary>
        /// The constructor defines the basics of chat command, and security access.
        /// </summary>
        /// <param name="security">Allowed level of access to this command.</param>
        /// <param name="flag">The accessibility of the command.</param>
        /// <param name="name">Name that appears in the help listing.</param>
        /// <param name="commands">Command text.</param>
        protected ChatCommand(uint security, ChatCommandAccessibility flag, string name, string[] commands)
        {
            Security = security;
            Flag = flag;
            Name = name;
            Commands = commands;
        }

        #endregion

        /// <summary>
        /// Runs the Chat command's specific help.
        /// </summary>
        public abstract void Help(ulong steamId, bool brief);

        /// <summary>
        /// Tests the Chat command for validility, and executes its content.
        /// </summary>
        /// <param name="steamId">SteamId of the player that invoked the command.</param>
        /// <param name="playerId">PlayerId of the player that invoked the command.</param>
        /// <param name="messageText">The command text.</param>
        /// <returns>Returns true if the chat command was valid and processed successfully, otherwise returns false.</returns>
        public abstract bool Invoke(ulong steamId, long playerId, string messageText);

        /// <summary>
        /// Replacement for the Finalizer the Keen killed off.
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Optional method that is called on every frame Before Simulation.
        /// </summary>
        public virtual void UpdateBeforeSimulation()
        {
        }

        /// <summary>
        /// Optional method that is called every 100 miliseconds Before Simulation.
        /// </summary>
        public virtual void UpdateBeforeSimulation100()
        {
        }

        /// <summary>
        /// Optional method that is called every 1000 miliseconds Before Simulation.
        /// </summary>
        public virtual void UpdateBeforeSimulation1000()
        {
        }

        /// <summary>
        /// Determins if the command has the given flag.
        /// </summary>
        /// <param name="flag"></param>
        /// <returns>Returns true if the command has the given flag.</returns>
        public bool HasFlag(ChatCommandAccessibility flag)
        {
            return Flag.HasFlag(flag);
        }
    }
}
