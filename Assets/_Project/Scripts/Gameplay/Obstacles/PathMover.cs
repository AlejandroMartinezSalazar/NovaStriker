using UnityEngine;

namespace NovaStriker.Gameplay.Obstacles
{
    /// <summary>
    /// Mueve un Rigidbody kinemático por puntos de ruta (muros y plataformas móviles).
    /// Todo se edita por instancia en el Inspector; los puntos también se arrastran en la escena.
    /// Los puntos son desplazamientos (en ejes del mundo) desde la posición inicial del objeto:
    /// si mueves el objeto en el nivel, su ruta se mueve con él.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PathMover : MonoBehaviour, ISupportSurface
    {
        [SerializeField] private Rigidbody _body;

        [Tooltip("Desplazamientos desde la posición inicial (ejes del mundo). El primero suele ser (0,0,0).")]
        [SerializeField] private Vector3[] _waypoints = { Vector3.zero, new Vector3(4f, 0f, 0f) };

        [Tooltip("Velocidad (m/s) por los tramos.")]
        [SerializeField, Min(0f)] private float _speed = 2f;

        [Tooltip("Pausa (s) en cada punto antes de salir hacia el siguiente.")]
        [SerializeField, Min(0f)] private float _waitAtWaypoint = 0.5f;

        [Tooltip("Ida y vuelta, o circuito cerrado que vuelve al primer punto.")]
        [SerializeField] private PathMode _mode = PathMode.PingPong;

        [Tooltip("Acelera y frena suavemente en cada tramo (recomendado para plataformas con futbolistas).")]
        [SerializeField] private bool _smooth = true;

        [Tooltip("Desfase inicial como fracción de la vuelta (0-1). Útil para que varios muros no vayan sincronizados.")]
        [SerializeField, Range(0f, 1f)] private float _startOffset;

        [Tooltip("Retraso (s) antes de empezar a moverse.")]
        [SerializeField, Min(0f)] private float _startDelay;

        private PathMotion _motion;
        private Vector3 _origin;
        private float _elapsed;
        private Vector3 _lastPosition;

        public Vector3 Origin => Application.isPlaying ? _origin : transform.position;
        public Vector3 Velocity { get; private set; }

        private void Awake()
        {
            if (_body == null)
            {
                _body = GetComponent<Rigidbody>();
            }

            _body.isKinematic = true;
            _origin = transform.position;
            _lastPosition = _origin;
            _motion = new PathMotion(_waypoints, _speed, _waitAtWaypoint, _mode, _smooth);
            _elapsed = _startOffset * _motion.CycleDuration - _startDelay;
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            _elapsed += deltaTime;

            Vector3 target = _origin + _motion.Evaluate(Mathf.Max(_elapsed, _startOffset * _motion.CycleDuration));
            Velocity = deltaTime > 0f ? (target - _lastPosition) / deltaTime : Vector3.zero;
            _lastPosition = target;
            _body.MovePosition(target);
        }

        /// <summary>Posición en el mundo del punto <paramref name="index"/> (para gizmos y el editor).</summary>
        public Vector3 GetWaypointWorld(int index) => Origin + _waypoints[index];

        public int WaypointCount => _waypoints?.Length ?? 0;

        private void OnDrawGizmosSelected()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.6f, 0.1f);
            for (int i = 0; i < _waypoints.Length; i++)
            {
                Vector3 point = GetWaypointWorld(i);
                Gizmos.DrawWireSphere(point, 0.15f);
                if (i > 0)
                {
                    Gizmos.DrawLine(GetWaypointWorld(i - 1), point);
                }
            }

            if (_mode == PathMode.Loop && _waypoints.Length > 2)
            {
                Gizmos.DrawLine(GetWaypointWorld(_waypoints.Length - 1), GetWaypointWorld(0));
            }
        }
    }
}
