namespace NovaStriker.Gameplay.Throwing
{
    /// <summary>
    /// Strategy: forma de llevar el balón al punto objetivo (recto, bombeado, con curva...).
    /// Cada nivel define la suya. Todas pasan por el objetivo; solo cambian tiempo de vuelo y aceleración extra.
    /// </summary>
    public interface IThrowStrategy
    {
        /// <summary>Identificador estable (para eventos, analítica y audio).</summary>
        string Id { get; }

        /// <summary>Calcula el tiro. Devuelve false si el objetivo no es alcanzable con esta estrategia.</summary>
        bool TryCreateThrow(in ThrowContext context, out ThrowCommand command);
    }
}
