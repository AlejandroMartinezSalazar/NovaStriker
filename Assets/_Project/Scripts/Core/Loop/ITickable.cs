namespace NovaStriker.Core.Loop
{
    /// <summary>Objeto C# puro que necesita actualizarse cada frame. Lo actualiza su raíz de composición.</summary>
    public interface ITickable
    {
        void Tick(float deltaTime);
    }
}
