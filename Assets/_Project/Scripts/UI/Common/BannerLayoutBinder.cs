using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Services;
using UnityEngine;

namespace NovaStriker.UI.Common
{
    /// <summary>
    /// Deja libre la franja del banner superior: baja la interfaz (todas las SafeAreaFitter bajo la raíz)
    /// y recorta el viewport de la cámara para que el juego no se dibuje detrás del anuncio.
    /// Lo crea la composición de cada escena; Dispose deja la cámara como estaba.
    /// </summary>
    public sealed class BannerLayoutBinder : IDisposable
    {
        /// <summary>Separación entre el banner y la interfaz (fracción del alto de pantalla, ~30 px en 1920).</summary>
        public const float MarginFraction = 0.015f;

        private readonly IAdsService _ads;
        private readonly Camera _camera;
        private readonly List<SafeAreaFitter> _fitters = new List<SafeAreaFitter>();
        private Camera _backgroundCamera;

        public BannerLayoutBinder(IAdsService ads, Transform uiRoot, Camera camera)
        {
            _ads = ads ?? throw new ArgumentNullException(nameof(ads));
            if (uiRoot == null)
            {
                throw new ArgumentNullException(nameof(uiRoot));
            }

            _camera = camera;
            uiRoot.GetComponentsInChildren(true, _fitters);
            _ads.BannerLayoutChanged += Apply;
            Apply();
        }

        public int FitterCount => _fitters.Count;

        public void Dispose()
        {
            _ads.BannerLayoutChanged -= Apply;
            if (_camera != null)
            {
                _camera.rect = new Rect(0f, 0f, 1f, 1f);
            }

            if (_backgroundCamera != null)
            {
                UnityEngine.Object.Destroy(_backgroundCamera.gameObject);
            }
        }

        private void Apply()
        {
            float banner = _ads.BannerHeightPixels;

            // Margen extra bajo el banner: AdMob pide que los botones no queden pegados al anuncio.
            float uiInset = banner > 0f ? banner + Screen.height * MarginFraction : 0f;
            foreach (SafeAreaFitter fitter in _fitters)
            {
                if (fitter != null)
                {
                    fitter.SetTopInset(uiInset);
                }
            }

            if (_camera == null)
            {
                return;
            }

            _camera.rect = SafeAreaMath.CameraViewport(Screen.safeArea, new Vector2(Screen.width, Screen.height), banner);
            if (banner > 0f && _backgroundCamera == null)
            {
                // La franja que no dibuja la cámara del juego se limpia en negro (si no, quedarían restos de frames).
                var gameObject = new GameObject("[BannerBackgroundCamera]");
                _backgroundCamera = gameObject.AddComponent<Camera>();
                _backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
                _backgroundCamera.backgroundColor = Color.black;
                _backgroundCamera.cullingMask = 0;
                _backgroundCamera.depth = _camera.depth - 1f;
            }
        }
    }
}
