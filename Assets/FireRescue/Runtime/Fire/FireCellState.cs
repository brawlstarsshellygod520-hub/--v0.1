namespace FireRescue
{
    /// <summary>
    /// Runtime state for one floor cell. FireField is the sole writer for this state.
    /// </summary>
    public struct FireCellState
    {
        public float Fuel;
        public float Oil;
        public float Intensity;
        public float Wetness;
        // A cell keeps one aggregate residue timer per fluid. Reapplying a timed
        // spray uses the larger remaining duration; repeated spray never shortens it.
        public float WaterRemaining;
        public float OilRemaining;
        public float PowderRemaining;
        public float Powder;

        public bool IsBurning => Intensity > .05f;
    }
}
