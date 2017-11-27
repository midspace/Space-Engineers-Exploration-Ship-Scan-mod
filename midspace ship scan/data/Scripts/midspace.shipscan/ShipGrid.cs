namespace midspace.shipscan
{
    using System.Collections.Generic;
    using System.Linq;
    using VRage.Game.ModAPI;
    using VRageMath;

    public enum MassCategory { Unknown = 0, Junk = 1, Tiny = 2, Small, Large, Huge, Enormous, Ridiculous };
    public enum SpeedCategory { Stationary = 0, Drifting = 3, Moving = 20, Flying };

    public class ShipGrid
    {
        public List<IMyCubeGrid> GridGroups { get; set; }

        public double Distance { get; set; }

        public Vector3D Position { get; set; }

        public IMyCubeGrid GetLargestGrid()
        {
            IMyCubeGrid largest = null;
            int maxCount = 0;
            foreach (var grid in GridGroups)
            {
                var blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks);

                if (maxCount < blocks.Count)
                {
                    maxCount = blocks.Count;
                    largest = grid;
                }
            }
            return largest;
        }

        public float GetMass()
        {
            return GridGroups.Where(g => g.Physics != null).Sum(g => g.Physics.Mass);
        }

        public int GetBlockCount()
        {
            int count = 0;

            foreach (var grid in GridGroups)
            {
                var blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks);

                count += blocks.Count;
            }

            return count;
        }

        public float GetCubedSize()
        {
            return (float)System.Math.Pow(GridGroups.Sum(g => g.LocalAABB.Size.X * g.LocalAABB.Size.Y * g.LocalAABB.Size.Z), 1 / 3d);
        }

        public MassCategory MassCategory
        {
            get
            {
                var mass = GetMass();
                var cubeCount = GetBlockCount();
                var cubedsize = GetCubedSize();

                if (cubedsize < 5 && cubeCount <=5) return MassCategory.Junk;
                if (cubedsize < 5) return MassCategory.Tiny;
                if (cubedsize < 10) return MassCategory.Small;
                if (cubedsize < 50) return MassCategory.Large;
                if (cubedsize < 100) return MassCategory.Huge;
                if (cubedsize < 500) return MassCategory.Enormous;


                //// Station's have no mass. Projections I'm not sure.  ;-(
                //if (mass == 0 || cubeCount == 0) return MassCategory.Unknown;
                //if (mass < 500 || cubeCount <= 2) return MassCategory.Junk;
                //if (mass < 3000 || cubeCount <= 10) return MassCategory.Tiny;
                //if (mass < 10000 || cubeCount <= 100) return MassCategory.Small;
                //if (mass < 100000 || cubeCount <= 500) return MassCategory.Large;
                //if (mass < 1000000 || cubeCount <= 1000) return MassCategory.Huge;
                //if (mass < 10000000 || cubeCount <= 10000) return MassCategory.Enormous;
                return MassCategory.Ridiculous;
            }
        }

        public SpeedCategory SpeedCategory
        {
            get
            {
                var grid = GetLargestGrid();
                var speed = grid.Physics != null ? grid.Physics.LinearVelocity.Length() : 0f;  // Can't use .Speed, as it doesn't preset the correct value under 01.063.008.
                if (speed == 0) return SpeedCategory.Stationary;
                if (speed < 3) return SpeedCategory.Drifting;
                if (speed < 20) return SpeedCategory.Moving;
                return SpeedCategory.Flying;
            }
        }
    }
}
