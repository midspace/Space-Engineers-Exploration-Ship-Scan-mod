namespace midspace.shipscan
{
    using System;
    using System.Collections.Generic;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    public static class Support
    {
        internal static Vector2 GetRotationAngle(MatrixD itemMatrix, Vector3D targetVector)
        {
            targetVector = Vector3D.Normalize(targetVector);
            // http://stackoverflow.com/questions/10967130/how-to-calculate-azimut-elevation-relative-to-a-camera-direction-of-view-in-3d
            // rotate so the camera is pointing straight down the z axis
            // (this is essentially a matrix multiplication)
            var obj = new Vector3D(Vector3D.Dot(targetVector, itemMatrix.Right), Vector3D.Dot(targetVector, itemMatrix.Up), Vector3D.Dot(targetVector, itemMatrix.Forward));
            var azimuth = Math.Atan2(obj.X, obj.Z);

            var proj = new Vector3D(obj.X, 0, obj.Z);
            var nrml = Vector3D.Dot(obj, proj);

            var elevation = Math.Acos(nrml);
            if (obj.Y < 0)
                elevation = -elevation;

            if (double.IsNaN(azimuth)) azimuth = 0;
            if (double.IsNaN(elevation)) elevation = 0;

            // Roll is not provided, as target is merely a direction.
            return new Vector2((float)(azimuth * 180 / Math.PI), (float)(elevation * 180 / Math.PI));
        }

        #region Find Cube in Grid

        public static IMyCubeBlock FindRotorBase(long entityId, IMyCubeGrid parent = null)
        {
            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, e => e is IMyCubeGrid);

            foreach (var entity in entities)
            {
                var cubeGrid = (IMyCubeGrid)entity;

                var blocks = new List<IMySlimBlock>();
                cubeGrid.GetBlocks(blocks, block => block != null && block.FatBlock != null &&
                    (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorAdvancedStator) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorStator) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorSuspension) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorBase)));

                foreach (var block in blocks)
                {
                    var motorBase = block.GetObjectBuilder() as MyObjectBuilder_MechanicalConnectionBlock;

                    if (motorBase == null || !motorBase.TopBlockId.HasValue || motorBase.TopBlockId.Value == 0 || !MyAPIGateway.Entities.EntityExists(motorBase.TopBlockId.Value))
                        continue;

                    if (motorBase.TopBlockId == entityId)
                        return block.FatBlock;
                }
            }

            return null;
        }

        #endregion
    }
}