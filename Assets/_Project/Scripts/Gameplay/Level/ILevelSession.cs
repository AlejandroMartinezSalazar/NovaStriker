namespace NovaStriker.Gameplay.Level
{
    /// <summary>Carga y descarga del nivel en juego (escenario, futbolistas, tiro y reglas).</summary>
    public interface ILevelSession
    {
        int LevelCount { get; }

        /// <summary>Índice del nivel cargado, o -1 si no hay ninguno.</summary>
        int CurrentLevelIndex { get; }

        bool HasLevel(int levelIndex);

        /// <summary>Descarga el nivel actual (si lo hay) y construye <paramref name="levelIndex"/> desde cero.</summary>
        void Load(int levelIndex);

        void Unload();
    }
}
