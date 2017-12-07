namespace midspace.shipscan
{
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    public class CommandScan : ChatCommand
    {
        private enum ScanType { ChatConsole, MissionScreen, GPSCoordinates };

        /// <summary>
        /// Temporary hotlist cache created when player requests a list of in game ships, populated only by search results.
        /// </summary>
        public readonly static List<TrackDetail> ShipCache = new List<TrackDetail>();

        public CommandScan()
            : base(ChatCommandSecurity.User, ChatCommandAccessibility.Client, "scan", new[] { "/scan", "/scan2", "/scan3" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/scan", "Scans for derelict ships using ship antenna.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            double minRange = 0;
            var match = Regex.Match(messageText, @"/scan\s{1,}(?<MINRANGE>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                minRange = double.Parse(match.Groups["MINRANGE"].Value, CultureInfo.InvariantCulture);
                if (minRange <= 0) minRange = 0;
                return ScanShips(steamId, minRange, ScanType.MissionScreen);
            }

            match = Regex.Match(messageText, @"/scan2\s{1,}(?<MINRANGE>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                minRange = double.Parse(match.Groups["MINRANGE"].Value, CultureInfo.InvariantCulture);
                if (minRange <= 0) minRange = 0;
                return ScanShips(steamId, minRange, ScanType.ChatConsole);
            }

            match = Regex.Match(messageText, @"/scan3\s{1,}(?<MINRANGE>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                minRange = double.Parse(match.Groups["MINRANGE"].Value, CultureInfo.InvariantCulture);
                if (minRange <= 0) minRange = 0;
                return ScanShips(steamId, minRange, ScanType.GPSCoordinates);
            }

            if (messageText.Equals("/scan", StringComparison.InvariantCultureIgnoreCase))
            {
                return ScanShips(steamId, 0, ScanType.MissionScreen);
            }
            if (messageText.Equals("/scan2", StringComparison.InvariantCultureIgnoreCase))
            {
                return ScanShips(steamId, 0, ScanType.ChatConsole);
            }
            if (messageText.Equals("/scan3", StringComparison.InvariantCultureIgnoreCase))
            {
                return ScanShips(steamId, 0, ScanType.GPSCoordinates);
            }

            return false;
        }

        private bool ScanShips(ulong steamId, double minRange, ScanType displayType)
        {
            // TODO: background progessing. GetEntitiesInSphere is a very intensive call.
            //MyAPIGateway.Parallel.

            var cockpit = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity as IMyCubeBlock;
            if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Parent == null || cockpit == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Scan failed", "Player is not in ship.");
                return true;
            }

            var definition = MyDefinitionManager.Static.GetCubeBlockDefinition(cockpit.BlockDefinition);
            var cockpitDefinition = definition as MyCockpitDefinition;
            var remoteDefinition = definition as MyRemoteControlDefinition;

            if ((cockpitDefinition == null || !cockpitDefinition.EnableShipControl)
                && (remoteDefinition == null || !remoteDefinition.EnableShipControl))
            {
                MyAPIGateway.Utilities.ShowMessage("Scan failed", "Player must be in cockpit/remote and cannot be passenger.");
                return true;
            }

            var cubeGrid = (IMyCubeGrid)MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetTopMostParent();
            var blocks = new List<IMySlimBlock>();
            cubeGrid.GetBlocks(blocks, b => b != null && b.FatBlock != null && !b.FatBlock.BlockDefinition.TypeId.IsNull && b.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_RadioAntenna) && b.FatBlock.IsWorking);

            if (blocks.Count == 0)
            {
                MyAPIGateway.Utilities.ShowMessage("Scan failed", "No working antenna found.");
                return true;
            }

            float effetiveRadius = 0f;
            Vector3D scanPoint = Vector3D.Zero; ;

            // TODO: maybe extend this to use all available fixed antenna on the ship/station, as the antenna may offer a better spread on a large ship/station.
            // Not planning to use all in-range antenna however.
            foreach (var block in blocks)
            {
                var relation = block.FatBlock.GetPlayerRelationToOwner();
                if (relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare)
                {
                    var z = block.FatBlock.GetObjectBuilderCubeBlock();
                    var y = (MyObjectBuilder_RadioAntenna)z;
                    if (y.EnableBroadcasting)
                    {
                        var radi = y.BroadcastRadius;
                        if (effetiveRadius < radi)
                        {
                            effetiveRadius = radi;
                            scanPoint = block.FatBlock.WorldAABB.Center;
                        }
                    }
                }
            }

            if (effetiveRadius == 0f)
            {
                MyAPIGateway.Utilities.ShowMessage("Scan failed", "No working/owned antenna found.");
                return true;
            }

            MyAPIGateway.Utilities.ShowMessage("Scanning", "...");

            List<ShipGrid> shipGrids = new List<ShipGrid>();
            var playerPosition = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();
            int fullCount = 0;

            MyAPIGateway.Parallel.StartBackground(
                delegate
                {
                    // Find all grids within range of the Antenna broadcast sphere.
                    //var sphere = new BoundingSphereD(scanPoint, effetiveRadius);
                    //var floatingList = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).Where(e => (e is IMyCubeGrid)).Cast<IMyCubeGrid>().ToArray();
                    var allEntites = new HashSet<IMyEntity>();

                    // Remove grids without physics, these should be projected grids.
                    MyAPIGateway.Entities.GetEntities(allEntites, e => (e is IMyCubeGrid) && Vector3D.Distance(scanPoint, e.WorldAABB.Center) <= effetiveRadius && e.Physics != null);
                    var floatingList = allEntites.Cast<IMyCubeGrid>().ToArray();


                    foreach (var cubeGridPart in floatingList)
                    {
                        //MyAPIGateway.Utilities.ShowMessage("name", string.Format("'{0}' {1}.", cubeGridPart.DisplayName, cubeGridPart.GetTopMostParent().DisplayName));

                        // Collate the grids, into gourps where they are joined by Rotors, Pistons, or Wheels.
                        if (!shipGrids.Any(s => s.GridGroups.Any(g => g.EntityId == cubeGridPart.EntityId)))
                        {
                            var gridGroups = cubeGridPart.GetAttachedGrids();

                            //MyAPIGateway.Utilities.ShowMessage("groups", string.Format("{0}.", gridGroups.Count));

                            // Check if the is not powered or Owned in any way.
                            if (!gridGroups.IsPowered() && !gridGroups.IsOwned())
                            {
                                var pos = gridGroups.Center();
                                var distance = Math.Sqrt((playerPosition - pos).LengthSquared());
                                if (distance >= minRange)
                                {
                                    var ship = new ShipGrid { GridGroups = gridGroups, Distance = distance, Position = pos };
                                    shipGrids.Add(ship);
                                }
                            }
                        }
                    }

                    fullCount = shipGrids.Count;

                    // Filter the ignore list.
                    var config = MainChatCommandLogic.Instance.Settings.Config;
                    shipGrids = shipGrids.Where(e => !(config.IgnoreJunk && e.MassCategory == MassCategory.Junk)).ToList();
                    shipGrids = shipGrids.Where(e => !(config.IgnoreTiny && e.MassCategory == MassCategory.Tiny)).ToList();
                    shipGrids = shipGrids.Where(e => !(config.IgnoreSmall && e.MassCategory == MassCategory.Small)).ToList();
                    shipGrids = shipGrids.Where(e => !(config.IgnoreLarge && e.MassCategory == MassCategory.Large)).ToList();
                    shipGrids = shipGrids.Where(e => !(config.IgnoreHuge && e.MassCategory == MassCategory.Huge)).ToList();
                    shipGrids = shipGrids.Where(e => !(config.IgnoreEnormous && e.MassCategory == MassCategory.Enormous)).ToList();
                    shipGrids = shipGrids.Where(e => !(config.IgnoreRidiculous && e.MassCategory == MassCategory.Ridiculous)).ToList();

                    ShipCache.Clear();

                }, delegate ()
            {
                switch (displayType)
                {
                    case ScanType.MissionScreen:
                        {
                            var prefix = string.Format("Scan range: {0}m : {1} derelict masses detected.", effetiveRadius, shipGrids.Count);
                            if (fullCount != shipGrids.Count)
                            {
                                prefix += string.Format("\r\n{0} masses ignored.", fullCount - shipGrids.Count);
                            }

                            var description = new StringBuilder();
                            var index = 1;
                            foreach (var ship in shipGrids.OrderBy(s => s.Distance))
                            {
                                var heading = Support.GetRotationAngle(MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix, ship.Position - playerPosition);
                                description.AppendFormat("#{0} : Rn:{3:N}m, El:{4:N}°, Az:{5:N}° : {1} {2}\r\n", index++, ship.SpeedCategory, ship.MassCategory, ship.Distance, heading.Y, heading.X);
                                ShipCache.Add(new TrackDetail(ship.Position, string.Format("{0} {1} Derelict", ship.SpeedCategory, ship.MassCategory), ship.GridGroups.Select(e => e.EntityId)));
                            }

                            MyAPIGateway.Utilities.ShowMissionScreen("Scan Results", prefix, " ", description.ToString(), null, "OK");
                        }
                        break;

                    case ScanType.ChatConsole:
                        {
                            var index = shipGrids.Count;
                            foreach (var ship in shipGrids.OrderByDescending(s => s.Distance))
                            {
                                var heading = Support.GetRotationAngle(MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix, ship.Position - playerPosition);
                                MyAPIGateway.Utilities.ShowMessage(string.Format("#{0}", index--), "Rn:{2:N}m, El:{3:N}°, Az:{4:N}° : {0} {1}", ship.SpeedCategory, ship.MassCategory, ship.Distance, heading.Y, heading.X);
                                ShipCache.Add(new TrackDetail(ship.Position, string.Format("{0} {1} Derelict", ship.SpeedCategory, ship.MassCategory), ship.GridGroups.Select(e => e.EntityId)));
                            }

                            MyAPIGateway.Utilities.ShowMessage("Scan range", "{0}m : {1} derelict masses detected.", effetiveRadius, shipGrids.Count);
                        }
                        break;

                    case ScanType.GPSCoordinates:
                        {
                            if (MainChatCommandLogic.Instance.Settings.Config.TrackEntites == null)
                                MainChatCommandLogic.Instance.Settings.Config.TrackEntites = new List<TrackEntity>();

                            var updateCount = 0;
                            foreach (var ship in shipGrids)
                            {
                                var entityIds = ship.GridGroups.Select(e => e.EntityId).ToList();

                                foreach (var entityId in entityIds)
                                {
                                    var trackEntity = MainChatCommandLogic.Instance.Settings.Config.TrackEntites.FirstOrDefault(t => t.Entities.Contains(entityId));
                                    if (trackEntity.GpsHash != 0)
                                    {
                                        MainChatCommandLogic.Instance.Settings.Config.TrackEntites.Remove(trackEntity);
                                        //MyAPIGateway.Session.GPS.RemoveLocalGps(trackEntity.GpsHash);
                                        MyAPIGateway.Session.GPS.RemoveGps(MyAPIGateway.Session.Player.IdentityId, trackEntity.GpsHash);
                                        updateCount++;
                                    }
                                }
                            }

                            foreach (var ship in shipGrids)
                            {
                                var name = string.Format("Derelict {0} {1}", ship.SpeedCategory, ship.MassCategory);
                                var description = "Derelict craft";
                                var gps = MyAPIGateway.Session.GPS.Create(name, description, ship.Position, true, false);

                                //MyAPIGateway.Session.GPS.AddLocalGps(gps);
                                MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);
                                MainChatCommandLogic.Instance.Settings.Config.TrackEntites.Add(new TrackEntity { GpsHash = gps.Hash, Entities = ship.GridGroups.Select(e => e.EntityId).ToList() });
                            }

                            MainChatCommandLogic.Instance.Settings.Save();
                            MyAPIGateway.Utilities.ShowMessage("Scan range", "{0}m : {1}/{2} new derelict masses detected.", effetiveRadius, shipGrids.Count - updateCount, shipGrids.Count);
                        }
                        break;
                }
            });

            return true;
        }
    }
}
