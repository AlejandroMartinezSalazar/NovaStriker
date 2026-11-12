namespace NovaStriker.Contracts.Data
{
    /// <summary>Motivo por el que un futbolista pasó a ragdoll.</summary>
    public enum KnockdownCause
    {
        None = 0,
        /// <summary>Golpe directo del balón.</summary>
        BallHit = 1,
        /// <summary>Perdió el apoyo bajo los pies o cae demasiado rápido.</summary>
        Fall = 2,
        /// <summary>La superficie de apoyo se inclinó más del límite.</summary>
        Tilt = 3,
        /// <summary>Lo arrastraron o empujaron de lado demasiado rápido.</summary>
        Pushed = 4,
        /// <summary>Impacto fuerte de una caja, una pared u otro futbolista.</summary>
        Impact = 5,
        /// <summary>Entró en una KillZone.</summary>
        OutOfBounds = 6,
    }
}
