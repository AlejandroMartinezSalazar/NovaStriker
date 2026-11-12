using UnityEngine;

namespace NovaStriker.Gameplay.Debris
{
    /// <summary>Genera fragmentos temporales (vidrio roto) a partir de una placa.</summary>
    public interface IDebrisSpawner
    {
        /// <param name="plate">Transform de la placa rota (posición, rotación y escala = tamaño).</param>
        /// <param name="columns">Columnas de la rejilla de fragmentos.</param>
        /// <param name="rows">Filas de la rejilla de fragmentos.</param>
        /// <param name="impactVelocity">Velocidad del objeto que la rompió; empuja los fragmentos en esa dirección.</param>
        void SpawnShards(Transform plate, int columns, int rows, Vector3 impactVelocity);
    }
}
