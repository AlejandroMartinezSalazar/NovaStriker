using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Services;
using UnityEngine;

namespace NovaStriker.UI.Localization
{
    /// <summary>
    /// Inyecta el servicio de idioma en todos los LocalizedText bajo una raíz (incluidos los inactivos).
    /// Devuelve un IDisposable que los da de baja: la raíz de composición lo libera al cerrar la escena.
    /// </summary>
    public static class LocalizedTextInstaller
    {
        private static readonly List<LocalizedText> Buffer = new List<LocalizedText>();

        public static IDisposable InstallAll(Transform root, ILocalization localization)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (localization == null)
            {
                throw new ArgumentNullException(nameof(localization));
            }

            Buffer.Clear();
            root.GetComponentsInChildren(true, Buffer);
            var installed = new List<LocalizedText>(Buffer);
            Buffer.Clear();

            foreach (LocalizedText text in installed)
            {
                text.Construct(localization);
            }

            return new Installation(installed);
        }

        private sealed class Installation : IDisposable
        {
            private List<LocalizedText> _texts;

            public Installation(List<LocalizedText> texts) => _texts = texts;

            public int Count => _texts?.Count ?? 0;

            public void Dispose()
            {
                if (_texts == null)
                {
                    return;
                }

                // Release solo toca campos de C#: vale aunque el objeto de Unity ya esté destruido.
                foreach (LocalizedText text in _texts)
                {
                    if (!ReferenceEquals(text, null))
                    {
                        text.Release();
                    }
                }

                _texts = null;
            }
        }
    }
}
