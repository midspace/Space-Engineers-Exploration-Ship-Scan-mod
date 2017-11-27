namespace midspace.shipscan
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    public static class Extensions
    {
        /// <summary>
        /// Find all grids attached to the specified grid, either by piston or rotor.
        /// This will iterate through all attached grids, until all are found.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>A list of all attached grids, including the original.</returns>
        public static List<IMyCubeGrid> GetAttachedGrids(this IMyEntity entity)
        {
            return GetAttachedGrids(entity as IMyCubeGrid);
        }

        /// <summary>
        /// Find all grids attached to the specified grid, either by piston or rotor.
        /// This will iterate through all attached grids, until all are found.
        /// </summary>
        /// <param name="cubeGrid"></param>
        /// <returns>A list of all attached grids, including the original.</returns>
        public static List<IMyCubeGrid> GetAttachedGrids(this IMyCubeGrid cubeGrid)
        {
            if (cubeGrid == null)
                return new List<IMyCubeGrid>();

            var results = new List<IMyCubeGrid> { cubeGrid };
            GetAttachedGrids(cubeGrid, ref results);
            return results;
        }

        private static void GetAttachedGrids(IMyCubeGrid cubeGrid, ref List<IMyCubeGrid> results)
        {
            if (cubeGrid == null)
                return;

            var blocks = new List<IMySlimBlock>();
            cubeGrid.GetBlocks(blocks, b => b != null && b.FatBlock != null && !b.FatBlock.BlockDefinition.TypeId.IsNull);

            foreach (var block in blocks)
            {
                //MyAPIGateway.Utilities.ShowMessage("Block", string.Format("{0}", block.FatBlock.BlockDefinition.TypeId));

                if (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorAdvancedStator) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorStator) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorSuspension) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorBase))
                {
                    // The MotorStator which inherits from MotorBase.
                    IMyMotorBase motorBase = block.FatBlock as IMyMotorBase;

                    if (motorBase == null)
                    {
                        //motorBase = Support.FindRotorBase(block.FatBlock.EntityId) as IMyMotorBase;
                        //if (motorBase == null)
                            continue;
                    }

                    IMyCubeGrid entityParent = motorBase.TopGrid;

                    //MyAPIGateway.Utilities.ShowMessage("fail check3", string.Format("{0}.", entityParent == null));

                    if (entityParent == null)
                        continue;
                    if (!results.Any(e => e.EntityId == entityParent.EntityId))
                    {
                        results.Add(entityParent);
                        GetAttachedGrids(entityParent, ref results);
                    }
                }
                else if (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorAdvancedRotor) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorRotor) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_RealWheel) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Wheel))
                {
                    // The Rotor Part.
                    IMyMotorRotor motorRotor = block.FatBlock as IMyMotorRotor;
                    IMyCubeGrid entityParent;
                    if (motorRotor == null || motorRotor.Base == null)
                    {
                        // Wheels appear to not properly populate the Stator property.
                        IMyCubeBlock altBlock = Support.FindRotorBase(motorRotor.EntityId);
                        if (altBlock == null)
                            continue;

                        entityParent = altBlock.CubeGrid;
                    }
                    else
                        entityParent = motorRotor.Base.CubeGrid;
                    if (!results.Any(e => e.EntityId == entityParent.EntityId))
                    {
                        results.Add(entityParent);
                        GetAttachedGrids(entityParent, ref results);
                    }
                }
                else if (block.FatBlock.BlockDefinition.TypeId == typeof (MyObjectBuilder_PistonTop))
                {
                    // The Piston Top.
                    IMyPistonTop pistonTop = block.FatBlock as IMyPistonTop;
                    if (pistonTop == null || pistonTop.Piston == null)
                        continue;

                    IMyCubeGrid entityParent = pistonTop.Piston.CubeGrid;
                    if (!results.Any(e => e.EntityId == entityParent.EntityId))
                    {
                        results.Add(entityParent);
                        GetAttachedGrids(entityParent, ref results);
                    }
                }
                else if (block.FatBlock.BlockDefinition.TypeId == typeof (MyObjectBuilder_ExtendedPistonBase) ||
                         block.FatBlock.BlockDefinition.TypeId == typeof (MyObjectBuilder_PistonBase))
                {
                    IMyPistonBase pistonBase = block.FatBlock as IMyPistonBase;
                    if (pistonBase == null || pistonBase.Top == null)
                        continue;

                    IMyCubeGrid entityParent = pistonBase.TopGrid;
                    if (entityParent == null)
                        continue;
                    if (!results.Any(e => e.EntityId == entityParent.EntityId))
                    {
                        results.Add(entityParent);
                        GetAttachedGrids(entityParent, ref results);
                    }
                }
            }
        }

        public static Vector3D Center(this IEnumerable<IMyCubeGrid> grids)
        {
            Vector3D min = Vector3D.MaxValue;
            Vector3D max = Vector3D.MinValue;

            foreach (var grid in grids)
            {
                min = Vector3D.Min(min, grid.WorldAABB.Min);
                max = Vector3D.Max(max, grid.WorldAABB.Max);
            }

            return (min + max) / 2.0;
        }

        public static bool IsPowered(this IEnumerable<IMyCubeGrid> grids)
        {
            bool isPowered = false;
            foreach (var grid in grids)
            {
                isPowered |= grid.IsPowered();
            }

            return isPowered;
        }

        public static bool IsPowered(this IMyCubeGrid grid)
        {
            var blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks, f => f.FatBlock != null
                && f.FatBlock is IMyFunctionalBlock && f.FatBlock.IsWorking
                && ((IMyFunctionalBlock)f.FatBlock).Enabled
                && f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Reactor));

            return (blocks.Count > 0);
        }

        public static bool IsOwned(this IEnumerable<IMyCubeGrid> grids)
        {
            bool isOwned = false;
            foreach (var grid in grids)
            {
                isOwned |= grid.BigOwners.Any(v => v != 0);
                isOwned |= grid.SmallOwners.Any(v => v != 0);
            }

            return isOwned;
        }

        /// <summary>
        /// Determines if the player is an Administrator of the active game session.
        /// </summary>
        /// <param name="player"></param>
        /// <returns>True if is specified player is an Administrator in the active game.</returns>
        public static bool IsAdmin(this IMyPlayer player)
        {
            // Offline mode. You are the only player.
            if (MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE)
            {
                return true;
            }

            // Hosted game, and the player is hosting the server.
            if (MyAPIGateway.Multiplayer.IsServerPlayer(player.Client))
            {
                return true;
            }

            // determine if client is admin of Dedicated server.
            var clients = MyAPIGateway.Session.GetCheckpoint("null").Clients;
            if (clients != null)
            {
                var client = clients.FirstOrDefault(c => c.SteamId == player.SteamUserId && c.IsAdmin);
                return client != null;
                // If user is not in the list, automatically assume they are not an Admin.
            }

            // clients is null when it's not a dedicated server.
            // Otherwise Treat everyone as Normal Player.

            return false;
        }

        /// <summary>
        /// Determines if the player is an Author/Creator.
        /// This is used expressly for testing of commands that are not yet ready 
        /// to be released to the public, and should not be visible to the Help command list or accessible.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsExperimentalCreator(this IMyPlayer player)
        {
            switch (player.SteamUserId)
            {
                case 76561197961224864L:
                    return true;
            }

            return false;
        }

        public static void ShowMessage(this IMyUtilities utilities, string sender, string messageText, params object[] args)
        {
            utilities.ShowMessage(sender, string.Format(messageText, args));
        }



        /// <summary>
        /// Replacement for IMyCubeGrid.LocalAABB, as it doesn't resize when the cubegrid loses cubes.
        /// </summary>
        /// <param name="cubeGrid"></param>
        /// <returns></returns>
        public static BoundingBox LocalSize(this IMyCubeGrid cubeGrid)
        {
            var blocks = new List<IMySlimBlock>();
            cubeGrid.GetBlocks(blocks);
            var size = new BoundingBox();

            var min = new Vector3D(int.MaxValue, int.MaxValue, int.MaxValue);
            var max = new Vector3D(int.MinValue, int.MinValue, int.MinValue);

            foreach (var block in blocks)
            {
                MyCubeBlockDefinition definition;
                MyBlockOrientation orientation;
                Vector3I cubePos;
                if (block.FatBlock != null)
                {
                    definition = MyDefinitionManager.Static.GetCubeBlockDefinition(block.FatBlock.BlockDefinition);
                    orientation = block.FatBlock.Orientation;
                    cubePos = block.FatBlock.Position;
                    var ob = block.GetObjectBuilder();
                    cubePos = ob.Min;
                }
                else
                {
                    var ob = block.GetObjectBuilder();
                    definition = MyDefinitionManager.Static.GetCubeBlockDefinition(ob);
                    orientation = ob.BlockOrientation;
                    cubePos = ob.Min;
                }

                //var pos = block.Position;


                if (definition == null || (definition.Size.X == 1 && definition.Size.Y == 1 && definition.Size.Z == 1))
                {
                    MyAPIGateway.Utilities.ShowMessage(definition.Id.SubtypeName, "pos={1}: size={0}", definition.Size, block.Position, cubePos);

                    min = Vector3D.Min(min, block.Position);
                    max = Vector3D.Max(max, block.Position);
                }
                else
                {
                    // resolve the cube size acording to the cube's orientation.
                    var orientSize = definition.Size.Transform(orientation).Abs();
                    MyAPIGateway.Utilities.ShowMessage(definition.Id.SubtypeName, "pos={2}: p={3}: size={0}", definition.Size, orientSize, block.Position, cubePos);
                    //MyAPIGateway.Utilities.ShowMessage("block", "pos={2}: p={3}: size={0}: oSize={1}", definition.Size, orientSize, block.Position, cubePos);

                    min = Vector3D.Min(min, block.Position);
                    max = Vector3D.Max(max, block.Position + orientSize);
                }
            }

            var pos = cubeGrid.GetPosition();
            size = new BoundingBox((min * cubeGrid.GridSize) - (cubeGrid.GridSize / 2), (max * cubeGrid.GridSize) + (cubeGrid.GridSize / 2));

            //MyAPIGateway.Utilities.ShowMessage("final", "{0} : {1}", size, size.Size);
            //MyAPIGateway.Utilities.ShowMessage("local", "{0} : {1}", cubeGrid.LocalAABB, cubeGrid.LocalAABB.Size);

            return size;
        }

        public static Vector3I Add(this Vector3I size, int value)
        {
            return new Vector3I(size.X + value, size.Y + value, size.Z + value);
        }

        public static Vector3I Transform(this Vector3I size, MyBlockOrientation orientation)
        {
            var matrix = Matrix.CreateFromDir(Base6Directions.GetVector(orientation.Forward), Base6Directions.GetVector(orientation.Up));
            var rotation = Quaternion.CreateFromRotationMatrix(matrix);
            return Vector3I.Transform(size, rotation);
        }

        public static Vector3I Abs(this Vector3I size)
        {
            return new Vector3I(Math.Abs(size.X), Math.Abs(size.Y), Math.Abs(size.Z));
        }
    }
}