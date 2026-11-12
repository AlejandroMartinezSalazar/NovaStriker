using System;
using System.Collections.Generic;
using UnityEngine;

namespace NovaStriker.Gameplay.Obstacles
{
    /// <summary>
    /// Inyecta dependencias en todos los obstáculos bajo una raíz (el nivel instanciado).
    /// Solo lo usa la capa de composición: es la única búsqueda de componentes permitida para obstáculos,
    /// y siempre está acotada a la raíz del nivel.
    /// </summary>
    public sealed class ObstacleInstaller
    {
        private readonly ObstacleServices _services;
        private readonly List<IObstacle> _buffer = new List<IObstacle>();

        public ObstacleInstaller(ObstacleServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>Devuelve cuántos obstáculos se inicializaron.</summary>
        public int InstallAll(Transform levelRoot)
        {
            if (levelRoot == null)
            {
                throw new ArgumentNullException(nameof(levelRoot));
            }

            _buffer.Clear();
            levelRoot.GetComponentsInChildren(true, _buffer);
            foreach (IObstacle obstacle in _buffer)
            {
                obstacle.Construct(_services);
            }

            int count = _buffer.Count;
            _buffer.Clear();
            return count;
        }
    }
}
