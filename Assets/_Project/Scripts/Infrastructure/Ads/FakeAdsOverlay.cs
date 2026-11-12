using System;
using UnityEngine;

namespace NovaStriker.Infrastructure.Ads
{
    /// <summary>Dibujo de los anuncios simulados con IMGUI (solo pruebas en el Editor).</summary>
    public sealed class FakeAdsOverlay : MonoBehaviour
    {
        private string _title;
        private string _rewardLabel;
        private Action<bool> _onClosed;
        private GUIStyle _style;

        public float BannerHeight { get; set; }

        public void ShowFullscreen(string title, string rewardLabel, Action<bool> onClosed)
        {
            _title = title;
            _rewardLabel = rewardLabel;
            _onClosed = onClosed;
        }

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height / 50f), alignment = TextAnchor.MiddleCenter };
            GUI.depth = -1000;

            if (BannerHeight > 0f)
            {
                GUI.color = new Color(0.55f, 0.55f, 0.55f, 1f);
                GUI.Box(new Rect(0f, 0f, Screen.width, BannerHeight), "BANNER (prueba)", _style);
                GUI.color = Color.white;
            }

            if (_onClosed == null)
            {
                return;
            }

            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), _title, _style);
            float buttonHeight = Screen.height / 12f;
            float buttonWidth = Screen.width * 0.7f;
            float x = (Screen.width - buttonWidth) * 0.5f;

            if (_rewardLabel != null && GUI.Button(new Rect(x, Screen.height * 0.6f, buttonWidth, buttonHeight), _rewardLabel, _style))
            {
                Close(true);
            }

            if (GUI.Button(new Rect(x, Screen.height * 0.6f + buttonHeight * 1.3f, buttonWidth, buttonHeight), "Cerrar", _style))
            {
                Close(false);
            }
        }

        private void Close(bool rewarded)
        {
            Action<bool> callback = _onClosed;
            _onClosed = null;
            callback?.Invoke(rewarded);
        }
    }
}
