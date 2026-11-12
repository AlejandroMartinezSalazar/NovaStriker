using System;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Contracts.Services;
using NovaStriker.Core.Events;
using UnityEngine;

namespace NovaStriker.Infrastructure.Online
{
    /// <summary>
    /// Inicio de sesión automático y sin contraseña. No bloquea nada: el juego arranca a la vez y, si falla
    /// (sin red, servidor caído, tiempo agotado), se reintenta solo con esperas crecientes; agotados los reintentos,
    /// se vuelve a probar cuando la app regresa del segundo plano. Recibe el tiempo por <see cref="Tick"/>.
    /// </summary>
    public sealed class PlayerAccountService : IPlayerAccount
    {
        internal static readonly float[] DefaultRetryDelays = { 5f, 20f, 60f };
        public const float DefaultTimeoutSeconds = 15f;

        private readonly ILoginProvider _provider;
        private readonly string _customId;
        private readonly IEventBus _eventBus;
        private readonly float _timeoutSeconds;
        private readonly float[] _retryDelays;

        private int _attempt;
        private int _retryIndex;
        private float _timeLeft;
        private float _retryTimer = -1f;

        public PlayerAccountService(ILoginProvider provider, string customId, IEventBus eventBus,
            float timeoutSeconds = DefaultTimeoutSeconds, float[] retryDelays = null)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _customId = customId;
            _eventBus = eventBus;
            _timeoutSeconds = timeoutSeconds > 0f ? timeoutSeconds : DefaultTimeoutSeconds;
            _retryDelays = retryDelays ?? DefaultRetryDelays;
        }

        public LoginStatus Status { get; private set; } = LoginStatus.NotStarted;
        public string PlayerId { get; private set; }
        public bool IsLoggedIn => Status == LoginStatus.LoggedIn;

        /// <summary>Intentos hechos (para pruebas y diagnóstico).</summary>
        internal int Attempts => _attempt;

        public event Action<LoginStatus> StatusChanged;

        /// <summary>Empieza el inicio de sesión. Llamar una vez al arrancar la app.</summary>
        public void Start()
        {
            if (Status != LoginStatus.NotStarted)
            {
                return;
            }

            if (!_provider.IsAvailable || string.IsNullOrEmpty(_customId))
            {
                string reason = _provider.IsAvailable ? "no hay id de jugador" : _provider.UnavailableReason;
                Debug.LogWarning($"[NovaStriker] Sin inicio de sesión ({reason}). El juego funciona igual, sin cuenta.");
                SetStatus(LoginStatus.Disabled);
                return;
            }

            Attempt();
        }

        public void Tick(float deltaTime)
        {
            if (Status == LoginStatus.InProgress)
            {
                _timeLeft -= deltaTime;
                if (_timeLeft <= 0f)
                {
                    Fail($"sin respuesta en {_timeoutSeconds:0} s");
                }
            }
            else if (Status == LoginStatus.Offline && _retryTimer > 0f)
            {
                _retryTimer -= deltaTime;
                if (_retryTimer <= 0f)
                {
                    Attempt();
                }
            }
        }

        /// <summary>La app vuelve del segundo plano: si se agotaron los reintentos, se prueba otra tanda.</summary>
        public void OnAppResumed()
        {
            if (Status == LoginStatus.Offline && _retryTimer <= 0f)
            {
                _retryIndex = 0;
                Attempt();
            }
        }

        private void Attempt()
        {
            int attempt = ++_attempt;
            _retryTimer = -1f;
            _timeLeft = _timeoutSeconds;
            SetStatus(LoginStatus.InProgress);

            try
            {
                _provider.Login(_customId, outcome => OnOutcome(attempt, outcome));
            }
            catch (Exception exception)
            {
                // Un fallo del SDK nunca debe tumbar el arranque.
                OnOutcome(attempt, LoginOutcome.Failed(exception.Message));
            }
        }

        private void OnOutcome(int attempt, LoginOutcome outcome)
        {
            if (outcome.Success)
            {
                // Un éxito tardío (tras agotarse el tiempo) también vale: la cuenta ya está creada en el servidor.
                if (Status == LoginStatus.LoggedIn || Status == LoginStatus.Disabled)
                {
                    return;
                }

                PlayerId = outcome.PlayerId;
                _retryTimer = -1f;
                Debug.Log($"[NovaStriker] Sesión iniciada. Jugador {PlayerId}{(outcome.NewlyCreated ? " (nuevo)" : string.Empty)}.");
                SetStatus(LoginStatus.LoggedIn);
                _eventBus?.Publish(new PlayerLoggedInEvent(PlayerId, outcome.NewlyCreated));
                return;
            }

            // Los errores solo cuentan si son del intento en curso (uno viejo no debe cortar al nuevo).
            if (attempt == _attempt && Status == LoginStatus.InProgress)
            {
                Fail(outcome.Error);
            }
        }

        private void Fail(string error)
        {
            bool willRetry = _retryIndex < _retryDelays.Length;
            _retryTimer = willRetry ? Mathf.Max(0.01f, _retryDelays[_retryIndex++]) : -1f;
            Debug.LogWarning($"[NovaStriker] No se pudo iniciar sesión ({error}). Se juega sin cuenta; " +
                             (willRetry ? $"reintento en {_retryTimer:0} s." : "se reintentará al volver a la app."));
            SetStatus(LoginStatus.Offline);
        }

        private void SetStatus(LoginStatus status)
        {
            if (Status == status)
            {
                return;
            }

            Status = status;
            StatusChanged?.Invoke(status);
        }
    }
}
