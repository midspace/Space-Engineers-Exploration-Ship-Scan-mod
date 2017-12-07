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
        /// Player is Admin of game.
        /// </summary>
        public const uint Admin = 100;
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

    public enum MassCategory
    {
        Unknown = 0,
        Junk = 1,
        Tiny = 2,
        Small,
        Large,
        Huge,
        Enormous,
        Ridiculous
    };

    public enum SpeedCategory
    {
        Stationary = 0,
        Drifting = 3,
        Moving = 20,
        Flying
    };
}
