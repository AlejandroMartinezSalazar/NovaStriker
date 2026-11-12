using UnityEngine;

namespace NovaStriker.UI.Common
{
    /// <summary>Convierte la zona segura de la pantalla (píxeles) en anclas normalizadas de un RectTransform.</summary>
    public static class SafeAreaMath
    {
        /// <param name="topInsetPixels">Franja extra que se reserva arriba, bajo la zona segura (p. ej. el banner).</param>
        public static void ToAnchors(Rect safeArea, Vector2 screenSize, out Vector2 anchorMin, out Vector2 anchorMax,
            float topInsetPixels = 0f)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                return;
            }

            anchorMin = new Vector2(Mathf.Clamp01(safeArea.xMin / screenSize.x), Mathf.Clamp01(safeArea.yMin / screenSize.y));
            float top = Mathf.Max(safeArea.yMin, safeArea.yMax - Mathf.Max(0f, topInsetPixels));
            anchorMax = new Vector2(Mathf.Clamp01(safeArea.xMax / screenSize.x), Mathf.Clamp01(top / screenSize.y));
        }

        /// <summary>
        /// Rect normalizado del viewport de cámara que deja libre la franja superior ocupada por la muesca
        /// y el banner (lo que hay sobre la zona segura + el alto del banner).
        /// </summary>
        public static Rect CameraViewport(Rect safeArea, Vector2 screenSize, float bannerHeightPixels)
        {
            if (screenSize.y <= 0f || bannerHeightPixels <= 0f)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            float reservedTop = (screenSize.y - safeArea.yMax) + bannerHeightPixels;
            float height = Mathf.Clamp01((screenSize.y - reservedTop) / screenSize.y);
            return new Rect(0f, 0f, 1f, height);
        }
    }
}
