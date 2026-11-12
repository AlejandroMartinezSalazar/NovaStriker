namespace NovaStriker.Contracts.Data
{
    /// <summary>
    /// Índices y nombres de las capas físicas del juego.
    /// Los índices los garantiza el setup de Etapa 0 (NovaStriker/Setup) y los verifica ProjectSetupTests.
    /// </summary>
    public static class GameLayers
    {
        public const int Ball = 6;
        public const int Ragdoll = 7;
        public const int Obstacle = 8;
        public const int Breakable = 9;
        public const int Environment = 10;
        public const int KillZone = 11;
        public const int Debris = 12;
        public const int AimBackstop = 13;

        /// <summary>Bocas de portal: triggers que solo detectan el balón.</summary>
        public const int Portal = 14;

        public const string BallName = "Ball";
        public const string RagdollName = "Ragdoll";
        public const string ObstacleName = "Obstacle";
        public const string BreakableName = "Breakable";
        public const string EnvironmentName = "Environment";
        public const string KillZoneName = "KillZone";
        public const string DebrisName = "Debris";
        public const string AimBackstopName = "AimBackstop";
        public const string PortalName = "Portal";

        /// <summary>Capas contra las que se resuelve el punto objetivo al apuntar.</summary>
        public const int AimMask =
            (1 << Ragdoll) | (1 << Obstacle) | (1 << Breakable) | (1 << Environment) | (1 << AimBackstop);

        /// <summary>Superficies sobre las que un futbolista puede estar de pie (sondeo de suelo).</summary>
        public const int CharacterSupportMask =
            (1 << Environment) | (1 << Obstacle) | (1 << Breakable) | (1 << Ragdoll);
    }
}
