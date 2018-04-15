namespace MidSpace.ShipScan.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    internal static class Extensions
    {
        #region grid

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

            // Should include connections via: Landing Gear, Connectors, Rotors, Pistons, Suspension.
            return MyAPIGateway.GridGroups.GetGroup(cubeGrid, GridLinkTypeEnum.Physical);
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

        //        ///// <summary>
        //        ///// Replacement for IMyCubeGrid.LocalAABB, as it doesn't resize when the cubegrid loses cubes.
        //        ///// </summary>
        //        ///// <param name="cubeGrid"></param>
        //        ///// <returns></returns>
        //        //public static BoundingBox LocalSize(this IMyCubeGrid cubeGrid)
        //        //{
        //        //    var blocks = new List<IMySlimBlock>();
        //        //    cubeGrid.GetBlocks(blocks);
        //        //    var size = new BoundingBox();

        //        //    var min = new Vector3D(int.MaxValue, int.MaxValue, int.MaxValue);
        //        //    var max = new Vector3D(int.MinValue, int.MinValue, int.MinValue);

        //        //    foreach (var block in blocks)
        //        //    {
        //        //        MyCubeBlockDefinition definition;
        //        //        MyBlockOrientation orientation;
        //        //        Vector3I cubePos;
        //        //        if (block.FatBlock != null)
        //        //        {
        //        //            definition = MyDefinitionManager.Static.GetCubeBlockDefinition(block.FatBlock.BlockDefinition);
        //        //            orientation = block.FatBlock.Orientation;
        //        //            cubePos = block.FatBlock.Position;
        //        //            var ob = block.GetObjectBuilder();
        //        //            cubePos = ob.Min;
        //        //        }
        //        //        else
        //        //        {
        //        //            var ob = block.GetObjectBuilder();
        //        //            definition = MyDefinitionManager.Static.GetCubeBlockDefinition(ob);
        //        //            orientation = ob.BlockOrientation;
        //        //            cubePos = ob.Min;
        //        //        }

        //        //        //var pos = block.Position;


        //        //        if (definition == null || (definition.Size.X == 1 && definition.Size.Y == 1 && definition.Size.Z == 1))
        //        //        {
        //        //            MyAPIGateway.Utilities.ShowMessage(definition.Id.SubtypeName, "pos={1}: size={0}", definition.Size, block.Position, cubePos);

        //        //            min = Vector3D.Min(min, block.Position);
        //        //            max = Vector3D.Max(max, block.Position);
        //        //        }
        //        //        else
        //        //        {
        //        //            // resolve the cube size acording to the cube's orientation.
        //        //            var orientSize = definition.Size.Transform(orientation).Abs();
        //        //            MyAPIGateway.Utilities.ShowMessage(definition.Id.SubtypeName, "pos={2}: p={3}: size={0}", definition.Size, orientSize, block.Position, cubePos);
        //        //            //MyAPIGateway.Utilities.ShowMessage("block", "pos={2}: p={3}: size={0}: oSize={1}", definition.Size, orientSize, block.Position, cubePos);

        //        //            min = Vector3D.Min(min, block.Position);
        //        //            max = Vector3D.Max(max, block.Position + orientSize);
        //        //        }
        //        //    }

        //        //    var pos = cubeGrid.GetPosition();
        //        //    size = new BoundingBox((min * cubeGrid.GridSize) - (cubeGrid.GridSize / 2), (max * cubeGrid.GridSize) + (cubeGrid.GridSize / 2));

        //        //    //MyAPIGateway.Utilities.ShowMessage("final", "{0} : {1}", size, size.Size);
        //        //    //MyAPIGateway.Utilities.ShowMessage("local", "{0} : {1}", cubeGrid.LocalAABB, cubeGrid.LocalAABB.Size);

        //        //    return size;
        //        //}

        //        #endregion

        //        #region Math

        //        //public static Vector3I Add(this Vector3I size, int value)
        //        //{
        //        //    return new Vector3I(size.X + value, size.Y + value, size.Z + value);
        //        //}

        //        //public static Vector3I Transform(this Vector3I size, MyBlockOrientation orientation)
        //        //{
        //        //    var matrix = Matrix.CreateFromDir(Base6Directions.GetVector(orientation.Forward), Base6Directions.GetVector(orientation.Up));
        //        //    var rotation = Quaternion.CreateFromRotationMatrix(matrix);
        //        //    return Vector3I.Transform(size, rotation);
        //        //}

        //        //public static Vector3I Abs(this Vector3I size)
        //        //{
        //        //    return new Vector3I(Math.Abs(size.X), Math.Abs(size.Y), Math.Abs(size.Z));
        //        //} 

        #endregion

        #region serialization

        public static bool TryWordParseBool(this string value, out bool result)
        {
            bool boolTest;
            if (bool.TryParse(value, out boolTest))
            {
                result = boolTest;
                return true;
            }

            if (value.Equals("on", StringComparison.InvariantCultureIgnoreCase) || value.Equals("yes", StringComparison.InvariantCultureIgnoreCase) || value.Equals("1", StringComparison.InvariantCultureIgnoreCase))
            {
                result = true;
                return true;
            }

            if (value.Equals("off", StringComparison.InvariantCultureIgnoreCase) || value.Equals("no", StringComparison.InvariantCultureIgnoreCase) || value.Equals("0", StringComparison.InvariantCultureIgnoreCase))
            {
                result = false;
                return true;
            }

            result = false;
            return false;
        }

        #endregion
    }
}