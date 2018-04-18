namespace MidSpace.ShipScan.SeModCore
{
    using System;
    using Messages;
    using ProtoBuf;

    /// <summary>
    /// This is for sending messages from Client to Server, where the Server will process them upon receipt.
    /// </summary>
    [ProtoContract]
    // These are the ModCore Pull Messages. Modder programmers should add their own Messages to the other Partial class.
    [ProtoInclude(50, typeof(PullChatCommand))]
    [ProtoInclude(51, typeof(PullConnectionRequest))]
    public abstract partial class PullMessageBase
    {
        #region properties

        /// <summary>
        /// The SteamId of the message's sender. Note that this will be set when the message is sent, so there is no need for setting it otherwise.
        /// </summary>
        [ProtoMember(1)]
        public ulong SenderSteamId;

        /// <summary>
        /// The display name of the message sender.
        /// </summary>
        [ProtoMember(2)]
        public string SenderDisplayName;

        /// <summary>
        /// The current display language of the sender.
        /// </summary>
        [ProtoMember(3)]
        public int SenderLanguage;

        /// <summary>
        /// Defines on which side the message should be processed. Note that this will be set when the message is sent, so there is no need for setting it otherwise.
        /// </summary>
        [ProtoMember(4)]
        public MessageSide Side;

        #endregion

        ///// <summary>
        ///// This will allow the serializer to automatically execute the Action in the same step as Deserialization,
        ///// and reduce the message handling in the ConnectionHelper.
        ///// Exception handling ConnectionHelper would have to be moved here too.
        ///// </summary>
        //[ProtoAfterDeserialization] // not yet whitelisted.
        //void AfterDeserialization() // is not invoked after deserialization from xml
        //{
        //    InvokeProcessing();
        //}

        public void InvokeProcessing()
        {
            MainChatCommandLogic.Instance.ServerLogger.WriteVerbose("Received -> {0}", this.GetType().Name);
            try
            {
                ProcessServer();
            }
            catch (Exception ex)
            {
                MainChatCommandLogic.Instance.ServerLogger.WriteException(ex, "Could not process message on Server.");
            }
        }

        /// <summary>
        /// When the message is recieved on the Server side, it will invoke this method.
        /// </summary>
        public abstract void ProcessServer();
    }
}
