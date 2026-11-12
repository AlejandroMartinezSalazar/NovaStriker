using NovaStriker.Contracts.Data;
using NovaStriker.Core.Events;

namespace NovaStriker.Contracts.Events
{
    /// <summary>Se construyó un nivel y empieza la partida (también al reiniciarlo).</summary>
    public readonly struct LevelStartedEvent : IGameEvent
    {
        /// <summary>Índice en el catálogo (0 = primer nivel).</summary>
        public readonly int LevelIndex;
        public readonly int LevelCount;
        public readonly string DisplayName;

        public LevelStartedEvent(int levelIndex, int levelCount, string displayName)
        {
            LevelIndex = levelIndex;
            LevelCount = levelCount;
            DisplayName = displayName;
        }

        /// <summary>Número para mostrar (1 = primer nivel).</summary>
        public int LevelNumber => LevelIndex + 1;
    }

    /// <summary>Cambió el número de objetivos derribados o de balones usados. Lo usa el HUD.</summary>
    public readonly struct LevelProgressChangedEvent : IGameEvent
    {
        public readonly int TargetsRemaining;
        public readonly int TargetsTotal;
        public readonly int BallsRemaining;
        public readonly int BallsTotal;

        public LevelProgressChangedEvent(int targetsRemaining, int targetsTotal, int ballsRemaining, int ballsTotal)
        {
            TargetsRemaining = targetsRemaining;
            TargetsTotal = targetsTotal;
            BallsRemaining = ballsRemaining;
            BallsTotal = ballsTotal;
        }
    }

    /// <summary>Se superó el nivel. IsLastLevel indica que tras él viene la pantalla "Próximamente" (D1).</summary>
    public readonly struct LevelCompletedEvent : IGameEvent
    {
        public readonly int LevelIndex;
        public readonly bool IsLastLevel;
        public readonly int BallsRemaining;

        public LevelCompletedEvent(int levelIndex, bool isLastLevel, int ballsRemaining)
        {
            LevelIndex = levelIndex;
            IsLastLevel = isLastLevel;
            BallsRemaining = ballsRemaining;
        }
    }

    /// <summary>Se perdió el nivel.</summary>
    public readonly struct LevelFailedEvent : IGameEvent
    {
        public readonly int LevelIndex;
        public readonly DefeatReason Reason;

        public LevelFailedEvent(int levelIndex, DefeatReason reason)
        {
            LevelIndex = levelIndex;
            Reason = reason;
        }
    }

    /// <summary>No quedan niveles: se muestra "Próximamente nuevos niveles" (D1).</summary>
    public readonly struct CampaignCompletedEvent : IGameEvent
    {
        public readonly int LevelCount;

        public CampaignCompletedEvent(int levelCount)
        {
            LevelCount = levelCount;
        }
    }

    /// <summary>Se publica tras cada transición de la máquina de estados de la partida.</summary>
    public readonly struct GameStateChangedEvent : IGameEvent
    {
        public readonly GameStateId From;
        public readonly GameStateId To;

        public GameStateChangedEvent(GameStateId from, GameStateId to)
        {
            From = from;
            To = to;
        }
    }
}
