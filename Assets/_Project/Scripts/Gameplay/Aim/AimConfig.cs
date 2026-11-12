using UnityEngine;

namespace NovaStriker.Gameplay.Aim
{
    [CreateAssetMenu(menuName = "NovaStriker/Config/Aim Config", fileName = "SO_AimConfig")]
    public sealed class AimConfig : ScriptableObject
    {
        [Tooltip("Duración mínima (s) entre poner y levantar el dedo para disparar. 0 = cualquier toque dispara (riesgo 3c).")]
        [Min(0f)] public float MinAimDuration = 0f;

        [Tooltip("Distancia máxima (m) del raycast de apuntado.")]
        [Min(1f)] public float RaycastMaxDistance = 200f;

        [Tooltip("Desviación del trazo, relativa a su longitud, que equivale a curva máxima.")]
        [Range(0.05f, 1f)] public float FullCurveRatio = 0.25f;

        [Tooltip("Longitud mínima del trazo (px) para que cuente su curvatura.")]
        [Min(0f)] public float MinChordPixels = 40f;

        [Tooltip("Puntos de la línea de trayectoria.")]
        [Range(2, 128)] public int TrajectorySamples = 40;
    }
}
