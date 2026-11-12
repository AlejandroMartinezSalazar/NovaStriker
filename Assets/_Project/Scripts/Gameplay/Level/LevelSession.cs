using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.Gameplay.Ball;
using NovaStriker.Gameplay.Characters;
using NovaStriker.Gameplay.Debris;
using NovaStriker.Gameplay.Level.Rules;
using NovaStriker.Gameplay.Throwing;
using UnityEngine;

namespace NovaStriker.Gameplay.Level
{
    /// <summary>
    /// Une todo lo que hace falta para jugar un nivel: lo construye con LevelBuilder, configura el tiro
    /// (punto de saque y estrategia del nivel), coloca la cámara y arranca la cuenta de reglas.
    /// Reiniciar = descargar (balones y fragmentos al pool, nivel destruido) y volver a construir.
    /// </summary>
    public sealed class LevelSession : ILevelSession, ILevelHintSource, IDisposable
    {
        private readonly LevelCatalog _catalog;
        private readonly LevelBuilder _builder;
        private readonly IThrowController _throwController;
        private readonly BallPool _ballPool;
        private readonly DebrisPool _debrisPool;
        private readonly ILevelCamera _camera;
        private readonly LevelRulesTracker _rules;
        private readonly IEventBus _eventBus;
        private readonly List<LevelCharacter> _characters = new List<LevelCharacter>();

        /// <summary>Altura (m) sobre los pies del futbolista a la que apunta la indicación del tutorial (el pecho).</summary>
        public const float AimHintHeight = 1f;

        private BuiltLevel _current;

        public LevelSession(
            LevelCatalog catalog,
            LevelBuilder builder,
            IThrowController throwController,
            BallPool ballPool,
            DebrisPool debrisPool,
            ILevelCamera camera,
            LevelRulesTracker rules,
            IEventBus eventBus)
        {
            _catalog = catalog != null ? catalog : throw new ArgumentNullException(nameof(catalog));
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _throwController = throwController ?? throw new ArgumentNullException(nameof(throwController));
            _ballPool = ballPool ?? throw new ArgumentNullException(nameof(ballPool));
            _debrisPool = debrisPool ?? throw new ArgumentNullException(nameof(debrisPool));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            if (_catalog.Count == 0)
            {
                throw new InvalidOperationException($"El catálogo '{_catalog.name}' no tiene niveles.");
            }
        }

        public int LevelCount => _catalog.Count;
        public int CurrentLevelIndex { get; private set; } = -1;

        public bool HasLevel(int levelIndex) => levelIndex >= 0 && levelIndex < _catalog.Count;

        public void Load(int levelIndex)
        {
            if (!HasLevel(levelIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(levelIndex), $"El catálogo tiene {_catalog.Count} niveles.");
            }

            Unload();

            LevelDefinition definition = _catalog.Get(levelIndex);
            if (definition.ThrowStrategy == null)
            {
                throw new InvalidOperationException($"El nivel '{definition.name}' no tiene estrategia de tiro asignada.");
            }

            _current = _builder.Build(definition);

            Transform ballSpawn = _current.Layout.BallSpawnPoint;
            Transform cameraPoint = _current.Layout.CameraPoint;
            _throwController.SetSpawnPosition(ballSpawn.position);
            _throwController.SetStrategy(definition.ThrowStrategy.CreateStrategy());
            _camera.MoveTo(cameraPoint.position, cameraPoint.rotation);

            _characters.Clear();
            bool hasTarget = false;
            foreach (FootballerController character in _current.Characters)
            {
                _characters.Add(new LevelCharacter(character.Id, character.IsTarget));
                hasTarget |= character.IsTarget;
            }

            if (!hasTarget)
            {
                throw new InvalidOperationException(
                    $"El nivel '{definition.name}' no tiene futbolistas objetivo: se ganaría sin tirar.");
            }

            CurrentLevelIndex = levelIndex;
            _eventBus.Publish(new LevelStartedEvent(levelIndex, _catalog.Count, definition.DisplayName));
            _rules.Begin(definition.BallCount, _characters);
        }

        public bool TryGetAimHint(out Vector3 from, out Vector3 to)
        {
            from = default;
            to = default;
            if (_current == null)
            {
                return false;
            }

            foreach (FootballerController character in _current.Characters)
            {
                if (character != null && character.IsTarget && !character.IsKnockedDown)
                {
                    from = _current.Layout.BallSpawnPoint.position;
                    to = character.transform.position + Vector3.up * AimHintHeight;
                    return true;
                }
            }

            return false;
        }

        public void Unload()
        {
            _rules.End();
            _throwController.ClearReadyBall();
            _ballPool.ReleaseAll();
            _debrisPool.ReleaseAll();

            if (_current != null)
            {
                _builder.Destroy(_current);
                _current = null;
            }

            CurrentLevelIndex = -1;
        }

        public void Dispose()
        {
            _rules.End();
            _current = null;
        }
    }
}
