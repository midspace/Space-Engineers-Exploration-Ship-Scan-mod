namespace midspace.shipscan
{
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VRage.Game.Components;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MyProgrammableBlock), false)]
    public class ProgrammableBlockLogic : MyGameLogicComponent
    {
        #region fields

        private MyObjectBuilder_EntityBase _objectBuilder;
        private bool _isInitilized;
        private IMyProgrammableBlock _programmableBlockEntity;

        #endregion

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _objectBuilder = objectBuilder;
            this.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            if (!_isInitilized)
            {
                // Use this space to hook up events. NOT TO PROCESS ANYTHING.
                _isInitilized = true;

                _programmableBlockEntity = (IMyProgrammableBlock)Entity;
                _programmableBlockEntity.CustomDataChanged += _programmableBlockEntity_CustomDataChanged;
            }
        }

        // Requires a ProgramableBlock to have this code "      Me.CustomData = argument;    "
        // Chat Commands are passed through to the Mod using the prefix "CHAT:" or "CHAT;"
        // ie "CHAT; /scan3"

        private void _programmableBlockEntity_CustomDataChanged(IMyTerminalBlock obj)
        {
            string data = obj.CustomData;
            //VRage.Utils.MyLog.Default.WriteLine($"####### CUSTOM VALUE CHANGED {data}");

            if (data.StartsWith("CHAT:", StringComparison.OrdinalIgnoreCase) || data.StartsWith("CHAT;", StringComparison.OrdinalIgnoreCase))
            {
                data = data.Substring(5).TrimStart();
                //VRage.Utils.MyLog.Default.WriteLine($"####### CUSTOM VALUE VETTED {data}");

                //_programmableBlockEntity.ProgramData = "faking it";
                //_programmableBlockEntity.StorageData = "hello9 world...";

                var blocks = new List<IMySlimBlock>();
                obj.CubeGrid.GetBlocks(blocks, f => f.FatBlock is IMyCockpit);

                //_programmableBlockEntity.OwnerId

                //VRage.Utils.MyLog.Default.WriteLine($"####### BLOCKCHECK1 {blocks.Count}");
                foreach (IMySlimBlock block in blocks)
                {
                    IMyCharacter testValue = ((IMyCockpit)block.FatBlock).Pilot;

                    //VRage.Utils.MyLog.Default.WriteLine($"####### BLOCKCHECK2 {block.BlockDefinition.DisplayNameText} {testValue == null}");
                    if (testValue != null)
                    {
                        //VRage.Utils.MyLog.Default.WriteLine($"####### BLOCKCHECK3 {testValue.EntityId} {_programmableBlockEntity.OwnerId}");

                        var players = new List<IMyPlayer>();
                        MyAPIGateway.Players.GetPlayers(players, p => p.Character.EntityId == testValue.EntityId);
                        IMyPlayer player = players.FirstOrDefault();

                        if (player != null)
                        {
                            //VRage.Utils.MyLog.Default.WriteLine($"####### BLOCKCHECK4 {player.IdentityId} {_programmableBlockEntity.OwnerId} {player.SteamUserId}");

                            ConnectionHelper.SendMessageToPlayer(player.SteamUserId, new MessageChatCommand { IdentityId = player.IdentityId, TextCommand = data });
                            return;
                        }
                    }
                }

                //_programmableBlockEntity.StorageData = "failed...";

                // TODO: do something with data.
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