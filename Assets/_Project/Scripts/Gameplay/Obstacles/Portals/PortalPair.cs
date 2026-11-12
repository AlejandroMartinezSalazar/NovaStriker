using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Gameplay.Ball;
using UnityEngine;

namespace NovaStriker.Gameplay.Obstacles.Portals
{
    /// <summary>
    /// Une dos portales: el balón que entra por uno sale por el centro del otro con la misma velocidad y dirección
    /// (en el mundo), así que la orientación de las bocas es solo visual. Funciona en los dos sentidos.
    /// Para no entrar en bucle (portal debajo del otro), limita los viajes por ventana de tiempo.
    /// </summary>
    public sealed class PortalPair : MonoBehaviour, IObstacle
    {
        [SerializeField] private Portal _a;
        [SerializeField] private Portal _b;

        [Tooltip("Viajes máximos dentro de la ventana; si se superan, los portales dejan pasar el balón sin llevarlo.")]
        [SerializeField, Min(1)] private int _maxTeleports = 6;

        [Tooltip("Ventana (s) para contar los viajes.")]
        [SerializeField, Min(0.1f)] private float _window = 2f;

        private ObstacleServices _services;
        private TeleportBudget _budget;
        private bool _warnedLoop;

        public ObstacleType Type => ObstacleType.Portal;

        public void Construct(ObstacleServices services)
        {
            _services = services;
        }

        private void Awake()
        {
            _budget = new TeleportBudget(_maxTeleports, _window);
            if (_a == null || _b == null || _a == _b)
            {
                Debug.LogError($"[NovaStriker] {name}: PortalPair necesita dos portales distintos (A y B).", this);
                return;
            }

            _a.Bind(this);
            _b.Bind(this);
        }

        internal void Teleport(Portal entry, BallView ball, Rigidbody body)
        {
            Portal exit = entry == _a ? _b : entry == _b ? _a : null;
            if (exit == null)
            {
                return;
            }

            if (!_budget.TryConsume(Time.time))
            {
                if (!_warnedLoop)
                {
                    _warnedLoop = true;
                    Debug.LogWarning($"[NovaStriker] {name}: el balón entra en bucle entre los portales; se deja pasar.", this);
                }

                return;
            }

            Vector3 from = ball.Position;
            Vector3 to = exit.Center;
            exit.ExpectArrival(body);
            ball.TeleportTo(to);
            _services?.EventBus.Publish(new BallTeleportedEvent(from, to));
        }

        private void OnDrawGizmos()
        {
            if (_a == null || _b == null)
            {
                return;
            }

            Gizmos.color = new Color(0.6f, 0.4f, 1f, 0.8f);
            Gizmos.DrawLine(_a.transform.position, _b.transform.position);
        }
    }
}
