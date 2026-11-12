using System;
using System.Collections.Generic;

namespace NovaStriker.Core.Utils
{
    /// <summary>
    /// Agrupa varios IDisposable (p. ej. suscripciones a eventos) para liberarlos juntos.
    /// Se liberan en orden inverso al de registro.
    /// </summary>
    public sealed class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _items = new List<IDisposable>();
        private bool _isDisposed;

        public int Count => _items.Count;
        public bool IsDisposed => _isDisposed;

        public void Add(IDisposable item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (_isDisposed)
            {
                // Si el grupo ya se liberó, lo nuevo se libera de inmediato para no filtrar suscripciones.
                item.Dispose();
                return;
            }

            _items.Add(item);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                _items[i].Dispose();
            }
            _items.Clear();
        }
    }
}
