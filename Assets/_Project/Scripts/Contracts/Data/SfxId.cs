namespace NovaStriker.Contracts.Data
{
    /// <summary>Efectos de sonido del juego. Cada uno se configura en SO_SoundLibrary (clips, volumen, variación).</summary>
    public enum SfxId
    {
        None = 0,
        Aim = 1,
        Kick = 2,
        BallGround = 3,
        BallBox = 4,
        BallWall = 5,
        BallBody = 6,
        Knockdown = 7,
        GlassCrack = 8,
        GlassBreak = 9,
        BoxImpact = 10,
        Victory = 11,
        Defeat = 12,
        Click = 13,
        Whoosh = 14,
        Portal = 15,
    }
}
