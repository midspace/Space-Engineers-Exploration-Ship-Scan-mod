namespace MidSpace.ShipScan
{
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using SeModCore;
    using SeModCore.Messages;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VRage.Game;
    using VRage.Game.Components;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;

    /// <summary>
    /// Logic to add into the ProgrammableBlock, to check when the CustomData field is changed.
    /// 
    /// This requires a ProgramableBlock to have this code "      Me.CustomData = argument;    "
    /// Chat Commands can be passed through to the ModAPI using the prefix "CHAT:" or "CHAT;"
    /// ie "CHAT; /scan3"
    /// </summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MyProgrammableBlock), false)]
    public class ProgrammableBlockLogicChatCmds : MyGameLogicComponent
    {
        #region fields

        private MyObjectBuilder_EntityBase _objectBuilder;
        private bool _isInitilized;
        private IMyProgrammableBlock _programmableBlockEntity;

        #endregion

        // This code will run on all clients and the server, so we need to isolate it to the server only.
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _objectBuilder = objectBuilder;
            if (MyAPIGateway.Session.OnlineMode.Equals(MyOnlineModeEnum.OFFLINE) || MyAPIGateway.Multiplayer.IsServer)
            {
                this.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

                if (!_isInitilized)
                {
                    // Use this space to hook up events. NOT TO PROCESS ANYTHING.
                    _isInitilized = true;

                    _programmableBlockEntity = (IMyProgrammableBlock)Entity;
                    _programmableBlockEntity.CustomDataChanged += _programmableBlockEntity_CustomDataChanged;
                }
            }
        }

        // this event appears to fire on all clients and the server.
        private void _programmableBlockEntity_CustomDataChanged(IMyTerminalBlock obj)
        {
            string data = obj.CustomData;
            //VRage.Utils.MyLog.Default.WriteLine($"####### CUSTOM VALUE CHANGED {data}");

            if (data.StartsWith("CHAT:", StringComparison.OrdinalIgnoreCase) || data.StartsWith("CHAT;", StringComparison.OrdinalIgnoreCase))
            {
                data = data.Substring(5).TrimStart();
                //VRage.Utils.MyLog.Default.WriteLine($"####### CUSTOM VALUE VETTED {data}");

                //_programmableBlockEntity.ProgramData = "faking it";
                //_programmableBlockEntity.StorageData = "hello world...";

                var blocks = new List<IMySlimBlock>();
                obj.CubeGrid.GetBlocks(blocks, f => f.FatBlock is IMyCockpit);

                // TODO: check multi grid.

                // check to see if the owner is currenting playing and is a pilot of the ship.
                // Also check for faction shared ownership.
                // This matches the owner checks in MessageInitiateScan, even though it does not read the same.
                // Blame this on the lack of ability to get the player or IdentityId from the Character.
                foreach (IMySlimBlock block in blocks)
                {
                    IMyCharacter pilotCharacter = ((IMyCockpit)block.FatBlock).Pilot;

                    if (pilotCharacter != null)
                    {
                        var players = new List<IMyPlayer>();
                        MyAPIGateway.Players.GetPlayers(players, p => p.Character != null && p.Character.EntityId == pilotCharacter.EntityId);
                        IMyPlayer player = players.FirstOrDefault();

                        if (player != null)
                        {
                            var relation = _programmableBlockEntity.GetUserRelationToOwner(player.IdentityId);
                            if (relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare)
                            {
                                ConnectionHelper.SendMessageToPlayer(player.SteamUserId, new PushChatCommand { IdentityId = player.IdentityId, TextCommand = data });
                                return;
                            }
                        }
                    }
                }

                // TODO: do something with data to display back in the PB somehow. The Echo() command isn't available in ModAPI sadly.
                //_programmableBlockEntity.ECHO = "Chat command failed to process. Owner of PB does not currently occupy a seat.";
            }
        }

        public override void Close()
        {
            if (_isInitilized)
            {
                _programmableBlockEntity.CustomDataChanged -= _programmableBlockEntity_CustomDataChanged;
            }
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return _objectBuilder;
        }
    }
}