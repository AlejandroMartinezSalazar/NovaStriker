using System;
using System.Collections.Generic;
using UnityEngine;

namespace NovaStriker.Gameplay.Obstacles
{
    /// <summary>
    /// Geometría de una caja de vidrio con cajas dentro: seis paneles alrededor de un hueco que deja sitio a una
    /// rejilla de cajas, con holgura para que no se toquen al empezar. C# puro (lo usa el Editor y se prueba).
    /// Ejes: el origen es el centro de la base exterior (se apoya en y = 0). Cada panel es un cubo escalado cuyo
    /// grosor va en su eje Z local, que es como DebrisPool reparte los fragmentos de una placa.
    /// </summary>
    public sealed class GlassCrateLayout
    {
        public readonly struct Panel
        {
            public readonly string Name;
            public readonly Vector3 LocalPosition;
            public readonly Vector3 LocalEuler;
            public readonly Vector3 Scale;

            public Panel(string name, Vector3 localPosition, Vector3 localEuler, Vector3 scale)
            {
                Name = name;
                LocalPosition = localPosition;
                LocalEuler = localEuler;
                Scale = scale;
            }
        }

        /// <param name="boxes">Cajas por eje (x, y, z), mínimo 1 en cada uno.</param>
        /// <param name="boxSize">Lado de cada caja interior (m).</param>
        /// <param name="thickness">Grosor de los paneles de vidrio (m).</param>
        /// <param name="gap">Holgura entre cajas y entre cajas y vidrio (m).</param>
        public GlassCrateLayout(Vector3Int boxes, float boxSize, float thickness, float gap)
        {
            if (boxes.x < 1 || boxes.y < 1 || boxes.z < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(boxes), "Debe haber al menos una caja por eje.");
            }

            if (boxSize <= 0f || thickness <= 0f || gap < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(boxSize), "Tamaños positivos y holgura no negativa.");
            }

            Boxes = boxes;
            BoxSize = boxSize;
            Thickness = thickness;
            Gap = gap;
            Interior = new Vector3(Span(boxes.x), Span(boxes.y), Span(boxes.z));
            Outer = Interior + Vector3.one * (2f * thickness);
        }

        public Vector3Int Boxes { get; }
        public float BoxSize { get; }
        public float Thickness { get; }
        public float Gap { get; }
        public Vector3 Interior { get; }
        public Vector3 Outer { get; }

        /// <summary>Centro del cuerpo de la caja de vidrio respecto al origen (base).</summary>
        public Vector3 CrateCenter => new Vector3(0f, Outer.y * 0.5f, 0f);

        /// <summary>Paneles respecto a CrateCenter.</summary>
        public IReadOnlyList<Panel> Panels()
        {
            float x = (Interior.x + Thickness) * 0.5f;
            float y = (Interior.y + Thickness) * 0.5f;
            float z = (Interior.z + Thickness) * 0.5f;
            var flat = new Vector3(90f, 0f, 0f);
            var side = new Vector3(0f, 90f, 0f);

            return new[]
            {
                new Panel("Panel_Bottom", new Vector3(0f, -y, 0f), flat, new Vector3(Outer.x, Outer.z, Thickness)),
                new Panel("Panel_Top", new Vector3(0f, y, 0f), flat, new Vector3(Outer.x, Outer.z, Thickness)),
                new Panel("Panel_Front", new Vector3(0f, 0f, -z), Vector3.zero, new Vector3(Outer.x, Interior.y, Thickness)),
                new Panel("Panel_Back", new Vector3(0f, 0f, z), Vector3.zero, new Vector3(Outer.x, Interior.y, Thickness)),
                new Panel("Panel_Left", new Vector3(-x, 0f, 0f), side, new Vector3(Interior.z, Interior.y, Thickness)),
                new Panel("Panel_Right", new Vector3(x, 0f, 0f), side, new Vector3(Interior.z, Interior.y, Thickness)),
            };
        }

        /// <summary>Centros de las cajas interiores respecto al origen (base), apoyadas sobre el panel inferior.</summary>
        public IReadOnlyList<Vector3> BoxCenters()
        {
            var centers = new List<Vector3>(Boxes.x * Boxes.y * Boxes.z);
            float step = BoxSize + Gap;
            for (int iy = 0; iy < Boxes.y; iy++)
            {
                for (int iz = 0; iz < Boxes.z; iz++)
                {
                    for (int ix = 0; ix < Boxes.x; ix++)
                    {
                        centers.Add(new Vector3(
                            (ix - (Boxes.x - 1) * 0.5f) * step,
                            Thickness + Gap + BoxSize * 0.5f + iy * step,
                            (iz - (Boxes.z - 1) * 0.5f) * step));
                    }
                }
            }

            return centers;
        }

        /// <summary>
        /// Centros en forma de pirámide (respecto al origen): cada fila de abajo arriba tiene una caja menos por eje
        /// horizontal y va centrada sobre la anterior. Con 2×2×1: dos abajo y una encima, en medio.
        /// </summary>
        public IReadOnlyList<Vector3> PyramidCenters()
        {
            var centers = new List<Vector3>();
            float step = BoxSize + Gap;
            for (int iy = 0; iy < Boxes.y; iy++)
            {
                int countX = Mathf.Max(1, Boxes.x - iy);
                int countZ = Mathf.Max(1, Boxes.z - iy);
                for (int iz = 0; iz < countZ; iz++)
                {
                    for (int ix = 0; ix < countX; ix++)
                    {
                        centers.Add(new Vector3(
                            (ix - (countX - 1) * 0.5f) * step,
                            Thickness + Gap + BoxSize * 0.5f + iy * step,
                            (iz - (countZ - 1) * 0.5f) * step));
                    }
                }
            }

            return centers;
        }

        private float Span(int count) => count * BoxSize + (count + 1) * Gap;
    }
}
