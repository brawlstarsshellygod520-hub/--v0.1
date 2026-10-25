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

        public bool IsBurning => Intensity > .05f;
    }
}
