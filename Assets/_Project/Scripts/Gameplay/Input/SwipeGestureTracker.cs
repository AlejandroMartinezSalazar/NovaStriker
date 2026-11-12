using System.Collections.Generic;
using UnityEngine;

namespace NovaStriker.Gameplay.Input
{
    /// <summary>
    /// Registra el trazo del dedo y calcula su curvatura.
    /// La curvatura es la mayor desviación del trazo respecto a la cuerda inicio→fin, con signo
    /// (positivo a la derecha de la cuerda), dividida por <c>fullCurveRatio · longitudCuerda</c> y limitada a [-1, 1].
    /// </summary>
    public sealed class SwipeGestureTracker
    {
        private readonly List<Vector2> _points = new List<Vector2>(64);
        private readonly float _fullCurveRatio;
        private readonly float _minChordPixels;
        private float _startTime;

        /// <param name="fullCurveRatio">Desviación (relativa a la cuerda) que equivale a curvatura máxima. P. ej. 0.25.</param>
        /// <param name="minChordPixels">Con cuerdas más cortas la curvatura es 0 (evita ruido en toques).</param>
        public SwipeGestureTracker(float fullCurveRatio, float minChordPixels)
        {
            _fullCurveRatio = Mathf.Max(0.01f, fullCurveRatio);
            _minChordPixels = Mathf.Max(0f, minChordPixels);
        }

        public bool IsTracking { get; private set; }

        public void Begin(Vector2 position, float time)
        {
            _points.Clear();
            _points.Add(position);
            _startTime = time;
            IsTracking = true;
        }

        public void AddPoint(Vector2 position)
        {
            if (!IsTracking)
            {
                return;
            }

            if (_points[_points.Count - 1] != position)
            {
                _points.Add(position);
            }
        }

        /// <summary>Curvatura si el gesto terminara ahora en <paramref name="currentEnd"/> (para la previsualización).</summary>
        public float EvaluateCurveBias(Vector2 currentEnd)
        {
            return IsTracking ? ComputeCurveBias(_points, currentEnd) : 0f;
        }

        public SwipeData End(Vector2 position, float time)
        {
            AddPoint(position);
            Vector2 start = _points.Count > 0 ? _points[0] : position;
            float bias = ComputeCurveBias(_points, position);
            IsTracking = false;
            return new SwipeData(start, position, Mathf.Max(0f, time - _startTime), bias);
        }

        public void Cancel()
        {
            IsTracking = false;
            _points.Clear();
        }

        private float ComputeCurveBias(List<Vector2> points, Vector2 end)
        {
            if (points.Count < 2)
            {
                return 0f;
            }

            Vector2 start = points[0];
            Vector2 chord = end - start;
            float chordLength = chord.magnitude;
            if (chordLength < _minChordPixels || chordLength < 1e-3f)
            {
                return 0f;
            }

            Vector2 direction = chord / chordLength;
            float strongest = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 offset = points[i] - start;
                // Producto cruzado 2D: negativo = a la derecha de la dirección. Se invierte para que derecha sea positivo.
                float rightward = -(direction.x * offset.y - direction.y * offset.x);
                if (Mathf.Abs(rightward) > Mathf.Abs(strongest))
                {
                    strongest = rightward;
                }
            }

            return Mathf.Clamp(strongest / (_fullCurveRatio * chordLength), -1f, 1f);
        }
    }
}
