using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;

namespace NovaStriker.Gameplay.Level.Rules
{
    /// <summary>
    /// Lleva la cuenta del nivel: objetivos derribados (también en cadena, por cajas o caídas), balones usados
    /// y balones todavía en juego (lanzados y sin terminar su recorrido).
    /// El número de objetivos sale de los futbolistas creados (no se configura a mano).
    /// Solo cuenta los futbolistas del nivel actual: los ids de otro nivel o repetidos se ignoran.
    /// Publica LevelProgressChangedEvent en cada cambio (lo usa el HUD).
    /// </summary>
    public sealed class LevelRulesTracker : IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly IDisposable _knockdownSubscription;
        private readonly IDisposable _settleSubscription;
        private readonly Dictionary<int, bool> _isTargetById = new Dictionary<int, bool>();
        private readonly HashSet<int> _knockedDown = new HashSet<int>();

        private int _targetsTotal;
        private int _targetsDown;
        private int _nonTargetsDown;
        private int _ballsTotal;
        private int _ballsUsed;

        public LevelRulesTracker(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _knockdownSubscription = _eventBus.Subscribe<CharacterKnockedDownEvent>(OnCharacterKnockedDown);
            _settleSubscription = _eventBus.Subscribe<BallSettledEvent>(OnBallSettled);
        }

        /// <summary>Se dispara tras cada cambio de la cuenta.</summary>
        public event Action Changed;

        public bool IsActive { get; private set; }

        /// <summary>Ya se usó el "+1 balón" en este intento del nivel (se reinicia al reconstruirlo).</summary>
        public bool ExtraBallUsed { get; private set; }

        /// <summary>Balones lanzados que aún no se detuvieron ni salieron del escenario.</summary>
        public int BallsInPlay { get; private set; }

        public LevelStatus Status => new LevelStatus(_targetsTotal, _targetsDown, _nonTargetsDown, _ballsTotal, _ballsUsed);

        /// <summary>Empieza a contar un nivel nuevo con sus futbolistas y su número de balones.</summary>
        public void Begin(int ballCount, IEnumerable<LevelCharacter> characters)
        {
            if (ballCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(ballCount), "El nivel necesita al menos un balón.");
            }

            if (characters == null)
            {
                throw new ArgumentNullException(nameof(characters));
            }

            _isTargetById.Clear();
            _knockedDown.Clear();
            _targetsTotal = 0;
            foreach (LevelCharacter character in characters)
            {
                if (_isTargetById.ContainsKey(character.Id))
                {
                    throw new ArgumentException($"El futbolista {character.Id} está repetido.", nameof(characters));
                }

                _isTargetById.Add(character.Id, character.IsTarget);
                if (character.IsTarget)
                {
                    _targetsTotal++;
                }
            }

            _targetsDown = 0;
            _nonTargetsDown = 0;
            _ballsTotal = ballCount;
            _ballsUsed = 0;
            BallsInPlay = 0;
            ExtraBallUsed = false;
            IsActive = true;
            NotifyChanged();
        }

        /// <summary>Deja de contar (nivel descargado). Los derribos posteriores se ignoran.</summary>
        public void End()
        {
            IsActive = false;
            BallsInPlay = 0;
            _isTargetById.Clear();
            _knockedDown.Clear();
        }

        /// <summary>Gasta un balón. Devuelve false si no quedaba ninguno.</summary>
        public bool RegisterShot()
        {
            if (!IsActive || _ballsUsed >= _ballsTotal)
            {
                return false;
            }

            _ballsUsed++;
            BallsInPlay++;
            NotifyChanged();
            return true;
        }

        /// <summary>Balones que da el anuncio recompensado de la pantalla de derrota (D5).</summary>
        public const int RewardedExtraBalls = 2;

        /// <summary>Concede los balones extra del anuncio (D5) una sola vez por intento. Devuelve false si ya se usó.</summary>
        public bool TryGrantExtraBall()
        {
            if (!IsActive || ExtraBallUsed)
            {
                return false;
            }

            ExtraBallUsed = true;
            AddBalls(RewardedExtraBalls);
            return true;
        }

        /// <summary>Añade balones al nivel en curso.</summary>
        public void AddBalls(int count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (!IsActive)
            {
                return;
            }

            _ballsTotal += count;
            NotifyChanged();
        }

        public void Dispose()
        {
            _knockdownSubscription.Dispose();
            _settleSubscription.Dispose();
            Changed = null;
        }

        private void OnCharacterKnockedDown(CharacterKnockedDownEvent e)
        {
            if (!IsActive || !_isTargetById.TryGetValue(e.CharacterId, out bool isTarget) || !_knockedDown.Add(e.CharacterId))
            {
                return;
            }

            if (isTarget)
            {
                _targetsDown++;
            }
            else
            {
                _nonTargetsDown++;
            }

            NotifyChanged();
        }

        private void OnBallSettled(BallSettledEvent e)
        {
            if (IsActive && BallsInPlay > 0)
            {
                BallsInPlay--;
            }
        }

        private void NotifyChanged()
        {
            LevelStatus status = Status;
            _eventBus.Publish(new LevelProgressChangedEvent(
                status.TargetsRemaining, status.TargetsTotal, status.BallsRemaining, status.BallsTotal));
            Changed?.Invoke();
        }
    }
}
