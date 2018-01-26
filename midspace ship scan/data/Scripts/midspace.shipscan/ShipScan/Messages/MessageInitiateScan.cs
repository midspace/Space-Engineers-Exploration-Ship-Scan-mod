namespace midspace.shipscan
{
    using ProtoBuf;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    /// <summary>
    /// This will process and run the scan, then return the results to the player.
    /// </summary>
    [ProtoContract]
    public class MessageInitiateScan : ModMessageBase
    {
        #region properties

        /// <summary>
        /// The minimum scan range. Items detected inside of this range will be ignored
        /// </summary>
        [ProtoMember(201)]
        public decimal MinRange;

        /// <summary>
        /// The type of scan type to carry out.
        /// </summary>
        [ProtoMember(202)]
        public ScanType DisplayType;

        [ProtoMember(203)]
        public SerializableMatrix PlayerPositionMatrix;

        [ProtoMember(204)]
        public long ControlledEntityId;

        #endregion

        public static void SendMessage(decimal minRange, ScanType displayType, SerializableMatrix playerPositionMatrix, long controlledEntityId)
        {
            ConnectionHelper.SendMessageToServer(new MessageInitiateScan { MinRange = minRange, DisplayType = displayType, PlayerPositionMatrix = playerPositionMatrix, ControlledEntityId = controlledEntityId });
        }

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            ScanSettingsEntity scanSettings = ScanDataManager.GetPlayerScanData(SenderSteamId);
            ScanShips(SenderSteamId, (double)MinRange, DisplayType, scanSettings);
        }

        private void ScanShips(ulong steamId, double minRange, ScanType displayType, ScanSettingsEntity settings)
        {
            IMyPlayer player = MyAPIGateway.Players.FindPlayerBySteamId(SenderSteamId);
            IMyEntity controlledEntity = MyAPIGateway.Entities.GetEntityById(ControlledEntityId);

            if (player == null )
            {
                MainChatCommandLogic.Instance.ServerLogger.WriteVerbose($"Scan failed. Player information passed was invalid, or old. SenderSteamId={SenderSteamId}");
                MyAPIGateway.Utilities.SendMessage(steamId, "Scan failed", "Mod/Game failure.");
                return;
            }

            if (controlledEntity == null)
            {
                MainChatCommandLogic.Instance.ServerLogger.WriteVerbose($"Scan failed. information passed was invalid, or old. ControlledEntityId={ControlledEntityId}");
                MyAPIGateway.Utilities.SendMessage(steamId, "Scan failed", "Mod/Game failure.");
                return;
            }

            // TODO: background progessing. GetEntitiesInSphere is a very intensive call.
            //MyAPIGateway.Parallel.

            var cockpit = controlledEntity as IMyCubeBlock;
            if (controlledEntity.Parent == null || cockpit == null)
            {
                MyAPIGateway.Utilities.SendMessage(steamId, "Scan failed", "Player is not in ship.");
                return;
            }

            var definition = MyDefinitionManager.Static.GetCubeBlockDefinition(cockpit.BlockDefinition);
            var cockpitDefinition = definition as MyCockpitDefinition;
            var remoteDefinition = definition as MyRemoteControlDefinition;

            if ((cockpitDefinition == null || !cockpitDefinition.EnableShipControl)
                && (remoteDefinition == null || !remoteDefinition.EnableShipControl))
            {
                MyAPIGateway.Utilities.SendMessage(steamId, "Scan failed", "Player must be in cockpit/remote and cannot be passenger.");
                return;
            }

            var cubeGrid = (IMyCubeGrid)controlledEntity.GetTopMostParent();
            var blocks = new List<IMySlimBlock>();
            cubeGrid.GetBlocks(blocks, b => b?.FatBlock != null && !b.FatBlock.BlockDefinition.TypeId.IsNull && b.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_RadioAntenna) && b.FatBlock.IsWorking);

            if (blocks.Count == 0)
            {
                MyAPIGateway.Utilities.SendMessage(steamId, "Scan failed", "No working antenna found.");
                return;
            }

            float effectiveRadius = 0f;
            Vector3D scanPoint = Vector3D.Zero;

            // TODO: maybe extend this to use all available fixed antenna on the ship/station, as the antenna may offer a better spread on a large ship/station.
            // Not planning to use all in-range antenna however.
            foreach (var block in blocks)
            {
                var relation = block.FatBlock.GetUserRelationToOwner(player.IdentityId);
                if (relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare)
                {
                    var z = block.FatBlock.GetObjectBuilderCubeBlock();
                    var y = (MyObjectBuilder_RadioAntenna)z;
                    if (y.EnableBroadcasting)
                    {
                        var radi = y.BroadcastRadius;
                        if (effectiveRadius < radi)
                        {
                            effectiveRadius = radi;
                            scanPoint = block.FatBlock.WorldAABB.Center;
                        }
                    }
                }
            }

            if (effectiveRadius == 0f)
            {
                MyAPIGateway.Utilities.SendMessage(steamId, "Scan failed", "No working/owned antenna found.");
                return;
            }

            MyAPIGateway.Utilities.SendMessage(steamId, "Scanning", "...");

            List<ShipGridExtension> shipGrids = new List<ShipGridExtension>();
            var playerPosition = controlledEntity.GetPosition();

            int fullCount = 0;

            MyAPIGateway.Parallel.StartBackground(
                delegate
                {
                    // Find all grids within range of the Antenna broadcast sphere.
                    var sphere = new BoundingSphereD(scanPoint, effectiveRadius);
                    var floatingList = MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere).Where(e => (e is IMyCubeGrid)).Cast<IMyCubeGrid>().ToArray();

                    //var allEntites = new HashSet<IMyEntity>();
                    // Remove grids without physics, these should be projected grids.
                    //MyAPIGateway.Entities.GetEntities(allEntites, e => (e is IMyCubeGrid) && Vector3D.Distance(scanPoint, e.WorldAABB.Center) <= effetiveRadius && e.Physics != null);
                    //var floatingList = allEntites.Cast<IMyCubeGrid>().ToArray();


                    foreach (var cubeGridPart in floatingList)
                    {
                        //MyAPIGateway.Utilities.SendMessage(steamId, "name", string.Format("'{0}' {1}.", cubeGridPart.DisplayName, cubeGridPart.GetTopMostParent().DisplayName));

                        // Collate the grids, into gourps where they are joined by Rotors, Pistons, or Wheels.
                        if (!shipGrids.Any(s => s.GridGroups.Any(g => g.EntityId == cubeGridPart.EntityId)))
                        {
                            var gridGroups = cubeGridPart.GetAttachedGrids();

                            //MyAPIGateway.Utilities.SendMessage(steamId, "groups", string.Format("{0}.", gridGroups.Count));

                            // Check if the is not powered or Owned in any way.
                            if (!gridGroups.IsPowered() && !gridGroups.IsOwned())
                            {
                                var pos = gridGroups.Center();
                                var distance = Math.Sqrt((playerPosition - pos).LengthSquared());
                                if (distance >= minRange)
                                {
                                    var ship = new ShipGridExtension { GridGroups = gridGroups, Distance = distance, Position = pos };
                                    shipGrids.Add(ship);
                                }
                            }
                        }
                    }

                    fullCount = shipGrids.Count;

                    // Filter the ignore list.
                    shipGrids = shipGrids.Where(e => !(settings.IgnoreJunk && e.MassCategory == MassCategory.Junk)).ToList();
                    shipGrids = shipGrids.Where(e => !(settings.IgnoreTiny && e.MassCategory == MassCategory.Tiny)).ToList();
                    shipGrids = shipGrids.Where(e => !(settings.IgnoreSmall && e.MassCategory == MassCategory.Small)).ToList();
                    shipGrids = shipGrids.Where(e => !(settings.IgnoreLarge && e.MassCategory == MassCategory.Large)).ToList();
                    shipGrids = shipGrids.Where(e => !(settings.IgnoreHuge && e.MassCategory == MassCategory.Huge)).ToList();
                    shipGrids = shipGrids.Where(e => !(settings.IgnoreEnormous && e.MassCategory == MassCategory.Enormous)).ToList();
                    shipGrids = shipGrids.Where(e => !(settings.IgnoreRidiculous && e.MassCategory == MassCategory.Ridiculous)).ToList();

                    settings.ScanHostList.Clear();

                }, delegate
            {
                switch (displayType)
                {
                    case ScanType.MissionScreen:
                        {
                            var prefix = $"Scan range: {effectiveRadius}m : {shipGrids.Count} derelict masses detected.";
                            if (fullCount != shipGrids.Count)
                            {
                                prefix += $"\r\n{fullCount - shipGrids.Count} masses ignored.";
                            }

                            var description = new StringBuilder();
                            var index = 1;
                            foreach (var ship in shipGrids.OrderBy(s => s.Distance))
                            {
                                var heading = Support.GetRotationAngle(PlayerPositionMatrix, ship.Position - playerPosition);
                                description.AppendFormat("#{0} : Rn:{3:N}m, El:{4:N}°, Az:{5:N}° : {1} {2}\r\n", index++, ship.SpeedCategory, ship.MassCategory, ship.Distance, heading.Y, heading.X);
                                settings.ScanHostList.Add(new TrackDetailEntity(ship.Position, $"{ship.SpeedCategory} {ship.MassCategory} Derelict" /*, ship.GridGroups.Select(e => e.EntityId) */ ));
                            }

                            MyAPIGateway.Utilities.SendMissionScreen(steamId, "Scan Results", prefix, " ", description.ToString(), null, "OK");
                        }
                        break;

                    case ScanType.ChatConsole:
                        {
                            var index = shipGrids.Count;
                            foreach (var ship in shipGrids.OrderByDescending(s => s.Distance))
                            {
                                var heading = Support.GetRotationAngle(PlayerPositionMatrix, ship.Position - playerPosition);
                                MyAPIGateway.Utilities.SendMessage(steamId, $"#{index--}", string.Format("Rn:{2:N}m, El:{3:N}°, Az:{4:N}° : {0} {1}", ship.SpeedCategory, ship.MassCategory, ship.Distance, heading.Y, heading.X));
                                settings.ScanHostList.Add(new TrackDetailEntity(ship.Position, $"{ship.SpeedCategory} {ship.MassCategory} Derelict" /*, ship.GridGroups.Select(e => e.EntityId) */ ));
                            }

                            MyAPIGateway.Utilities.SendMessage(steamId, "Scan range", "{0}m : {1} derelict masses detected.", effectiveRadius, shipGrids.Count);
                        }
                        break;

                    case ScanType.GpsCoordinates:
                        {
                            if (settings.ScanListGpsEntities == null)
                                settings.ScanListGpsEntities = new List<TrackGpsEntity>();

                            var updateCount = 0;
                            foreach (var ship in shipGrids)
                            {
                                var entityIds = ship.GridGroups.Select(e => e.EntityId).ToList();

                                foreach (var entityId in entityIds)
                                {
                                    var trackEntity = settings.ScanListGpsEntities.FirstOrDefault(t => t.Entities.Contains(entityId));
                                    if (trackEntity != null && trackEntity.GpsHash != 0)
                                    {
                                        settings.ScanListGpsEntities.Remove(trackEntity);
                                        //MyAPIGateway.Session.GPS.RemoveLocalGps(trackEntity.GpsHash);
                                        MyAPIGateway.Session.GPS.RemoveGps(player.IdentityId, trackEntity.GpsHash);
                                        updateCount++;
                                    }
                                }
                            }

                            foreach (var ship in shipGrids)
                            {
                                var name = $"Derelict {ship.SpeedCategory} {ship.MassCategory}";
                                var description = "Derelict craft";
                                var gps = MyAPIGateway.Session.GPS.Create(name, description, ship.Position, true, false);

                                //MyAPIGateway.Session.GPS.AddLocalGps(gps);
                                MyAPIGateway.Session.GPS.AddGps(player.IdentityId, gps);
                                settings.ScanListGpsEntities.Add(new TrackGpsEntity { GpsHash = gps.Hash, Entities = ship.GridGroups.Select(e => e.EntityId).ToList() });
                            }

                            MyAPIGateway.Utilities.SendMessage(steamId, "Scan range", "{0}m : {1}/{2} new derelict masses detected.", effectiveRadius, shipGrids.Count - updateCount, shipGrids.Count);
                        }
                        break;
                }
            });
        }

    }
}
