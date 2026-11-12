using UnityEngine;

namespace NovaStriker.Gameplay.Throwing
{
    /// <summary>Datos de entrada para que una estrategia calcule un tiro.</summary>
    public readonly struct ThrowContext
    {
        /// <summary>Posición del balón antes del lanzamiento.</summary>
        public readonly Vector3 Origin;

        /// <summary>Punto del mundo bajo el dedo al levantarlo (o el actual, para la previsualización).</summary>
        public readonly Vector3 Target;

        /// <summary>Gravedad de la física (Physics.gravity).</summary>
        public readonly Vector3 Gravity;

        /// <summary>Paso fijo de la física (Time.fixedDeltaTime). Se usa para compensar la integración discreta.</summary>
        public readonly float FixedDeltaTime;

        /// <summary>Curvatura del gesto en [-1, 1]. Positivo: el trazo se arqueó hacia la derecha de la pantalla.</summary>
        public readonly float CurveBias;

        public ThrowContext(Vector3 origin, Vector3 target, Vector3 gravity, float fixedDeltaTime, float curveBias)
        {
            Origin = origin;
            Target = target;
            Gravity = gravity;
            FixedDeltaTime = fixedDeltaTime;
            CurveBias = curveBias;
        }
    }
}
