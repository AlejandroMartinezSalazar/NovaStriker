namespace NovaStriker.Gameplay.Aim
{
    /// <summary>Parámetros de apuntado en C# puro (se construyen desde AimConfig).</summary>
    public readonly struct AimSettings
    {
        public readonly float MinAimDuration;
        public readonly float FullCurveRatio;
        public readonly float MinChordPixels;
        public readonly int TrajectorySamples;

        public AimSettings(float minAimDuration, float fullCurveRatio, float minChordPixels, int trajectorySamples)
        {
            MinAimDuration = minAimDuration;
            FullCurveRatio = fullCurveRatio;
            MinChordPixels = minChordPixels;
            TrajectorySamples = trajectorySamples;
        }

        public static AimSettings From(AimConfig config) =>
            new AimSettings(config.MinAimDuration, config.FullCurveRatio, config.MinChordPixels, config.TrajectorySamples);
    }
}
