using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Services;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NovaStriker.UI.Common
{
    /// <summary>
    /// Añade el sonido de click a todos los botones bajo una raíz de UI (lo usa la capa de composición,
    /// acotado al canvas de la escena). Dispose quita los listeners.
    /// </summary>
    public sealed class ButtonClickSounds : IDisposable
    {
        private readonly List<Button> _buttons = new List<Button>();
        private readonly UnityAction _play;

        public ButtonClickSounds(Transform root, IAudioService audio)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (audio == null)
            {
                throw new ArgumentNullException(nameof(audio));
            }

            _play = () => audio.PlayUi(SfxId.Click);
            root.GetComponentsInChildren(true, _buttons);
            foreach (Button button in _buttons)
            {
                button.onClick.AddListener(_play);
            }
        }

        public int Count => _buttons.Count;

        public void Dispose()
        {
            foreach (Button button in _buttons)
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(_play);
                }
            }

            _buttons.Clear();
        }
    }
}
