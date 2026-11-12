using System;
using System.Collections.Generic;
using UnityEngine;

namespace NovaStriker.Gameplay.Obstacles
{
    /// <summary>
    /// Movimiento por puntos de ruta como función pura del tiempo: Evaluate(t) siempre da el mismo resultado,
    /// así no se acumula error y el movimiento se puede reiniciar o adelantar sin deriva.
    /// En cada punto se espera <c>waitAtWaypoint</c> segundos antes de salir hacia el siguiente.
    /// </summary>
    public sealed class PathMotion
    {
        private readonly struct Segment
        {
            public readonly Vector3 From;
            public readonly Vector3 To;
            public readonly float Duration;

            public Segment(Vector3 from, Vector3 to, float duration)
            {
                From = from;
                To = to;
                Duration = duration;
            }
        }

        private readonly List<Segment> _segments = new List<Segment>();
        private readonly float _wait;
        private readonly bool _smooth;
        private readonly Vector3 _rest;

        /// <param name="waypoints">Al menos 2 para que haya movimiento.</param>
        /// <param name="speed">Velocidad (m/s) por los tramos.</param>
        /// <param name="waitAtWaypoint">Pausa (s) en cada punto.</param>
        /// <param name="mode">Ida y vuelta o circuito.</param>
        /// <param name="smooth">Acelera y frena suavemente en cada tramo.</param>
        public PathMotion(IReadOnlyList<Vector3> waypoints, float speed, float waitAtWaypoint, PathMode mode, bool smooth)
        {
            if (waypoints == null)
            {
                throw new ArgumentNullException(nameof(waypoints));
            }

            _wait = Mathf.Max(0f, waitAtWaypoint);
            _smooth = smooth;
            _rest = waypoints.Count > 0 ? waypoints[0] : Vector3.zero;

            if (waypoints.Count < 2 || speed <= 0f)
            {
                return; // Sin movimiento: se queda en el primer punto.
            }

            var order = new List<int>();
            for (int i = 0; i < waypoints.Count; i++)
            {
                order.Add(i);
            }

            if (mode == PathMode.PingPong)
            {
                for (int i = waypoints.Count - 2; i >= 0; i--)
                {
                    order.Add(i);
                }
            }
            else
            {
                order.Add(0);
            }

            for (int i = 0; i < order.Count - 1; i++)
            {
                Vector3 from = waypoints[order[i]];
                Vector3 to = waypoints[order[i + 1]];
                _segments.Add(new Segment(from, to, Vector3.Distance(from, to) / speed));
            }

            foreach (Segment segment in _segments)
            {
                CycleDuration += _wait + segment.Duration;
            }
        }

        /// <summary>Duración (s) de una vuelta completa. 0 si no hay movimiento.</summary>
        public float CycleDuration { get; }

        public bool IsMoving => CycleDuration > 0f;

        /// <summary>Posición (relativa al origen del objeto) en el instante <paramref name="time"/>.</summary>
        public Vector3 Evaluate(float time)
        {
            if (!IsMoving)
            {
                return _rest;
            }

            float t = Mathf.Repeat(time, CycleDuration);
            foreach (Segment segment in _segments)
            {
                if (t < _wait)
                {
                    return segment.From;
                }
                t -= _wait;

                if (t < segment.Duration)
                {
                    float k = segment.Duration > 0f ? t / segment.Duration : 1f;
                    if (_smooth)
                    {
                        k = k * k * (3f - 2f * k); // Smoothstep: velocidad cero en los extremos.
                    }
                    return Vector3.LerpUnclamped(segment.From, segment.To, k);
                }
                t -= segment.Duration;
            }

            return _segments[_segments.Count - 1].To;
        }
    }
}
