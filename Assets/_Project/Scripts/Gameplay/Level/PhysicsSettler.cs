using System.Collections.Generic;
using UnityEngine;

namespace NovaStriker.Gameplay.Level
{
    /// <summary>
    /// Deja que la física de un nivel recién construido se asiente un instante y después duerme sus cuerpos
    /// (cajas apiladas y futbolistas). Una pila de cuerpos dinámicos nunca queda quieta del todo en PhysX:
    /// tiembla y se desliza poco a poco. Dormidos, no se mueven hasta que algo los toca (el balón, otra caja),
    /// y entonces PhysX despierta todo lo que está en contacto y caen como siempre.
    /// Lo añade LevelBuilder al escenario; se destruye con él.
    /// </summary>
    public sealed class PhysicsSettler : MonoBehaviour
    {
        private readonly List<Rigidbody> _bodies = new List<Rigidbody>();
        private SettleCountdown _countdown;

        /// <param name="settleTime">Segundos de física antes de dormir: lo justo para que todo apoye (huecos de pocos cm).</param>
        public void Begin(float settleTime, IEnumerable<Rigidbody> bodies)
        {
            _countdown = new SettleCountdown(settleTime);
            _bodies.Clear();
            _bodies.AddRange(bodies);
            enabled = true;
        }

        private void FixedUpdate()
        {
            if (_countdown == null || !_countdown.Step(Time.fixedDeltaTime))
            {
                return;
            }

            foreach (Rigidbody body in _bodies)
            {
                // Un futbolista ya derribado (ragdoll) tiene su cuerpo raíz cinemático: no se toca.
                if (body != null && !body.isKinematic)
                {
                    body.Sleep();
                }
            }

            _bodies.Clear();
            enabled = false;
        }
    }
}
