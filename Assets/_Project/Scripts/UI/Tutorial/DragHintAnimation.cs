using UnityEngine;

namespace NovaStriker.UI.Tutorial
{
    /// <summary>
    /// Ciclo de la animación "pon el dedo, arrástralo hasta el futbolista y suelta", en bucle. C# puro.
    /// Fases: aparece → presiona (anillo en el origen) → arrastra → se queda un instante → suelta (anillo en el
    /// destino) → desaparece → pausa.
    /// </summary>
    public static class DragHintAnimation
    {
        public const float Appear = 0.3f;
        public const float Press = 0.25f;
        public const float Drag = 1.1f;
        public const float Hold = 0.2f;
        public const float Release = 0.35f;
        public const float FadeOut = 0.35f;
        public const float Pause = 0.45f;

        public static float CycleDuration => Appear + Press + Drag + Hold + Release + FadeOut + Pause;

        /// <summary>Estado de la animación en un instante.</summary>
        public readonly struct Frame
        {
            /// <summary>0 = dedo en el origen, 1 = en el destino.</summary>
            public readonly float Progress;
            public readonly float HandAlpha;
            /// <summary>1 = apoyado en la pantalla; algo mayor = levantado.</summary>
            public readonly float HandScale;
            public readonly float RingAlpha;
            public readonly float RingScale;
            /// <summary>El anillo de toque está en el destino (al soltar) o en el origen (al presionar).</summary>
            public readonly bool RingAtEnd;
            /// <summary>El dedo está tocando la pantalla (entre presionar y soltar).</summary>
            public readonly bool Touching;

            public Frame(float progress, float handAlpha, float handScale, float ringAlpha, float ringScale, bool ringAtEnd, bool touching)
            {
                Progress = progress;
                HandAlpha = handAlpha;
                HandScale = handScale;
                RingAlpha = ringAlpha;
                RingScale = ringScale;
                RingAtEnd = ringAtEnd;
                Touching = touching;
            }
        }

        public const float LiftedScale = 1.15f;

        public static Frame Evaluate(float time)
        {
            float t = Mathf.Repeat(Mathf.Max(0f, time), CycleDuration);

            if (t < Appear)
            {
                return new Frame(0f, t / Appear, LiftedScale, 0f, 0f, false, false);
            }

            t -= Appear;
            if (t < Press)
            {
                float k = t / Press;
                return new Frame(0f, 1f, Mathf.Lerp(LiftedScale, 1f, k), 0.8f * k, Mathf.Lerp(0.4f, 1f, k), false, true);
            }

            t -= Press;
            if (t < Drag)
            {
                float k = t / Drag;
                return new Frame(SmoothStep(k), 1f, 1f, 0.8f * (1f - Mathf.Clamp01(k * 3f)), 1f, false, true);
            }

            t -= Drag;
            if (t < Hold)
            {
                return new Frame(1f, 1f, 1f, 0f, 0f, true, true);
            }

            t -= Hold;
            if (t < Release)
            {
                float k = t / Release;
                return new Frame(1f, 1f, Mathf.Lerp(1f, LiftedScale, k), 0.9f * (1f - k), Mathf.Lerp(1f, 1.8f, k), true, false);
            }

            t -= Release;
            if (t < FadeOut)
            {
                return new Frame(1f, 1f - t / FadeOut, LiftedScale, 0f, 0f, true, false);
            }

            return new Frame(1f, 0f, LiftedScale, 0f, 0f, true, false);
        }

        private static float SmoothStep(float k)
        {
            k = Mathf.Clamp01(k);
            return k * k * (3f - 2f * k);
        }
    }
}
