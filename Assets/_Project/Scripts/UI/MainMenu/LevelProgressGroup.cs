using System;
using System.Collections.Generic;

namespace NovaStriker.UI.MainMenu
{
    /// <summary>
    /// Agrupa los niveles de N en N (10 por defecto) y calcula el estado de cada cuadradito del grupo
    /// que contiene al nivel actual: en el nivel 14 se ven los niveles 11-20.
    /// Solo se dibujan los niveles que existen (el último grupo puede ser más corto).
    /// </summary>
    public static class LevelProgressGroup
    {
        public const int DefaultGroupSize = 10;

        /// <returns>Índice del primer nivel del grupo.</returns>
        public static int Build(int currentLevelIndex, int levelCount, int groupSize, List<LevelSquareState> squares)
        {
            if (squares == null)
            {
                throw new ArgumentNullException(nameof(squares));
            }

            if (groupSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(groupSize));
            }

            squares.Clear();
            if (levelCount <= 0)
            {
                return 0;
            }

            int current = Math.Min(Math.Max(currentLevelIndex, 0), levelCount - 1);
            int groupStart = current / groupSize * groupSize;
            int groupEnd = Math.Min(groupStart + groupSize, levelCount);
            for (int level = groupStart; level < groupEnd; level++)
            {
                squares.Add(level < current ? LevelSquareState.Completed
                    : level == current ? LevelSquareState.Current
                    : LevelSquareState.Locked);
            }

            return groupStart;
        }
    }
}
