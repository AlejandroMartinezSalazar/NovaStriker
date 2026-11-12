namespace NovaStriker.Contracts.Data
{
    /// <summary>Tipos de obstáculo que se pueden colocar en un nivel.</summary>
    public enum ObstacleType
    {
        None = 0,
        /// <summary>Caja dinámica: se apila y el balón la derriba.</summary>
        Box = 1,
        /// <summary>Caja estática: puede flotar en el aire y sostener futbolistas.</summary>
        StaticBox = 2,
        /// <summary>Muro estático muy rebotador, para carambolas.</summary>
        Wall = 3,
        /// <summary>Vidrio irrompible: deja ver, pero bloquea balón y apuntado.</summary>
        Glass = 4,
        /// <summary>Vidrio que se agrieta por etapas y se rompe.</summary>
        BreakableGlass = 5,
        /// <summary>Muro que se mueve por una ruta.</summary>
        MovingWall = 6,
        /// <summary>Plataforma que se mueve por una ruta llevando futbolistas.</summary>
        MovingPlatform = 7,
        /// <summary>Caja de vidrio rompible y desplazable con cajas dentro, que caen al romperse.</summary>
        GlassCrate = 8,
        Portal = 9,
    }
}
