namespace midspace.shipscan
{
    using System;

    public enum MessageSide : byte
    {
        None = 0,
        ServerSide = 1,
        ClientSide = 2
    }

    //[Flags]
    //public enum ChatCommandSecurity
    //{
    //    /// <summary>
    //    /// Default state, uninitilized.
    //    /// </summary>
    //    None = 0x0,

    //    /// <summary>
    //    /// The normal average player can access these command
    //    /// </summary>
    //    User = 0x1,

    //    /// <summary>
    //    /// Player is Admin of game.
    //    /// </summary>
    //    Admin = 0x2,

    //    /// <summary>
    //    /// Player is designer or creator. Used for testing commands only.
    //    /// </summary>
    //    Experimental = 0x4
    //};

    public class ChatCommandSecurity
    {
        /// <summary>
        /// The normal average player can access these command
        /// </summary>
        public const uint User = 0;

        /// <summary>
        /// Can edit scripts when the scripter role is enabled
        /// </summary>
        public const uint Scripter = 50;

        /// <summary>
        /// Can kick and ban players, has access to 'Show All Players' option in Admin Tools menu
        /// </summary>
        public const uint Moderator = 80;

        /// <summary>
        /// Has access to Space Master tools
        /// </summary>
        public const uint SpaceMaster = 100;

        /// <summary>
        /// Has access to Admin tools
        /// </summary>
        public const uint Admin = 200;

        /// <summary>
        /// Admins listed in server config, cannot be demoted
        /// </summary>
        public const uint Owner = 500;
    }

    [Flags]
    public enum ChatCommandAccessibility
    {
        /// <summary>
        /// No flag set for this command.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Shows that this command is not ready for use, thus only accessible for experimental users.
        /// </summary>
        Experimental = 0x1,

        /// <summary>
        /// Shows that this command can only be used in singleplayer.
        /// </summary>
        SingleplayerOnly = 0x2,

        /// <summary>
        /// Shows that this command can only be used in multiplayer.
        /// </summary>
        MultiplayerOnly = 0x4,

        /// <summary>
        /// Command runs Client side.
        /// </summary>
        Client = 0x8,

        /// <summary>
        /// Command runs Server side.
        /// </summary>
        Server = 0x10
    }
}
