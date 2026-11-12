namespace NovaStriker.Gameplay.Level.Rules
{
    /// <summary>Foto del estado del nivel que consultan las condiciones de victoria y derrota.</summary>
    public readonly struct LevelStatus
    {
        public readonly int TargetsTotal;
        public readonly int TargetsDown;
        public readonly int NonTargetsDown;
        public readonly int BallsTotal;
        public readonly int BallsUsed;

        public LevelStatus(int targetsTotal, int targetsDown, int nonTargetsDown, int ballsTotal, int ballsUsed)
        {
            TargetsTotal = targetsTotal;
            TargetsDown = targetsDown;
            NonTargetsDown = nonTargetsDown;
            BallsTotal = ballsTotal;
            BallsUsed = ballsUsed;
        }

        public int TargetsRemaining => TargetsTotal - TargetsDown;
        public int BallsRemaining => BallsTotal - BallsUsed;
        public bool AllTargetsDown => TargetsTotal > 0 && TargetsDown >= TargetsTotal;
    }
}
