using System;

namespace NovaStriker.Infrastructure.Audio
{
    /// <summary>Elige una variante al azar sin repetir la anterior (C# puro).</summary>
    public static class VariantPicker
    {
        /// <param name="randomValue">Número aleatorio en [0, 1).</param>
        public static int Next(int count, int lastIndex, float randomValue)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count == 1)
            {
                return 0;
            }

            float clamped = randomValue < 0f ? 0f : randomValue >= 1f ? 0.9999f : randomValue;
            if (lastIndex < 0 || lastIndex >= count)
            {
                return (int)(clamped * count);
            }

            // Se elige entre las count - 1 restantes y se salta la anterior.
            int index = (int)(clamped * (count - 1));
            return index >= lastIndex ? index + 1 : index;
        }
    }
}
