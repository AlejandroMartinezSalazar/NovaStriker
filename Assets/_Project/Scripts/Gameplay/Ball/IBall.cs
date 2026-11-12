using NovaStriker.Gameplay.Throwing;
using UnityEngine;

namespace NovaStriker.Gameplay.Ball
{
    /// <summary>Lo que el ThrowController necesita de un balón. Permite probarlo sin física.</summary>
    public interface IBall
    {
        Vector3 Position { get; }

        void Launch(in ThrowCommand command);
    }
}
