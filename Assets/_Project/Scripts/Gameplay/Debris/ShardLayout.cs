using System.Collections.Generic;
using UnityEngine;

namespace NovaStriker.Gameplay.Debris
{
    /// <summary>
    /// Reparte una placa (ancho × alto × grosor) en una rejilla de fragmentos.
    /// Devuelve centros y tamaños en el espacio local de la placa centrada en el origen. C# puro.
    /// </summary>
    public static class ShardLayout
    {
        public readonly struct Shard
        {
            public readonly Vector3 LocalCenter;
            public readonly Vector3 Size;

            public Shard(Vector3 localCenter, Vector3 size)
            {
                LocalCenter = localCenter;
                Size = size;
            }
        }

        public static void Compute(Vector3 plateSize, int columns, int rows, List<Shard> output)
        {
            output.Clear();
            columns = Mathf.Max(1, columns);
            rows = Mathf.Max(1, rows);

            var size = new Vector3(plateSize.x / columns, plateSize.y / rows, plateSize.z);
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    var center = new Vector3(
                        -plateSize.x * 0.5f + size.x * (column + 0.5f),
                        -plateSize.y * 0.5f + size.y * (row + 0.5f),
                        0f);
                    output.Add(new Shard(center, size));
                }
            }
        }
    }
}
