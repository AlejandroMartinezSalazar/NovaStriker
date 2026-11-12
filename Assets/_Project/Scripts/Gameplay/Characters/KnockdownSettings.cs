using System;
using UnityEngine;

namespace NovaStriker.Gameplay.Characters
{
    /// <summary>Umbrales que deciden cuándo un futbolista pasa a ragdoll.</summary>
    [Serializable]
    public struct KnockdownSettings
    {
        [Tooltip("Tras aparecer, durante este tiempo (s) se ignoran caída/inclinación/arrastre para que se asiente.")]
        [Min(0f)] public float ArmingDelay;

        [Header("Caída")]
        [Tooltip("Tiempo (s) seguido sin apoyo bajo los pies para considerarlo una caída.")]
        [Min(0f)] public float UngroundedGrace;

        [Tooltip("Velocidad vertical hacia abajo (m/s) que dispara la caída de inmediato.")]
        [Min(0f)] public float FallSpeed;

        [Header("Inclinación")]
        [Tooltip("Inclinación máxima (grados) de la superficie de apoyo.")]
        [Range(0f, 90f)] public float MaxSupportTiltDegrees;

        [Tooltip("Tiempo (s) seguido por encima de la inclinación máxima (filtra bordes y ruido).")]
        [Min(0f)] public float TiltGrace;

        [Header("Arrastre")]
        [Tooltip("Velocidad horizontal (m/s) a partir de la cual se considera que lo empujan o arrastran.")]
        [Min(0f)] public float PushedSpeed;

        [Header("Choques")]
        [Tooltip("Velocidad relativa mínima (m/s) del balón para derribar.")]
        [Min(0f)] public float BallHitMinSpeed;

        [Tooltip("Velocidad relativa mínima (m/s) de cajas, paredes u otros futbolistas para derribar.")]
        [Min(0f)] public float ImpactMinSpeed;

        public static KnockdownSettings Default => new KnockdownSettings
        {
            ArmingDelay = 0.5f,
            UngroundedGrace = 0.2f,
            FallSpeed = 2.5f,
            MaxSupportTiltDegrees = 25f,
            TiltGrace = 0.08f,
            PushedSpeed = 1.5f,
            BallHitMinSpeed = 2f,
            ImpactMinSpeed = 2.5f,
        };
    }
}
