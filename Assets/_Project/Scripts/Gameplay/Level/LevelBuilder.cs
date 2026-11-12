using System;
using System.Collections.Generic;
using NovaStriker.Gameplay.Characters;
using NovaStriker.Gameplay.Factories;
using NovaStriker.Gameplay.Obstacles;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NovaStriker.Gameplay.Level
{
    /// <summary>
    /// Construye y destruye niveles: instancia el escenario, inyecta los obstáculos y crea los futbolistas
    /// con la Factory en sus puntos de aparición. No sabe nada de reglas ni de tiro.
    /// </summary>
    public sealed class LevelBuilder
    {
        /// <summary>Segundos de física antes de dormir el nivel (PhysicsSettler).</summary>
        internal const float SettleTime = 0.4f;

        private readonly ICharacterFactory _characterFactory;
        private readonly ObstacleInstaller _obstacleInstaller;
        private readonly Transform _levelParent;
        private readonly List<FootballerSpawnPoint> _spawnBuffer = new List<FootballerSpawnPoint>();

        public LevelBuilder(ICharacterFactory characterFactory, ObstacleInstaller obstacleInstaller, Transform levelParent)
        {
            _characterFactory = characterFactory ?? throw new ArgumentNullException(nameof(characterFactory));
            _obstacleInstaller = obstacleInstaller ?? throw new ArgumentNullException(nameof(obstacleInstaller));
            _levelParent = levelParent;
        }

        public BuiltLevel Build(LevelDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (definition.Layout == null)
            {
                throw new InvalidOperationException($"El nivel '{definition.name}' no tiene escenario (Layout) asignado.");
            }

            LevelLayout layout = Object.Instantiate(definition.Layout, _levelParent);
            layout.name = definition.Layout.name;

            if (layout.BallSpawnPoint == null || layout.CameraPoint == null)
            {
                Object.Destroy(layout.gameObject);
                throw new InvalidOperationException(
                    $"El escenario '{definition.Layout.name}' necesita BallSpawnPoint y CameraPoint asignados.");
            }

            _obstacleInstaller.InstallAll(layout.transform);

            _spawnBuffer.Clear();
            layout.CollectSpawnPoints(_spawnBuffer);
            var characters = new List<FootballerController>(_spawnBuffer.Count);
            foreach (FootballerSpawnPoint spawn in _spawnBuffer)
            {
                CharacterDefinition character = spawn.Character;
                if (character == null)
                {
                    _spawnBuffer.Clear();
                    Destroy(new BuiltLevel(definition, layout, characters));
                    throw new InvalidOperationException(
                        $"'{definition.Layout.name}/{spawn.name}' no tiene CharacterDefinition asignada.");
                }

                Transform point = spawn.transform;
                characters.Add(_characterFactory.Create(character, point.position, point.rotation));
            }

            _spawnBuffer.Clear();
            StartSettling(layout, characters);
            return new BuiltLevel(definition, layout, characters);
        }

        /// <summary>
        /// Tras un instante de física, duerme las cajas y los futbolistas para que las pilas no tiemblen ni resbalen.
        /// Menor que KnockdownSettings.ArmingDelay (0,5 s): el asentamiento nunca cuenta como caída.
        /// </summary>
        private static void StartSettling(LevelLayout layout, List<FootballerController> characters)
        {
            var bodies = new List<Rigidbody>(layout.GetComponentsInChildren<Rigidbody>());
            foreach (FootballerController character in characters)
            {
                // Solo el cuerpo raíz: los huesos del ragdoll son cinemáticos mientras baila.
                if (character.TryGetComponent(out Rigidbody body))
                {
                    bodies.Add(body);
                }
            }

            layout.gameObject.AddComponent<PhysicsSettler>().Begin(SettleTime, bodies);
        }

        /// <summary>Elimina el nivel. Se desactiva antes de destruir para que no participe en la física del frame.</summary>
        public void Destroy(BuiltLevel level)
        {
            if (level == null)
            {
                return;
            }

            foreach (FootballerController character in level.Characters)
            {
                DestroyObject(character != null ? character.gameObject : null);
            }

            DestroyObject(level.Layout != null ? level.Layout.gameObject : null);
        }

        private static void DestroyObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            gameObject.SetActive(false);
            Object.Destroy(gameObject);
        }
    }
}
